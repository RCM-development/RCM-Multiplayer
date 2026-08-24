using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PimDeWitte.UnityMainThreadDispatcher;
using RCM_Coop.Network;
using RCM_Coop.Network.Helpers;
using TestMod;
using static LandscapeGenerator;
using static Profiler;
using static RCM_Coop.EntitiesManager;
using static RCM_Coop.Network.GameProtocols;
using static RCM_Coop.Network.GameProtocols.ServerJoinResponseFailed;
using static RCM_Coop.Network.PlayerManager;

namespace RCM_Coop
{
    internal class GameServer
    {
        Session session;
        string session_password = "";
        PlayerManager players;
        byte last_player_id = 1;
        byte NewPlayerID() => last_player_id++;
        public GameServer(Session session){
            this.session = session;
            players = new PlayerManager();
            players.AddOurselves(0);
            session.data_recieved_callback = RouteOnDataRecieved;
            session.connection_terminated_callback = RouteOnConnectionTerminated;
            session.connection_opened_callback = RouteOnConnectionOpened;
        }




        HashSet<TcpClient> unconnected_clients = new();

        class client_id_struct { public TcpClient client; public byte id; public bool is_ingame; }
        List<client_id_struct> clients = new();
        bool IsAuthenticated(TcpClient client){
            foreach (var item in clients)
                if (item.client == client)
                    return true;

            RCMManager.Log($"[Co-op] client failed authentication check. {client.Client.RemoteEndPoint}");
            return false;
        }
        byte GetCLientId(TcpClient client){
            foreach (var item in clients)
                if (item.client == client)
                    return item.id;
            return 255;
        }
        void UpdateClientStatus(TcpClient client){
            foreach (var item in clients)
                if (item.client == client){
                    item.is_ingame = true;
                    return;
            }
            RCMManager.Log($"[Co-op] couldn't find client to update status of... {client.Client.RemoteEndPoint}");
        }

