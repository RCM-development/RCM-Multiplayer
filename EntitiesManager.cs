using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestMod;
using UnityEngine;
using UnityEngine.UIElements;
using static RCM_Coop.CoopManager;
using static RCM_Coop.Network.GameProtocols;
using static RCM_Coop.Network.GameProtocols.ServerEntitiesPositionUpdate;
using static RCM_Coop.Network.GameProtocols.ServerFullEntityData;

namespace RCM_Coop{
    public static class EntitiesManager{

        static uint entities_first_free_id = 0;
        static uint entities_highest_id = 0;
        static uint entities_assigned_count = 0;
        const uint MAX_ENTITY_IDS = 4096;
        static EntityController[] NetworkedEntities = new EntityController[MAX_ENTITY_IDS];
        static EntityServerInfo[] EntityServerInfos = new EntityServerInfo[MAX_ENTITY_IDS];
        static Dictionary<EntityController, uint> NetowrkedEntityIDs = new Dictionary<EntityController, uint>();
        
        // only needed for host
        struct EntityServerInfo{
            public EntityServerInfo(){}
            public bool position_updated = false;
        }
        public static void NotifyEntityMoved(EntityController entity){
            ushort id = IdFromEntity(entity);
            if (id != 0xffff){
                EntityServerInfos[id].position_updated = true;
            }
        }
        private static float accumulator = 0f;
        private const float interval = 0.1f; // 100 ms
        public static void Update(){
            float dt = Time.unscaledDeltaTime;
            accumulator += dt;
            if (accumulator >= interval){
                accumulator -= interval;  
                // check if we're in game
                // for now we'll just do entities assigned count
                if (entities_assigned_count > 0 && CoopManager.IsServerUp()){
                    List<EntityPosition> positions = new();
                    foreach (var entity_struct in IterateNetworkedEntities()){
                        uint id = entity_struct.Key;
                        EntityController entity = entity_struct.Value;
                        if (EntityServerInfos[id].position_updated){
                            EntityServerInfos[id].position_updated = false;
                            positions.Add(new EntityPosition() { network_id = (ushort)id, pos_x = entity.gameObject.transform.position.x, pos_y = entity.gameObject.transform.position.y, pos_z = entity.gameObject.transform.position.z });
                        }
                    }
                    if (positions.Count > 0){
                        SendServerInGamePacket(new ServerEntitiesPositionUpdate(positions));
                    }
                }
            }
        }
        public static void RecievePositionUpdates(List<EntityPosition> positions)
        {
            foreach (var item in positions){
                EntityController entity = NetworkedEntities[item.network_id];
                if (entity != null)
                {
                    entity.gameObject.transform.position = new Vector3(item.pos_x, item.pos_y, item.pos_z);
                    entity.UpdateCachedPosition();
                }
                else
                {
                    RCMManager.Log($"[Co-op] position sync for entity that doesn't exist. id: {item.network_id}");
                }
            }
        }


        public static IEnumerable<KeyValuePair<uint, EntityController>> IterateNetworkedEntities(){
            for  (uint i = 0; i < entities_highest_id; i++){
                EntityController entity = NetworkedEntities[i];
                if (entity != null)
                    yield return new KeyValuePair<uint, EntityController>(i, NetworkedEntities[i]);
        }}
        public static ushort IdFromEntity(EntityController entity){
            if (NetowrkedEntityIDs.TryGetValue(entity, out uint id)){
                return (ushort)id;
            }
            //foreach (var entity_struct in IterateNetworkedEntities())
            //    if (entity == entity_struct.Value) return (ushort)entity_struct.Key;
            return (ushort)0xffff;
        }

