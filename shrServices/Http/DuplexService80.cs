using shrNet;

using System;
using System.Buffers;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace shrServices
{
    public sealed class DuplexService80 : DuplexService
    {
        private const string DateTimeFormat = "ddd, dd MMM yyyy HH:mm:ss";

        private enum Args
        {
            Http_404_NotFound,
            Http_200_OK,
            Date_,
            Server_,
            Connection_Close,
            LastModified_,
            AcceptRanges_Bytes,
            ContentLength_,
            ContentType_TextHtml,
            ContentType_TextPlain,

            _Gmt,

            Delimiter,
        }

        private enum Requests
        {
            NotFound,

            Index,
            SystemMotd,
            GameMotd,

            Total
        }

        private static readonly byte[][] _args =
        [
            "HTTP/1.1 404 Not Found"u8.ToArray(),
            "HTTP/1.1 200 OK"u8.ToArray(),
            "Date: "u8.ToArray(),
            "Server: "u8.ToArray(),
            "Connection: close"u8.ToArray(),
            "Last-Modified: "u8.ToArray(),
            "Accept-Ranges: bytes"u8.ToArray(),
            "Content-Length: "u8.ToArray(),
            "Content-Type: text/html"u8.ToArray(),
            "Content-Type: text/plain"u8.ToArray(),

            " GMT"u8.ToArray(),

            "\r\n\r\n"u8.ToArray()
        ];

        private static readonly byte[][] _requests =
        [
            null,

            "GET / HTTP/1.1"u8.ToArray(),
            "GET /motd/sys/motd.txt HTTP/1.1"u8.ToArray(),
            "GET /motd/starfleetcommand2/motd.txt HTTP/1.1"u8.ToArray()
        ];

        private static byte[][] _responses;

        public static void Initialize(string appName, string[] motd)
        {
            Contract.Assert(motd.Length == 2);

            _responses =
            [
                // not found
                GetHtml("404 Not Found", "Not Found", "The requested URL was not found on this server."),

                // index
                GetHtml(appName, "Index", "Under construction..."),

                // system message
                GetMotd(motd[0]),

                // game message
                GetMotd(motd[1]),

                // (total) server
                Encoding.UTF8.GetBytes(appName),

                // (total + 1) last modified
                Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString(DateTimeFormat, CultureInfo.InvariantCulture))
            ];
        }

        private static byte[] GetHtml(string title, string h1, string p)
        {
            return Encoding.UTF8.GetBytes($"<html><head><title>{title}</title><meta http-equiv=\"Content-Type\" content=\"text/html\"></head><body><h1>{h1}</h1><p>{p}</p></body></html>");
        }

        private static byte[] GetMotd(string message)
        {
            return Encoding.UTF8.GetBytes($" {message}");
        }

        private static void GetResponse(Span<byte> buffer, Args http, Args contentType, Requests content, out Span<byte> s)
        {
            bool connectionClose = buffer.IndexOf(_args[(int)Args.Connection_Close]) != -1;

            s = buffer;

            Utils.Append(ref s, _args[(int)http]);
            Utils.Append(ref s, "\r\n");

            Utils.Append(ref s, _args[(int)Args.Date_]);
            Utils.Append(ref s, DateTime.UtcNow.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
            Utils.Append(ref s, _args[(int)Args._Gmt]);
            Utils.Append(ref s, "\r\n");

            Utils.Append(ref s, _args[(int)Args.Server_]);
            Utils.Append(ref s, _responses[(int)Requests.Total]);
            Utils.Append(ref s, "\r\n");

            if (!connectionClose)
            {
                Utils.Append(ref s, _args[(int)Args.Connection_Close]);
                Utils.Append(ref s, "\r\n");
            }

            Utils.Append(ref s, _args[(int)Args.LastModified_]);
            Utils.Append(ref s, _responses[(int)Requests.Total + 1]);
            Utils.Append(ref s, _args[(int)Args._Gmt]);
            Utils.Append(ref s, "\r\n");

            Utils.Append(ref s, _args[(int)Args.AcceptRanges_Bytes]);
            Utils.Append(ref s, "\r\n");

            Utils.Append(ref s, _args[(int)Args.ContentLength_]);
            Utils.Append(ref s, _responses[(int)content].Length);
            Utils.Append(ref s, "\r\n");

            Utils.Append(ref s, _args[(int)contentType]);
            Utils.Append(ref s, _args[(int)Args.Delimiter]);

            Utils.Append(ref s, _responses[(int)content]);

            s = buffer[..^s.Length];
        }

        public DuplexService80(IPAddress address) : base(address, port: 27002, dataMinSize: 0, dataMaxSize: 1024, _args[(int)Args.Delimiter])
        {
            _processMessageMethod += ProcessMessageAsync;
        }

        private ValueTask<int> ProcessMessageAsync(DuplexServiceTransport client, ReadOnlySequence<byte> sequence)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(_dataMaxSize);

            try
            {
                client.Read(sequence, buffer, out Span<byte> msg);

                // checks the request

                if (msg.StartsWith(_requests[(int)Requests.Index]))
                    GetResponse(buffer, Args.Http_200_OK, Args.ContentType_TextHtml, Requests.Index, out msg);
                else if (msg.StartsWith(_requests[(int)Requests.SystemMotd]))
                    GetResponse(buffer, Args.Http_200_OK, Args.ContentType_TextPlain, Requests.SystemMotd, out msg);
                else if (msg.StartsWith(_requests[(int)Requests.GameMotd]))
                    GetResponse(buffer, Args.Http_200_OK, Args.ContentType_TextPlain, Requests.GameMotd, out msg);
                else
                    GetResponse(buffer, Args.Http_404_NotFound, Args.ContentType_TextHtml, Requests.NotFound, out msg);

                // sends the response

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
