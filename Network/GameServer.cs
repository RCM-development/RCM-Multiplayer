using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PimDeWitte.UnityMainThreadDispatcher;
using RCM_Coop.Network;
using RCM_Coop.Network.Entities;
using RCM_Coop.Network.Helpers;
using RCM_GUI;
using UnityEngine;
using static LandscapeGenerator;
using static Profiler;
using static RCM_Coop.CoopManager;
using static RCM_Coop.Network.Entities.EntitiesManager;
using static RCM_Coop.Network.GameProtocols;
using static RCM_Coop.Network.GameProtocols.ServerJoinResponseFailed;
using static RCM_Coop.Network.PlayerManager;
namespace RCM_Coop{

    internal class GameServer : NetworkedGame{
        string session_password = "";
        byte last_player_id = 1;
        byte NewPlayerID() => last_player_id++;
        public GameServer(Session session){
            this.session = session;
            PlayerManager.AddOurselves(0, new Color32(0, 255, 0, 255));
            session.data_recieved_callback = RouteOnDataRecieved;
            session.connection_terminated_callback = RouteOnConnectionTerminated;
            session.connection_opened_callback = RouteOnConnectionOpened;
        }
        public void Release() {
            if (session == null) return;
            session.Terminate();
            session = null;
        }




        HashSet<TcpClient> unconnected_clients = new();

