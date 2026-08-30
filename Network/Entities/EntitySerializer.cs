using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestMod;
using UnityEngine;
using static RCM_Coop.CoopManager;
namespace RCM_Coop.Network.Entities{

    public class EntitySerializer{
        public class entity_state{
            public string entity_id;
            public ushort network_id;
            public byte owning_player;
            public ushort parent_controller_id;
            public ushort parent_transform_id;
            public ushort parent_transform_index;
            public Vector3 pos;
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

            //RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 1");
            // get parent by networked id
            EntityController parent = null;
            if (state.parent_controller_id != 0xffff){
                parent = NetworkedEntities.EntityFromId(state.parent_controller_id).entity;
                //RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 1a");
                if (parent == null && !no_skipping)
                    return null;
            }
            //RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 2");
            // then if this object is attached, we use its parent's networked id and index the child we're attached to
            Transform parent_transform = null;
            if (state.parent_transform_id != 0xffff && state.parent_transform_index != 0xffff){
                EntityController transform_parent = NetworkedEntities.EntityFromId(state.parent_controller_id).entity;

                //RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 2a");
                if (transform_parent == null){
                    // then we'll have to push this entry to be back of the list!!
                    //RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 2b");
                    if (!no_skipping) return null;
                } else{
                    // otherwise we found the target and we can iterate through child nodes to get target
                    int child_index = 0;
                    //RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 2c");
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
                    //RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 2d");
                    if (parent_transform == null) RCMManager.Log($"[Co-op] could not resolve child node to attach sync'd entity to: '{state.entity_id}', transform parent: '{transform_parent.entityId}'");
            }}

