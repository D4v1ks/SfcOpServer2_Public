using shrNet;

using System;
using System.Buffers;
using System.Net;
using System.Threading.Tasks;

namespace shrServices
{
    public sealed class DuplexService29900 : DuplexService
    {
        private const string ServerChallenge = "0000000000";

        private enum Args
        {
            Final,

            Lc,
            Challenge,
            NewUser,
            Email,
            Nick,
            Password,
            Nur,
            Userid,
            Profileid,
            Login,
            User,
            Response,
            Sesskey,
            Proof,
            //Uniquenick,
            Logout,

            Id_1,
            //Lt_x4TGX3SbkAkNIJt3Hc4N
        }

        private readonly static byte[][] _args =
        [
            "\\final\\"u8.ToArray(),

            "\\lc\\"u8.ToArray(),
            "\\challenge\\"u8.ToArray(),
            "\\newuser\\"u8.ToArray(),
            "\\email\\"u8.ToArray(),
            "\\nick\\"u8.ToArray(),
            "\\password\\"u8.ToArray(),
            "\\nur\\"u8.ToArray(),
            "\\userid\\"u8.ToArray(),
            "\\profileid\\"u8.ToArray(),
            "\\login\\"u8.ToArray(),
            "\\user\\"u8.ToArray(),
            "\\response\\"u8.ToArray(),
            "\\sesskey\\"u8.ToArray(),
            "\\proof\\"u8.ToArray(),
            //"\\uniquenick\\"u8.ToArray(),
            "\\logout\\"u8.ToArray(),

            "\\id\\1"u8.ToArray(),
            //"\\lt\\x4TGX3[SbkAk]NIJt3Hc4N__"u8.ToArray()
        ];

        private readonly static byte[][] _responses =
        [
            "\\error\\\\err\\100\\fatal\\\\errmsg\\A profile with this email already exists.\\id\\1\\final\\"u8.ToArray(),
            "\\error\\\\err\\516\\fatal\\\\errmsg\\A profile with this nick already exists.\\id\\1\\final\\"u8.ToArray(),
            "\\error\\\\err\\515\\fatal\\\\errmsg\\The profile provided is incorrect.\\id\\1\\final\\"u8.ToArray()
        ];

        public DuplexService29900(IPAddress address) : base(address, port: 29900, dataMinSize: 0, dataMaxSize: 1024, _args[(int)Args.Final])
        {
            _handshakeClientMethod += HandshakeClientAsync;
            _processMessageMethod += ProcessMessageAsync;
        }

        private ValueTask<int> HandshakeClientAsync(DuplexServiceTransport client)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(_dataMaxSize);

