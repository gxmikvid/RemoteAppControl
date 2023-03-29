using System;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Net.Sockets;

namespace RemoteAppControl
{
    public class Functions
    {
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
        private static string DecodeWebSocketFrame(byte[] data)
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
        public static Process[] getIconsAndProcesses()
        {
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
                    appIcon.ToBitmap().Save(@"..\Page\icons\icon" + i + ".ico");
                }
            }
            return processes;
        }
        public static void httpRequest(Socket socket)
        {
            byte[] dataBuffer = new byte[1024];
            socket.Receive(dataBuffer);
            string request = DecodeWebSocketFrame(dataBuffer);
            string requestedFile = request.Split(' ')[1];
            if (requestedFile == "/")
            {
                requestedFile = "/index.html";
            }
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
            socket.Close();
        }
        public static void update(string data)
        {
            Program.WSServer.WebSocketServices.Broadcast("Hello, clients!");
        }
    }
}
