using System;
using System.Net;
using System.Net.Sockets;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;
using System.Collections.Generic;
using System.IO;

namespace RemoteAppControl
{
    internal class Program
    {
        //variable must be global
        public static WebSocketServer WSServer;
        static void Main(string[] args)
        {
            Process[] processes = Functions.getIconsAndProcesses();
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
        }
    }
}