            try
            {
                /*
                    \lc\1\challenge\0000000000\id\1\final\
                */

                Span<byte> msg = buffer;

                Utils.Append(ref msg, _args[(int)Args.Lc]);
                Utils.Append(ref msg, 1);
                Utils.Append(ref msg, _args[(int)Args.Challenge]);
                Utils.Append(ref msg, ServerChallenge);
                Utils.Append(ref msg, _args[(int)Args.Id_1]);
                Utils.Append(ref msg, _args[(int)Args.Final]);

                msg = buffer.AsSpan()[..^msg.Length];

                // send handshake

                client.Write(msg);

                return ValueTask.FromResult(msg.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private ValueTask<int> ProcessMessageAsync(DuplexServiceTransport client, ReadOnlySequence<byte> sequence)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(_dataMaxSize);

            try
            {
                client.Read(sequence, buffer, out Span<byte> msg);

                // checks the request

                if (msg.StartsWith(_args[(int)Args.NewUser]))
                {
                    /*
                        \newuser\\email\d4v1ks@hotmail.com\nick\D4v1ks\password\sfcRulz\id\1\final\
                    */

                    if (
                        Utils.TryGetValue(msg, _args[(int)Args.Email], 92, out string email) &&
                        Utils.TryGetValue(msg, _args[(int)Args.Nick], 92, out string nick) &&
                        Utils.TryGetValue(msg, _args[(int)Args.Password], 92, out string password)
                    )
                    {
                        int r = GamespyService.AddUser(email, nick, password, out GamespyUser user);

                        if (r == -1)
                            msg = _responses[0];
                        else if (r == -2)
                            msg = _responses[1];
                        else
                        {
                            msg = buffer;

                            Utils.Append(ref msg, _args[(int)Args.Nur]);
                            Utils.Append(ref msg, _args[(int)Args.Userid]);
                            Utils.Append(ref msg, user.Id);
                            Utils.Append(ref msg, _args[(int)Args.Profileid]);
                            Utils.Append(ref msg, user.Id);
                            Utils.Append(ref msg, _args[(int)Args.Id_1]);
                            Utils.Append(ref msg, _args[(int)Args.Final]);

                            msg = buffer.AsSpan()[..^msg.Length];
                        }

                        goto sendResponse;
                    }
                }
                else if (msg.StartsWith(_args[(int)Args.Login]))
                {
                    /*
                        \login\\challenge\IoWIbSnjMf2pv9iUioRgF9ySYLV2r72p\user\JohnDoe2@johndoe2@hotmail.com\userid\12617\profileid\19465\response\08eeb76f1241ac0777a18aac726c03d1\firewall\1\port\0\id\1\final\
                        \login\\challenge\iIH2SY9bWx0HmRWyFJy42C1J38OzKufQ\user\D4v1ks@d4v1ks@hotmail.com\response\b7d51dd7a4efbf68db2725c1b7652c5c\firewall\1\port\0\id\1\final\
                    */

                    if (
                        Utils.TryGetValue(msg, _args[(int)Args.Challenge], 92, out string challenge) &&
                        Utils.TryGetValue(msg, _args[(int)Args.User], 92, out string username) &&
                        Utils.TryGetValue(msg, _args[(int)Args.Response], 92, out string response)
                    )
                    {
                        GamespyService.GetUser(username, out GamespyUser user);

                        if (user != null)
                        {
                            string password = Utils.GetHash(user.Password);
                            string userData = $"{password}                                                {user.Username}";
                            string clientResponse = Utils.GetHash($"{userData}{challenge}{ServerChallenge}{password}");
                            string serviceResponse = Utils.GetHash($"{userData}{ServerChallenge}{challenge}{password}");

                            if (clientResponse.Equals(response, StringComparison.Ordinal))
                            {
                                // \lc\2\sesskey\239289\proof\8f1295968d534a3c6816d24dc172d92e\userid\12617\profileid\19465\uniquenick\JohnDoe2@johndoe2@hotmail.com\lt\x4TGX3[SbkAk]NIJt3Hc4N__\id\1\final\

                                msg = buffer;

                                Utils.Append(ref msg, _args[(int)Args.Lc]);
                                Utils.Append(ref msg, 2);
                                Utils.Append(ref msg, _args[(int)Args.Sesskey]);
                                Utils.Append(ref msg, user.Id);
                                Utils.Append(ref msg, _args[(int)Args.Proof]);
                                Utils.Append(ref msg, serviceResponse);
                                Utils.Append(ref msg, _args[(int)Args.Userid]);
                                Utils.Append(ref msg, user.Id);
                                Utils.Append(ref msg, _args[(int)Args.Profileid]);
                                Utils.Append(ref msg, user.Id);
                                //Utils.Append(ref msg, _args[(int)Args.Uniquenick]);
                                //Utils.Append(ref msg, user.Username);
                                //Utils.Append(ref msg, _args[(int)Args.Lt_x4TGX3SbkAkNIJt3Hc4N]);
                                Utils.Append(ref msg, _args[(int)Args.Id_1]);
                                Utils.Append(ref msg, _args[(int)Args.Final]);

                                msg = buffer.AsSpan()[..^msg.Length];

                                goto sendResponse;
                            }
                        }

                        msg = _responses[2];

                        goto sendResponse;
                    }
                }
                else if (msg.StartsWith(_args[(int)Args.Logout]))
                {
                    /*
                        \logout\\sesskey\239288\final\
                    */

                    goto skipResponse;
                }

                return ValueTask.FromResult(-1);

            skipResponse:

                return ValueTask.FromResult(0);

            sendResponse:

                client.Write(msg);

                return ValueTask.FromResult(msg.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