        public static void EntitySpawned(EntityController entity, bool called_from_above){
            if (entities_assigned_count >= MAX_ENTITY_IDS){
                RCMManager.Log("[Co-op] totally networked entities has overflown. cannot network any more entities.");
                return;
            }
            if (string.IsNullOrWhiteSpace(entity.entityId)){
                RCMManager.Log("[Co-op] tried initiate sync of entity with no entity ID, cant replicate across... for now.");
                return;
            }

            // assign a network ID
            uint assigned_id = entities_first_free_id;
            EntitySpawnedAt(entity, assigned_id);

            // find our next valid free ID
            while (true){
                entities_first_free_id += 1;
                if (entities_first_free_id > entities_highest_id) 
                    entities_highest_id = entities_first_free_id;

                if (entities_first_free_id >= MAX_ENTITY_IDS){
                    RCMManager.Log("[Co-op] networked entities first free has overflown, impending network entities overflow.");
                    break;
                }
                if (NetworkedEntities[entities_first_free_id] == null) break;
            }

            // replicate creation event to clients
            if (IsServerUp()){
                spawned_entity_state state = new();
                CompileEntity(entity, (ushort)assigned_id, state);
                state.spawned_from_above = called_from_above;
                SendServerInGamePacket(new ServerUnitSpawned(state));
            }
        }
        static void EntitySpawnedAt(EntityController entity, uint network_id){
            if (NetworkedEntities[network_id] != null){
                RCMManager.Log($"[Co-op] somehow we just overwrote an entity in entities manager... overwrote a {NetworkedEntities[network_id].entityId} with a {entity.entityId} at networkid {network_id}");
            } else entities_assigned_count += 1;

            NetworkedEntities[network_id] = entity;
            NetowrkedEntityIDs[entity] = network_id;
            RCMManager.Log($"[Co-op] new entity: {entity.entityId} id {network_id}");
        }

        public static void EntityDestroyed(EntityController entity, bool withoutTriggeringDestructionActions, EntityController originator){
            if (!NetowrkedEntityIDs.ContainsKey(entity)){
                RCMManager.Log("[Co-op] entity destroyed but wasn't being tracked by entity manager...");
                return;
            }

            uint entity_id = NetowrkedEntityIDs[entity];
            EntityDestroyedAt(entity_id, entity);

            // replicate destruction event to clients
            if (IsServerUp()){
                ushort id = IdFromEntity(originator);
                if (originator != null && id == 0xffff) RCMManager.Log($"[Co-op] entity destroyed: {entity.entityId} id: {entity_id} by: {originator.entityId}, but couldn't match destroyer up to networked id...");
                SendServerInGamePacket(new ServerUnitDestroyed((ushort)entity_id, id, withoutTriggeringDestructionActions));
            }
        }
        public static void EntityDestroyedAt(uint network_id, EntityController entity = null){
            if (NetworkedEntities[network_id] == null){
                RCMManager.Log($"[Co-op] somehow we just tried to destroy an entity that doesn't exist, id: {network_id}");
                return;
            }

            if (entity == null) entity = NetworkedEntities[network_id];
            if (NetowrkedEntityIDs.ContainsKey(entity)) NetowrkedEntityIDs.Remove(entity);

            NetworkedEntities[network_id] = null;
            entities_assigned_count -= 1;

            // re-evaluate our new free index
            entities_first_free_id = Math.Min(entities_first_free_id, network_id);

            RCMManager.Log($"[Co-op] entity destroyed: {entity.entityId} id {network_id}");
        }

        public static void RecievedSpawn(EntityController entity, uint network_id)
        {
            EntitySpawnedAt(entity, network_id);
        }
        public static void RecievedDestroy(ushort entity_id, ushort originator_id, bool withoutTriggeringDestructionActions){
            EntityController entity = NetworkedEntities[entity_id];
            EntityController originator = NetworkedEntities[originator_id];

            if (entity_id != 0xffff && entity == null)
                RCMManager.Log($"[Co-op] entity id destroyed:{entity_id} but couldn't find target network id in our list");
            if (originator_id != 0xffff && originator == null)
                RCMManager.Log($"[Co-op] entity {entity.entityId} destroyed by originator:{originator_id} but couldn't find target network id in our list");

            Patch_EntityController_Destroy.Original(entity, withoutTriggeringDestructionActions, originator);
            EntityDestroyed(entity, false, originator);
        }



