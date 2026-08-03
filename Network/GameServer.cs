using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TestMod;
using static System.Collections.Specialized.BitVector32;

namespace RCM_Coop
{
    internal class GameServer
    {
        Session session;
        GameServer(Session session){
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
            // Send a data packet to the client
            
            // figure out what state our session is in, so we can replicate accordingly

            // for now we're going to assume that we're in a loaded & waiting state


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
                case entity_tags.Player: return "Player";
                case entity_tags.Ai: return "AI";
                case entity_tags.Neutral: return "Neutral";
                case entity_tags.WorldMesh: return "WorldMesh";
                case entity_tags.World: return "World";
                case entity_tags.Button: return "Button";
                default: return "";
            }
        }
        entity_tags EnumFromTag(string tag){
            switch (tag){
                case "Player": return entity_tags.Player;
                case "AI": return entity_tags.Ai;
                case "Neutral": return entity_tags.Neutral;
                case "WorldMesh": return entity_tags.WorldMesh;
                case "World": return entity_tags.World;
                case "Button": return entity_tags.Button;
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
