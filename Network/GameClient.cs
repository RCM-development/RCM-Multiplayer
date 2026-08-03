using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TestMod;

namespace RCM_Coop.Network
{
    internal class GameClient
    {
        Session session;
        GameClient(Session session){
            this.session = session;
            session.data_recieved_callback = OnDataReceived;
            session.connection_terminated_callback = OnConnectionTerminated;
            session.connection_opened_callback = OnConnectionOpened;
        }


        void OnDataReceived(byte[] data, TcpClient client){
            RCMManager.Log($"[Co-op] Received {data.Length} bytes from {client.Client.RemoteEndPoint}");
        }
        void OnConnectionTerminated(TcpClient client){
            RCMManager.Log($"[Co-op] Connection terminated with {client.Client.RemoteEndPoint}");
        }
        void OnConnectionOpened(TcpClient client){
            RCMManager.Log($"[Co-op] Connection opened with {client.Client.RemoteEndPoint}, sending a data packet");

            SendJoinRequest();
        }


        void SendJoinRequest(){
            string username = "player02_client";
            string password = "password";

            // serialize data
            session.SendTCP(GameProtocols.SerializeClientJoinRequest(username, password));
        }
    }
}
