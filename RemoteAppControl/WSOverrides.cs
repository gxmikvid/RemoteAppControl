using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace RemoteAppControl
{
    public class WSOverrides : WebSocketBehavior
    {
        protected override void OnOpen()
        {
            Program.WSServer.WebSocketServices["/"].Sessions.SendTo(Encoding.UTF8.GetBytes(Program.processes.Length.ToString()), ID);
        }
        protected override void OnClose(CloseEventArgs e)
        {

        }
        protected override void OnMessage(MessageEventArgs e)
        {
            Functions.update(e.Data);
        }
    }
}
