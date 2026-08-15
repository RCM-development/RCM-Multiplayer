using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using RCM_Coop.Network;
using RCM_Coop.Network.Helpers;
using TestMod;
using static System.Collections.Specialized.BitVector32;
using static LandscapeGenerator;
using static RCM_Coop.Network.GameProtocols;
using static RCM_Coop.Network.GameProtocols.ServerJoinResponseFailed;

namespace RCM_Coop
{
    internal class GameServer
    {
        Session session;
        string session_password = "";
        PlayerManager players;
        byte last_player_id = 1;
        byte NewPlayerID() => last_player_id++;
        GameServer(Session session){
            this.session = session;
            players = new PlayerManager();
            players.AddOurselves(0);

            session.data_recieved_callback = OnDataReceived;
            session.connection_terminated_callback = OnConnectionTerminated;
            session.connection_opened_callback = OnConnectionOpened;
        }




        HashSet<TcpClient> unconnected_clients = new();

        struct client_id_struct { public TcpClient client; public byte id; }
        List<client_id_struct> clients = new();
        bool IsAuthenticated(TcpClient client){
            foreach (var item in clients)
                if (item.client == client)
                    return true;
            return false;
        }
        byte GetCLientId(TcpClient client){
            foreach (var item in clients)
                if (item.client == client)
                    return item.id;
            return 255;
        }

        void OnDataReceived(byte[] data, TcpClient client){
            RCMManager.Log($"[Co-op] Received {data.Length} bytes from {client.Client.RemoteEndPoint}");
            
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
                                
                                // add to linker & players
                                clients.Add(new() { client = client, id = allocated_id });
                                players.AddPlayer(e.username, allocated_id);
                            }
                            unconnected_clients.Remove(client);
                        }
                        break;
                    case ServerPlayerHasJoined e:
                        break;
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


        enum entity_tags{
            Player,
            Ai,
            Neutral,
            WorldMesh,
            World,
            Button
        }
        string TagFromEnum(entity_tags tag){
            switch (tag){
                case entity_tags.Player: return Tags.Player;
                case entity_tags.Ai: return Tags.Ai;
                case entity_tags.Neutral: return Tags.Neutral;
                case entity_tags.WorldMesh: return Tags.WorldMesh;
                case entity_tags.World: return Tags.World;
                case entity_tags.Button: return Tags.Button;
                default: return "";
            }
        }
        entity_tags EnumFromTag(string tag){
            switch (tag){
                case Tags.Player: return entity_tags.Player;
                case Tags.Ai: return entity_tags.Ai;
                case Tags.Neutral: return entity_tags.Neutral;
                case Tags.WorldMesh: return entity_tags.WorldMesh;
                case Tags.World: return entity_tags.World;
                case Tags.Button: return entity_tags.Button;
                default: return entity_tags.Neutral;
            }
        }


        struct entity_state{
            public ushort network_id;
            public uint entity_id;
            public float pos_x;
            public float pos_y;
            public float pos_z;
            public float rot_yaw;
            public float scale;
            public entity_tags tag;

        }


        struct jip_gamestate
        {
            List<entity_state> entities;

        }

    }
}
