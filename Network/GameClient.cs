using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PimDeWitte.UnityMainThreadDispatcher;
using RCM_Coop.Network.Helpers;
using TestMod;
using static RCM_Coop.Network.GameProtocols;
namespace RCM_Coop.Network{

    internal class GameClient{
        Session session;
        PlayerManager players;
        public GameClient(Session session){
            this.session = session;
            players = new PlayerManager();
            session.data_recieved_callback = RouteOnDataRecieved;
            session.connection_terminated_callback = RouteOnConnectionTerminated;
            session.connection_opened_callback = RouteOnConnectionOpened;
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
            RCMManager.Log($"[Co-op] Received {data.Length} bytes from {client.Client.RemoteEndPoint}");
            try{
                foreach (var packet in DeserializePackets(data))
                    switch (packet){
                        case ServerJoinResponseOk e:
                            RCMManager.Log($"[Co-op] accepted into session, waiting for host...");
                            players.AddOurselves(e.player_id);
                            // enter awaiting host screen
                            break;
                        case ServerJoinResponseFailed e:
                            switch (e.join_error){
                                case ServerJoinResponseFailed.JoinError.username_taken: RCMManager.Log($"[Co-op] join failed, reason: username is taken"); break;
                                case ServerJoinResponseFailed.JoinError.session_full: RCMManager.Log($"[Co-op] join failed, reason: session is full"); break;
                                case ServerJoinResponseFailed.JoinError.already_connected: RCMManager.Log($"[Co-op] join failed, reason: already connected or poor TCP connection"); break;
                                case ServerJoinResponseFailed.JoinError.bad_password:
                                    RCMManager.Log($"[Co-op] join failed, reason: bad password");
                                    break;
                                case ServerJoinResponseFailed.JoinError.rejected:
                                    RCMManager.Log($"[Co-op] join failed, reason: unspecified");
                                    break;
                            }
                            session.Terminate();
                            break;
                        case ServerPlayerHasJoined e:
                            RCMManager.Log($"[Co-op] player joined: {e.username}");
                            players.AddPlayer(e.username, e.player_id);
                            break;
                        case ServerPlayerHasLeft e:
                            RCMManager.Log($"[Co-op] player left: {players.GetPlayer(e.player_id)?.username}");
                            players.RemovePlayer(e.player_id);
                            break;
                        case ServerFullEntityData e:
                            RCMManager.Log($"[Co-op] recieved entities list");
                            EntitiesManager.DecompileEntities(e.entities);
                            break;
                        case ServerUnitSpawned e:
                            RCMManager.Log($"[Co-op] recieved entity spawn event");
                            EntitiesManager.DecompileEntity(e.entity, true);
                            break;
                        case ServerUnitDestroyed e:
                            RCMManager.Log($"[Co-op] recieved entity destroyed event");
                            EntitiesManager.RecievedDestroy(e.parent_id, e.originator_id, e.dont_use_destruction_effects);
                            break;
                        case ServerEntitiesPositionUpdate e:
                            RCMManager.Log($"[Co-op] recieved entity pos update event");
                            EntitiesManager.RecievePositionUpdates(e.positions);
                            break;
                    }
            } catch (Exception ex){
                RCMManager.Log($"[Co-op] failed to read recieved packets: {ex.Message} callstack: {ex.StackTrace}");
            }
        }
        void OnConnectionTerminated(TcpClient client){
            RCMManager.Log($"[Co-op] Connection terminated with {client.Client.RemoteEndPoint}");

            // TODO: exit client multiplayer mode??
            // - return to main menu or stay in session if game is running
        }
        void OnConnectionOpened(TcpClient client){
            RCMManager.Log($"[Co-op] Connection opened with {client.Client.RemoteEndPoint}, sending join packet");

            session.SendTCP(new ClientJoinRequest("username123", "password123"));
        }

        public void SendMapLoadedRequest(){
            RCMManager.Log("sending map loaded request");
            session.SendTCP(new ClientMapLoaded());
        }

    }
}
