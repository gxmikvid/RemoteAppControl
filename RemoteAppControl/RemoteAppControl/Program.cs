using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;

class RAC {
    #region socketStuff
    static string DecodeWebSocketFrame(byte[] data)
    {
        byte opcode = (byte)(data[0] & 0x0F);
        bool isMasked = (data[1] & 0x80) == 0x80;
        int payloadLength = data[1] & 0x7F;

        int offset = 2;
        if (payloadLength == 126)
        {
            payloadLength = (data[2] << 8) | data[3];
            offset += 2;
        }
        else if (payloadLength == 127)
        {
            payloadLength = (int)(
                ((long)data[2] << 56) |
                ((long)data[3] << 48) |
                ((long)data[4] << 40) |
                ((long)data[5] << 32) |
                ((long)data[6] << 24) |
                ((long)data[7] << 16) |
                ((long)data[8] << 8) |
                data[9]);
            offset += 8;
        }

        byte[] mask = null;
        if (isMasked)
        {
            mask = new byte[] { data[offset], data[offset + 1], data[offset + 2], data[offset + 3] };
            offset += 4;
        }

        byte[] payload = new byte[payloadLength];
        Array.Copy(data, offset, payload, 0, payloadLength);

        if (isMasked)
        {
            for (int i = 0; i < payloadLength; i++)
            {
                payload[i] ^= mask[i % 4];
            }
        }

        return Encoding.UTF8.GetString(payload);
    }
    static string ComputeWebSocketHandshakeSecurityHash(string key)
    {
        string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        string concatenated = key + guid;
        byte[] concatenatedBytes = Encoding.UTF8.GetBytes(concatenated);
        byte[] hashedBytes;

        using (SHA1 sha1 = SHA1.Create())
        {
            hashedBytes = sha1.ComputeHash(concatenatedBytes);
        }

        return Convert.ToBase64String(hashedBytes);
    }
    static void HandleClient(TcpClient client)
    {
        byte[] buffer = new byte[1024];
        int bytesRead = client.GetStream().Read(buffer, 0, buffer.Length);
        string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        //parse WebSocket key from request
        int keyStart = request.IndexOf("Sec-WebSocket-Key:") + 19;
        int keyEnd = request.IndexOf("\r\n", keyStart);
        string key = request.Substring(keyStart, keyEnd - keyStart).Trim();
        string acceptKey = ComputeWebSocketHandshakeSecurityHash(key);

        string response = "HTTP/1.1 101 Switching Protocols\r\n" +
            "Connection: Upgrade\r\n" +
            "Upgrade: websocket\r\n" +
            $"Sec-WebSocket-Accept: {acceptKey}\r\n\r\n";
        //send key back to complete handshake
        byte[] responseBuffer = Encoding.UTF8.GetBytes(response);
        Stream clientStream = client.GetStream();
        clientStream.Write(responseBuffer, 0, responseBuffer.Length);

        handleData(clientStream);
    }
    #endregion
    #region httpStuff
    private static string GetContentType(string fileExtension)
    {
        switch (fileExtension)
        {
            case ".html":
                return "text/html";
            case ".css":
                return "text/css";
            case ".js":
                return "text/javascript";
            case ".jpg":
            case ".jpeg":
                return "image/jpeg";
            case ".png":
                return "image/png";
            case ".gif":
                return "image/gif";
            default:
                return "application/octet-stream";
        }
    }
    private static void httpRequest(Socket socket)
    {
        string requestedFile = "/index.html";
        string fileExtension = Path.GetExtension(requestedFile);
        string contentType = GetContentType(fileExtension);

        try
        {
            byte[] fileContents = File.ReadAllBytes(@"..\Page" + requestedFile);
            byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n" +
                "Content-Type: " + contentType + "\r\n" +
                "Content-Length: " + fileContents.Length + "\r\n" +
                "Connection: close\r\n\r\n");

            socket.Send(response);
            socket.Send(fileContents);
        }
        catch (FileNotFoundException)
        {
            byte[] notFoundContents = Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n" +
                "Content-Type: text/plain\r\n" +
                "Content-Length: 13\r\n" +
                "Connection: close\r\n\r\n" +
                "404 Not Found");

            socket.Send(notFoundContents);
        }
    }
    #endregion
    static void handleData(Stream clientStream)
    {
        while (true)
        {
            byte[] dataBuffer = new byte[1024];
            int dataBytesRead = clientStream.Read(dataBuffer, 0, dataBuffer.Length);

            if (dataBytesRead == 0)
            {
                Console.WriteLine("Client disconnected.");
                break;
            }

            Console.WriteLine($"Received data from client: {DecodeWebSocketFrame(dataBuffer)}");
            
        }
    }
    static void Main()
    {
        //get processes, arguments and icons
        string iconfolder = @"..\page\icons";
        if (Directory.Exists(iconfolder))
        {
            Directory.Delete(iconfolder, true);
        }
        Directory.CreateDirectory(iconfolder);
        string[] commands = File.ReadAllLines(@"..\RunCMDs.txt");
        Process[] processes = new Process[commands.Length];
        for (int i = 0; i < commands.Length; i++)
        {
            string[] cmd_args = Regex.Split(commands[i], $"\t\t");
            processes[i] = new Process();
            processes[i].StartInfo.FileName = cmd_args[0];
            processes[i].StartInfo.Arguments = cmd_args[1];
            Icon appIcon = Icon.ExtractAssociatedIcon(cmd_args[0]);
            if (appIcon != null)
            {
                appIcon.ToBitmap().Save(@"..\Page\icons\icon"+i+".ico");
            }
        }
        //start http server
        Socket httpsocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        httpsocket.Bind(new IPEndPoint(IPAddress.Any, 80));
        httpsocket.Listen(10);
        Console.WriteLine($"Server started on port 8080.");

        Task.Run(() =>
        {
            while (true)
            {
                var httpclientsocket = httpsocket.Accept();
                httpRequest(httpclientsocket);
            }
        });
        //start ws server
        TcpListener websocketserver = new TcpListener(IPAddress.Any, 8080);
        websocketserver.Start();

        Task.Run(() =>
        {
            while (true)
            {
                TcpClient websocketclient = websocketserver.AcceptTcpClient();
                Console.WriteLine("Client connected.");

                HandleClient(websocketclient);
            }
        });
        //stop console from closing
        Console.TreatControlCAsInput = true;

        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);
            if (key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.C)
            {
                break;
            }
        }
    }
}