        enum client_state{
            not_in_session,
            in_menu,
            in_game_awaiting_data,
            in_game
        }
        class client_id_struct { public TcpClient client; public byte id; public client_state state; }
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
        void UpdateClientStatus(TcpClient client, client_state new_state){
            foreach (var item in clients)
                if (item.client == client){
                    item.state = new_state;
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
                                else if (!PlayerManager.IsUsernameTaken(e.username) || string.IsNullOrWhiteSpace(e.username)){
                                    RCMManager.Log($"[Co-op] client attempted to connect but username already taken: '{e.username}' client: {client.Client.RemoteEndPoint}");
                                    session.SendTCP(new ServerJoinResponseFailed(JoinError.username_taken), client);
                                    CloseAfterDelay(client, 1000);
                                }
                                // otherwise successfully joined, send join response
                                else{
                                    RCMManager.Log($"[Co-op] client joined: '{e.username}' client: {client.Client.RemoteEndPoint}");

                                    byte allocated_id = NewPlayerID();
                                    session.SendTCP(new ServerJoinResponseOk(allocated_id), client);
                                    foreach (var player in PlayerManager.GetPlayersList())
                                        session.SendTCP(new ServerPlayerHasJoined(player.id, player.username, player.color), client);
                                    // send to everyone
                                    session.SendTCP(new ServerPlayerHasJoined(allocated_id, e.username, e.color));
                                    // add to linker & players
                                    clients.Add(new() { client = client, id = allocated_id, state = client_state.not_in_session });
                                    PlayerManager.AddPlayer(e.username, allocated_id, e.color);

                                    // prompt them to start joining if our game is in progress or initial rewards phase
                                    if (SceneManagerWrapper.IsIntermissionScreen()
                                    || SceneManagerWrapper.IsGame()
                                    || SceneManagerWrapper.IsReward()
                                    || SceneManagerWrapper.IsShop()
                                    || SceneManagerWrapper.IsRunSetup()){
                                        session.SendTCP(new ServerBeginNewRun(), client);
                                    }
                                }
                                unconnected_clients.Remove(client);
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
                        case ClientExecuteCommnand e:
                            if (IsAuthenticated(client)){
                                RCMManager.Log($"[Co-op] client sent command");
                                if (e.entity != null) e.entity.ExecuteCommand(e.command, e.processingType);
                            }
                            break;
                        case ClientUnitProduce e:
                            if (IsAuthenticated(client)){
                                RCMManager.Log($"[Co-op] client sent entity production command");
                                if (e.entity != null) e.entity.Produce();
                            }
                            break;
                        case ClientUnitAbortProduction e:
                            if (IsAuthenticated(client)){
                                RCMManager.Log($"[Co-op] client sent entity abort production command");
                                if (e.entity != null) e.entity.AbortProduction();
                            }
                            break;
                        case ClientPlacementRequest e:
                            if (IsAuthenticated(client)){
                                RCMManager.Log($"[Co-op] client sent engi build request");
                                if (e.engi != null) CoopManager.RecievedPlacementRequest(e);
                            }
                            break;
                        case ClientRequestDrop e:
                            if (IsAuthenticated(client)){
                                RCMManager.Log($"[Co-op] client sent drop request");
                                Patch_Drop_Start.ServerRecieve(e);
                            }
                            break;

                        case ClientMapLoaded e:
                            if (IsAuthenticated(client)){
                                if (entities_spawned)
                                {
                                    RCMManager.Log($"[Co-op] client said green to go, sending all entity data");
                                    session.SendTCP(new ServerFullEntityData(EntitySerializer.CompileEntities()), client);
                                    // update player status to now be in game
                                    UpdateClientStatus(client, client_state.in_game);
                                }
                                else
                                {
                                    RCMManager.Log($"[Co-op] client said green to go, however we haven't loaded yet, so just wait a sec on that data");
                                    // update player status to be ready to recieve data
                                    UpdateClientStatus(client, client_state.in_game_awaiting_data);
                                }
                            }
                            break;
                        case ClientStartersSelected e:
                            if (IsAuthenticated(client)){
                                RCMManager.Log($"[Co-op] client said starters selected, sending game save file");
                                if (SceneManagerWrapper.IsGame() 
                                || SceneManagerWrapper.IsIntermissionScreen()
                                || SceneManagerWrapper.IsReward()
                                || SceneManagerWrapper.IsShop())
                                {
                                    // NOTE: cannot send this as we need to send our save file PRIOR to game start, as it technically progresses
                                    if (SceneManagerWrapper.IsGame())
                                    {
                                        //SendServerMenuPacket(new ServerBeginStageLoad(last_written_savegame_json, MetaGame._instance.ToJson()));
                                        session.SendTCP(new ServerBeginStageLoad(last_written_savegame_json, MetaGame._instance.ToJson()), client);
                                    }
                                    else
                                    {
                                        session.SendTCP(new ServerStageUpdated(last_written_savegame_json, MetaGame._instance.ToJson()), client);
                                    }
                                }
                                // update player status to now be in game
                                UpdateClientStatus(client, client_state.in_menu);
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
                string username = PlayerManager.GetPlayer(player_id)?.username;
                PlayerManager.RemovePlayer(player_id);
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
                if (item.state == client_state.in_game)
                    session.SendTCP(packet, item.client);
        }

        public void ResetClientLoadStatesForNextStage(){
            entities_spawned = false;
            SendServerMenuPacket(new ServerStageUpdated(Game.ToJson(), MetaGame._instance.ToJson()));
            foreach (var item in clients){
                if (item.state != client_state.not_in_session){ // still picking they starter unit?
                    item.state = client_state.in_menu;
                    return;
        }}}
        public void ResetClientLoadStatesForMainMenu(){
            entities_spawned = false;
            SendServerMenuPacket(new ServerReturnToMenu());
            foreach (var item in clients){
                if (item.state != client_state.not_in_session){ // still picking they starter unit?
                    item.state = client_state.not_in_session;
                    return;
        }}}
        bool entities_spawned = false;
        public void BeginReplicatingGameEntities(){
            entities_spawned = true;
            foreach (var item in clients){
                if (item.state == client_state.in_game_awaiting_data){
                    item.state = client_state.in_game;
                    session.SendTCP(new ServerFullEntityData(EntitySerializer.CompileEntities()), item.client);
                    return;
        }}
        }
    }
}
