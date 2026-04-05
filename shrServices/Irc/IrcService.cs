// loosely based on https://www.rfc-editor.org/rfc/rfc1459

using shrNet;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace shrServices
{
    public class IrcService : IDisposable
    {
        // public constants

        public const int MaximumBufferSize = 1024;
        public const int MininumChannelSize = 100;

        // private constants

        private const string CRLF = "\r\n";

        private const int NICKLEN = 31;
        private const int CHANLEN = 64;

        private const string defaultChannelModes = "nt";

        private const long defaultPingInterval = 60000; // ms
        private const long defaultPingTolerance = 5000; // ms

        // public static variables

        public static readonly byte[] Delimiter = Encoding.UTF8.GetBytes(CRLF.Substring(CRLF.Length - 1, 1));

        // private variables

        private readonly string _hostname;
        private readonly DuplexListener _listener6667;

        private readonly string[] _motd;
        private readonly string[] _logo;
        private readonly byte[] _ping;

        private readonly ConcurrentDictionary<int, IIrcClient> _clients; // clientId, client
        private readonly ConcurrentDictionary<string, int> _nicks; // nick, clientId
        private readonly ConcurrentDictionary<string, int> _users; // user, clientId

        private readonly Dictionary<string, IrcChannel> _channels; // channelName, { clientId, flags }
        private readonly Dictionary<int, object> _whitelist; // clientId, null

        private readonly DuplexQueue _mainQueue;

        private long _closing;

        // public functions

        public IrcService(IPAddress address, int port, string[] motd, string[] logo)
        {
            _hostname = address.ToString();
            _listener6667 = new(address, port, ProcessSocketAsync);

            _motd = motd;
            _logo = logo;
            _ping = ":SfcRulz"u8.ToArray();

            Contract.Assert(_ping[0] == (byte)':');

            _clients = new();
            _nicks = new(StringComparer.OrdinalIgnoreCase);
            _users = new();

            _channels = [];
            _whitelist = []; // list of users that can receive messages starting with '!' (sfc2op server commands)

            _mainQueue = new(isShared: true);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _closing, 1) == 0)
            {
                _listener6667.Dispose();
                _mainQueue.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        public async Task StartAsync()
        {
            Task service = ProcessServiceAsync();
            Task listener = _listener6667.StartAsync();

            await Task.WhenAny(service, listener);

            Dispose();
        }

        public IrcClientStream CreateInternalClient()
        {
            IrcProfile profile = new();
            DuplexQueue outbountQueue = new(isShared: false);

            IrcClientStream clientSide = new(profile, outbountQueue, _mainQueue);
            IrcClientStream serverSide = new(profile, null, outbountQueue);

            AddIrcClient(serverSide);

            return clientSide;
        }

        public void CloseInternalClient(IIrcClient clientSide)
        {
            if (_clients.TryGetValue(clientSide.Id, out IIrcClient serverSide))
                Quit(serverSide, null);

            clientSide.Dispose();
        }

        public bool TryGetClient(string nick, out IIrcClient client)
        {
            if (_nicks.TryGetValue(nick, out int id) && _clients.TryGetValue(id, out client))
                return true;

            client = null;

            return false;
        }

        // private functions

        private async Task ProcessSocketAsync(Socket socket)
        {
            IrcClientSocket client = null;

            try
            {
                DuplexSocket.Initialize(
                    socket,
                    receiveTimeout: (int)(defaultPingInterval + defaultPingTolerance),
                    sendTimout: (int)defaultPingTolerance
                );

                client = new(socket, _mainQueue);

                AddIrcClient(client);

                await client.Transport.StartAsync();
            }
            catch (Exception)
            { }
            finally
            {
                if (client != null)
                    Quit(client, null);
                else
                    socket.Dispose();
            }
        }

        private void AddIrcClient(IIrcClient client)
        {
            if (!_clients.TryAdd(client.Id, client))
                throw new NotSupportedException();

            client.LastTick = Environment.TickCount64;
            client.Modes = 0;
        }

        private async Task ProcessServiceAsync()
        {
            try
            {
                List<ReadOnlyMemory<byte>> a = new(MaximumBufferSize);
                StringBuilder t = new(MaximumBufferSize << 1);
                SortedList<string, object> n = new(MininumChannelSize);

                long lastPing = 0;

                while (Interlocked.Read(ref _closing) == 0)
                {
                    // gets the current tick

                    long currentTick = Environment.TickCount64;

                    // tries to process a message

                    if (_mainQueue.TryDequeue(out DuplexMessage message))
                    {
                        try
                        {
                            if (!_clients.TryGetValue(message.Id, out IIrcClient client) || client.IsDisposing)
                                continue;

                            if (!TryParseMessage(message.Buffer, message.Length, a))
                                goto closeClient;

                            int c = a.Count;

                            if (client.Nick == null)
                            {
                                if (a[0].Span.SequenceEqual("NICK"u8))
                                {
                                    if (c == 2)
                                    {
                                        if (TryNICK(client, a))
                                            goto nextMessage;
                                    }

                                    goto closeClient;
                                }
                            }
                            else if (client.User == null)
                            {
                                if (a[0].Span.SequenceEqual("USER"u8))
                                {
                                    if (c == 5)
                                    {
                                        if (TryUSER(client, a, t))
                                            goto nextMessage;
                                    }

                                    goto closeClient;
                                }
                            }
                            else
                            {
                                if (a[0].Span.SequenceEqual("PRIVMSG"u8))
                                {
                                    if (c == 3)
                                    {
                                        if (TryPRIVMSG(client, a, t))
                                            goto nextMessage;
                                    }
                                }
                                else if (a[0].Span.SequenceEqual("NOTICE"u8))
                                {
                                    if (c == 3)
                                    {
                                        if (TryNOTICE(client, a, t))
                                            goto nextMessage;
                                    }
                                }
                                else if (a[0].Span.SequenceEqual("JOIN"u8))
                                {
                                    if (c >= 2)
                                    {
                                        if (TryJOIN(client, c, a, t, n))
                                            goto nextMessage;
                                    }
                                }
                                else if (a[0].Span.SequenceEqual("PART"u8))
                                {
                                    if (c >= 2)
                                    {
                                        if (TryPART(client, c, a))
                                            goto nextMessage;
                                    }
                                }
                                else if (a[0].Span.SequenceEqual("MODE"u8))
                                {
                                    if (c == 2)
                                    {
                                        if (TryMODE_2(client, a, t))
                                            goto nextMessage;
                                    }
                                    else if (c == 3)
                                    {
                                        if (TryMODE_3(client, a, t))
                                            goto nextMessage;
                                    }
                                }
                                else if (a[0].Span.SequenceEqual("NAMES"u8))
                                {
                                    if (c >= 2)
                                    {
                                        if (TryNAMES(client, c, a, t, n))
                                            goto nextMessage;
                                    }
                                }
                                else if (a[0].Span.SequenceEqual("PING"u8))
                                {
                                    if (c == 2)
                                    {
                                        if (TryPING(client, a, t))
                                            goto nextMessage;
                                    }
                                }
                                else if (a[0].Span.SequenceEqual("QUIT"u8))
                                {
                                    if (c == 2)
                                    {
                                        if (TryQUIT(client, a))
                                            goto nextMessage;
                                    }

                                    goto closeClient;
                                }
                            }

                            if (a[0].Span.SequenceEqual("PONG"u8))
                            {
                                if (c == 2)
                                {
                                    if (a[1].Span.SequenceEqual(_ping))
                                        goto nextMessage;
                                }

                                goto closeClient;
                            }

#if DEBUG
                            else if (a[0].Span.SequenceEqual("CAP"u8))
                            {
                                // CAP LS 302

                                goto nextMessage;
                            }
                            else if (a[0].Span.SequenceEqual("TOPIC"u8))
                            {
                                // TOPIC #portugal

                                goto nextMessage;
                            }
                            else if (a[0].Span.SequenceEqual("USERHOST"u8))
                            {
                                // USERHOST d4v1ks

                                goto nextMessage;
                            }

                            Debugger.Break(); // not implemented
#endif

                        closeClient:

                            Quit(client, null);

                        nextMessage:

                            client.LastTick = currentTick;
                        }
                        finally
                        {
                            message.Release();
                        }
                    }

                    // tries to ping everyone

                    if (currentTick - lastPing >= 250)
                    {
                        lastPing = currentTick;

                        TryPingEveryone(currentTick);
                    }

                    // waits a little

                    await Task.Delay(1);
                }
            }
            catch (Exception)
            { }
        }

        private static bool TryParseMessage(byte[] buffer, int size, List<ReadOnlyMemory<byte>> a)
        {
            Contract.Assert(buffer[0] != 58);

            a.Clear();

            while (buffer[size - 1] <= 31)
            {
                size--;

                if (size == 0)
                    return false;
            }

            int i = 0;
            int j = 0;

            do
            {
                if (buffer[j] == 58)
                {
                    // assuming we are processing the last argument then we add it

                    if (j > 0)
                    {
                        a.Add(new ReadOnlyMemory<byte>(buffer, j, size - j));

                        break;
                    }

                    return false;
                }

                // skips everything until we find any " " or ","

                while (j < size && buffer[j] != 32 && buffer[j] != 44)
                    j++;

                // assuming we found an argument we add it

                a.Add(new ReadOnlyMemory<byte>(buffer, i, j - i));

                // skips the next " " and ","

                do
                    j++;
                while ((j < size) && (buffer[j] == 32 || buffer[j] == 44));

                // sets the new starting position

                i = j;
            }
            while (j < size);

            return a.Count != 0;
        }

        private bool TryNICK(IIrcClient client, List<ReadOnlyMemory<byte>> a)
        {
            // NICK D4v1ks3074930439

            ReadOnlyMemory<byte> a1 = a[1];

            if (a1.Length <= NICKLEN)
            {
                string s1 = Encoding.UTF8.GetString(a1.Span);

                if (_nicks.TryAdd(s1, client.Id))
                {
                    client.Nick = s1;

                    return true;
                }
            }

            return false;
        }

        private bool TryUSER(IIrcClient client, List<ReadOnlyMemory<byte>> a, StringBuilder t)
        {
            /*
                USER d4v1ks@hotmail.com 127.0.0.1 192.168.1.71 :D4v1ks
                    -> D4v1ks3074930439!d4v1ks@192.168.1.71
            */

            ReadOnlySpan<byte> a4 = a[4].Span;

            if (a4[0] == (byte)':')
            {
                string originalName = Encoding.UTF8.GetString(a4[1..]);
                string formattedName = originalName.Replace(" ", string.Empty).ToLowerInvariant();

                string user = $"{client.Nick}!{formattedName}@{client.LocalIP}";

                if (_users.TryAdd(user, client.Id))
                {
                    client.Name = originalName;
                    client.User = user;

                    // :d4v1ks.ddns.net 001 D4v1ks3074930439 :Welcome to the REDE-SANTOS IRC Network D4v1ks3074930439!d4v1ks@192.168.1.71

                    t.Clear();

                    t.Append(':');
                    t.Append(_hostname);
                    t.Append(" 001 ");
                    t.Append(client.Nick);
                    t.Append(" :Welcome to the universe of Star Fleet Command!");
                    t.Append(CRLF);

                    WriteTo(client, t);

                    // :d4v1ks.ddns.net 002 Test :Your host is d4v1ks.ddns.net, running version InspIRCd-3

                    t.Clear();

                    t.Append(':');
                    t.Append(_hostname);
                    t.Append(" 002 ");
                    t.Append(client.Nick);
                    t.Append(" :Your communications host is ");
                    t.Append(_hostname);
                    t.Append(CRLF);

                    WriteTo(client, t);

                    // :d4v1ks.ddns.net 005 John AWAYLEN=200 CASEMAPPING=rfc1459 CHANLIMIT=#:20 CHANMODES=Ybw,k,l,Oimnpst CHANLEN=64 CHANTYPES=# ELIST=CMNTU EXTBAN=,O HOSTLEN=64 KEYLEN=32 KICKLEN=255 LINELEN=512 :are supported by this server
                    // :d4v1ks.ddns.net 005 John MAXLIST=bw:100 MAXTARGETS=20 MODES=20 NETWORK=RedeCasa NICKLEN=31 OPERLOG OVERRIDE PREFIX=(yov)!@+ SAFELIST STATUSMSG=!@+ TOPICLEN=307 USERLEN=11 WHOX :are supported by this server

                    t.Clear();

                    t.Append(':');
                    t.Append(_hostname);
                    t.Append(" 005 ");
                    t.Append(client.Nick);

                    t.Append(" CHANMODES=");
                    t.Append(defaultChannelModes);

                    t.Append(" CHANLEN=");
                    t.Append(CHANLEN);
                    t.Append(" CHANTYPES=#");

                    t.Append(" LINELEN=");
                    t.Append(MaximumBufferSize);

                    t.Append(" NICKLEN=");
                    t.Append(NICKLEN);

                    t.Append(" :are supported");
                    t.Append(CRLF);

                    WriteTo(client, t);

                    // :d4v1ks.ddns.net 375 Test :d4v1ks.ddns.net message of the day

                    t.Clear();

                    t.Append(':');
                    t.Append(_hostname);
                    t.Append(" 375 ");
                    t.Append(client.Nick);
                    t.Append(" :");
                    t.Append(CRLF);

                    WriteTo(client, t);

                    // :d4v1ks.ddns.net 372 Test :- Welcome!

                    if (_motd != null)
                    {
                        for (int i = 0; i < _motd.Length; i++)
                        {
                            t.Clear();

                            t.Append(':');
                            t.Append(_hostname);
                            t.Append(" 372 ");
                            t.Append(client.Nick);
                            t.Append(" :-");
                            t.Append(_motd[i]);
                            t.Append(CRLF);

                            WriteTo(client, t);
                        }
                    }

                    if (_logo != null)
                    {
                        for (int i = 0; i < _logo.Length; i++)
                        {
                            t.Clear();

                            t.Append(':');
                            t.Append(_hostname);
                            t.Append(" 372 ");
                            t.Append(client.Nick);
                            t.Append(" :-");
                            t.Append(_logo[i]);
                            t.Append(CRLF);

                            WriteTo(client, t);
                        }
                    }

                    // :d4v1ks.ddns.net 376 Test :End of message of the day.

                    t.Clear();

                    t.Append(':');
                    t.Append(_hostname);
                    t.Append(" 376 ");
                    t.Append(client.Nick);
                    t.Append(" :");
                    t.Append(CRLF);

                    WriteTo(client, t);

#if VERBOSE
                    Debug.WriteLine(client.Nick + " joined the IRC service");
#endif

                    return true;
                }
            }

            return false;
        }

        private void WriteTo(IIrcClient client, StringBuilder t)
        {
            Span<byte> span = stackalloc byte[t.Length];

            bool isConverted = Utils.TryConvert(t, span);

            Contract.Assert(isConverted);

            client.TryWrite(span);
        }

        private bool TryPRIVMSG(IIrcClient client, List<ReadOnlyMemory<byte>> a, StringBuilder t)
        {
            // PRIVMSG #help :Hello World

            ReadOnlySpan<byte> a2 = a[2].Span;

            if (a2[0] == (byte)':')
            {
                string s2 = Encoding.UTF8.GetString(a2);
                string s1 = Encoding.UTF8.GetString(a[1].Span);

                t.Clear();

                t.Append(':');
                t.Append(client.User);
                t.Append(" PRIVMSG ");
                t.Append(s1);
                t.Append(' ');
                t.Append(s2);
                t.Append(CRLF);

                if (_channels.TryGetValue(s1, out IrcChannel channel))
                {
                    if (_whitelist.Count == 0 || a2[0] != (byte)':')
                        BroadcastTo(channel, client.Id, t.ToString());
                    else
                        BroadcastTo(_whitelist, t.ToString());

                    return true;
                }

                if (_nicks.TryGetValue(s1, out int id) && id != client.Id && _clients.TryGetValue(id, out IIrcClient johnDoe))
                {
                    WriteTo(johnDoe, t);

                    return true;
                }
            }

            return false;
        }

        private bool TryNOTICE(IIrcClient client, List<ReadOnlyMemory<byte>> a, StringBuilder t)
        {
            // NOTICE #help :Hello World

            ReadOnlySpan<byte> a2 = a[2].Span;

            if (a2[0] == (byte)':')
            {
                string s2 = Encoding.UTF8.GetString(a2);
                string s1 = Encoding.UTF8.GetString(a[1].Span);

                t.Clear();

                t.Append(':');
                t.Append(client.User);
                t.Append(" NOTICE ");
                t.Append(s1);
                t.Append(' ');
                t.Append(s2);
                t.Append(CRLF);

                if (_channels.TryGetValue(s1, out IrcChannel channel))
                {
                    BroadcastTo(channel, client.Id, t.ToString());

                    return true;
                }

                if (_nicks.TryGetValue(s1, out int id) && id != client.Id && _clients.TryGetValue(id, out IIrcClient johnDoe))
                {
                    WriteTo(johnDoe, t);

                    return true;
                }
            }

            return false;
        }

        private bool TryJOIN(IIrcClient client, int c, List<ReadOnlyMemory<byte>> a, StringBuilder t, SortedList<string, object> n)
        {
            // JOIN #help

            for (int i = 1; i < c; i++)
            {
                ReadOnlySpan<byte> ai = a[i].Span;

                if (ai.Length <= CHANLEN && ai[0] == (byte)'#')
                {
                    string si = Encoding.UTF8.GetString(ai);

                    if (_channels.TryGetValue(si, out IrcChannel channel))
                    {
                        // checks if the current client already joined the channel

                        if (channel.Clients.ContainsKey(client.Id))
                            continue;
                    }
                    else
                    {
                        // creates and adds the new channel

                        channel = new IrcChannel(si);

                        _channels.Add(si, channel);
                    }

                    // adds the current client to the channel, and sets the defaults flags

                    channel.Clients.Add(client.Id, 0);

                    // :D4v1ks3074930439!d4v1ks@192.168.1.71 JOIN :#General@Standard

                    t.Clear();

                    t.Append(':');
                    t.Append(client.User);
                    t.Append(" JOIN :");
                    t.Append(si);
                    t.Append(CRLF);

                    BroadcastTo(channel.Clients, t.ToString());

                    // lists the names currently in the channel

                    ListNamesTo(client, channel, t, n);
                }
            }

            return true;
        }

        private bool TryPART(IIrcClient client, int c, List<ReadOnlyMemory<byte>> a)
        {
            // <Client> PART #General@Standard

            for (int i = 1; i < c; i++)
            {
                ReadOnlySpan<byte> ai = a[i].Span;

                if (ai.Length <= CHANLEN && ai[0] == (byte)'#')
                {
                    string si = Encoding.UTF8.GetString(ai);

                    if (_channels.TryGetValue(si, out IrcChannel channel) && channel.Clients.ContainsKey(client.Id))
                    {
                        // <Server> :D4v1ks3074930439!d4v1ks@192.168.1.71 PART #General@Standard

                        BroadcastTo(channel.Clients, $":{client.User} PART {si} :{CRLF}");
                        RemoveFrom(channel, client);
                    }
                }
            }

            return true;
        }

        private bool TryMODE_2(IIrcClient client, List<ReadOnlyMemory<byte>> a, StringBuilder t)
        {
            string s1 = Encoding.UTF8.GetString(a[1].Span);

            if (_channels.ContainsKey(s1))
            {
                /*
                    <Client> MODE #General@Standard
                    <Server> :d4v1ks.ddns.net 324 D4v1ks3074930439 #General@Standard +nt
                */

                t.Clear();

                t.Append(':');
                t.Append(_hostname);
                t.Append(" 324 ");
                t.Append(client.Nick);
                t.Append(' ');
                t.Append(s1);
                t.Append(" +");
                t.Append(defaultChannelModes);
                t.Append(CRLF);

                WriteTo(client, t);

                return true;
            }

            if (_nicks.TryGetValue(s1, out int id))
            {
                if (client.Id == id && client.Modes != 0)
                {
                    /*
                        <Client> MODE John
                        <Server> :d4v1ks.ddns.net 221 D4v1ks3074930439 +i
                    */

                    t.Clear();

                    t.Append(':');
                    t.Append(_hostname);
                    t.Append(" 221 ");
                    t.Append(client.Nick);
                    t.Append(" +");
                    t.Append(client.Modes);
                    t.Append(CRLF);

                    WriteTo(client, t);

                    return true;
                }
            }

            return false;
        }

        private bool TryMODE_3(IIrcClient client, List<ReadOnlyMemory<byte>> a, StringBuilder t)
        {
            string s1 = Encoding.UTF8.GetString(a[1].Span);

            if (_channels.TryGetValue(s1, out _))
            {
                /*
                    <Client> MODE #General@Standard -t
                    <Server> :D4v1ks3074930439!d4v1ks@192.168.1.71 MODE #General@Standard -t

                    <Client> MODE #General@Standard +t
                    <Server> :D4v1ks3074930439!d4v1ks@192.168.1.71 MODE #General@Standard +t
                */

                return true;
            }

            if (_nicks.TryGetValue(s1, out int id))
            {
                if (client.Id == id)
                {
                    /*
                        <Client> MODE D4v1ks3074930439 -i
                        <Server> :D4v1ks3074930439 MODE D4v1ks3074930439 :-i

                        <Client> MODE D4v1ks3074930439 +i
                        <Server> :D4v1ks3074930439 MODE D4v1ks3074930439 :+i
                    */

                    if (TryUpdateModes(client.Modes, a[2].Span, out long current))
                    {
                        long changes = client.Modes ^ current;

                        if (changes != 0)
                        {
                            // checks the flag 'W'

                            long flag = 1L << 'W' - 'A';

                            if ((changes & flag) != 0)
                            {
                                if ((current & flag) == 0)
                                    _whitelist.Remove(client.Id);
                                else
                                    _whitelist.Add(client.Id, null);
                            }

                            // tells the client what changed

                            t.Clear();

                            t.Append(':');
                            t.Append(client.Nick);
                            t.Append(" MODE ");
                            t.Append(s1);
                            t.Append(" :");

                            AppendModes(t, '+', changes & current);
                            AppendModes(t, '-', changes & client.Modes);

                            client.Modes = current;

                            t.Append(CRLF);

                            WriteTo(client, t);
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryUpdateModes(long source, ReadOnlySpan<byte> flags, out long current)
        {
            int op = 0;

            current = source;

            for (int i = 0; i < flags.Length; i++)
            {
                if (flags[i] == '+')
                    op = 1;
                else if (flags[i] == '-')
                    op = 2;
                else if (op != 0)
                {
                    long flag;

                    if (flags[i] >= 'A' && flags[i] <= 'Z')
                        flag = 1L << flags[i] - 'A';
                    else if (flags[i] >= 'a' && flags[i] <= 'z')
                        flag = 1L << flags[i] - 'a';
                    else
                        return false;

                    if (op == 1)
                        current |= flag;
                    else
                        current &= ~flag;
                }
                else
                    return false;
            }

            return true;
        }

        private static void AppendModes(StringBuilder t, char op, long modes)
        {
            if (modes != 0)
            {
                t.Append(op);

                do
                {
                    int c = BitOperations.TrailingZeroCount(modes);

                    t.Append((char)(c + 'a'));

                    modes ^= 1L << c;
                }
                while (modes != 0);
            }
        }

        private bool TryNAMES(IIrcClient client, int c, List<ReadOnlyMemory<byte>> a, StringBuilder t, SortedList<string, object> n)
        {
            // NAMES #channel1 [, #channel2]

            for (int i = 1; i < c; i++)
            {
                ReadOnlySpan<byte> ai = a[i].Span;

                if (ai.Length <= CHANLEN && ai[0] == (byte)'#')
                {
                    string si = Encoding.UTF8.GetString(ai);

                    if (_channels.TryGetValue(si, out IrcChannel channel))
                        ListNamesTo(client, channel, t, n);
                }

            }

            return true;
        }

        private bool TryPING(IIrcClient client, List<ReadOnlyMemory<byte>> a, StringBuilder t)
        {
            ReadOnlySpan<byte> a1 = a[1].Span;

            if (a1[0] == (byte)':')
            {
                string s1 = Encoding.UTF8.GetString(a1);

                t.Clear();

                t.Append("PONG ");
                t.Append(s1);
                t.Append(CRLF);

                WriteTo(client, t);

                return true;
            }

            return false;
        }

        private bool TryQUIT(IIrcClient client, List<ReadOnlyMemory<byte>> a)
        {
            ReadOnlySpan<byte> a1 = a[1].Span;

            if (a1[0] == (byte)':')
            {
                string s1 = Encoding.UTF8.GetString(a1);

                Quit(client, s1);

                return true;
            }

            return false;
        }

        // ... helpers

        private void Quit(IIrcClient client, string msg)
        {
            Contract.Assert(msg == null || msg.StartsWith(':'));

            // first we try to close the client

            client.Dispose();

            // then we try to remove the client

            _clients.TryRemove(client.Id, out _);

            if (client.Nick != null)
                _nicks.TryRemove(client.Nick, out _);

            if (client.User != null && _users.TryRemove(client.User, out _) && !_users.IsEmpty)
            {
                Span<byte> s0 = stackalloc byte[MaximumBufferSize];
                Span<byte> s1 = s0;

                Utils.Append(ref s1, ":"u8);
                Utils.Append(ref s1, client.User);
                Utils.Append(ref s1, " QUIT "u8);

                if (msg == null)
                    Utils.Append(ref s1, ":"u8);
                else
                    Utils.Append(ref s1, msg);

                Utils.Append(ref s1, "\r\n"u8);

                s1 = s0[..(MaximumBufferSize - s1.Length)];

                foreach (KeyValuePair<int, IIrcClient> p in _clients)
                    p.Value.TryWrite(s1);
            }

            foreach (KeyValuePair<string, IrcChannel> p in _channels)
                RemoveFrom(p.Value, client);

            _whitelist.Remove(client.Id);
        }

        private void TryPingEveryone(long currentTick)
        {
            if (_clients.IsEmpty)
                return;

            Span<byte> s0 = stackalloc byte[MaximumBufferSize];
            Span<byte> s1 = s0;

            Utils.Append(ref s1, "PING "u8);
            Utils.Append(ref s1, _ping);
            Utils.Append(ref s1, "\r\n"u8);

            s1 = s0[..(MaximumBufferSize - s1.Length)];

            foreach (KeyValuePair<int, IIrcClient> p in _clients)
            {
                IIrcClient client = p.Value;

                if (currentTick - client.LastTick >= defaultPingInterval)
                {
                    client.LastTick = currentTick;

                    client.TryWrite(s1);
                }
            }
        }

        private void BroadcastTo(IrcChannel destination, int sourceId, string sourceMsg)
        {
            Span<byte> span = stackalloc byte[sourceMsg.Length];

            if (!Utils.TryConvert(sourceMsg, span))
                throw new NotSupportedException();

            foreach (KeyValuePair<int, object> p in destination.Clients)
            {
                int clientId = p.Key;

                if (clientId != sourceId && _clients.TryGetValue(clientId, out IIrcClient client))
                    client.TryWrite(span);
            }
        }

        private void BroadcastTo(Dictionary<int, object> destination, string sourceMsg)
        {
            Span<byte> span = stackalloc byte[sourceMsg.Length];

            if (!Utils.TryConvert(sourceMsg, span))
                throw new NotSupportedException();

            foreach (KeyValuePair<int, object> p in destination)
            {
                int clientId = p.Key;

                if (_clients.TryGetValue(clientId, out IIrcClient client))
                    client.TryWrite(span);
            }
        }

        private void ListNamesTo(IIrcClient client, IrcChannel channel, StringBuilder t, SortedList<string, object> n)
        {
            Contract.Assert(n.Count == 0);

            foreach (KeyValuePair<int, object> p in channel.Clients)
            {
                if (_clients.TryGetValue(p.Key, out IIrcClient member))
                    n.Add(member.Nick, null);
            }

            Contract.Assert(n.Count != 0);

            // :d4v1ks.ddns.net 353 Test = #help :Test

            IEnumerator<KeyValuePair<string, object>> e = n.GetEnumerator();

            e.MoveNext();

        startNewLine:

            t.Clear();
            t.Append(':');
            t.Append(_hostname);
            t.Append(" 353 ");
            t.Append(client.Nick);
            t.Append(" = ");
            t.Append(channel.Name);
            t.Append(" :");
            t.Append(e.Current.Key);

        getNextNick:

            if (e.MoveNext())
            {
                if (t.Length <= (MaximumBufferSize - (NICKLEN + 2)))
                {
                    t.Append(' ');
                    t.Append(e.Current.Key);

                    goto getNextNick;
                }

                t.Append(CRLF);

                WriteTo(client, t);

                goto startNewLine;
            }

            t.Append(CRLF);

            WriteTo(client, t);

            // :d4v1ks.ddns.net 366 Test #help :End of /NAMES list.

            t.Clear();
            t.Append(':');
            t.Append(_hostname);
            t.Append(" 366 ");
            t.Append(client.Nick);
            t.Append(' ');
            t.Append(channel.Name);
            t.Append(" :End of /NAMES list.");
            t.Append(CRLF);

            WriteTo(client, t);

            // clears the list

            n.Clear();
        }

        private void RemoveFrom(IrcChannel channel, IIrcClient client)
        {
            if (channel.Clients.Remove(client.Id) && channel.Clients.Count == 0)
                _channels.Remove(channel.Name);
        }
    }
}
