using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestMod;

namespace RCM_Coop{
    static class EntitiesManager{

        static uint entities_first_free_id = 0;
        static uint entities_assigned_count = 0;
        const uint MAX_ENTITY_IDS = 4096;
        static EntityController[] NetworkedEntities = new EntityController[MAX_ENTITY_IDS];
        static Dictionary<EntityController, uint> NetowrkedEntityIDs = new Dictionary<EntityController, uint>();

        public static void EntitySpawned(EntityController entity, bool called_from_above){
            if (entities_assigned_count >= MAX_ENTITY_IDS){
                RCMManager.Log("totally networked entities has overflown. cannot network any more entities.");
                return;
            }

            // assign a network ID
            entities_assigned_count += 1;
            NetworkedEntities[entities_first_free_id] = entity;
            NetowrkedEntityIDs[entity] = entities_first_free_id;

            // find our next valid free ID
            while (true){
                entities_first_free_id += 1;
                if (entities_first_free_id >= MAX_ENTITY_IDS){
                    RCMManager.Log("networked entities first free has overflown, impending network entities overflow.");
                    break;
                }
                if (NetworkedEntities[entities_first_free_id] == null) break;
            }

            // replicate creation event to clients


        }

        public static void EntityDestroyed(EntityController entity, bool withoutTriggeringDestructionActions, EntityController originator){
            uint entity_id = NetowrkedEntityIDs[entity];
            NetworkedEntities[entity_id] = null;
            NetowrkedEntityIDs.Remove(entity);
            entities_assigned_count -= 1;

            // re-evaluate our new free index
            entities_first_free_id = Math.Min(entities_first_free_id, entity_id);

            // replicate destruction event to clients


        }
    }
}
