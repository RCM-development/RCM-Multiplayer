using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestMod;
using UnityEngine;
using UnityEngine.Networking.Types;
namespace RCM_Coop.Network.Entities{

    public static class NetworkedEntities{
        static uint entities_first_free_id = 0;
        static uint entities_highest_id = 0;
        static uint entities_assigned_count = 0;
        const uint MAX_ENTITY_IDS = 4096;
        static EntityNetworkData[] Entities = new EntityNetworkData[MAX_ENTITY_IDS];
        static Dictionary<EntityController, uint> NetworkedEntityIDs = new Dictionary<EntityController, uint>();
        public struct EntityNetworkData{
            public EntityNetworkData(){}
            public EntityController entity = null; // client + server
            public byte owning_player = 255; // client + server
            public bool position_updated = false; // client + server (client uses to track if has recieved a position yet...)
            public Vector3 last_server_position; // client
        }

        public static IEnumerable<KeyValuePair<uint, EntityNetworkData>> IterateNetworkedEntities(){
            for  (uint i = 0; i < entities_highest_id; i++){
                EntityNetworkData entity = Entities[i];
                if (entity.entity != null)
                    yield return new KeyValuePair<uint, EntityNetworkData>(i, entity);
        }}
        public static ushort IdFromEntity(EntityController entity){
            if (entity == null) return (ushort)0xffff; 
            if (NetworkedEntityIDs.TryGetValue(entity, out uint id))
                return (ushort)id;
            return (ushort)0xffff;
        }
        public static EntityNetworkData EntityFromId(uint network_id){
            if (network_id >= MAX_ENTITY_IDS) return new EntityNetworkData();
            return Entities[network_id];
        }
        public static EntityNetworkData EntityFromEntity(EntityController entity){
            ushort val = IdFromEntity(entity);
            if (val == 0xffff) return new();
            else return EntityFromId(val);
        }


        public static void WritePositionUpdated(EntityController entity, bool value){
            ushort id = IdFromEntity(entity);
            if (id != 0xffff) WritePositionUpdated(id, value);
        }
        public static void WritePositionUpdated(uint network_id, bool value){
            if (network_id >= MAX_ENTITY_IDS || Entities[network_id].entity == null) return;
            Entities[network_id].position_updated = value;
        }
        public static void WriteLastPosition(EntityController entity, Vector3 value){
            ushort id = IdFromEntity(entity);
            if (id != 0xffff) WriteLastPosition(id, value);
        }
        public static void WriteLastPosition(uint network_id, Vector3 value){
            if (network_id >= MAX_ENTITY_IDS || Entities[network_id].entity == null) return;
            Entities[network_id].last_server_position = value;
            // we also set the position updated flag so that we know this unit has a valid last known position
            Entities[network_id].position_updated = true;
        }

        public static void InsertEntity(EntityController entity, byte owning_player, uint network_id){
            var entry = Entities[network_id];
            if (entry.entity != null){
                // remove previous entity from lookup system
                if (NetworkedEntityIDs.ContainsKey(entry.entity)) NetworkedEntityIDs.Remove(entry.entity);
                RCMManager.Log($"[Co-op] somehow we just overwrote an entity in entities manager... overwrote a {entry.entity.entityId} with a {entity.entityId} at networkid {network_id}");
            }
            else entities_assigned_count += 1;

            EntityNetworkData new_entity_data = new();
            new_entity_data.entity = entity;
            new_entity_data.owning_player = owning_player;
            Entities[network_id] = new_entity_data;
            NetworkedEntityIDs[entity] = network_id;
            RCMManager.Log($"[Co-op] new entity: {entity.entityId} id {network_id}");
        }
        public static bool AllocEntity(EntityController entity, byte owning_player){
            if (entities_assigned_count >= MAX_ENTITY_IDS){
                RCMManager.Log("[Co-op] WARNING: totally networked entities has overflown. cannot network any more entities.");
                return false;
            }
            InsertEntity(entity, owning_player, entities_first_free_id);

            // find our next valid free ID
            while (true){
                entities_first_free_id += 1;
                if (entities_first_free_id > entities_highest_id)
                    entities_highest_id = entities_first_free_id;

                if (entities_first_free_id >= MAX_ENTITY_IDS) {
                    RCMManager.Log("[Co-op] WARNING: networked entities first free has overflown, impending network entities overflow.");
                    break;
                }
                if (Entities[entities_first_free_id].entity == null) break;
            }
            return true;
        }

        static void DeallocEntity(uint network_id, EntityController entity){
            // note that we dont do any error checks to make sure values are right, but as long as entities are allocated & paired up correctly they cant be deallocated incorrectly
            if (NetworkedEntityIDs.ContainsKey(entity)) NetworkedEntityIDs.Remove(entity);

            Entities[network_id] = new(); // reset slot data
            entities_assigned_count -= 1;
            // re-evaluate our new free index
            entities_first_free_id = Math.Min(entities_first_free_id, network_id);
            RCMManager.Log($"[Co-op] entity destroyed: {entity.entityId} id {network_id}");
        }
        public static bool DeallocEntity(uint network_id){
            EntityController entity = EntityFromId(network_id).entity;
            if (entity == null){
                RCMManager.Log($"[Co-op] somehow we just tried to destroy an entity that doesn't exist, id: {network_id}");
                return false;
            }
            DeallocEntity(network_id, entity);
            return true;
        }
        public static bool DeallocEntity(EntityController entity){
            ushort entity_id = IdFromEntity(entity);
            if (entity_id == 0xffff){
                RCMManager.Log("[Co-op] entity destroyed but wasn't being tracked by entity manager...");
                return false;
            }
            DeallocEntity(entity_id, entity);
            return true;
        }

    }
}