        public class entity_state{
            public string entity_id;
            public ushort network_id;
            public ushort parent_controller_id;
            public ushort parent_transform_id;
            public ushort parent_transform_index;
            public float pos_x;
            public float pos_y;
            public float pos_z;
            public float rot_yaw;
            public float scale;
            public enum entity_tags : byte{
                Player,
                Ai,
                Neutral,
                WorldMesh,
                World,
                Button
            }
            public entity_tags tag;
            public string TagFromEnum(){
                switch (tag){
                    case entity_tags.Player:    return Tags.Player;
                    case entity_tags.Ai:        return Tags.Ai;
                    case entity_tags.Neutral:   return Tags.Neutral;
                    case entity_tags.WorldMesh: return Tags.WorldMesh;
                    case entity_tags.World:     return Tags.World;
                    case entity_tags.Button:    return Tags.Button;
                    default:                    return "";
            }}
            public void EnumFromTag(string tag){
                switch (tag){
                    case Tags.Player:    this.tag = entity_tags.Player; return;
                    case Tags.Ai:        this.tag = entity_tags.Ai; return;
                    case Tags.Neutral:   this.tag = entity_tags.Neutral; return;
                    case Tags.WorldMesh: this.tag = entity_tags.WorldMesh; return;
                    case Tags.World:     this.tag = entity_tags.World; return;
                    case Tags.Button:    this.tag = entity_tags.Button; return;
                    default:             this.tag = entity_tags.Neutral; return;
            }}
        }
        public class spawned_entity_state : entity_state{
            public bool spawned_from_above;
        }
        public static EntityController DecompileEntity(entity_state state, bool no_skipping){

            RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 1");
            // get parent by networked id
            EntityController parent = null;
            if (state.parent_controller_id != 0xffff){
                parent = NetworkedEntities[state.parent_controller_id];
                RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 1a");
                if (parent == null && !no_skipping)
                    return null;
            }
            RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 2");
            // then if this object is attached, we use its parent's networked id and index the child we're attached to
            Transform parent_transform = null;
            if (state.parent_transform_id != 0xffff && state.parent_transform_index != 0xffff){
                EntityController transform_parent = NetworkedEntities[state.parent_controller_id];

                RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 2a");
                if (transform_parent == null){
                    // then we'll have to push this entry to be back of the list!!
                    RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 2b");
                    if (!no_skipping) return null;
                } else{
                    // otherwise we found the target and we can iterate through child nodes to get target
                    int child_index = 0;
                    RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 2c");
                    Transform Traverse(Transform current){
                        foreach (Transform child in current){
                            if (child_index == state.parent_transform_index)
                                return child;

                            child_index += 1;
                            Transform result = Traverse(child);
                            if (result != null) return result;
                        }
                        return null;
                    }
                    parent_transform = Traverse(transform_parent.gameObject.transform);
                    // if we cant find anything then theres nothing we can really do about it, but this shouldn't happen? we'd be more likely to find the wrong child than none at all
                    RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 2d");
                    if (parent_transform == null) RCMManager.Log($"[Co-op] could not resolve child node to attach sync'd entity to: '{state.entity_id}', transform parent: '{transform_parent.entityId}'");
            }}

            //RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 3");
            //EntityController result;
            //if (state is spawned_entity_state spawned_state)
            //result = Reverse_InstantiateEntity.Original(
            //    state.entity_id,
            //    new Vector3(spawned_state.pos_x, spawned_state.pos_y, spawned_state.pos_z),
            //    parent,
            //    spawned_state.TagFromEnum(),
            //    $"[{spawned_state.network_id}] {spawned_state.entity_id}",
            //    parent_transform,
            //    UnitRole.None,
            //    spawned_state.spawned_from_above,
            //    "co-op sync"
            //);
            //else result = Reverse_InstantiateEntity.Original(
            //    state.entity_id,
            //    new Vector3(state.pos_x, state.pos_y, state.pos_z),
            //    parent,
            //    state.TagFromEnum(),
            //    $"[{state.network_id}] {state.entity_id}",
            //    parent_transform,
            //    UnitRole.None,
            //    false,
            //    "co-op sync"
            //);

            //RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 4");
            RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 3 - START");
            EntityController result = null;
            try
            {
                // Log state information
                RCMManager.Log($"[Co-op] state type: {state.GetType().Name}");
                RCMManager.Log($"[Co-op] state.network_id: {state.network_id}");
                RCMManager.Log($"[Co-op] state.entity_id: {state.entity_id}");
                RCMManager.Log($"[Co-op] state.pos: ({state.pos_x},{state.pos_y},{state.pos_z})");
                // Log parent information
                RCMManager.Log($"[Co-op] parent: {parent} parent_transform: {parent_transform}");

                // Log tag
                string tag = state is spawned_entity_state sss ? sss.TagFromEnum() : state.TagFromEnum();
                RCMManager.Log($"[Co-op] tag: '{tag}'");

                // Log EntityBalancingStore lookup
                RCMManager.Log($"[Co-op] Attempting EntityBalancingStore.PrefabLocation for entityId: '{state.entity_id}'");
                string prefabLocation = EntityBalancingStore.PrefabLocation(state.entity_id);
                RCMManager.Log($"[Co-op] prefabLocation result: '{prefabLocation}'");

                // Log Resources.Load attempt
                if (!string.IsNullOrEmpty(prefabLocation))
                {
                    RCMManager.Log($"[Co-op] Attempting Resources.Load: '{prefabLocation}'");
                    UnityEngine.Object resourceObj = Resources.Load(prefabLocation);
                    RCMManager.Log($"[Co-op] Resources.Load result: {resourceObj}");
                    if (resourceObj == null)
                    {
                        RCMManager.Log($"[Co-op] ERROR: Resources.Load returned null!");
                        return null;
                    }
                }
                else
                {
                    RCMManager.Log($"[Co-op] ERROR: prefabLocation is empty/null!");
                    return null;
                }

                if (state is spawned_entity_state spawned_state3)
                    result = Patch_InstantiateEntity_Stub.Original(
                        state.entity_id,
                        new Vector3(spawned_state3.pos_x, spawned_state3.pos_y, spawned_state3.pos_z),
                        parent,
                        spawned_state3.TagFromEnum(),
                        $"[{spawned_state3.network_id}] {spawned_state3.entity_id}",
                        parent_transform,
                        UnitRole.None,
                        spawned_state3.spawned_from_above,
                        "co-op sync"
                    );
                else
                    result = Patch_InstantiateEntity_Stub.Original(
                        state.entity_id,
                        new Vector3(state.pos_x, state.pos_y, state.pos_z),
                        parent,
                        state.TagFromEnum(),
                        $"[{state.network_id}] {state.entity_id}",
                        parent_transform,
                        UnitRole.None,
                        false,
                        "co-op sync"
                    );

                RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 4 - InstantiateEntity returned: {result}");
            }
            catch (Exception ex)
            {
                RCMManager.Log($"[Co-op] EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                RCMManager.Log($"[Co-op] Stack trace: {ex.StackTrace}");
                throw;
            }







            if (result != null){
                RecievedSpawn(result, state.network_id);
                result.gameObject.transform.eulerAngles = new Vector3(
                    result.gameObject.transform.eulerAngles.x,
                    state.rot_yaw,
                    result.gameObject.transform.eulerAngles.z
                );
                result.gameObject.transform.localScale = Vector3.one * state.scale;
            }
            RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 5");
            return result;
        }
        public static void DecompileEntities(List<entity_state> entities){

            RCMManager.Log($"[Co-op] beginning entities list decompile... {entities.Count} total");
            List<entity_state> retry_1 = new();
            foreach (var entity in entities)
                if (DecompileEntity(entity, false) == null)
                    retry_1.Add(entity);

            RCMManager.Log($"[Co-op] stage 2 entities list decompile... {retry_1.Count} retrying");
            List<entity_state> retry_2 = new();
            foreach (var entity in retry_1)
                if (DecompileEntity(entity, false) == null)
                    retry_2.Add(entity);

            RCMManager.Log($"[Co-op] stage 3 entities list decompile... {retry_2.Count} retrying");
            foreach (var entity in retry_2)
                if (DecompileEntity(entity, false) == null)
                    RCMManager.Log($"[Co-op] couldn't create sync'd entity after 3rd attempt: {entity.entity_id}, id {entity.network_id}");
        }
        static entity_state CompileEntity(EntityController entity, ushort id, entity_state state){
            if (string.IsNullOrWhiteSpace(entity.entityId)){
                RCMManager.Log($"[Co-op] cant properly serialize entity as it has no entity ID, name: {entity.gameObject.name}");
            }
            state.entity_id = entity.entityId;
            state.network_id = id;

            // noting that this can output 0xffff if cant find parent network id
            if (entity.Parent != null)
                state.parent_controller_id = EntitiesManager.IdFromEntity(entity.Parent);
            else state.parent_controller_id = (ushort)0xffff;

            // for now, check for transform parent, if there is we'll try to identify and then just get the child transform index
            if (entity.gameObject.transform.parent != null){
                EntityController transform_par_entity = entity.gameObject.transform.parent.GetComponentInParent<EntityController>();
                if (transform_par_entity != null){

                    ushort transform_parent_id = EntitiesManager.IdFromEntity(transform_par_entity);
                    if (transform_parent_id != 0xffff){

                        int child_index = 0;
                        int Traverse(Transform current){
                            foreach (Transform child in current){
                                if (entity.gameObject.transform.parent == child)
                                    return child_index;

                                child_index += 1;
                                int result = Traverse(child);
                                if (result != -1) return result;
                            }
                            return -1;
                        }

                        int resulting_child_index = Traverse(transform_par_entity.gameObject.transform);
                        if (resulting_child_index != -1 && resulting_child_index < 0xffff){
                            state.parent_transform_id = transform_parent_id;
                            state.parent_transform_index = (ushort)resulting_child_index;
                        }else{
                            RCMManager.Log($"[Co-op] when serializing entity list, unable to find child transform index that we're attached to, entity: '{entity.entityId}', transform parent: '{transform_par_entity.entityId}'");
                            state.parent_transform_id = 0xffff;
                            state.parent_transform_index = 0xffff;
                        }
                    }else{
                        RCMManager.Log($"[Co-op] when serializing entity list, unable to get networked id from parent transform entity, entity: '{entity.entityId}', transform parent: '{transform_par_entity.entityId}'");
                        state.parent_transform_id = 0xffff;
                        state.parent_transform_index = 0xffff;
                    }
                }else{
                    RCMManager.Log($"[Co-op] when serializing entity list, unable to find find entitycontroller component in any parent, entity: '{entity.entityId}'");
                    state.parent_transform_id = 0xffff;
                    state.parent_transform_index = 0xffff;
                }
            }else{
                state.parent_transform_id = 0xffff;
                state.parent_transform_index = 0xffff;
            }
            state.pos_x = entity.gameObject.transform.position.x;
            state.pos_y = entity.gameObject.transform.position.y;
            state.pos_z = entity.gameObject.transform.position.z;
            state.rot_yaw = entity.gameObject.transform.eulerAngles.y;
            state.scale = entity.gameObject.transform.localScale.x;
            state.EnumFromTag(entity.gameObject.tag);
            return state;
        }
        public static List<entity_state> CompileEntities(){
            List<entity_state> entities = new();
            foreach (var entity_struct in EntitiesManager.IterateNetworkedEntities()){
                uint id = entity_struct.Key;
                EntityController entity = entity_struct.Value;
                entities.Add(CompileEntity(entity, (ushort)id, new entity_state()));
            }

            return entities;
        }
    }
}
