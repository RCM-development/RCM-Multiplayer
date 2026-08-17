using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCM_Coop.Network.Helpers;
using TestMod;
using UnityEngine;
using UnityEngine.UIElements;
using static RCM_Coop.EntitiesManager;
using static RCM_Coop.EntitiesManager.entity_state;
namespace RCM_Coop.Network{
    public class GameProtocols{
        enum proto : byte{
            NONE = 0,
            ClientJoinRequest, // {str:username, str:password}
            ServerJoinResponseOk, // {byte:player_id}
            ServerJoinResponseFailed, // {byte:error}
            ServerPlayerHasJoined, // {byte:player_id, string:username}
            ServerPlayerHasLeft, // {byte:player_id}

            // todo: joining protocols
            ServerFullEntityData, // {}
            ServerUnitSpawned, // {}
            ServerUnitDestroyed, // {}

            ClientMapLoaded, // {nil}

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
                    case proto.ServerPlayerHasLeft:         yield return new ServerPlayerHasLeft(packet); break;
                    case proto.ClientMapLoaded:             yield return new ClientMapLoaded(packet); break;
                    case proto.ServerFullEntityData:        yield return new ServerFullEntityData(packet); break;
                    case proto.ServerUnitSpawned:           yield return new ServerUnitSpawned(packet); break;
                    case proto.ServerUnitDestroyed:         yield return new ServerUnitDestroyed(packet); break;
                    default: RCMManager.Log($"[Co-op] deserialized bad protocol value: {(byte)current_proto}"); yield break;
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
                this.username = username;
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
        public class ServerPlayerHasLeft : SerializablePacket{
            public byte player_id;
            public ServerPlayerHasLeft(byte player_id){
                this.player_id = player_id;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerPlayerHasLeft);
                packet.SerializeByte(player_id);
                return packet.GetData();
            }
            public ServerPlayerHasLeft(PacketReader packet){
                player_id = packet.DeserializeByte();
            }
        }
        public class ClientMapLoaded : SerializablePacket{
            public ClientMapLoaded(){ }
            public override byte[] Serialize(){
                // overkill as hell
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ClientMapLoaded);
                return packet.GetData();
            }
            public ClientMapLoaded(PacketReader packet){ }
        }
        
        
        static void SerializeEntityState(PacketWriter packet, entity_state entity){
            packet.SerializeString(entity.entity_id);
            packet.SerializeShort((short)entity.network_id);
            packet.SerializeShort((short)entity.parent_controller_id);
            packet.SerializeShort((short)entity.parent_transform_id);
            packet.SerializeShort((short)entity.parent_transform_index);
            // we're going to serialize all floats as floats as fixed numbers 
            // -3276.8 to 3276.7 (0.1 intervals)
            int x = (int)(entity.pos_x * 10);
            int y = (int)(entity.pos_y * 10);
            int z = (int)(entity.pos_z * 10);
            if      (x > short.MaxValue) x = short.MaxValue;
            else if (x < short.MinValue) x = short.MinValue;
            if      (y > short.MaxValue) y = short.MaxValue;
            else if (y < short.MinValue) y = short.MinValue;
            if      (z > short.MaxValue) z = short.MaxValue;
            else if (z < short.MinValue) z = short.MinValue;
            packet.SerializeShort((short)x);
            packet.SerializeShort((short)y);
            packet.SerializeShort((short)z);
            // clamp rotation into 0-360 then fill up into ushort
            float yaw = entity.rot_yaw;
            yaw = (yaw % 360f + 360f) % 360f;
            yaw *= 182.04166666666f; // ushort.max / 360
            int yaw_fixed = (int)yaw;
            if (yaw_fixed > ushort.MaxValue) 
                yaw_fixed = ushort.MaxValue;
            else if (yaw_fixed < 0) 
                        yaw_fixed = 0;
            packet.SerializeShort((short)yaw_fixed);
            // clamp scale into ushort as well i guess? 0 - 655.35 (0.01 intervals)
            int scale = (int)(entity.scale * 100);
            if (scale > ushort.MaxValue) x = ushort.MaxValue;
            else if (scale < 0) x = 0;
            packet.SerializeShort((short)scale);
            // then tag
            packet.SerializeByte((byte)entity.tag);
        }
        static entity_state DeserializeEntityState(PacketReader packet, entity_state entity){
            entity.entity_id = packet.DeserializeString();
            entity.network_id = (ushort)packet.DeserializeShort();
            entity.parent_controller_id = (ushort)packet.DeserializeShort();
            entity.parent_transform_id = (ushort)packet.DeserializeShort();
            entity.parent_transform_index = (ushort)packet.DeserializeShort();
            entity.pos_x = packet.DeserializeShort() / 10.0f;
            entity.pos_y = packet.DeserializeShort() / 10.0f;
            entity.pos_z = packet.DeserializeShort() / 10.0f;
            entity.rot_yaw = packet.DeserializeShort() / 182.04166666666f;
            entity.scale = packet.DeserializeShort() / 100.0f;
            entity.tag = (entity_tags)packet.DeserializeByte();
            return entity;
        }
        public class ServerFullEntityData : SerializablePacket{
            public List<entity_state> entities;
            public ServerFullEntityData(List<entity_state> entities){
                this.entities = entities;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                if (entities.Count > short.MaxValue) throw new Exception("wayyy too many entities to sync");
                packet.SerializeByte((byte)proto.ServerFullEntityData);
                packet.SerializeShort((short)entities.Count);
                foreach (var entity in entities)
                    SerializeEntityState(packet, entity);
                return packet.GetData();
            }
            public ServerFullEntityData(PacketReader packet){
                entities = new();
                int entity_count = packet.DeserializeShort();
                for (int i = 0; i < entity_count; i++)
                    entities.Add(DeserializeEntityState(packet, new entity_state()));
            }
        }
        public class ServerUnitSpawned : SerializablePacket{
            public spawned_entity_state entity;
            public ServerUnitSpawned(spawned_entity_state entity){
                this.entity = entity;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitSpawned);
                SerializeEntityState(packet, entity);
                packet.SerializeByte(entity.spawned_from_above ? (byte)1 : (byte)0);
                return packet.GetData();
            }
            public ServerUnitSpawned(PacketReader packet){
                entity = new spawned_entity_state();
                DeserializeEntityState(packet, entity);
                entity.spawned_from_above = packet.DeserializeByte() > 0;
            }
        }
        public class ServerUnitDestroyed : SerializablePacket{
            public ushort parent_id;
            public ushort originator_id;
            public bool dont_use_destruction_effects;
            public ServerUnitDestroyed(ushort parent_id, ushort originator_id, bool dont_use_destruction_effects){
                this.parent_id = parent_id;
                this.originator_id = originator_id;
                this.dont_use_destruction_effects = dont_use_destruction_effects;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitDestroyed);
                packet.SerializeShort((short)parent_id);
                packet.SerializeShort((short)originator_id);
                packet.SerializeByte(dont_use_destruction_effects ? (byte)1 : (byte)0);
                return packet.GetData();
            }
            public ServerUnitDestroyed(PacketReader packet){
                parent_id = (ushort)packet.DeserializeShort();
                originator_id = (ushort)packet.DeserializeShort();
                dont_use_destruction_effects = packet.DeserializeByte() > 0;
            }
        }
    }
}
