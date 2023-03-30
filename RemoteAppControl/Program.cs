using System;
using System.Net;
using System.Net.Sockets;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenHardwareMonitor.Hardware;

namespace RemoteAppControl
{
    internal class Program
    {
        //global variables
        public static WebSocketServer WSServer;
        public static Process[] processes = Functions.getIconsAndProcesses();
        public static Computer computer = new Computer();
        
        static void Main(string[] args)
        {
            //start sensors
            computer.Open();
            computer.CPUEnabled = true;
            computer.RAMEnabled = true;
            //start http server
            Socket httpsocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            httpsocket.Bind(new IPEndPoint(IPAddress.Any, 80));
            httpsocket.Listen(10);
            Console.WriteLine("HTTP server started on 0.0.0.0:80");

            Task.Run(() =>
            {
                while (true)
                {
                    var httpclientsocket = httpsocket.Accept();
                    Task.Run(() => {Functions.httpRequest(httpclientsocket);});
                }
            });
            //start ws server
            WSServer = new WebSocketServer("ws://0.0.0.0:8080");
            WSServer.AddWebSocketService<WSOverrides>("/");
            WSServer.Start();
            Console.WriteLine("WebSocket server started on ws://0.0.0.0:8080/");
            //update per second
            Task.Run(() =>
            {
                while (true)
                {
                    Functions.update("");
                    Thread.Sleep(1000);
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
            httpsocket.Close();
            WSServer.Stop();
            computer.Close();
        }
    }
}