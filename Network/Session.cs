using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TestMod;
using static Profiler;
using static RCM_Coop.Network.GameProtocols;
namespace RCM_Coop{

    public class Session{
        protected const int TcpPort = 5500;
        protected const int MAX_PACKET_SIZE = 131072;


        public bool is_server;
        public bool is_alive;
        public virtual void Terminate() { }
        public void SendTCP(SerializablePacket packet) => SendTCP(packet.Serialize());
        public async void SendTCP(SerializablePacket packet, TcpClient target) => await SendTCP(packet.Serialize(), target);
        protected virtual async void SendTCP(byte[] data) { }
        protected async Task SendTCP(byte[] data, TcpClient target){
            //RCMManager.Log($"[Co-op] sending packet of size: {data.Length}");
            if (data.Length > MAX_PACKET_SIZE){
                RCMManager.Log("Data too large for TCP packet, cant send.");
                return;
            }

            try
            { if (target != null && is_alive) await target.GetStream().WriteAsync(data, 0, data.Length);
            } catch (Exception ex) { RCMManager.Log("Error sending TCP message: " + ex.Message); }
        }
        public Action<byte[], TcpClient> data_recieved_callback;
        public Action<TcpClient> connection_opened_callback;
        public Action<TcpClient> connection_terminated_callback;
    }

    class SessionServer : Session{
        TcpListener listener;
        List<TcpClient> tcpClients = new List<TcpClient>();
        public SessionServer(){
            is_alive = true; is_server = true;
            Task.Run(StartTcpServer);
        }
        public override void Terminate(){
            if (!is_alive) return;
            RCMManager.Log("Server TCP session terminating");
            is_alive = false;
            try{
                if (listener != null) listener.Stop();
                foreach (var c in tcpClients) {
                    connection_terminated_callback?.Invoke(c);
                    c.Close();
            }} catch (Exception e) { RCMManager.Log("SERVER TERMINATING ERROR: " + e.Message); }
        }
        protected override async void SendTCP(byte[] data){
            for (int i = 0; i < tcpClients.Count; i++)
                await SendTCP(data, tcpClients[i]);
        }
        private async Task StartTcpServer(){
            try{
                listener = new TcpListener(IPAddress.Any, TcpPort);
                listener.Start();
                RCMManager.Log("Server TCP alive");

                while (is_alive){
                    var client = await listener.AcceptTcpClientAsync();
                    RCMManager.Log("Server TCP connection established");
                    if (!is_alive) break;

                    tcpClients.Add(client);
                    connection_opened_callback?.Invoke(client);
                    _ = Task.Run(() => TcpListen(client));
                }
            } catch (Exception e){ RCMManager.Log("SERVER init TCP ERROR: " + e.Message); }
            Terminate();
        }
        private async Task TcpListen(TcpClient client){
            byte[] buffer = new byte[MAX_PACKET_SIZE];
            try{ while (is_alive){
                int read = await client.GetStream().ReadAsync(buffer, 0, buffer.Length);
                if (read == 0) break;
                byte[] data = new byte[read];
                Buffer.BlockCopy(buffer, 0, data, 0, read);
                data_recieved_callback.Invoke(data, client);
            }} catch (Exception e) { 
                RCMManager.Log("SERVER listen TCP ERROR: " + e.Message);
                connection_terminated_callback?.Invoke(client);
                client.Close();
            }
            tcpClients.Remove(client);
        }
    }

    class SessionClient : Session{
        TcpClient tcpClient;
        IPAddress host_address;
        public SessionClient(IPAddress host_address){
            is_alive = true; is_server = false; this.host_address = host_address;
            Task.Run(StartTcpClient);
        }
        public override void Terminate(){
            if (!is_alive) return;
            RCMManager.Log("Client TCP session terminating");
            is_alive = false;
            connection_terminated_callback?.Invoke(tcpClient);
            try{ if (tcpClient != null) tcpClient.Close();
            } catch (Exception ex){ RCMManager.Log("Error terminating session client: " + ex.Message);}
        }
        protected override async void SendTCP(byte[] data) => await SendTCP(data, tcpClient);
        private async Task StartTcpClient()
        {
            try{tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(host_address, TcpPort);
                RCMManager.Log("Client TCP connection established");
                connection_opened_callback?.Invoke(tcpClient);

                byte[] buffer = new byte[MAX_PACKET_SIZE];
                while (is_alive){
                    int read = await tcpClient.GetStream().ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0) break;
                    byte[] data = new byte[read];
                    Buffer.BlockCopy(buffer, 0, data, 0, read);
                    data_recieved_callback?.Invoke(data, tcpClient);
                }
            } catch (Exception e){ RCMManager.Log("CLIENT listen TCP ERROR: " + e.Message); }
            Terminate();
        }
    }
    
}
