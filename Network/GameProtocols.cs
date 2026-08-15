using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCM_Coop.Network.Helpers;
namespace RCM_Coop.Network{
    public class GameProtocols{
        enum proto : byte{
            NONE = 0,
            ClientJoinRequest, // {str:username, str:password}
            ServerJoinResponseOk, // {byte:player_id}
            ServerJoinResponseFailed, // {byte:error}
            ServerPlayerHasJoined, // {byte:player_id, string:username}





            server_game_status_loading, // includes map stage info for client to join & generate off of
            server_game_status_waiting, // includes initial game state data in packet
            server_game_status_started,

            client_game_status_ready,


        }
        public static IEnumerable<SerializablePacket> DeserializePackets(byte[] data){
            PacketReader packet = new PacketReader(data);
            while (true){
                proto current_proto = (proto)packet.DeserializeByte();
                switch (current_proto){
                    case proto.ClientJoinRequest:           yield return new ClientJoinRequest(packet); break;
                    case proto.ServerJoinResponseOk:        yield return new ServerJoinResponseOk(packet); break;
                    case proto.ServerJoinResponseFailed:    yield return new ServerJoinResponseFailed(packet); break;
                    case proto.ServerPlayerHasJoined:       yield return new ServerPlayerHasJoined(packet); break;
                    default: yield break;
            }}
        }
        public abstract class SerializablePacket{ public virtual byte[] Serialize() => [];}
        public class ClientJoinRequest : SerializablePacket{
            public string username;
            public string password;
            public ClientJoinRequest(string username, string password){
                this.username = username;
                this.password = password;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ClientJoinRequest);
                packet.SerializeString(username);
                packet.SerializeString(password);
                return packet.GetData();
            }
            public ClientJoinRequest(PacketReader packet){
                username = packet.DeserializeString();
                password = packet.DeserializeString();
            }
        }
        public class ServerJoinResponseOk : SerializablePacket{
            public byte player_id;
            public ServerJoinResponseOk(byte player_id){
                this.player_id = player_id;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerJoinResponseOk);
                packet.SerializeByte(player_id);
                return packet.GetData();
            }
            public ServerJoinResponseOk(PacketReader packet){
                player_id = packet.DeserializeByte();
            }
        }
        public class ServerJoinResponseFailed : SerializablePacket{
            public enum JoinError : byte{ username_taken, bad_password, already_connected, session_full, rejected }
            public JoinError join_error;
            public ServerJoinResponseFailed(JoinError join_error){
                this.join_error = join_error;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerJoinResponseFailed);
                packet.SerializeByte((byte)join_error);
                return packet.GetData();
            }
            public ServerJoinResponseFailed(PacketReader packet){
                join_error = (JoinError)packet.DeserializeByte();
            }
        }
        public class ServerPlayerHasJoined : SerializablePacket{
            public byte player_id;
            public string username;
            public ServerPlayerHasJoined(byte player_id, string username){
                this.player_id = player_id;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerPlayerHasJoined);
                packet.SerializeByte(player_id);
                packet.SerializeString(username);
                return packet.GetData();
            }
            public ServerPlayerHasJoined(PacketReader packet){
                player_id = packet.DeserializeByte();
                username = packet.DeserializeString();
            }
        }
    }
}
