
using System.Net;

namespace MDrude.Networking.WebSockets;

public class WSHandshaker : TCPHandshaker<WSServerConnection> {

    public override async Task<bool> Handshake(WSServerConnection conn) {

        string header = await ReadHeader(conn.Stream);
        Regex getRegex = new Regex(@"^GET(.*)HTTP\/1\.1", RegexOptions.IgnoreCase);
        Match getRegexMatch = getRegex.Match(header);

        if (getRegexMatch.Success) {

            string[] lines = header.Split('\n');

            foreach (string line in lines) {

                if (line.ToLower().StartsWith("user-agent:")) {

                    int index = line.IndexOf(':') + 1;

                    if (index >= line.Length) {
                        continue;
                    }

                    conn.Meta.UserAgent = line.Substring(index, line.Length - index).Trim();

                } else if (line.ToLower().StartsWith("x-forwarded-for:")) {

                    int index = line.IndexOf(':') + 1;

                    if (index >= line.Length) {
                        continue;
                    }

                    conn.Meta.IP = line.Substring(index, line.Length - index).Trim()
                        .Split(',').First().Trim();

                }

            }

            if (string.IsNullOrEmpty(conn.Meta.IP)) {
                conn.Meta.IP = ((IPEndPoint)conn.Socket.RemoteEndPoint).Address.ToString();
            }

            await DoHandshake(conn.Stream, header);
            return true;

        }

        return false;

    }

    public override async Task<bool> ClientHandshake(Stream stream) {

        Memory<byte> buffer = Encoding.UTF8.GetBytes(GetSendHeader());
        await stream.WriteAsync(buffer);

        string result = await ReadHeader(stream);

        return result != null && result.ToLower().Contains("upgrade: websocket");

    }

    private async Task DoHandshake(Stream ns, string data) {

        string response = "HTTP/1.1 101 Switching Protocols" + Environment.NewLine
            + "Connection: Upgrade" + Environment.NewLine
            + "Upgrade: websocket" + Environment.NewLine
            + "Sec-Websocket-Accept: " + Convert.ToBase64String(
                SHA1.Create().ComputeHash(
                    Encoding.UTF8.GetBytes(
                        new Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                    )
                )
            ) + Environment.NewLine
            + Environment.NewLine;

        await WriteHeader(ns, response);

    }

    private async Task WriteHeader(Stream stream, string content) {

        byte[] buffer = Encoding.UTF8.GetBytes(content);
        await stream.WriteAsync(buffer, 0, buffer.Length);

    }

    private async Task<string> ReadHeader(Stream stream) {

        int len = 32768;
        int read = 0;
        byte[] buffer = new byte[len];

        read = await stream.ReadAsync(buffer, 0, buffer.Length);

        string header = Encoding.UTF8.GetString(buffer);

        if (header.Contains("\r\n\r\n")) {

            return header;

        }

        return null;

    }

    private string GetSendHeader() {

        return "GET / HTTP/1.1" + Environment.NewLine
            + $"Host: Host" + Environment.NewLine
            + $"Connection: upgrade" + Environment.NewLine
            + $"Pragma: no-cache" + Environment.NewLine
            + $"User-Agent: Mozilla/5.0 (None) Chrome" + Environment.NewLine
            + $"Upgrade: websocket" + Environment.NewLine
            + $"Origin: websocket" + Environment.NewLine
            + $"Sec-WebSocket-Version: 13" + Environment.NewLine
            + $"Accept-Encoding: gzip, deflate, br" + Environment.NewLine
            + $"Accept-Language: en,en-US;q=0.9" + Environment.NewLine
            + $"Sec-WebSocket-Key: {RandomGen.CreateBase64Key()}" + Environment.NewLine
            + $"Sec-WebSocket-Extensions: " + Environment.NewLine
            + Environment.NewLine;

    }

}
