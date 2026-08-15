using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using RCM_Coop.Network.Helpers;
using TestMod;
using static Profiler;
using static RCM_Coop.Network.GameProtocols;
namespace RCM_Coop.Network{

    internal class GameClient{
        Session session;
        PlayerManager players;
        GameClient(Session session){
            this.session = session;
            players = new PlayerManager();
            session.data_recieved_callback = OnDataReceived;
            session.connection_terminated_callback = OnConnectionTerminated;
            session.connection_opened_callback = OnConnectionOpened;
        }


        void OnDataReceived(byte[] data, TcpClient client){
            RCMManager.Log($"[Co-op] Received {data.Length} bytes from {client.Client.RemoteEndPoint}");

            foreach (var packet in DeserializePackets(data))
                switch (packet){
                    case ServerJoinResponseOk e:
                        RCMManager.Log($"[Co-op] accepted into session");
                        players.AddOurselves(e.player_id);
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


    }
}
