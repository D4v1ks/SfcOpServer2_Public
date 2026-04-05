using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace shrServices
{
    public abstract class GamespyGame : IDisposable
    {
        private enum Args
        {
            Status,
            Gamename_Gamever_Location_Serverver_Validclientver_Hostname,
            Hostport,
            Mapname,
            Gametype,
            Maxnumplayers,
            Numplayers,
            Maxnumloggedonplayers,
            Numloggedonplayers,
            Gamemode_Racelist,
            Password_Final_Queryid
        }

        private static readonly byte[][] _args;

        private static int _lastId;

        protected readonly string _hostAddress;
        protected readonly string _hostPort;

        protected string _hostName;
        protected string _earlyMap;
        protected string _middleMap;
        protected string _lateMap;
        protected string _gameType;
        protected int _maxNumPlayers;
        protected int _numPlayers;
        protected int _maxNumLoggedOnPlayers;
        protected int _numLoggedOnPlayers;
        protected uint _raceList;

        protected long _isDisposing;

        private readonly int _id;
        private readonly byte[] _endPoint;

        private Socket _socket;

        public int Id => _id;

        static GamespyGame()
        {
            _args =
            [
                "\\status\\"u8.ToArray(),

                Encoding.UTF8.GetBytes(
                    "\\gamename\\" + GamespyService.GameName +
                    "\\gamever\\" + GamespyService.GameVersion +
                    "\\location\\0" +
                    "\\serverver\\" + GamespyService.ClientVersion +
                    "\\validclientver\\" + GamespyService.ClientVersion +
                    "\\hostname\\"
                ),

                "\\hostport\\"u8.ToArray(),
                "\\mapname\\"u8.ToArray(),
                "\\gametype\\"u8.ToArray(),
                "\\maxnumplayers\\"u8.ToArray(),
                "\\numplayers\\"u8.ToArray(),
                "\\maxnumloggedonplayers\\"u8.ToArray(),
                "\\numloggedonplayers\\"u8.ToArray(),
                "\\gamemode\\Open\\racelist\\"u8.ToArray(),
                "\\password\\\\final\\\\queryid\\1.1"u8.ToArray()
            ];
        }

        public GamespyGame(IPAddress address, int port)
        {
            _hostAddress = address.ToString();
            _hostPort = port.ToString(CultureInfo.InvariantCulture);

            _id = Interlocked.Increment(ref _lastId);

            byte[] ip = address.GetAddressBytes();

            Contract.Assert(ip.Length == 4);

            _endPoint =
            [
                ip[0],
                ip[1],
                ip[2],
                ip[3],
                (byte)(port >> 8),
                (byte)port
            ];
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposing, 1L) == 0L)
                TryDispose();

            GC.SuppressFinalize(this);
        }

        public async Task AdvertiseAsync()
        {
            byte[] buffer = null;

            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                {
                    ExclusiveAddressUse = true
                };

                _socket.Bind(new IPEndPoint(BitConverter.ToUInt32(_endPoint, 0), _endPoint[5] | _endPoint[4] << 8));

                buffer = ArrayPool<byte>.Shared.Rent(2048);

                Memory<byte> receiveMemory = new(buffer);
                EndPoint receiveEP = new IPEndPoint(IPAddress.Any, 0);

                Contract.Assert(_args[(int)Args.Status].Length == 8);

                ulong statusRequest = BitConverter.ToUInt64(_args[(int)Args.Status], 0);

                GamespyService.AddGame(this);

                SocketReceiveFromResult result;

                while (Interlocked.Read(ref _isDisposing) == 0)
                {
                    try
                    {
                        result = await _socket.ReceiveFromAsync(receiveMemory, SocketFlags.None, receiveEP);
                    }
                    catch (SocketException e)
                    {
                        if (e.SocketErrorCode == SocketError.OperationAborted)
                            break;

                        continue;
                    }
                    catch (Exception)
                    {
                        break;
                    }

#if VERBOSE
                    Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [UdpClient] " + Encoding.UTF8.GetString(buffer, 0, result.ReceivedBytes));
#endif

                    if (result.ReceivedBytes != 8 || Unsafe.ReadUnaligned<ulong>(ref buffer[0]) != statusRequest)
                        continue;

                    GetStatus(buffer, out ReadOnlyMemory<byte> sendMemory);

                    try
                    {
                        await _socket.SendToAsync(sendMemory, SocketFlags.None, result.RemoteEndPoint);
                    }
                    catch (SocketException e)
                    {
                        if (e.SocketErrorCode == SocketError.OperationAborted)
                            break;

                        continue;
                    }
                    catch (Exception)
                    {
                        break;
                    }

#if VERBOSE
                    Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [UdpServer] " + Encoding.UTF8.GetString(sendMemory.Span));
#endif

                }
            }
            catch (Exception)
            { }
            finally
            {
                if (buffer != null)
                    ArrayPool<byte>.Shared.Return(buffer);

                GamespyService.RemoveGame(this);

                Dispose();
            }
        }

        public void AppendEndPoint(ref Span<byte> destination)
        {
            _endPoint.CopyTo(destination);

            destination = destination[6..];
        }

        protected virtual void TryDispose()
        {
            _socket?.Dispose();
        }

        private void GetStatus(byte[] buffer, out ReadOnlyMemory<byte> status)
        {
            Span<byte> s = new(buffer);

            Utils.Append(ref s, _args[(int)Args.Gamename_Gamever_Location_Serverver_Validclientver_Hostname]);
            Utils.Append(ref s, _hostName);
            Utils.Append(ref s, _args[(int)Args.Hostport]);
            Utils.Append(ref s, _hostPort);
            Utils.Append(ref s, _args[(int)Args.Mapname]);
            Utils.Append(ref s, _earlyMap);
            Utils.Append(ref s, (byte)' ');
            Utils.Append(ref s, _middleMap);
            Utils.Append(ref s, (byte)' ');
            Utils.Append(ref s, _lateMap);
            Utils.Append(ref s, _args[(int)Args.Gametype]);
            Utils.Append(ref s, _gameType);
            Utils.Append(ref s, _args[(int)Args.Maxnumplayers]);
            Utils.Append(ref s, _maxNumPlayers);
            Utils.Append(ref s, _args[(int)Args.Numplayers]);
            Utils.Append(ref s, _numPlayers);
            Utils.Append(ref s, _args[(int)Args.Maxnumloggedonplayers]);
            Utils.Append(ref s, _maxNumLoggedOnPlayers);
            Utils.Append(ref s, _args[(int)Args.Numloggedonplayers]);
            Utils.Append(ref s, _numLoggedOnPlayers);
            Utils.Append(ref s, _args[(int)Args.Gamemode_Racelist]);

            uint m = _raceList;
            int n = 0;

            while (m != 0u)
            {
                if ((m & 1u) != 0u)
                {
                    Utils.Append(ref s, n);
                    Utils.Append(ref s, (byte)' ');
                }

                m >>= 1;
                n++;
            }

            Utils.Append(ref s, _args[(int)Args.Password_Final_Queryid]);

            status = new(buffer, 0, buffer.Length - s.Length);
        }
    }
}