            //RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 4");
            //RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 3 - START");
            EntityController result = null;
            try
            {
                // Log state information
                //RCMManager.Log($"[Co-op] state type: {state.GetType().Name}");
                //RCMManager.Log($"[Co-op] state.network_id: {state.network_id}");
                //RCMManager.Log($"[Co-op] state.entity_id: {state.entity_id}");
                //RCMManager.Log($"[Co-op] state.pos: ({state.pos.p},{state.pos_y},{state.pos_z})");
                //// Log parent information
                //RCMManager.Log($"[Co-op] parent: {parent} parent_transform: {parent_transform}");

                // Log tag
                //string tag = state is spawned_entity_state sss ? sss.TagFromEnum() : state.TagFromEnum();
                //RCMManager.Log($"[Co-op] tag: '{tag}'");

                // Log EntityBalancingStore lookup
                //RCMManager.Log($"[Co-op] Attempting EntityBalancingStore.PrefabLocation for entityId: '{state.entity_id}'");
                string prefabLocation = EntityBalancingStore.PrefabLocation(state.entity_id);
                //RCMManager.Log($"[Co-op] prefabLocation result: '{prefabLocation}'");

                // Log Resources.Load attempt
                if (!string.IsNullOrEmpty(prefabLocation)){
                    //RCMManager.Log($"[Co-op] Attempting Resources.Load: '{prefabLocation}'");
                    UnityEngine.Object resourceObj = Resources.Load(prefabLocation);
                    //RCMManager.Log($"[Co-op] Resources.Load result: {resourceObj}");
                    if (resourceObj == null){
                        RCMManager.Log($"[Co-op] ERROR: Resources.Load returned null!");
                        return null;
                    }
                }else{
                    RCMManager.Log($"[Co-op] ERROR: prefabLocation is empty/null!");
                    return null;
                }

                if (state is spawned_entity_state spawned_state3)
                    result = Patch_InstantiateEntity_Stub.PrefixedOriginal(
                        spawned_state3.entity_id,
                        spawned_state3.pos,
                        parent,
                        spawned_state3.TagFromEnum(),
                        $"[{spawned_state3.network_id}] {spawned_state3.entity_id}",
                        parent_transform,
                        UnitRole.None,
                        spawned_state3.spawned_from_above,
                        $"COOPSYNC NETWORKED_ID='{spawned_state3.network_id}' OWNER_ID='{spawned_state3.owning_player}'"
                    );
                else
                    result = Patch_InstantiateEntity_Stub.PrefixedOriginal(
                        state.entity_id,
                        state.pos,
                        parent,
                        state.TagFromEnum(),
                        $"[{state.network_id}] {state.entity_id}",
                        parent_transform,
                        UnitRole.None,
                        false,
                        $"COOPSYNC NETWORKED_ID='{state.network_id}' OWNER_ID='{state.owning_player}'"
                    );

                //RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 4 - InstantiateEntity returned: {result}");
            }
            catch (Exception ex)
            {
                RCMManager.Log($"[Co-op] EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                RCMManager.Log($"[Co-op] Stack trace: {ex.StackTrace}");
                throw;
            }

            if (result != null){
                result.gameObject.transform.eulerAngles = new Vector3(
                    result.gameObject.transform.eulerAngles.x,
                    state.rot_yaw,
                    result.gameObject.transform.eulerAngles.z
                );
                result.gameObject.transform.localScale = Vector3.one * state.scale;
            }
            //RCMManager.Log($"[Co-op] decomp [{state.network_id}] checkpoint 5");
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
        public static entity_state CompileEntity(EntityController entity, ushort id, byte owning_player, entity_state state) {
            if (string.IsNullOrWhiteSpace(entity.entityId)) {
                RCMManager.Log($"[Co-op] cant properly serialize entity as it has no entity ID, name: {entity.gameObject.name}");
            }
            state.entity_id = entity.entityId;
            state.network_id = id;
            state.owning_player = owning_player;

            // noting that this can output 0xffff if cant find parent network id
            if (entity.Parent != null)
                state.parent_controller_id = NetworkedEntities.IdFromEntity(entity.Parent);
            else state.parent_controller_id = (ushort)0xffff;

            // for now, check for transform parent, if there is we'll try to identify and then just get the child transform index
            if (entity.gameObject.transform.parent != null) {
                EntityController transform_par_entity = entity.gameObject.transform.parent.GetComponentInParent<EntityController>();
                if (transform_par_entity != null) {

                    ushort transform_parent_id = NetworkedEntities.IdFromEntity(transform_par_entity);
                    if (transform_parent_id != 0xffff) {

                        int child_index = 0;
                        int Traverse(Transform current) {
                            foreach (Transform child in current) {
                                if (entity.gameObject.transform.parent == child)
                                    return child_index;

                                child_index += 1;
                                int result = Traverse(child);
                                if (result != -1) return result;
                            }
                            return -1;
                        }

                        int resulting_child_index = Traverse(transform_par_entity.gameObject.transform);
                        if (resulting_child_index != -1 && resulting_child_index < 0xffff) {
                            state.parent_transform_id = transform_parent_id;
                            state.parent_transform_index = (ushort)resulting_child_index;
                        } else {
                            RCMManager.Log($"[Co-op] when serializing entity list, unable to find child transform index that we're attached to, entity: '{entity.entityId}', transform parent: '{transform_par_entity.entityId}'");
                            state.parent_transform_id = 0xffff;
                            state.parent_transform_index = 0xffff;
                        }
                    } else {
                        RCMManager.Log($"[Co-op] when serializing entity list, unable to get networked id from parent transform entity, entity: '{entity.entityId}', transform parent: '{transform_par_entity.entityId}'");
                        state.parent_transform_id = 0xffff;
                        state.parent_transform_index = 0xffff;
                    }
                } else {
                    RCMManager.Log($"[Co-op] when serializing entity list, unable to find find entitycontroller component in any parent, entity: '{entity.entityId}'");
                    state.parent_transform_id = 0xffff;
                    state.parent_transform_index = 0xffff;
                }
            } else {
                state.parent_transform_id = 0xffff;
                state.parent_transform_index = 0xffff;
            }
            state.pos = entity.gameObject.transform.position;

            state.rot_yaw = entity.gameObject.transform.eulerAngles.y;
            state.scale = entity.gameObject.transform.localScale.x;
            state.EnumFromTag(entity.gameObject.tag);
            return state;
        }
        public static List<entity_state> CompileEntities(){
            List<entity_state> entities = new();
            foreach (var entity_struct in NetworkedEntities.IterateNetworkedEntities()){
                uint id = entity_struct.Key;
                entities.Add(CompileEntity(entity_struct.Value.entity, (ushort)id, entity_struct.Value.owning_player, new entity_state()));
            }

            return entities;
        }
    }
}