        void RouteOnDataRecieved(byte[] data, TcpClient client){
            UnityMainThreadDispatcher.Enqueue(() => { OnDataReceived(data, client); });
        }
        void RouteOnConnectionTerminated(TcpClient client){
            UnityMainThreadDispatcher.Enqueue(() => { OnConnectionTerminated(client); });
        }
        void RouteOnConnectionOpened(TcpClient client){
            UnityMainThreadDispatcher.Enqueue(() => { OnConnectionOpened(client); });
        }
        void OnDataReceived(byte[] data, TcpClient client){
            //RCMManager.Log($"[Co-op] Received {data.Length} bytes from {client.Client.RemoteEndPoint}");
            try{
                foreach (var packet in DeserializePackets(data))
                    switch (packet){
                        case ClientJoinRequest e:
                            if (!unconnected_clients.Contains(client)){
                                RCMManager.Log($"[Co-op] client attempted to connect but was not in our unconnected clients list: {client.Client.RemoteEndPoint}");
                                session.SendTCP(new ServerJoinResponseFailed(JoinError.already_connected), client);
                                CloseAfterDelay(client, 1000);
                            } else{ 
                                // check password
                                if (!string.IsNullOrWhiteSpace(session_password) && session_password != e.password){
                                    RCMManager.Log($"[Co-op] client attempted to connect but bad password: '{e.password}' client: {client.Client.RemoteEndPoint}");
                                    session.SendTCP(new ServerJoinResponseFailed(JoinError.bad_password), client);
                                    CloseAfterDelay(client, 1000);
                                }
                                // check username
                                else if (!players.IsUsernameTaken(e.username) || string.IsNullOrWhiteSpace(e.username)){
                                    RCMManager.Log($"[Co-op] client attempted to connect but username already taken: '{e.username}' client: {client.Client.RemoteEndPoint}");
                                    session.SendTCP(new ServerJoinResponseFailed(JoinError.username_taken), client);
                                    CloseAfterDelay(client, 1000);
                                }
                                // otherwise successfully joined, send join response
                                else{
                                    RCMManager.Log($"[Co-op] client joined: '{e.username}' client: {client.Client.RemoteEndPoint}");

                                    byte allocated_id = NewPlayerID();
                                    session.SendTCP(new ServerJoinResponseOk(allocated_id), client);
                                    foreach (var player in players.GetPlayersList())
                                        session.SendTCP(new ServerPlayerHasJoined(player.id, player.username), client);
                                    // send to everyone
                                    session.SendTCP(new ServerPlayerHasJoined(allocated_id, e.username));
                                    // add to linker & players
                                    clients.Add(new() { client = client, id = allocated_id, is_ingame = false });
                                    players.AddPlayer(e.username, allocated_id);
                                }
                                unconnected_clients.Remove(client);
                            }
                            break;
                        case ClientMapLoaded e:
                            if (IsAuthenticated(client)){
                                RCMManager.Log($"[Co-op] client said green to go, sending all entity data");
                                session.SendTCP(new ServerFullEntityData(EntitiesManager.CompileEntities()), client);
                                // update player status to now be in game
                                UpdateClientStatus(client);
                            }
                            break;
                        case ClientTimeSlow e:
                            if (IsAuthenticated(client)){
                                RCMManager.Log($"[Co-op] client said slow time");
                                if (!Navigator.IsSlowedDown)
                                    Navigator.SlowDown();
                            }
                            break;
                        case ClientTimeNormal e:
                            if (IsAuthenticated(client)){
                                RCMManager.Log($"[Co-op] client said normal time");
                                if (Navigator.IsSlowedDown)
                                    Navigator.ResetToDefaultSpeed();
                            }
                            break;
                        case ClientTimePaused e:
                            if (IsAuthenticated(client)){
                                RCMManager.Log($"[Co-op] client said pause time");
                                if (!Navigator._isPaused)
                                    Navigator.Pause();
                            }
                            break;
                        case ClientTimeUnpaused e:
                            if (IsAuthenticated(client)){
                                RCMManager.Log($"[Co-op] client said unpause time");
                                if (Navigator._isPaused)
                                    Navigator.Unpause();
                            }
                            break;
                        default:
                            RCMManager.Log($"[Co-op] recieved packet of unsupported type: {packet.GetType().Name}");
                            break;
            }} catch (Exception ex){
                RCMManager.Log($"[Co-op] failed to read recieved packets: {ex.Message} callstack: {ex.StackTrace}");
            }
        }
        void OnConnectionTerminated(TcpClient client){
            RCMManager.Log($"[Co-op] Connection terminated with {client.Client.RemoteEndPoint}");

            byte player_id = GetCLientId(client);
            if (player_id != 255){
                string username = players.GetPlayer(player_id)?.username;
                players.RemovePlayer(player_id);
                session.SendTCP(new ServerPlayerHasLeft(player_id));
                RCMManager.Log($"[Co-op] Player disconnected from session: '{username}'");
            }
        }
        void OnConnectionOpened(TcpClient client){
            RCMManager.Log($"[Co-op] Connection opened with {client.Client.RemoteEndPoint}");
            unconnected_clients.Add(client);
            CloseUnconnectedAfterDelay(client, 5000);
        }


        async void CloseAfterDelay(TcpClient client, int miliseconds){
            await Task.Delay(miliseconds);
            client.Close();
            RCMManager.Log($"[Co-op] closed client after delayed termination, client: {client.Client.RemoteEndPoint}");
        }
        async void CloseUnconnectedAfterDelay(TcpClient client, int miliseconds){
            await Task.Delay(miliseconds);
            if (!unconnected_clients.Contains(client)) return;
            client.Close();
            unconnected_clients.Remove(client);
            RCMManager.Log($"[Co-op] closed client after not having connected in time, client: {client.Client.RemoteEndPoint}");
        }



        public void SendPacketToAuthenticated(SerializablePacket packet){
            foreach (var item in clients)
                session.SendTCP(packet, item.client);
        }
        public void SendPacketToInGame(SerializablePacket packet){
            foreach (var item in clients)
                if (item.is_ingame)
                    session.SendTCP(packet, item.client);
        }

    }
}
