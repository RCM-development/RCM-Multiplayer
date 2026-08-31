using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCM_Coop.Network.Entities;
using RCM_Coop.Network.Helpers;
using TestMod;
using UnityEngine;
using UnityEngine.UIElements;
using static EntityCommand;
using static RCM_Coop.Network.Entities.EntitiesManager;
using static RCM_Coop.Network.Entities.EntitySerializer;
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
            // WIP entity protocols
            ServerUnitActivateSkillPosition,
            ServerUnitActivateSkillTarget,
            ServerUnitActivateSkill,
            ServerUnitAttackMovePosition,
            ServerUnitAttackMoveTarget,
            ServerUnitAttack,
            ServerUnitOnReadyToShoot,
            ServerUnitFollowTarget,
            ServerUnitFollowPosition,
            ServerUnitStop,
            ServerUnitTeleport,
            ServerUnitMoveTo,
            ServerUnitRepairArmor,
            ServerUnitHeal,
            ServerUnitChargeShield,
            ServerUnitTakeDamage,
            ServerUnitProduce,
            ServerUnitAbortProduction,
            ServerUnitChargeMana,
            ServerUnitSetStatusEffect,
            ServerUnitRemoveActiveStatus,
            ServerUnitRemoveStatusEffect,
            ServerUnitRankUp,
            // end WIP entity protocols
            // then we have other gameplay protocols
            //ServerBuildingSpawning,
            ServerTimeSlow,
            ClientTimeSlow,
            ServerTimeNormal,
            ClientTimeNormal,
            ServerTimePaused,
            ClientTimePaused,
            ServerTimeUnpaused,
            ClientTimeUnpaused,

            ClientExecuteCommnand,
            ClientUnitProduce,
            ClientUnitAbortProduction,
            //
            ServerPlacementBegin,
            ServerPlacementShockwave,
            ServerPlacementReleased,
            ClientPlacementRequest,
            //
            ClientMapLoaded, // {nil}
            ServerEntitiesPositionUpdate, // {}
        }
        public static IEnumerable<SerializablePacket> DeserializePackets(byte[] data){
            PacketReader packet = new PacketReader(data);
            while (true){
                proto current_proto = (proto)packet.DeserializeByte();
                switch (current_proto){
                    case proto.ClientJoinRequest:               yield return new ClientJoinRequest(packet); break;
                    case proto.ServerJoinResponseOk:            yield return new ServerJoinResponseOk(packet); break;
                    case proto.ServerJoinResponseFailed:        yield return new ServerJoinResponseFailed(packet); break;
                    case proto.ServerPlayerHasJoined:           yield return new ServerPlayerHasJoined(packet); break;
                    case proto.ServerPlayerHasLeft:             yield return new ServerPlayerHasLeft(packet); break;
                    case proto.ClientMapLoaded:                 yield return new ClientMapLoaded(packet); break;
                    case proto.ServerFullEntityData:            yield return new ServerFullEntityData(packet); break;
                    case proto.ServerUnitSpawned:               yield return new ServerUnitSpawned(packet); break;
                    case proto.ServerUnitDestroyed:             yield return new ServerUnitDestroyed(packet); break;
                    case proto.ServerEntitiesPositionUpdate:    yield return new ServerEntitiesPositionUpdate(packet); break;
                    case proto.ServerUnitActivateSkillPosition: yield return new ServerUnitActivateSkillPosition(packet); break;
                    case proto.ServerUnitActivateSkillTarget:   yield return new ServerUnitActivateSkillTarget(packet); break;
                    case proto.ServerUnitActivateSkill:         yield return new ServerUnitActivateSkill(packet); break;
                    case proto.ServerUnitAttackMovePosition:    yield return new ServerUnitAttackMovePosition(packet); break;
                    case proto.ServerUnitAttackMoveTarget:      yield return new ServerUnitAttackMoveTarget(packet); break;
                    case proto.ServerUnitAttack:                yield return new ServerUnitAttack(packet); break;
                    case proto.ServerUnitOnReadyToShoot:        yield return new ServerUnitOnReadyToShoot(packet); break;
                    case proto.ServerUnitFollowTarget:          yield return new ServerUnitFollowTarget(packet); break;
                    case proto.ServerUnitFollowPosition:        yield return new ServerUnitFollowPosition(packet); break;
                    case proto.ServerUnitStop:                  yield return new ServerUnitStop(packet); break;
                    case proto.ServerUnitTeleport:              yield return new ServerUnitTeleport(packet); break;
                    case proto.ServerUnitMoveTo:                yield return new ServerUnitMoveTo(packet); break;
                    case proto.ServerUnitRepairArmor:           yield return new ServerUnitRepairArmor(packet); break;
                    case proto.ServerUnitHeal:                  yield return new ServerUnitHeal(packet); break;
                    case proto.ServerUnitChargeShield:          yield return new ServerUnitChargeShield(packet); break;
                    case proto.ServerUnitTakeDamage:            yield return new ServerUnitTakeDamage(packet); break;
                    case proto.ServerUnitProduce:               yield return new ServerUnitProduce(packet); break;
                    case proto.ServerUnitAbortProduction:       yield return new ServerUnitAbortProduction(packet); break;
                    case proto.ServerUnitChargeMana:            yield return new ServerUnitChargeMana(packet); break;
                    case proto.ServerUnitSetStatusEffect:       yield return new ServerUnitSetStatusEffect(packet); break;
                    case proto.ServerUnitRemoveActiveStatus:    yield return new ServerUnitRemoveActiveStatus(packet); break;
                    case proto.ServerUnitRemoveStatusEffect:    yield return new ServerUnitRemoveStatusEffect(packet); break;
                    case proto.ServerUnitRankUp:                yield return new ServerUnitRankUp(packet); break;
                    case proto.ServerTimeSlow:                  yield return new ServerTimeSlow(packet); break;
                    case proto.ClientTimeSlow:                  yield return new ClientTimeSlow(packet); break;
                    case proto.ServerTimeNormal:                yield return new ServerTimeNormal(packet); break;
                    case proto.ClientTimeNormal:                yield return new ClientTimeNormal(packet); break;
                    case proto.ServerTimePaused:                yield return new ServerTimePaused(packet); break;
                    case proto.ClientTimePaused:                yield return new ClientTimePaused(packet); break;
                    case proto.ServerTimeUnpaused:              yield return new ServerTimeUnpaused(packet); break;
                    case proto.ClientTimeUnpaused:              yield return new ClientTimeUnpaused(packet); break;
                    case proto.ClientExecuteCommnand:           yield return new ClientExecuteCommnand(packet); break;
                    case proto.ClientUnitProduce:               yield return new ClientUnitProduce(packet); break;
                    case proto.ClientUnitAbortProduction:       yield return new ClientUnitAbortProduction(packet); break;
                    case proto.ServerPlacementBegin:            yield return new ServerPlacementBegin(packet); break;
                    case proto.ServerPlacementShockwave:        yield return new ServerPlacementShockwave(packet); break;
                    case proto.ServerPlacementReleased:         yield return new ServerPlacementReleased(packet); break;
                    case proto.ClientPlacementRequest:          yield return new ClientPlacementRequest(packet); break;
                    default: yield break;
            }}
        }
        public abstract class SerializablePacket{ public virtual byte[] Serialize() => [];}
        public class ClientJoinRequest : SerializablePacket{
            public string username;
            public string password;
            public Color color;
            public ClientJoinRequest(string username, string password, Color color){
                this.username = username;
                this.password = password;
                this.color = color;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ClientJoinRequest);
                packet.SerializeString(username);
                SerializeColor(packet, color);
                return packet.GetData();
            }
            public ClientJoinRequest(PacketReader packet){
                username = packet.DeserializeString();
                password = packet.DeserializeString();
                color = DeseralizeColor(packet);
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
            public Color color;
            public ServerPlayerHasJoined(byte player_id, string username, Color color){
                this.player_id = player_id;
                this.username = username;
                this.color = color;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerPlayerHasJoined);
                packet.SerializeByte(player_id);
                packet.SerializeString(username);
                SerializeColor(packet, color);
                return packet.GetData();
            }
            public ServerPlayerHasJoined(PacketReader packet){
                player_id = packet.DeserializeByte();
                username = packet.DeserializeString();
                color = DeseralizeColor(packet);
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

        #region SERIALIZE SPECIAL TYPES
        static void SerializePosition(PacketWriter packet, Vector3 pos){
            // we're going to serialize all floats as floats as fixed numbers 
            // -3276.8 to 3276.7 (0.1 intervals)
            int x = (int)(pos.x * 10);
            int y = (int)(pos.y * 10);
            int z = (int)(pos.z * 10);
            if (x > short.MaxValue) x = short.MaxValue;
            else if (x < short.MinValue) x = short.MinValue;
            if (y > short.MaxValue) y = short.MaxValue;
            else if (y < short.MinValue) y = short.MinValue;
            if (z > short.MaxValue) z = short.MaxValue;
            else if (z < short.MinValue) z = short.MinValue;
            packet.SerializeShort((short)x);
            packet.SerializeShort((short)y);
            packet.SerializeShort((short)z);
        }
        static Vector3 DeseralizePosition(PacketReader packet){
            return new(
                packet.DeserializeShort() / 10.0f,
                packet.DeserializeShort() / 10.0f,
                packet.DeserializeShort() / 10.0f
            );
        }
        static void SerializeColor(PacketWriter packet, Color col){
            Color32 c = (Color32)col;
            packet.SerializeByte(c.r);
            packet.SerializeByte(c.g);
            packet.SerializeByte(c.b);
        }
        static Color DeseralizeColor(PacketReader packet){
            return new Color32(
                packet.DeserializeByte(),
                packet.DeserializeByte(),
                packet.DeserializeByte(),
                255
            );
        }
        static void SerializeEntityController(PacketWriter packet, EntityController entity){
            ushort network_id = NetworkedEntities.IdFromEntity(entity);
            if (network_id == 0xffff && entity != null) RCMManager.Log($"Failed to serialize Entity '{entity.entityId}' for networked event...");
            packet.SerializeShort((short)network_id);
        }
        static EntityController DeserializeEntityController(PacketReader packet){
            ushort network_id = (ushort)packet.DeserializeShort();
            EntityController entity = NetworkedEntities.EntityFromId(network_id).entity;
            if (network_id != 0xffff && entity == null) RCMManager.Log($"Failed to deserialize entity networked id '{network_id}' for networked event...");
            return entity;
        }

        // serializes floats between null (-1) to 655.34
        static void SerializeNullableStat(PacketWriter packet, float stat){
            ushort val = 0xffff;
            stat *= 100; // so we capture 2 decimal points
            if (stat >= 0xffff) val = 0xfffe;
            else if (stat >= 0) val = (ushort)stat;
            packet.SerializeShort((short)val);
        }
        static float DeserializeNullableStat(PacketReader packet){
            ushort val = (ushort)packet.DeserializeShort();
            if (val == 0xffff) return -1;
            return val / 100f;
        }
        static void SerializeSignedStat(PacketWriter packet, float stat){
            int val = (int)(stat * 100f); 
            if (val > short.MaxValue) val = short.MaxValue;
            else if (val < short.MinValue) val = short.MinValue;
            packet.SerializeShort((short)val);
            RCMManager.Log($"[Co-op] serializing signed stat: '{stat}' short: '{val}'");
        }
        static float DeserializeSignedStat(PacketReader packet)
        {
            float result = packet.DeserializeShort() / 100f;
            RCMManager.Log($"[Co-op] deserializing signed stat: '{result}'");
            return result;
        }
        
        static void SerializeVec2Int(PacketWriter packet, Vector2Int position){
            packet.SerializeShort((short)position.x);
            packet.SerializeShort((short)position.y);
        }
        static Vector2Int DeserializeVec2Int(PacketReader packet){
            short vec_x = packet.DeserializeShort();
            short vec_y = packet.DeserializeShort();
            return new Vector2Int(vec_x, vec_y);
        }
        static void SerializeNullableVec2Int(PacketWriter packet, Vector2Int? position){
            if (position == null)
            {
                packet.SerializeShort(short.MinValue);
                packet.SerializeShort(short.MinValue);
            }
            else
            {
                packet.SerializeShort((short)position.Value.x);
                packet.SerializeShort((short)position.Value.y);
            }
        }
        static Vector2Int? DeserializeNullableVec2Int(PacketReader packet){
            short vec_x = packet.DeserializeShort();
            short vec_y = packet.DeserializeShort();
            if (vec_x == short.MinValue && vec_y == short.MinValue)
                return null;
            return new Vector2Int(vec_x, vec_y);
        }

        static void SerializeEntityState(PacketWriter packet, entity_state entity){
            packet.SerializeString(entity.entity_id);
            packet.SerializeByte(entity.owning_player);
            packet.SerializeByte(entity.child_controller_index);
            packet.SerializeShort((short)entity.network_id);
            packet.SerializeShort((short)entity.parent_controller_id);
            packet.SerializeShort((short)entity.parent_transform_id);
            packet.SerializeShort((short)entity.parent_transform_index);
            // we're going to serialize all floats as floats as fixed numbers 
            // -3276.8 to 3276.7 (0.1 intervals)
            SerializePosition(packet, entity.pos);
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
            if (scale > ushort.MaxValue) scale = ushort.MaxValue;
            else if (scale < 0) scale = 0;
            packet.SerializeShort((short)scale);
            // then tag
            packet.SerializeByte((byte)entity.tag);
        }
        static entity_state DeserializeEntityState(PacketReader packet, entity_state entity){
            entity.entity_id = packet.DeserializeString();
            entity.owning_player = packet.DeserializeByte();
            entity.child_controller_index = packet.DeserializeByte();
            entity.network_id = (ushort)packet.DeserializeShort();
            entity.parent_controller_id = (ushort)packet.DeserializeShort();
            entity.parent_transform_id = (ushort)packet.DeserializeShort();
            entity.parent_transform_index = (ushort)packet.DeserializeShort();
            entity.pos = DeseralizePosition(packet);
            entity.rot_yaw = packet.DeserializeShort() / 182.04166666666f;
            entity.scale = packet.DeserializeShort() / 100.0f;
            entity.tag = (entity_state.entity_tags)packet.DeserializeByte();
            return entity;
        }
        #endregion
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
        public class ServerEntitiesPositionUpdate : SerializablePacket{
            public struct EntityPosition{
                public EntityController entity;
                public Vector3 pos;
            }
            public List<EntityPosition> positions;
            public ServerEntitiesPositionUpdate(List<EntityPosition> positions){
                this.positions = positions;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerEntitiesPositionUpdate);
                packet.SerializeShort((short)positions.Count);
                foreach (EntityPosition item in positions){
                    SerializeEntityController(packet, item.entity);
                    SerializePosition(packet, item.pos);
                }
                return packet.GetData();
            }
            public ServerEntitiesPositionUpdate(PacketReader packet){
                positions = new();
                short pos_count = packet.DeserializeShort();
                for (int i = 0; i < pos_count; i++){
                    EntityPosition entity = new EntityPosition();
                    entity.entity = DeserializeEntityController(packet);
                    entity.pos = DeseralizePosition(packet);
                    positions.Add(entity);
                }
            }
        }

        // being wip entity thingos section
        public class ServerUnitActivateSkillPosition : SerializablePacket{
            public EntityController entity;
            public Vector3 pos;
            public ServerUnitActivateSkillPosition(EntityController entity, Vector3 pos){
                this.entity = entity; this.pos = pos;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitActivateSkillPosition);
                SerializeEntityController(packet, entity);
                SerializePosition(packet, pos);
                return packet.GetData();
            }
            public ServerUnitActivateSkillPosition(PacketReader packet){
                entity = DeserializeEntityController(packet);
                pos = DeseralizePosition(packet);
            }
        }
        public class ServerUnitActivateSkillTarget : SerializablePacket{
            public EntityController entity;
            public EntityController target;
            public ServerUnitActivateSkillTarget(EntityController entity, EntityController target){
                this.entity = entity; this.target = target;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitActivateSkillTarget);
                SerializeEntityController(packet, entity);
                SerializeEntityController(packet, target);
                return packet.GetData();
            }
            public ServerUnitActivateSkillTarget(PacketReader packet){
                entity = DeserializeEntityController(packet);
                target = DeserializeEntityController(packet);
            }
        }
        public class ServerUnitActivateSkill : SerializablePacket{
            public EntityController entity;
            public int? count;
            public ServerUnitActivateSkill(EntityController entity, int? count){
                this.entity = entity; this.count = count;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitActivateSkill);
                SerializeEntityController(packet, entity);
                ushort val;
                if (count == null)  val = 0xffff;
                else                val = (ushort)count;
                packet.SerializeShort((short)val);
                return packet.GetData();
            }
            public ServerUnitActivateSkill(PacketReader packet){
                entity = DeserializeEntityController(packet);
                ushort val = (ushort)packet.DeserializeShort();
                if (val == 0xffff)  count = null;
                else                count = val;
            }
        }
        
        public class ServerUnitAttackMovePosition : SerializablePacket{
            public EntityController entity;
            public Vector3 pos;
            public ServerUnitAttackMovePosition(EntityController entity, Vector3 pos){
                this.entity = entity; this.pos = pos;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitAttackMovePosition);
                SerializeEntityController(packet, entity);
                SerializePosition(packet, pos);
                return packet.GetData();
            }
            public ServerUnitAttackMovePosition(PacketReader packet){
                entity = DeserializeEntityController(packet);
                pos = DeseralizePosition(packet);
            }
        }
        public class ServerUnitAttackMoveTarget : SerializablePacket{
            public EntityController entity;
            public EntityController target;
            public ServerUnitAttackMoveTarget(EntityController entity, EntityController target){
                this.entity = entity; this.target = target;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitAttackMoveTarget);
                SerializeEntityController(packet, entity);
                SerializeEntityController(packet, target);
                return packet.GetData();
            }
            public ServerUnitAttackMoveTarget(PacketReader packet){
                entity = DeserializeEntityController(packet);
                target = DeserializeEntityController(packet);
            }
        }
        public class ServerUnitAttack : SerializablePacket{
            public EntityController entity;
            public EntityController target;
            public ServerUnitAttack(EntityController entity, EntityController target){
                this.entity = entity; this.target = target;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitAttack);
                SerializeEntityController(packet, entity);
                SerializeEntityController(packet, target);
                return packet.GetData();
            }
            public ServerUnitAttack(PacketReader packet){
                entity = DeserializeEntityController(packet);
                target = DeserializeEntityController(packet);
            }
        }
        public class ServerUnitOnReadyToShoot : SerializablePacket{
            public EntityController entity;
            public EntityController target;
            public ServerUnitOnReadyToShoot(EntityController entity, EntityController target){
                this.entity = entity; this.target = target;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitOnReadyToShoot);
                SerializeEntityController(packet, entity);
                SerializeEntityController(packet, target);
                return packet.GetData();
            }
            public ServerUnitOnReadyToShoot(PacketReader packet){
                entity = DeserializeEntityController(packet);
                target = DeserializeEntityController(packet);
            }
        }
        public class ServerUnitFollowTarget : SerializablePacket{
            public EntityController entity;
            public EntityController target;
            public ServerUnitFollowTarget(EntityController entity, EntityController target){
                this.entity = entity; this.target = target;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitFollowTarget);
                SerializeEntityController(packet, entity);
                SerializeEntityController(packet, target);
                return packet.GetData();
            }
            public ServerUnitFollowTarget(PacketReader packet){
                entity = DeserializeEntityController(packet);
                target = DeserializeEntityController(packet);
            }
        }
        public class ServerUnitFollowPosition : SerializablePacket{
            public EntityController entity;
            public Vector3 pos;
            public float distance;
            public ServerUnitFollowPosition(EntityController entity, Vector3 pos, float distance){
                this.entity = entity; this.pos = pos;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitFollowPosition);
                SerializeEntityController(packet, entity);
                SerializePosition(packet, pos);
                SerializeNullableStat(packet, distance);
                return packet.GetData();
            }
            public ServerUnitFollowPosition(PacketReader packet){
                entity = DeserializeEntityController(packet);
                pos = DeseralizePosition(packet);
                distance = DeserializeNullableStat(packet);
            }
        }
        public class ServerUnitStop : SerializablePacket{
            public EntityController entity;
            public ServerUnitStop(EntityController entity){
                this.entity = entity;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitStop);
                SerializeEntityController(packet, entity);
                return packet.GetData();
            }
            public ServerUnitStop(PacketReader packet){
                entity = DeserializeEntityController(packet);
            }
        }
        public class ServerUnitTeleport : SerializablePacket{
            public EntityController entity;
            public Vector3 pos;
            public bool dont_trigger_events;
            public ServerUnitTeleport(EntityController entity, Vector3 pos, bool dont_trigger_events){
                this.entity = entity; this.pos = pos; this.dont_trigger_events = dont_trigger_events;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitTeleport);
                SerializeEntityController(packet, entity);
                SerializePosition(packet, pos);
                packet.SerializeByte(dont_trigger_events ? (byte)1 : (byte)0);
                return packet.GetData();
            }
            public ServerUnitTeleport(PacketReader packet){
                entity = DeserializeEntityController(packet);
                pos = DeseralizePosition(packet);
                dont_trigger_events = packet.DeserializeByte() > 0;
            }
        }
        public class ServerUnitMoveTo : SerializablePacket{
            public EntityController entity;
            public Vector3 pos;
            public bool counts_as_move_command;
            public HeightLayer? restrictedToHeightLayer; 
            public Vector2Int? clickPositionCell;
            public ServerUnitMoveTo(EntityController entity, Vector3 pos, bool counts_as_move_command, HeightLayer? restrictedToHeightLayer, Vector2Int? clickPositionCell){
                this.entity = entity; this.pos = pos; this.counts_as_move_command = counts_as_move_command; this.restrictedToHeightLayer = restrictedToHeightLayer; this.clickPositionCell = clickPositionCell;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitMoveTo);
                SerializeEntityController(packet, entity);
                SerializePosition(packet, pos);
                packet.SerializeByte(counts_as_move_command ? (byte)1 : (byte)0);
                packet.SerializeByte(restrictedToHeightLayer == null? (byte)255 : (byte)restrictedToHeightLayer.Value);
                SerializeNullableVec2Int(packet, clickPositionCell);
                return packet.GetData();
            }
            public ServerUnitMoveTo(PacketReader packet){
                entity = DeserializeEntityController(packet);
                pos = DeseralizePosition(packet);
                counts_as_move_command = packet.DeserializeByte() > 0;
                byte val = packet.DeserializeByte();
                if (val == 255)
                     restrictedToHeightLayer = null;
                else restrictedToHeightLayer = (HeightLayer)val;
                clickPositionCell = DeserializeNullableVec2Int(packet);
            }
        }
        public class ServerUnitRepairArmor : SerializablePacket{
            public EntityController entity;
            public float new_shield_value;
            public EntityController originator;
            public bool dont_trigger_events;
            public ServerUnitRepairArmor(EntityController entity, float new_shield_value, EntityController originator, bool dont_trigger_events){
                this.entity = entity; this.new_shield_value = new_shield_value; this.originator = originator; this.dont_trigger_events = dont_trigger_events;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitRepairArmor);
                SerializeEntityController(packet, entity);
                SerializeNullableStat(packet, new_shield_value);
                SerializeEntityController(packet, originator);
                packet.SerializeByte(dont_trigger_events ? (byte)1 : (byte)0);
                return packet.GetData();
            }
            public ServerUnitRepairArmor(PacketReader packet){
                entity = DeserializeEntityController(packet);
                new_shield_value = DeserializeNullableStat(packet);
                originator = DeserializeEntityController(packet);
                dont_trigger_events = packet.DeserializeByte() > 0;
            }
        }
        public class ServerUnitHeal : SerializablePacket{
            public EntityController entity;
            public float new_health_value;
            public EntityController originator;
            public bool dont_trigger_has_healed;
            public bool dont_trigger_being_healed;
            public ServerUnitHeal(EntityController entity, float new_health_value, EntityController originator, bool dont_trigger_has_healed, bool dont_trigger_being_healed){
                this.entity = entity; this.new_health_value = new_health_value; this.originator = originator; this.dont_trigger_has_healed = dont_trigger_has_healed; this.dont_trigger_being_healed = dont_trigger_being_healed;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitHeal);
                SerializeEntityController(packet, entity);
                packet.SerializeFloat(new_health_value);
                SerializeEntityController(packet, originator);
                packet.SerializeByte(dont_trigger_has_healed ? (byte)1 : (byte)0);
                packet.SerializeByte(dont_trigger_being_healed ? (byte)1 : (byte)0);
                return packet.GetData();
            }
            public ServerUnitHeal(PacketReader packet){
                entity = DeserializeEntityController(packet);
                new_health_value = packet.DeserializeFloat();
                originator = DeserializeEntityController(packet);
                dont_trigger_has_healed = packet.DeserializeByte() > 0;
                dont_trigger_being_healed = packet.DeserializeByte() > 0;
            }
        }
        public class ServerUnitChargeShield : SerializablePacket{
            public EntityController entity;
            public float new_shield_value;
            public ServerUnitChargeShield(EntityController entity, float new_shield_value){
                this.entity = entity; this.new_shield_value = new_shield_value;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitChargeShield);
                SerializeEntityController(packet, entity);
                SerializeNullableStat(packet, new_shield_value);
                return packet.GetData();
            }
            public ServerUnitChargeShield(PacketReader packet){
                entity = DeserializeEntityController(packet);
                new_shield_value = DeserializeNullableStat(packet);
            }
        }
        public class ServerUnitTakeDamage : SerializablePacket{
            public EntityController entity;
            public float new_health_value;
            public EntityController originator;
            public bool dont_trigger_has_damaged;
            public bool ignore_armor;
            public ServerUnitTakeDamage(EntityController entity, float new_health_value, EntityController originator, bool dont_trigger_has_damaged, bool ignore_armor){
                this.entity = entity; this.new_health_value = new_health_value; this.originator = originator; this.dont_trigger_has_damaged = dont_trigger_has_damaged; this.ignore_armor = ignore_armor;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitTakeDamage);
                SerializeEntityController(packet, entity);
                packet.SerializeFloat(new_health_value);
                SerializeEntityController(packet, originator);
                packet.SerializeByte(dont_trigger_has_damaged ? (byte)1 : (byte)0);
                packet.SerializeByte(ignore_armor ? (byte)1 : (byte)0);
                return packet.GetData();
            }
            public ServerUnitTakeDamage(PacketReader packet){
                entity = DeserializeEntityController(packet);
                new_health_value = packet.DeserializeFloat();
                originator = DeserializeEntityController(packet);
                dont_trigger_has_damaged = packet.DeserializeByte() > 0;
                ignore_armor = packet.DeserializeByte() > 0;
            }
        }
        public class ServerUnitProduce : SerializablePacket{
            public EntityController entity;
            public bool instant_production;
            public bool for_free;
            public bool dont_trigger_events;
            public ServerUnitProduce(EntityController entity, bool instant_production, bool for_free, bool dont_trigger_events){
                this.entity = entity; this.instant_production = instant_production; this.for_free = for_free; this.dont_trigger_events = dont_trigger_events;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitProduce);
                SerializeEntityController(packet, entity);
                packet.SerializeByte(instant_production ? (byte)1 : (byte)0);
                packet.SerializeByte(for_free ? (byte)1 : (byte)0);
                packet.SerializeByte(dont_trigger_events ? (byte)1 : (byte)0);
                return packet.GetData();
            }
            public ServerUnitProduce(PacketReader packet){
                entity = DeserializeEntityController(packet);
                instant_production = packet.DeserializeByte() > 0;
                for_free = packet.DeserializeByte() > 0;
                dont_trigger_events = packet.DeserializeByte() > 0;
            }
        }
        public class ServerUnitAbortProduction : SerializablePacket{
            public EntityController entity;
            public ServerUnitAbortProduction(EntityController entity){
                this.entity = entity;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitAbortProduction);
                SerializeEntityController(packet, entity);
                return packet.GetData();
            }
            public ServerUnitAbortProduction(PacketReader packet){
                entity = DeserializeEntityController(packet);
            }
        }
        public class ServerUnitChargeMana : SerializablePacket{ 
            public EntityController entity;
            public float new_mana_value;
            public bool display_delta;
            public ServerUnitChargeMana(EntityController entity, float new_mana_value, bool display_delta){
                this.entity = entity; this.new_mana_value = new_mana_value; this.display_delta = display_delta;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitChargeMana);
                SerializeEntityController(packet, entity);
                float full_entity_mana = entity.CurrentMana + new_mana_value;
                SerializeNullableStat(packet, full_entity_mana);
                packet.SerializeByte(display_delta ? (byte)1 : (byte)0);
                return packet.GetData();
            }
            public ServerUnitChargeMana(PacketReader packet){
                entity = DeserializeEntityController(packet);
                new_mana_value = DeserializeNullableStat(packet);
                if (entity != null)
                {
                    new_mana_value = new_mana_value - entity.CurrentMana;
                }
                display_delta = packet.DeserializeByte() > 0;
            }
        }
        public class ServerUnitSetStatusEffect : SerializablePacket{ 
            public EntityController entity;
            public StatusEffect statusEffect;
            public SetStatusEffect.DurationType durationType;
            public float duration; // TODO: we want to serialize this as something smaller than a float please, too excessive
            public ServerUnitSetStatusEffect(EntityController entity, StatusEffect statusEffect, SetStatusEffect.DurationType durationType, float duration){
                this.entity = entity; this.statusEffect = statusEffect; this.durationType = durationType; this.duration = duration;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitSetStatusEffect);
                SerializeEntityController(packet, entity);
                packet.SerializeInt((int)statusEffect);
                packet.SerializeByte((byte)durationType);
                packet.SerializeFloat(duration);
                return packet.GetData();
            }
            public ServerUnitSetStatusEffect(PacketReader packet){
                entity = DeserializeEntityController(packet);
                statusEffect = (StatusEffect)packet.DeserializeInt();
                durationType = (SetStatusEffect.DurationType)packet.DeserializeByte();
                duration = packet.DeserializeFloat();
            }
        }
        public class ServerUnitRemoveActiveStatus : SerializablePacket{ 
            public EntityController entity;
            public StatusEffect statusEffect;
            public ServerUnitRemoveActiveStatus(EntityController entity, StatusEffect statusEffect){
                this.entity = entity; this.statusEffect = statusEffect;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitRemoveActiveStatus);
                SerializeEntityController(packet, entity);
                packet.SerializeInt((int)statusEffect);
                return packet.GetData();
            }
            public ServerUnitRemoveActiveStatus(PacketReader packet){
                entity = DeserializeEntityController(packet);
                statusEffect = (StatusEffect)packet.DeserializeInt();
            }
        }
        public class ServerUnitRemoveStatusEffect : SerializablePacket{ 
            public EntityController entity;
            public StatusEffect statusEffect;
            public ServerUnitRemoveStatusEffect(EntityController entity, StatusEffect statusEffect){
                this.entity = entity; this.statusEffect = statusEffect;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitRemoveStatusEffect);
                SerializeEntityController(packet, entity);
                packet.SerializeInt((int)statusEffect);
                return packet.GetData();
            }
            public ServerUnitRemoveStatusEffect(PacketReader packet){
                entity = DeserializeEntityController(packet);
                statusEffect = (StatusEffect)packet.DeserializeInt();
            }
        }
        public class ServerUnitRankUp : SerializablePacket{ 
            public EntityController entity;
            public int amount;
            public ServerUnitRankUp(EntityController entity, int amount){
                this.entity = entity; this.amount = amount;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerUnitRankUp);
                SerializeEntityController(packet, entity);
                packet.SerializeShort((short)amount);
                return packet.GetData();
            }
            public ServerUnitRankUp(PacketReader packet){
                entity = DeserializeEntityController(packet);
                amount = packet.DeserializeShort();
            }
        }
        public class ServerTimeSlow : SerializablePacket{ 
            public ServerTimeSlow(){}
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerTimeSlow);
                return packet.GetData();
            }
            public ServerTimeSlow(PacketReader packet){}
        }
        public class ClientTimeSlow : SerializablePacket{ 
            public ClientTimeSlow(){}
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ClientTimeSlow);
                return packet.GetData();
            }
            public ClientTimeSlow(PacketReader packet){}
        }
        public class ServerTimeNormal : SerializablePacket{ 
            public ServerTimeNormal(){}
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerTimeNormal);
                return packet.GetData();
            }
            public ServerTimeNormal(PacketReader packet){}
        }
        public class ClientTimeNormal : SerializablePacket{ 
            public ClientTimeNormal(){}
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ClientTimeNormal);
                return packet.GetData();
            }
            public ClientTimeNormal(PacketReader packet){}
        }
        public class ServerTimePaused : SerializablePacket{ 
            public ServerTimePaused(){}
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerTimePaused);
                return packet.GetData();
            }
            public ServerTimePaused(PacketReader packet){}
        }
        public class ClientTimePaused : SerializablePacket{ 
            public ClientTimePaused(){}
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ClientTimePaused);
                return packet.GetData();
            }
            public ClientTimePaused(PacketReader packet){}
        }
        public class ServerTimeUnpaused : SerializablePacket{ 
            public ServerTimeUnpaused(){}
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerTimeUnpaused);
                return packet.GetData();
            }
            public ServerTimeUnpaused(PacketReader packet){}
        }
        public class ClientTimeUnpaused : SerializablePacket{ 
            public ClientTimeUnpaused(){}
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ClientTimeUnpaused);
                return packet.GetData();
            }
            public ClientTimeUnpaused(PacketReader packet){}
        }
        public class ClientExecuteCommnand : SerializablePacket{
            public EntityController entity;
            public EntityCommand command;
            public EntityController.CommandProcessingType processingType;
            public ClientExecuteCommnand(EntityController entity, EntityCommand command, EntityController.CommandProcessingType processingType){
                this.entity = entity; this.command = command; this.processingType = processingType;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ClientExecuteCommnand);
                SerializeEntityController(packet, entity);
                packet.SerializeByte((byte)command.command);
                SerializePosition(packet, command.destination);
                SerializeNullableVec2Int(packet, command.clickPositionCell);
                SerializeEntityController(packet, command.target);
                SerializeNullableStat(packet, command.floatValue);
                //SerializeEntityController(packet, command.skillPlaceholderEntity);
                ushort val;
                if (command.numberOfUnactivatedStatusFlagsInGroup == null) val = 0xffff;
                else val = (ushort)command.numberOfUnactivatedStatusFlagsInGroup;
                packet.SerializeShort((short)val);
                packet.SerializeShort((short)command.intValue);
                packet.SerializeShort((short)command.buildingSize);
                //packet.SerializeString(command.tag);
                packet.SerializeByte((byte)command.role);
                //packet.SerializeString(command.intent);
                packet.SerializeByte((byte)processingType);
                return packet.GetData();
            }
            public ClientExecuteCommnand(PacketReader packet){
                entity = DeserializeEntityController(packet);
                command = new();
                command.command = (EntityCommand.Command)packet.DeserializeByte();
                command.destination = DeseralizePosition(packet);
                command.clickPositionCell = DeserializeNullableVec2Int(packet);
                command.target = DeserializeEntityController(packet);
                command.floatValue = DeserializeNullableStat(packet);
                //command.skillPlaceholderEntity = DeserializeEntityController(packet);
                ushort val = (ushort)packet.DeserializeShort();
                if (val == 0xffff) command.numberOfUnactivatedStatusFlagsInGroup = null;
                else command.numberOfUnactivatedStatusFlagsInGroup = val;
                command.intValue = packet.DeserializeShort();
                command.buildingSize = packet.DeserializeShort();
                //command.tag = packet.DeserializeString();
                command.role = (EntityCommand.Role)packet.DeserializeByte();
                //command.intent = packet.DeserializeString();
                processingType = (EntityController.CommandProcessingType)packet.DeserializeByte();
            }
        }
        public class ClientUnitProduce : SerializablePacket{
            public EntityController entity;
            public ClientUnitProduce(EntityController entity){
                this.entity = entity;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ClientUnitProduce);
                SerializeEntityController(packet, entity);
                return packet.GetData();
            }
            public ClientUnitProduce(PacketReader packet){
                entity = DeserializeEntityController(packet);
            }
        }
        public class ClientUnitAbortProduction : SerializablePacket{
            public EntityController entity;
            public ClientUnitAbortProduction(EntityController entity){
                this.entity = entity;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ClientUnitAbortProduction);
                SerializeEntityController(packet, entity);
                return packet.GetData();
            }
            public ClientUnitAbortProduction(PacketReader packet){
                entity = DeserializeEntityController(packet);
            }
        }



        public class ServerPlacementBegin : SerializablePacket{
            public bool is_1x1;
            public Vector3 position;
            public ServerPlacementBegin(bool is_1x1, Vector3 position){
                this.is_1x1 = is_1x1; this.position = position;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerPlacementBegin);
                packet.SerializeByte(is_1x1 ? (byte)1 : (byte)0);
                SerializePosition(packet, position);
                return packet.GetData();
            }
            public ServerPlacementBegin(PacketReader packet){
                is_1x1 = packet.DeserializeByte() > 0;
                position = DeseralizePosition(packet);
            }
        }
        public class ServerPlacementShockwave : SerializablePacket{
            public bool is_1x1;
            public Vector3 position;
            public ServerPlacementShockwave(bool is_1x1, Vector3 position){
                this.is_1x1 = is_1x1; this.position = position;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerPlacementShockwave);
                packet.SerializeByte(is_1x1 ? (byte)1 : (byte)0);
                SerializePosition(packet, position);
                return packet.GetData();
            }
            public ServerPlacementShockwave(PacketReader packet){
                is_1x1 = packet.DeserializeByte() > 0;
                position = DeseralizePosition(packet);
            }
        }
        
        public class ServerPlacementReleased : SerializablePacket{
            public ushort ghost_id;
            public ServerPlacementReleased(ushort ghost_id){
                this.ghost_id = ghost_id;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ServerPlacementReleased);
                packet.SerializeShort((short)ghost_id);
                return packet.GetData();
            }
            public ServerPlacementReleased(PacketReader packet){
                ghost_id = (ushort)packet.DeserializeShort();
            }
        }
        public class ClientPlacementRequest : SerializablePacket{
            public EntityController engi;
            public string entityId;
            public Vector3 pos;
            public List<Vector2Int> cells;
            public ushort ghost_id;
            public ClientPlacementRequest(EntityController engi, string entityId, Vector3 pos, List<Vector2Int> cells, ushort ghost_id){
                this.engi = engi; this.entityId = entityId; this.pos = pos; this.cells = cells; this.ghost_id = ghost_id;
            }
            public override byte[] Serialize(){
                PacketWriter packet = new();
                packet.SerializeByte((byte)proto.ClientPlacementRequest);
                SerializeEntityController(packet, engi);
                packet.SerializeString(entityId);
                SerializePosition(packet, pos);
                packet.SerializeShort((short)ghost_id);
                packet.SerializeByte((byte)cells.Count);
                foreach (var v in cells)
                    SerializeVec2Int(packet, v);

                return packet.GetData();
            }
            public ClientPlacementRequest(PacketReader packet){
                engi = DeserializeEntityController(packet);
                entityId = packet.DeserializeString();
                pos = DeseralizePosition(packet);
                ghost_id = (ushort)packet.DeserializeShort();
                cells = new List<Vector2Int>();
                int count = packet.DeserializeByte();
                for (int i = 0; i < count; i++)
                    cells.Add(DeserializeVec2Int(packet));
            }
        }



    }
}
