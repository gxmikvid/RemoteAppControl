using System;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace RemoteAppControl
{
    public class WSOverrides : WebSocketBehavior
    {
        protected override void OnOpen()
        {

        }
        protected override void OnClose(CloseEventArgs e)
        {

        }
        protected override void OnMessage(MessageEventArgs e)
        {
            Console.WriteLine("Received message from " + Context.UserEndPoint + ": " + e.Data);
            Functions.update(e.Data);
        }
    }
}
