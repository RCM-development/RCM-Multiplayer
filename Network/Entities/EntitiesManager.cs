using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestMod;
using UnityEngine;
using UnityEngine.UIElements;
using static RCM_Coop.CoopManager;
using static RCM_Coop.Network.Entities.EntitySerializer;
using static RCM_Coop.Network.GameProtocols;
using static RCM_Coop.Network.GameProtocols.ServerEntitiesPositionUpdate;
using static RCM_Coop.Network.GameProtocols.ServerFullEntityData;
namespace RCM_Coop.Network.Entities{

    public static class EntitiesManager{


        #region ENTITY POSITION FORCE SYNC
        private static float accumulator = 0f;
        private const float interval = 0.1f; // 100 ms
        public static void Update(){
            if (!CoopManager.IsServerUp()) return;
            accumulator += Time.unscaledDeltaTime;
            if (accumulator < interval) return;
            accumulator -= interval;

            List<EntityPosition> positions = new();
            foreach (var entity_struct in NetworkedEntities.IterateNetworkedEntities()){
                uint id = entity_struct.Key;
                NetworkedEntities.EntityNetworkData entity = entity_struct.Value;
                if (entity.position_updated){
                    NetworkedEntities.WritePositionUpdated(id, false);
                    positions.Add(new EntityPosition() { entity = entity.entity, pos = entity.entity.gameObject.transform.position });
                }
            }
            if (positions.Count > 0){
                SendServerInGamePacket(new ServerEntitiesPositionUpdate(positions));
            }
        }
        public static void NotifyEntityMoved(EntityController entity){
            NetworkedEntities.WritePositionUpdated(entity, true);
        }
        public static void RecievePositionUpdates(List<EntityPosition> positions){
            foreach (var item in positions){
                if (item.entity != null){
                    item.entity.gameObject.transform.position = item.pos;
                    // lock in new coords into the system
                    NetworkedEntities.WriteLastPosition(item.entity, item.pos);
                    item.entity.UpdateCachedPosition();
                    
                }
                else RCMManager.Log($"[Co-op] position sync for entity that doesn't exist.");
            }
        }
        #endregion


        public static PlayerManager.Player GetEntityPlayer(EntityController entity){
            if (entity != null){
                var val = NetworkedEntities.EntityFromEntity(entity);
                bool is_engi = (entity.Role & UnitRole.Engineer) > 0;
                if (is_engi)
                {
                    RCMManager.Log($"[Co-op] NOTE: engi get player called, owner_id={val.owning_player}");
                }
                return PlayerManager.GetPlayer(val.owning_player);
            }
            return null;
        }
        public static bool IsEntityOwnedByUs(EntityController entity){
            if (!CoopManager.IsSessionUp()) return true;

            PlayerManager.Player entity_owner = EntitiesManager.GetEntityPlayer(entity);
            byte owner_id = 255;
            if (entity_owner != null) owner_id = entity_owner.id;


            bool is_engi = (entity.Role & UnitRole.Engineer) > 0;
            if (is_engi)
            {
                RCMManager.Log($"[Co-op] NOTE: engi ownership status checked, has_player={entity_owner != null}, owner_id={owner_id}, our_id={PlayerManager.GetOurPlayerID()}");
            }

            // basically if our player ID matches or unit has no known owner and we're server host
            return ((owner_id == PlayerManager.GetOurPlayerID() && owner_id != 255) || (owner_id == 255 && CoopManager.IsServerUp()));
        }

        #region ENTITY CREATION/DELETION
        public static void EntitySpawned(EntityController entity, bool called_from_above, byte owning_player = 255){
            if (string.IsNullOrWhiteSpace(entity.entityId)){
                RCMManager.Log("[Co-op] tried initiate sync of entity with no entity ID, cant replicate across... for now.");
                return;
            }
            bool is_engi = (entity.OriginEntity?.Role & UnitRole.Engineer) > 0;
            if (is_engi)
            {
                RCMManager.Log("[Co-op] NOTE: ENGI-CREATED-BUILDING SPAWNED WITH PLAYER ID: " + owning_player);
            }
            // find new owner if we didn't provide one
            if (owning_player == 255){
                // see if the creator of this entity has an owner
                PlayerManager.Player owner = GetEntityPlayer(entity.OriginEntity);
                if (owner != null)
                     owning_player = owner.id;
                else owning_player = PlayerManager.GetHostPlayerID();
            }
            if (is_engi)
            {
                RCMManager.Log("[Co-op] NOTE: ENGI-CREATED-BUILDING NOW HAS PLAYER ID: " + owning_player);
            }

            if (!NetworkedEntities.AllocEntity(entity, owning_player)) return;

            // replicate creation event to clients
            if (IsServerUp()){
                spawned_entity_state state = new();
                EntitySerializer.CompileEntity(entity, NetworkedEntities.IdFromEntity(entity), owning_player, state);
                state.spawned_from_above = called_from_above;
                SendServerInGamePacket(new ServerUnitSpawned(state));
            }
        }
        public static void EntityDestroyed(EntityController entity, bool withoutTriggeringDestructionActions, EntityController originator){
           ushort entity_id = NetworkedEntities.IdFromEntity(originator);
            if (!NetworkedEntities.DeallocEntity(entity)) return;
            if (IsServerUp()){
                ushort originator_id = NetworkedEntities.IdFromEntity(originator);
                if (originator != null && originator_id == 0xffff) RCMManager.Log($"[Co-op] entity destroyed: {entity.entityId} id: {entity_id} by: {originator.entityId}, but couldn't match destroyer up to networked id...");
                SendServerInGamePacket(new ServerUnitDestroyed(entity_id, originator_id, withoutTriggeringDestructionActions));
            }
        }
        #endregion

        public static void RecievedSpawn(entity_state entity)
        {
            EntitySerializer.DecompileEntity(entity, true);
        }
        public static void RecievedSpawns(List<entity_state> entities)
        {
            EntitySerializer.DecompileEntities(entities);
        }
        public static void RecievedDestroy(ushort entity_id, ushort originator_id, bool withoutTriggeringDestructionActions){
            NetworkedEntities.EntityNetworkData entity = NetworkedEntities.EntityFromId(entity_id);
            NetworkedEntities.EntityNetworkData originator = NetworkedEntities.EntityFromId(originator_id);

            if (entity_id != 0xffff && entity.entity == null) RCMManager.Log($"[Co-op] entity id destroyed:{entity_id} but couldn't find target network id in our list");
            if (originator_id != 0xffff && originator.entity == null) RCMManager.Log($"[Co-op] entity {entity.entity.entityId} destroyed by originator:{originator_id} but couldn't find target network id in our list");

            Patch_EntityController_Destroy.Original(entity.entity, withoutTriggeringDestructionActions, originator.entity);
            EntityDestroyed(entity.entity, false, null);
        }



    }
}
