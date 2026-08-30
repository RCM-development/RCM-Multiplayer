using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PimDeWitte.UnityMainThreadDispatcher;
using RCM_Coop.Network.Entities;
using RCM_Coop.Network.Helpers;
using TestMod;
using UnityEngine;
using static RCM_Coop.CoopManager;
using static RCM_Coop.Network.GameProtocols;
namespace RCM_Coop.Network{

    internal class GameClient : NetworkedGame{
        public GameClient(Session session){
            this.session = session;
            players = new PlayerManager();
            session.data_recieved_callback = RouteOnDataRecieved;
            session.connection_terminated_callback = RouteOnConnectionTerminated;
            session.connection_opened_callback = RouteOnConnectionOpened;
        }

        void RouteOnDataRecieved(byte[] data, TcpClient client){
            UnityMainThreadDispatcher.Enqueue(() => { OnDataReceived(data, client); });
        }
        void RouteOnConnectionTerminated(TcpClient client){
            UnityMainThreadDispatcher.Enqueue(() => { OnConnectionTerminated(client); });
        }
        void RouteOnConnectionOpened(TcpClient client){
            UnityMainThreadDispatcher.Enqueue(() => { OnConnectionOpened(client); });
        }
        void OnDataReceived(byte[] data, TcpClient client){
            //RCMManager.Log($"[Co-op] Received {data.Length} bytes from {client.Client.RemoteEndPoint}");
            try{
                foreach (var packet in DeserializePackets(data))
                    switch (packet){
                        case ServerJoinResponseOk e:
                            RCMManager.Log($"[Co-op] accepted into session, waiting for host...");
                            players.AddOurselves(e.player_id, submited_color);
                            // enter awaiting host screen
                            break;
                        case ServerJoinResponseFailed e:
                            switch (e.join_error){
                                case ServerJoinResponseFailed.JoinError.username_taken: RCMManager.Log($"[Co-op] join failed, reason: username is taken"); break;
                                case ServerJoinResponseFailed.JoinError.session_full: RCMManager.Log($"[Co-op] join failed, reason: session is full"); break;
                                case ServerJoinResponseFailed.JoinError.already_connected: RCMManager.Log($"[Co-op] join failed, reason: already connected or poor TCP connection"); break;
                                case ServerJoinResponseFailed.JoinError.bad_password:
                                    RCMManager.Log($"[Co-op] join failed, reason: bad password");
                                    break;
                                case ServerJoinResponseFailed.JoinError.rejected:
                                    RCMManager.Log($"[Co-op] join failed, reason: unspecified");
                                    break;
                            }
                            session.Terminate();
                            break;
                        case ServerPlayerHasJoined e:
                            RCMManager.Log($"[Co-op] player joined: {e.username}");
                            players.AddPlayer(e.username, e.player_id, e.color);
                            break;
                        case ServerPlayerHasLeft e:
                            RCMManager.Log($"[Co-op] player left: {PlayerManager.GetPlayer(e.player_id)?.username}");
                            players.RemovePlayer(e.player_id);
                            break;
                        case ServerFullEntityData e:
                            RCMManager.Log($"[Co-op] recieved entities list");
                            EntitiesManager.RecievedSpawns(e.entities);
                            break;
                        case ServerUnitSpawned e:
                            EntitiesManager.RecievedSpawn(e.entity);
                            break;
                        case ServerUnitDestroyed e:
                            EntitiesManager.RecievedDestroy(e.parent_id, e.originator_id, e.dont_use_destruction_effects);
                            break;
                        case ServerEntitiesPositionUpdate e:
                            EntitiesManager.RecievePositionUpdates(e.positions);
                            break;

                        case ServerUnitActivateSkillPosition e: 
                            if (e.entity != null) Patch_EntityController_ActivateSkill_Vector3.Original(e.entity, e.pos); 
                            break;
                        case ServerUnitActivateSkillTarget e:
                            if (e.entity != null && e.target != null) Patch_EntityController_ActivateSkill_Target.Original(e.entity, e.target); 
                            break;
                        case ServerUnitActivateSkill e:
                            if (e.entity != null) Patch_EntityController_ActivateSkill_Int.Original(e.entity, e.count); 
                            break;
                        case ServerUnitAttackMovePosition e:
                            if (e.entity != null) Patch_EntityController_AttackMove_Vector3.Original(e.entity, e.pos); 
                            break;
                        case ServerUnitAttackMoveTarget e:
                            if (e.entity != null && e.target != null) Patch_EntityController_AttackMove_Target.Original(e.entity, e.target); 
                            break;
                        case ServerUnitAttack e:
                            if (e.entity != null && e.target != null) Patch_EntityController_Attack.Original(e.entity, e.target); 
                            break;
                        case ServerUnitOnReadyToShoot e:
                            if (e.entity != null && e.target != null) Patch_EntityController_OnReadyToShootOnTarget.Original(e.entity, e.target); 
                            break;
                        case ServerUnitFollowTarget e:
                            if (e.entity != null && e.target != null) Patch_EntityController_Follow_Target.Original(e.entity, e.target); 
                            break;
                        case ServerUnitFollowPosition e:
                            if (e.entity != null) Patch_EntityController_Follow_Position.Original(e.entity, e.pos, e.distance); 
                            break;
                        case ServerUnitStop e:
                            if (e.entity != null) Patch_EntityController_Stop.Original(e.entity); 
                            break;
                        case ServerUnitTeleport e:
                            if (e.entity != null) Patch_EntityController_Teleport.Original(e.entity, e.pos, e.dont_trigger_events); 
                            break;
                        case ServerUnitMoveTo e:
                            if (e.entity != null) Patch_EntityController_MoveTo.Original(e.entity, e.pos, e.counts_as_move_command, e.restrictedToHeightLayer, e.clickPositionCell); 
                            break;
                        case ServerUnitRepairArmor e:
                            if (e.entity != null) Patch_EntityController_RepairArmor.Original(e.entity, e.new_shield_value, e.originator, e.dont_trigger_events); 
                            break;
                        case ServerUnitHeal e:
                            if (e.entity != null) Patch_EntityController_Heal.Original(e.entity, e.new_health_value, e.originator, e.dont_trigger_has_healed, e.dont_trigger_being_healed); 
                            break;
                        case ServerUnitChargeShield e:
                            if (e.entity != null) Patch_EntityController_ChargeShield.Original(e.entity, e.new_shield_value, true); 
                            break;
                        case ServerUnitTakeDamage e:
                            if (e.entity != null) Patch_EntityController_TakeDamage.Original(e.entity, e.new_health_value, e.originator, e.dont_trigger_has_damaged, e.ignore_armor); 
                            break;
                        case ServerUnitProduce e:
                            if (e.entity != null) Patch_EntityController_Produce.Original(e.entity, e.instant_production, e.for_free, e.dont_trigger_events); 
                            break;
                        case ServerUnitAbortProduction e:
                            if (e.entity != null) Patch_EntityController_AbortProduction.Original(e.entity); 
                            break;
                        case ServerUnitChargeMana e:
                            RCMManager.Log($"[Co-op] recieved charge mana command: {e.entity.entityId} new value: {e.new_mana_value}, display delta: {e.display_delta}");
                            if (e.entity != null) Patch_EntityController_ChargeMana.Original(e.entity, e.new_mana_value, e.display_delta); 
                            break;
                        case ServerUnitSetStatusEffect e:
                            if (e.entity != null) Patch_EntityController_SetActiveStatusEffect.Original(e.entity, e.statusEffect, e.durationType, e.duration);
                            break;
                        case ServerUnitRemoveActiveStatus e:
                            if (e.entity != null) Patch_EntityController_RemoveStatusEffectFromActiveStatusEffects.Original(e.entity, e.statusEffect);
                            break;
                        case ServerUnitRemoveStatusEffect e:
                            if (e.entity != null) Patch_EntityController_RemoveStatusEffect.Original(e.entity, e.statusEffect);
                            break;
                        case ServerUnitRankUp e:
                            if (e.entity != null) Patch_EntityController_RankUp.Original(e.entity, e.amount);
                            break;

                        case ServerTimeSlow e:
                            Patch_Navigator_SlowDown.Original(true);
                            break;
                        case ServerTimeNormal e:
                            Patch_Navigator_ResetToDefaultSpeed.Original();
                            break;
                        case ServerTimePaused e:
                            Patch_Navigator_Pause.Original();
                            break;
                        case ServerTimeUnpaused e:
                            Patch_Navigator_Unpause.Original();
                            break;

                        case ServerPlacementBegin e:
                            CoopManager.RecievedPlacementIndicator(e);
                            break;
                        case ServerPlacementShockwave e:
                            CoopManager.RecievedPlacementShockwave(e);
                            break;
                        case ServerPlacementReleased e:
                            CoopManager.RecievedReleasePlacementGhost(e.ghost_id);
                            break;
                    }
            } catch (Exception ex){
                RCMManager.Log($"[Co-op] failed to read recieved packets: {ex.Message} callstack: {ex.StackTrace}");
            }
        }
        void OnConnectionTerminated(TcpClient client){
            RCMManager.Log($"[Co-op] Connection terminated with {client.Client.RemoteEndPoint}");

            // TODO: exit client multiplayer mode??
            // - return to main menu or stay in session if game is running
        }
        Color submited_color;
        void OnConnectionOpened(TcpClient client){
            RCMManager.Log($"[Co-op] Connection opened with {client.Client.RemoteEndPoint}, sending join packet");
            submited_color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1f);
            session.SendTCP(new ClientJoinRequest("username123", "password123", submited_color));
        }

        public void SendMapLoadedRequest(){
            RCMManager.Log("sending map loaded request");
            session.SendTCP(new ClientMapLoaded());
        }
        public void SendPacketToInGame(SerializablePacket packet){
            session.SendTCP(packet);
        }
    }
}
