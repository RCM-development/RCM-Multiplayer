

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using Microsoft.Win32;
using PimDeWitte.UnityMainThreadDispatcher;
using RCM_Coop.Network;
using RCM_Coop.Network.Entities;
using Shapes;
using SmartTutorial;
using TestMod;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Networking.Types;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static EntityIdentifier;
using static RCM_Coop.Network.GameProtocols;
using static RCM_Coop.Network.PlayerManager;
namespace RCM_Coop {

    [BepInDependency(RCMManager.IDENTIFIER, BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(IDENTIFIER, "Co-op Plugin", "1.0.0.0")]
    internal class CoopManager : BaseUnityPlugin {
        const string IDENTIFIER = "RCM.plugins.coop";
        static RCMModUI mod;
        public static CoopManager coop;
        private void Awake() {
            new Harmony(IDENTIFIER).PatchAll();
            coop = this;
            DontDestroyOnLoad(this.gameObject);
            Chainloader.ManagerObject.hideFlags = HideFlags.HideAndDontSave;

            RCMManager.ConnectMod("Co-op").ContinueWith(t => {
                mod = t.Result;

                UpdateUI();

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        private void Update() {
            UnityMainThreadDispatcher.Update();
            EntitiesManager.Update();
        }
        static void UpdateUI() {
            mod.ClearFields();
            if (is_connecting)
            {
                mod.CreateLabelField("connecting...");
            }
            else if (session == null)
            {
                mod.CreateButtonField("connect", BeginConnect);
            }
            else
            {
                if (session.is_server)
                {
                    mod.CreateLabelField("running as server");
                }
                else
                {
                    mod.CreateLabelField("running as client");
                    mod.CreateButtonField("request data", RequestEntityData);
                }
                mod.CreateButtonField("disconnect", BeginDisconnect);
            }

        }

        static bool is_connecting = false;
        static Session session = null;
        static bool is_client => session?.is_server == false;

        static NetworkedGame networked_game = null;
        static async void BeginConnect() {
            is_connecting = true;
            UpdateUI();
            RCMManager.Log("beginning connect");
            session = await Session.StartAutoAsync();
            if (session.is_server)
            {
                networked_game = new GameServer(session);
            }
            else
            {
                networked_game = new GameClient(session);
            }
            is_connecting = false;
            UpdateUI();
        }
        static void BeginDisconnect()
        {
            if (session != null)
            {
                networked_game = null;
                session.Terminate();
                session = null;
                UpdateUI();
            }
        }
        static void RequestEntityData()
        {
            RCMManager.Log("entity button pressed");
            if (is_client && session != null)
                ((GameClient)networked_game).SendMapLoadedRequest();
        }

        public static bool IsServerUp() => (networked_game != null && session != null && !is_client);
        public static bool IsClientUp() => (networked_game != null && session != null && is_client);
        public static bool IsSessionUp() => networked_game != null && session != null;
        public static void SendServerInGamePacket(SerializablePacket packet) {
            if (IsServerUp())
            {
                ((GameServer)networked_game).SendPacketToInGame(packet);
            }
        }
        public static void SendClientInGamePacket(SerializablePacket packet) {
            if (IsClientUp())
            {
                ((GameClient)networked_game).SendPacketToInGame(packet);
            }
        }

        public class ree : Exception {
            public ree() { }
            public ree(string message) : base(message) { }
            public ree(string message, Exception inner) : base(message, inner) { }
        }


        #region map seeds
        // force seed
        [HarmonyPatch(typeof(InitMap), "StartInit")]
        public static class InitMapPatch_StartInit {
            [HarmonyPrefix]
            public static bool Prefix(ref LandscapeGenerator landscapeGenerator, ref LandscapeGenerator fallbackLandscapeGenerator, ref DefaultAiBehaviour ai, ref bool asCoroutine, ref int? landscapeGeneratorSeed, ref bool landscapeGeneratorWasInstantiated) {
                landscapeGeneratorSeed = 1;
                RCMManager.Log($"[Co-op] StartInit Prefix: landscapeGeneratorSeed set to {landscapeGeneratorSeed}");
                return true;
            }
        }
        #endregion

        static bool next_entity_from_above_state = false; // messed up solution to terrible problem
        #region entity spawning stubs
        // stub out entity instantiation
        [HarmonyPatch(typeof(EntityFactory), "InstantiateEntity")] public static class Patch_InstantiateEntity_Stub {
            [HarmonyPrefix] public static bool Prefix(string entityId, Vector3 position, EntityController originEntity, string tag, string name, Transform parentTransform, UnitRole additionalRoles, bool hasBeenCalledFromAbove, string instantiationInfo, ref EntityController __result) {
                next_entity_from_above_state = hasBeenCalledFromAbove;
                if (is_client) {
                    __result = null;
                    return false;
                }
                return true;
            }
            public static EntityController PrefixedOriginal(string entityId, Vector3 position, EntityController originEntity, string tag, string name, Transform parentTransform, UnitRole additionalRoles, bool hasBeenCalledFromAbove, string instantiationInfo) {
                next_entity_from_above_state = hasBeenCalledFromAbove;
                return Original(entityId, position, originEntity, tag, name, parentTransform, additionalRoles, hasBeenCalledFromAbove, instantiationInfo);
            }
            [HarmonyReversePatch] private static EntityController Original(string entityId, Vector3 position, EntityController originEntity, string tag, string name, Transform parentTransform, UnitRole additionalRoles, bool hasBeenCalledFromAbove, string instantiationInfo) {
                throw new NotImplementedException("Stub for reverse patch");
            }
        }
        // this stubs out all entities created at map generation
        [HarmonyPatch(typeof(EntityFactory), "InstantiateEntityFromPrefab")] public static class Patch_InstantiateEntityFromPrefab_Stub {
            [HarmonyPrefix] public static bool Prefix(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale, string tag, string instantiationInfo) {
                if (is_client) return false;
                return true;
            }
            [HarmonyReversePatch] public static void Original(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale, string tag, string instantiationInfo) {
                throw new NotImplementedException("Stub for reverse patch");
            }
        }


        // this stubs out engineer and entities spawned at start via hacks
        [HarmonyPatch(typeof(SetupPlayerStart), "SpawnStartEntities")] public static class Patch_SetupPlayerStart_SpawnStartEntities {
            [HarmonyPrefix]
            public static bool Prefix() {
                if (is_client) return false;
                return true;
            } }
        // stub out extra object spawning
        [HarmonyPatch(typeof(SpawnObject), "RunForEveryIdentifiedEntity")] public static class Patch_SpawnObject_RunForEveryIdentifiedEntity {
            [HarmonyPrefix]
            public static bool Prefix(SpawnObject __instance, EntityController entity, EventPayload payload, int index) {
                // if the action wants to spawn an entity then we say no since network will sync it anyway...
                if (__instance.initEntityController && is_client) return false;
                return true;
            }
        }
        #endregion

        #region game over stubs
        // patch out game losing conditions
        [HarmonyPatch(typeof(Game), "Lose")]
        public static class Patch_Game_Lose {
            [HarmonyPrefix]
            public static bool Prefix() {
                RCMManager.Log($"Game.Lose hit:  {new StackTrace(true).ToString()}");
                if (is_client) {
                    return false;
                }
                return true;
            } }
        [HarmonyPatch(typeof(FinishLevel), "Lose_Static")]
        public static class Patch_FinishLevel_Lose_Static {
            [HarmonyPrefix]
            public static bool Prefix() {
                RCMManager.Log($"FinishLevel.Lose_Static hit:  {new StackTrace(true).ToString()}");
                if (is_client) {
                    return false;
                }
                return true;
            } }
        [HarmonyPatch(typeof(FinishLevel), "Win_Static")]
        public static class Patch_FinishLevel_Win_Static {
            [HarmonyPrefix]
            public static bool Prefix() {
                RCMManager.Log($"FinishLevel.Win_Static hit:  {new StackTrace(true).ToString()}");
                if (is_client) {
                    return false;
                }
                return true;
            } }
        #endregion


        //[HarmonyPatch(typeof(EntityController), "OnHasBeenInstantiated")] public static class Patch_EntityController_OnHasBeenInstantiated {
        //    [HarmonyPrefix] public static bool Prefix(EntityController __instance, bool hasBeenCalledFromAbove) {
        //    }
        //}

        static bool has_run_initial_engi = false;
        static bool block_next_init = false;
        [HarmonyPatch(typeof(EntityController), "Init")] public static class Patch_EntityController_Init {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, EntityController originEntity) {
                if (block_next_init) return true;

                // skip entity management if not server, as client recieves all relevant stuff via the sync'd session data
                if (!is_client) {
                    __instance.OriginEntity = originEntity; // important that we do this before running init so our other stuff can access this value

                    EntitiesManager.EntitySpawned(__instance, next_entity_from_above_state);

                    // TODO: TEMP SOLUTION I KNOW ITS DOODY
                    // spawn in all the other engineers too
                    if (!has_run_initial_engi && (EntityBalancingStore.UnitRoles(__instance.entityId) & UnitRole.Engineer) > 0) {
                        has_run_initial_engi = true;
                        foreach (var player in PlayerManager.AllPlayers()) {
                            if (player.id != PlayerManager.GetHostPlayerID()) {
                                block_next_init = true;
                                EntityController next_engi = EntityFactory.InstantiateEntity(__instance.entityId, __instance.gameObject.transform.position, null, __instance.gameObject.tag, __instance.gameObject.name + " for " + player.username, null, UnitRole.None, false, " co op spawner");
                                block_next_init = false;

                                EntitiesManager.EntitySpawned(next_engi, false, 255, player.id);
                                next_engi.canNotBeSelected = true;
                            }
                        }
                    }
                } else{
                    // check instantiation info for info to insert into 
                    string input = __instance.gameObject.name;
                    // Find the last occurrence of "COOPSYNC"
                    int coopIndex = input.LastIndexOf("COOPSYNC");
                    if (coopIndex >= 0) {
                        int start = coopIndex + "COOPSYNC".Length;
                        int nidStart = input.IndexOf("NETWORKED_ID='", start) + "NETWORKED_ID='".Length;
                        int nidEnd = input.IndexOf("'", nidStart);
                        int oidStart = input.IndexOf("OWNER_ID='", start) + "OWNER_ID='".Length;
                        int oidEnd = input.IndexOf("'", oidStart);
                        uint networkedId = uint.Parse(input.Substring(nidStart, nidEnd - nidStart));
                        uint ownerId = uint.Parse(input.Substring(oidStart, oidEnd - oidStart));

                        // as a failsafe we're going to prevent overwriting, becasue this function probably trips up on units with child entity controller thingos..
                        EntityController exisiting = NetworkedEntities.EntityFromId(networkedId).entity;
                        if (exisiting == null)
                        {
                            RCMManager.Log($"[Co-op] client loading entity: '{input}'");
                            NetworkedEntities.InsertEntity(__instance, (byte)ownerId, 255, networkedId);
                        }
                        else
                        {
                            RCMManager.Log($"[Co-op] we just tried overwriting an entity via gameobject name info. curr_name: '{input}' existing: '{exisiting.gameObject.name}'");
                        }
                    }
                    else RCMManager.Log("[Co-op] entity init'd without any network data in spawn info");

                }

                if (__instance.canNotBeSelected == false && __instance.gameObject.CompareTag("Player")) // __instance.IsControlledByPlayer isn't set yet, so cant use
                    __instance.canNotBeSelected = !EntitiesManager.IsEntityOwnedByUs(__instance);
                return true;
            }
        }
        [HarmonyPatch(typeof(EntityController), "InitChildEntityControllers")] public static class Patch_EntityController_InitChildEntityControllers{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance){

                for (int i = 0; i < __instance.childEntityControllers.Count; i++){
                    EntityController entityController = __instance.childEntityControllers[i];
                    entityController.doNotRegisterGlobally = true;
                    entityController.gameObject.tag = __instance.tag;
                    block_next_init = true;
                    entityController.Init(__instance);
                    block_next_init = false;
                    entityController.SetUniqueEntityId(EntityFactory.NextUniqueEntityId());

                    RCMManager.Log($"[Co-op] just init a child entity controller index: {i}");
                    // if server then we need to call specific function broadcast creation
                    if (IsServerUp()) EntitiesManager.EntitySpawned(__instance, false, (byte)i);
                }

                return false;
            }
        }


        [HarmonyPatch(typeof(EntityController), "Destroy")] public static class Patch_EntityController_Destroy {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, bool withoutTriggeringDestructionActions, EntityController originator) {
                if (is_client) return false; EntitiesManager.EntityDestroyed(__instance, withoutTriggeringDestructionActions, originator); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, bool withoutTriggeringDestructionActions, EntityController originator) { throw new ree("err"); }
        }

        const float MAX_CLIENT_UNIT_DIST_FROM_SERVER = 5.0f;
        [HarmonyPatch(typeof(EntityController), "UpdateCachedPosition")] public static class Patch_EntityController_UpdateCachedPosition {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance) {
                // cleints dont need to track this
                if (!is_client) EntitiesManager.NotifyEntityMoved(__instance);
                // TODO: clamp clients unit's to some max range so  that they never wonder off own their own
                else {
                    // get last known server pos
                    var v = NetworkedEntities.EntityFromEntity(__instance);
                    if (v.entity != null && v.position_updated) {
                        Vector3 offset = __instance.gameObject.transform.position - v.last_server_position;
                        float dist = offset.magnitude;
                        if (dist > MAX_CLIENT_UNIT_DIST_FROM_SERVER) {
                            __instance.gameObject.transform.position = v.last_server_position + offset.normalized * MAX_CLIENT_UNIT_DIST_FROM_SERVER;
                        }
                    }
                }

                return true;
            }
        }


        #region ENTITY STATE PATCHES
        [HarmonyPatch(typeof(EntityController), "ActivateSkill", new Type[] { typeof(Vector3) })] public static class Patch_EntityController_ActivateSkill_Vector3 {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, Vector3 position) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitActivateSkillPosition(__instance, position)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, Vector3 position) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "ActivateSkill", new Type[] { typeof(EntityController) })] public static class Patch_EntityController_ActivateSkill_Target {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, EntityController target) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitActivateSkillTarget(__instance, target)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, EntityController target) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "ActivateSkill", new Type[] { typeof(int?) })] public static class Patch_EntityController_ActivateSkill_Int {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, int? numberOfUnactivatedStatusFlagsInGroup) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitActivateSkill(__instance, numberOfUnactivatedStatusFlagsInGroup)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, int? numberOfUnactivatedStatusFlagsInGroup) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "AttackMove", new Type[] { typeof(Vector3) })] public static class Patch_EntityController_AttackMove_Vector3 {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, Vector3 position) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitAttackMovePosition(__instance, position)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, Vector3 position) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "AttackMove", new Type[] { typeof(EntityController) })] public static class Patch_EntityController_AttackMove_Target {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, EntityController entity) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitAttackMoveTarget(__instance, entity)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, EntityController entity) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "Attack")] public static class Patch_EntityController_Attack {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, EntityController entity) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitAttack(__instance, entity)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, EntityController entity) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "OnReadyToShootOnTarget")] public static class Patch_EntityController_OnReadyToShootOnTarget {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, EntityController target) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitOnReadyToShoot(__instance, target)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, EntityController target) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "Follow", new Type[] { typeof(EntityController) })] public static class Patch_EntityController_Follow_Target {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, EntityController entity) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitFollowTarget(__instance, entity)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, EntityController entity) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "Follow", new Type[] { typeof(Vector3), typeof(float) })] public static class Patch_EntityController_Follow_Position {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, Vector3 position, float distance) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitFollowPosition(__instance, position, distance)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, Vector3 position, float distance) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "Stop")] public static class Patch_EntityController_Stop {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitStop(__instance)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "Teleport")] public static class Patch_EntityController_Teleport {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, Vector3 destination, bool doNotTriggerTeleportEvents) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitTeleport(__instance, destination, doNotTriggerTeleportEvents)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, Vector3 destination, bool doNotTriggerTeleportEvents) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "MoveTo", new Type[] { typeof(Vector3), typeof(bool), typeof(HeightLayer?), typeof(Vector2Int?) })] public static class Patch_EntityController_MoveTo {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, Vector3 destination, bool countsAsMoveCommand, HeightLayer? restrictedToHeightLayer, Vector2Int? clickPositionCell) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitMoveTo(__instance, destination, countsAsMoveCommand, restrictedToHeightLayer, clickPositionCell)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, Vector3 destination, bool countsAsMoveCommand, HeightLayer? restrictedToHeightLayer, Vector2Int? clickPositionCell) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "RepairArmor")] public static class Patch_EntityController_RepairArmor {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, float amount, EntityController originator, bool doNotFireOnHasRepairedArmor) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitRepairArmor(__instance, amount, originator, doNotFireOnHasRepairedArmor)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, float amount, EntityController originator, bool doNotFireOnHasRepairedArmor) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "Heal")] public static class Patch_EntityController_Heal {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, float amount, EntityController originator, bool doNotFireOnHasHealed, bool doNotFireOnBeingHealed) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitHeal(__instance, amount, originator, doNotFireOnHasHealed, doNotFireOnBeingHealed)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, float amount, EntityController originator, bool doNotFireOnHasHealed, bool doNotFireOnBeingHealed) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "ChargeShield")] public static class Patch_EntityController_ChargeShield {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, float amount, bool displayDeltaInBar) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitChargeShield(__instance, amount)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, float amount, bool displayDeltaInBar) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "TakeDamage")] public static class Patch_EntityController_TakeDamage {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, float amount, EntityController originator, bool doNotFireOnHasDealtDamage, bool ignoreArmor) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitTakeDamage(__instance, amount, originator, doNotFireOnHasDealtDamage, ignoreArmor)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, float amount, EntityController originator, bool doNotFireOnHasDealtDamage, bool ignoreArmor) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "Produce")] public static class Patch_EntityController_Produce {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, bool instantProduction, bool forFree, bool doNotTriggerHasProducedEvent) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitProduce(__instance, instantProduction, forFree, doNotTriggerHasProducedEvent)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, bool instantProduction, bool forFree, bool doNotTriggerHasProducedEvent) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "AbortProduction")] public static class Patch_EntityController_AbortProduction {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance) {
                if (is_client) {
                    SendClientInGamePacket(new ClientUnitAbortProduction(__instance));
                    return false;
                }
                SendServerInGamePacket(new ServerUnitAbortProduction(__instance)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "ChargeMana")] public static class Patch_EntityController_ChargeMana {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, float amount, bool displayDeltaInBar) {
                if (is_client) return false;
                // if increment is small, only send if it ticks over to the next whole number
                if (Math.Floor(__instance.CurrentMana) != Math.Floor(__instance.CurrentMana + amount))
                {
                    SendServerInGamePacket(new ServerUnitChargeMana(__instance, amount, displayDeltaInBar));
                }
                return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, float amount, bool displayDeltaInBar) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "SetActiveStatusEffect")] public static class Patch_EntityController_SetActiveStatusEffect {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, StatusEffect statusEffect, SetStatusEffect.DurationType durationType, float duration) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitSetStatusEffect(__instance, statusEffect, durationType, duration)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, StatusEffect statusEffect, SetStatusEffect.DurationType durationType, float duration) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "RemoveStatusEffectFromActiveStatusEffects")] public static class Patch_EntityController_RemoveStatusEffectFromActiveStatusEffects {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, StatusEffect statusEffect) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitRemoveActiveStatus(__instance, statusEffect)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, StatusEffect statusEffect) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "RemoveStatusEffect")] public static class Patch_EntityController_RemoveStatusEffect {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, StatusEffect statusEffect) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitRemoveStatusEffect(__instance, statusEffect)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, StatusEffect statusEffect) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "RankUp")] public static class Patch_EntityController_RankUp {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, int amount) {
                if (is_client) return false; SendServerInGamePacket(new ServerUnitRankUp(__instance, amount)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, int amount) { throw new ree("err"); }
        }
        #endregion

        #region PAUSE GAME PATCHES
        [HarmonyPatch(typeof(Navigator), "SlowDown")] public static class Patch_Navigator_SlowDown {
            [HarmonyPrefix] public static bool Prefix(bool withMessage) {
                if (is_client) {
                    SendClientInGamePacket(new ClientTimeSlow());
                    return false;
                }
                SendServerInGamePacket(new ServerTimeSlow()); return true;
            }
            [HarmonyReversePatch] public static void Original(bool withMessage) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(Navigator), "ResetToDefaultSpeed")] public static class Patch_Navigator_ResetToDefaultSpeed {
            [HarmonyPrefix] public static bool Prefix() {
                if (is_client) {
                    SendClientInGamePacket(new ClientTimeNormal());
                    return false;
                }
                SendServerInGamePacket(new ServerTimeNormal());
                return true;
            }
            [HarmonyReversePatch] public static void Original() { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(Navigator), "Pause")] public static class Patch_Navigator_Pause {
            [HarmonyPrefix] public static bool Prefix() {
                if (is_client) {
                    SendClientInGamePacket(new ClientTimePaused());
                    return false;
                }
                SendServerInGamePacket(new ServerTimePaused());
                return true;
            }
            [HarmonyReversePatch] public static void Original() { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(Navigator), "Unpause")] public static class Patch_Navigator_Unpause {
            [HarmonyPrefix] public static bool Prefix() {
                if (is_client) {
                    SendClientInGamePacket(new ClientTimeUnpaused());
                    return false;
                }
                SendServerInGamePacket(new ServerTimeUnpaused());
                return true;
            }
            [HarmonyReversePatch] public static void Original() { throw new ree("err"); }
        }
        #endregion


        #region CLIENT EXECUTE COMMAND STUBS
        [HarmonyPatch(typeof(RemoveTagAiAction), "AppendToCommandChain")] public static class Patch_RemoveTagAiAction_AppendToCommandChain {
            [HarmonyPrefix] public static bool Prefix(EntityController entity, List<EntityController> aiCommandEntities, AiEventPayload payload) => !is_client;
        }
        [HarmonyPatch(typeof(MoveAiAction), "MoveToRandomCell")] public static class Patch_MoveAiAction_MoveToRandomCell {
            [HarmonyPrefix] public static bool Prefix(EntityController entity) => !is_client;
        }
        [HarmonyPatch(typeof(MoveAiAction), "MoveToPrecalculatedEntity")] public static class Patch_MoveAiAction_MoveToPrecalculatedEntity {
            [HarmonyPrefix] public static bool Prefix(EntityController entity) => !is_client;
        }
        [HarmonyPatch(typeof(MoveAiAction), "AppendToCommandChain")] public static class Patch_MoveAiAction_AppendToCommandChain {
            [HarmonyPrefix] public static bool Prefix(EntityController entity, List<EntityController> aiCommandEntities, AiEventPayload payload) => !is_client;
        }
        [HarmonyPatch(typeof(GiveUniqueAiGroupIdAiAction), "AppendToCommandChain")] public static class Patch_GiveUniqueAiGroupIdAiAction_AppendToCommandChain {
            [HarmonyPrefix] public static bool Prefix(EntityController entity, List<EntityController> aiCommandEntities, AiEventPayload payload) => !is_client;
        }
        [HarmonyPatch(typeof(GatherAiAction), "AppendToCommandChain")] public static class Patch_GatherAiAction_AppendToCommandChain {
            [HarmonyPrefix] public static bool Prefix(EntityController entity, List<EntityController> aiCommandEntities, AiEventPayload payload) => !is_client;
        }
        [HarmonyPatch(typeof(FollowAiAction), "AppendToCommandChain")] public static class Patch_FollowAiAction_AppendToCommandChain {
            [HarmonyPrefix] public static bool Prefix(EntityController entity, List<EntityController> aiCommandEntities, AiEventPayload payload) => !is_client;
        }

        [HarmonyPatch(typeof(EntityController))][HarmonyPatch("ResumeCommandChainExecution")][HarmonyPatch(new Type[] { })] public static class Patch_EntityController_ResumeCommandChainExecution {
            [HarmonyPrefix] public static bool Prefix() => !is_client;
        }
        [HarmonyPatch(typeof(EntityController), "ExecuteNextCommand")] public static class Patch_EntityController_ExecuteNextCommand {
            [HarmonyPrefix] public static bool Prefix() => !is_client;
        }
        [HarmonyPatch(typeof(ChangeSpeedAiAction), "AppendToCommandChain")] public static class Patch_ChangeSpeedAiAction_AppendToCommandChain {
            [HarmonyPrefix] public static bool Prefix(EntityController entity, List<EntityController> aiCommandEntities, AiEventPayload payload) => !is_client;
        }
        [HarmonyPatch(typeof(AttackMoveAiAction), "AppendToCommandChain")] public static class Patch_AttackMoveAiAction_AppendToCommandChain {
            [HarmonyPrefix] public static bool Prefix(EntityController entity, List<EntityController> aiCommandEntities, AiEventPayload payload) => !is_client;
        }
        [HarmonyPatch(typeof(AddTagAiAction), "AppendToCommandChain")] public static class Patch_AddTagAiAction_AppendToCommandChain {
            [HarmonyPrefix] public static bool Prefix(EntityController entity, List<EntityController> aiCommandEntities, AiEventPayload payload) => !is_client;
        }
        [HarmonyPatch(typeof(AddMoveCommand), "RunForEveryIdentifiedEntity")] public static class Patch_AddMoveCommand_RunForEveryIdentifiedEntity {
            [HarmonyPrefix] public static bool Prefix(EntityController entity, EventPayload payload, int index) => !is_client;
        }
        [HarmonyPatch(typeof(AddMoveCommand), "RunAtOnceBefore")] public static class Patch_AddMoveCommand_RunAtOnceBefore {
            [HarmonyPrefix] public static bool Prefix(List<EntityController> entities, EventPayload payload) => !is_client;
        }
        [HarmonyPatch(typeof(AddFollowCommand), "RunForEveryIdentifiedEntity")] public static class Patch_AddFollowCommand_RunForEveryIdentifiedEntity {
            [HarmonyPrefix] public static bool Prefix(EntityController entity, EventPayload payload, int index) => !is_client;
        }
        [HarmonyPatch(typeof(AddFleeCommand), "Flee")] public static class Patch_AddFleeCommand_Flee {
            [HarmonyPrefix] public static bool Prefix(EntityController fleeingEntity, EntityController entityToFleeFrom) => !is_client;
        }
        [HarmonyPatch(typeof(AddAttackCommand), "RunForEveryIdentifiedEntity")] public static class Patch_AddAttackCommand_RunForEveryIdentifiedEntity {
            [HarmonyPrefix] public static bool Prefix(EntityController entity, EventPayload payload, int index) => !is_client;
        }
        [HarmonyPatch(typeof(AddActivateSkillCommand), "RunAtOnceBefore")] public static class Patch_AddActivateSkillCommand_RunAtOnceBefore {
            [HarmonyPrefix] public static bool Prefix(List<EntityController> entities, EventPayload payload) => !is_client;
        }


        [HarmonyPatch(typeof(EntityController), "ExecuteCommand")] public static class Patch_EntityController_ExecuteCommand {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, EntityCommand command, EntityController.CommandProcessingType processingType) {
                if (is_client) {
                    SendClientInGamePacket(new ClientExecuteCommnand(__instance, command, processingType));
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ShowMultipleSkillsWidget), "FactoryButtonClicked")] public static class Patch_ShowMultipleSkillsWidget_FactoryButtonClicked {
            [HarmonyPrefix] public static bool Prefix(ShowMultipleSkillsWidget __instance, int buttonIndex, bool silent) {
                if (!is_client) return true;

                List<EntityController> list = __instance._entityControllersLists[buttonIndex];
                if (list.Count < 1) return false;
                ProductionInfo productionInfo = list[0].ProductionInfo;
                if (UnitCap.CurrentPlayerCapacityIncludingQueued(productionInfo.entityId) <= 0) {
                    if (!silent)
                        ShowMessageBox.ShowNotEnoughCapacityMessage_Static();
                    return false;
                }
                if (Bank.ActualBalance("Player") < productionInfo.cost) {
                    if (!silent)
                        ShowMessageBox.ShowNotEnoughCreditsMessage_Static();
                    return false;
                }
                int num = int.MaxValue;
                EntityController entityController = null;
                foreach (EntityController entityController2 in __instance._entityControllersLists[buttonIndex]) {
                    int? inProductionCount = entityController2.InProductionCount;
                    int? num2 = inProductionCount;
                    int num3 = num;
                    if ((num2.GetValueOrDefault() < num3) & (num2 != null)) {
                        num = inProductionCount.Value;
                        entityController = entityController2;
                    }
                }
                if (entityController != null) {
                    SendClientInGamePacket(new ClientUnitProduce(entityController));
                    TutorialController.AddUsedInput_Static(HasUsedCertainInputCondition.Input.BuildUnitInFactory);
                }
                if (!silent && __instance.audioSource && __instance.clickProduceButtonAudio)
                    __instance.audioSource.PlayOneShot(__instance.clickProduceButtonAudio);

                return false;
            }
        }
        #endregion


        #region UNIT OWNERSHIP - APPLYING PLAYER MATERIALS
        [HarmonyPatch(typeof(EntityController), "ReplaceMaterial")] public static class Patch_EntityController_ReplaceMaterial {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, Material newMaterial) {
                if (!__instance.materialToReplace
                || !__instance.IsControlledByPlayer) return true; // run normal logic if not player owned unit

                Material uniqueMat = new Material(newMaterial);
                Player player = EntitiesManager.GetEntityPlayer(__instance);
                if (player != null) {
                    if (uniqueMat.HasProperty("Color_72ECFA4B"))
                        uniqueMat.SetColor("Color_72ECFA4B", player.color);
                    RCMManager.Log("entity had material applied from player id: " + player.id);
                }

                MaterialHelper.ReplaceMaterialInAllHierarchies(__instance.transform, __instance.materialToReplace, uniqueMat);
                return false;
            }
        }
        #endregion

        #region UNIT OWNERSHIP - ENGINEER SELECTION
        [HarmonyPatch(typeof(ExistingControllers), "get_Engineer")] public static class Patch_ExistingControllers_Engineer {
            [HarmonyPrefix] public static bool Prefix(ExistingControllers __instance, ref EntityController __result) {
                if (__instance.Engineers().Count == 0) {
                    __result = null;
                    return false;
                }

                // sort through engineers to find one thats ours, but always return one or else it probably breaks the code
                foreach (var engi in __instance.Engineers())
                    if (__result == null || EntitiesManager.IsEntityOwnedByUs(engi))
                        __result = engi;
                return false;
            }
        }
        [HarmonyPatch(typeof(ExistingControllers), nameof(ExistingControllers.Engineers))]
        public static class Patch_ExistingControllers_Engineers {
            [HarmonyPostfix]
            public static void Postfix(ExistingControllers __instance, ref HashSet<EntityController> __result) {
                EntityController fallback = __result.FirstOrDefault();

                __result = new HashSet<EntityController>(__result);
                __result.RemoveWhere(e => !EntitiesManager.IsEntityOwnedByUs(e));
                if (__result.Count <= 0 && fallback != null) {
                    __result = new HashSet<EntityController>();
                    __result.Add(fallback);
                    return;
                }
                return;
            }
        }
        #endregion

        #region UNIT OWNERSHIP - SELECTION FILTERS
        [HarmonyPatch(typeof(EntityController), "Select")] public static class Patch_EntityController_Select {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance) {
                if (__instance.canNotBeSelected)
                    return false;
                if (!__instance.IsControlledByPlayer)
                    return false;
                // then do check to see if we own the unit
                if (!EntitiesManager.IsEntityOwnedByUs(__instance))
                    return false;

                __instance.IsSelected = true;
                if (__instance.IsControlledByPlayer && __instance.selectionCircle)
                {
                    __instance.selectionCircle.SetActive(true);
                }
                if (__instance.visualizeRangeWhileSelected != EntityController.RangeToVisualize.No)
                {
                    __instance.ShowRangeCircle();
                }
                if (__instance.crystalFillGameObject)
                {
                    __instance.crystalFillGameObject.SetActive(true);
                }
                if (Game.Options.PlayerHealthBars == 1)
                {
                    __instance.ShowHealthShieldManaAndArmorBar();
                }
                SelectionSoundPlayer.PlaySelectionSound_Static(__instance.entityId);
                if (__instance.hasActiveSkill && __instance.activeSkillOrProduction == EntityController.ActiveSkillOrProduction.Production && __instance.IsControlledByPlayer && __instance.rallyPointLineRenderer && __instance._rallyPoint != null)
                {
                    __instance.rallyPointLineRenderer.enabled = true;
                }
                __instance.RunActions(EntityController.Event.OnSelected, new EventPayload
                {
                    Self = __instance,
                    Position = __instance.Position
                });
                return false;
            }
        }
        [HarmonyPatch(typeof(EntityController), "Deselect")] public static class Patch_EntityController_Deselect {
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, bool __result) {
                __result = false;
                if (!__instance.IsSelected)
                    return false;
                // then do check to see if we own the unit
                if (!EntitiesManager.IsEntityOwnedByUs(__instance))
                    return false;

                if (__instance.IsControlledByPlayer){
                    __instance.IsSelected = false;
                    if (__instance.selectionCircle)
                    {
                        __instance.selectionCircle.SetActive(false);
                    }
                    __instance.HideRangeCircle();
                    if (__instance.crystalFillGameObject)
                    {
                        __instance.crystalFillGameObject.SetActive(false);
                    }
                    if (Game.Options.PlayerHealthBars == 1)
                    {
                        __instance.HideHealthShieldManaAndArmorBar();
                    }
                    if (__instance.hasActiveSkill && __instance.activeSkillOrProduction == EntityController.ActiveSkillOrProduction.Production)
                    {
                        EventManager.PublishGlobally<IFactorySelectionListener>(delegate (IFactorySelectionListener x)
                        {
                            x.OnFactoryDeselected();
                        }, __instance.gameObject.tag);
                        if (__instance.rallyPointLineRenderer)
                        {
                            __instance.rallyPointLineRenderer.enabled = false;
                        }
                    }
                    __instance.RunActions(EntityController.Event.OnDeselected, new EventPayload
                    {
                        Self = __instance,
                        Position = __instance.Position
                    });
                }
                __result = true;
                return false;
            }
        }
        #endregion



        static Dictionary<ushort, GameObject> placement_ghosts = new();
        public static void RecievedReleasePlacementGhost(ushort id){
            if (placement_ghosts.TryGetValue(id, out GameObject g)){
                GameObject.Destroy(g);
                placement_ghosts.Remove(id);
            }
        }
        public static void RecievedPlacementIndicator(ServerPlacementBegin request){
            GameObject.Instantiate<GameObject>((request.is_1x1) ? PlaceBuildings._instance.placementEffect1X1 : PlaceBuildings._instance.placementEffect2X2, request.position, Quaternion.identity);
        }
        public static void RecievedPlacementShockwave(ServerPlacementShockwave request){
            Game.ShaderTextures.StartShockwave(request.position, (request.is_1x1) ? PlaceBuildings._instance.shockwaveSize1X1 : PlaceBuildings._instance.shockwaveSize2X2);
        }
        public static void RecievedPlacementRequest(ClientPlacementRequest request){
            coop.StartCoroutine(CustomInstantiateBuildingWithEffect(PlaceBuildings._instance, request.engi, new BlueprintInfo(request.entityId, "", "building " + request.entityId + request.ghost_id), request.pos, request.cells, null, request.ghost_id));
        }

        private static IEnumerator ClientStubRoutine(){ yield break; }
        private static IEnumerator CustomInstantiateBuildingWithEffect(PlaceBuildings __instance, EntityController engineer, BlueprintInfo info, Vector3 placementPosition, List<Vector2Int> cells, GameObject placementGhostForThisBuilding, ushort client_ghost_identifier = 0xffff){

            engineer.ExecuteCommand(EntityCommand.PlaceBuilding(placementPosition, __instance._dropId + 1, __instance._buildingSize), PlaceBuildings.CurrentCommandProcessingType);
            __instance._dropId++;
            int thisDropId = __instance._dropId;
            VisualizeCommandChain.VisualizeCommand(placementPosition);
            foreach (Vector2Int vector2Int in cells)
            {
                __instance._buildingInProgressCells.Add(vector2Int);
                __instance._buildingInProgressCellsToOccupiedCells.Add(vector2Int, cells);
            }
            yield return null;
            while (thisDropId > __instance._endAllDropsUntilThisDropId && engineer && !engineer.IsDestroyed)
            {
                bool flag = false;
                if (engineer)
                {
                    if (engineer.IsExecutingPlaceBuildingCommand(thisDropId))
                    {
                        Vector2 placementPosition2d = new Vector2(placementPosition.x, placementPosition.z);
                        int currentBuildingSize = ((cells.Count == 1) ? 1 : 2);
                        bool engineerIsAlwaysInSkillRange = EntityBalancingStore.SkillRange(Game.Engineer, false) == -1;
                        while (thisDropId > __instance._endAllDropsUntilThisDropId && engineer && !engineer.IsDestroyed)
                        {
                            bool flag2 = false;
                            if (engineer && !engineer.CommandChainExecutionIsPaused && (engineerIsAlwaysInSkillRange || Vector2.Distance(engineer.Position2d, placementPosition2d) <= engineer.MinDistanceEngineerNeedsToBuild(currentBuildingSize)))
                            {
                                flag2 = true;
                                engineer.ExecuteNextCommand();
                            }
                            if (flag2)
                            {
                                float engineerStartedSteppingAsideTimestamp = float.MinValue;
                                HashSet<EntityController> unitsThatSteppedAside = new HashSet<EntityController>();
                                while (thisDropId > __instance._endAllDropsUntilThisDropId && engineer && !engineer.IsDestroyed)
                                {
                                    bool flag3 = false;
                                    __instance._playerUnitsToDestroy.Clear();
                                    foreach (EntityController entityController in ExistingControllers.Instance.PlayerUnits())
                                    {
                                        bool flag4 = cells.Contains(entityController.CurrentCell);
                                        bool flag5 = cells.Contains(entityController.CurrentDestinationCell);
                                        if (entityController.HasRole(UnitRole.Engineer))
                                        {
                                            if (flag4)
                                            {
                                                Vector2Int? vector2Int2 = Pathfinding.NearestReachableCellAround(cells, entityController.CurrentCell, __instance._buildingInProgressCells, null, true);
                                                if (vector2Int2 == null)
                                                {
                                                    ReleaseNetworkedGhostEntity(client_ghost_identifier);
                                                    __instance.EndThisDrop(info, cells, placementGhostForThisBuilding);
                                                    yield break;
                                                }
                                                flag3 = true;
                                                entityController.PauseCommandChainExecution();
                                                entityController.MoveTo(__instance._grid.Grid2World(vector2Int2.Value), false, null);
                                                engineerStartedSteppingAsideTimestamp = Time.time;
                                            }
                                        }
                                        else if (flag4 || flag5)
                                        {
                                            flag3 = true;
                                            Vector2Int? vector2Int3 = Pathfinding.NearestReachableCellAround(cells, entityController.CurrentCell, __instance._buildingInProgressCells, null, true);
                                            if (entityController.EntityId == "DeadRoboRemainder" || vector2Int3 == null)
                                            {
                                                if (flag4)
                                                {
                                                    __instance._playerUnitsToDestroy.Add(entityController);
                                                }
                                                else
                                                {
                                                    entityController.PauseCommandChainExecution();
                                                    entityController.Stop();
                                                    unitsThatSteppedAside.Add(entityController);
                                                }
                                            }
                                            else
                                            {
                                                if (entityController.isHarvesting)
                                                {
                                                    entityController.PauseHarvesting();
                                                }
                                                else
                                                {
                                                    entityController.PauseCommandChainExecution();
                                                }
                                                entityController.MoveTo(__instance._grid.Grid2World(vector2Int3.Value), false, null);
                                                unitsThatSteppedAside.Add(entityController);
                                            }
                                        }
                                    }
                                    for (int i = __instance._playerUnitsToDestroy.Count - 1; i >= 0; i--)
                                    {
                                        __instance._playerUnitsToDestroy[i].Destroy(true, null);
                                    }
                                    if (!flag3)
                                    {
                                        if (engineer && engineer.CommandChainExecutionIsPaused)
                                        {
                                            while (Time.time - engineerStartedSteppingAsideTimestamp < 3f && engineer.IsMoving)
                                            {
                                                yield return null;
                                            }
                                            engineer.ResumeCommandChainExecution();
                                        }
                                        ReleaseNetworkedGhostEntity(client_ghost_identifier);
                                        if (placementGhostForThisBuilding)
                                        {
                                            GameObject.Destroy(placementGhostForThisBuilding);
                                        }

                                        // sync here
                                        SendServerInGamePacket(new ServerPlacementBegin(cells.Count == 1, placementPosition));
                                        GameObject.Instantiate<GameObject>((cells.Count == 1) ? __instance.placementEffect1X1 : __instance.placementEffect2X2, placementPosition, Quaternion.identity);
                                        __instance.DestroyEntitiesOnCells(cells);
                                        __instance._grid.MarkCellsAsBuilding(cells);
                                        if (engineer.IsNotExecutingAnyCommand)
                                        {
                                            engineer.Stop();
                                        }
                                        foreach (EntityController entityController2 in unitsThatSteppedAside)
                                        {
                                            if (entityController2)
                                            {
                                                if (entityController2.isHarvesting)
                                                {
                                                    entityController2.ResumeHarvesting();
                                                }
                                                else
                                                {
                                                    entityController2.ResumeCommandChainExecution();
                                                }
                                            }
                                        }
                                        yield return new WaitForSeconds(__instance.buildingDropTime);
                                        __instance.DestroyEntitiesOnCells(cells);
                                        __instance._entityIdsInProgress.Remove(info.entityId);
                                        // edited to set engineer as spawning entity !!!
                                        EntityController entityController3 = EntityFactory.InstantiateEntity(info.entityId, placementPosition, engineer, "Player", info.factoryName, null, UnitRole.None, true, "PlaceBuildings");
                                        if (entityController3 != null)
                                        {
                                            entityController3.crystalAmountThePlayerPaidForIt = EntityBalancingStore.Cost(info.entityId, false);
                                            // sync here
                                            SendServerInGamePacket(new ServerPlacementShockwave(cells.Count == 1, placementPosition));
                                            Game.ShaderTextures.StartShockwave(placementPosition, (cells.Count == 1) ? __instance.shockwaveSize1X1 : __instance.shockwaveSize2X2);
                                            foreach (Vector2Int vector2Int4 in cells)
                                            {
                                                EntityController entityController4 = __instance._grid.ChestOnCell(vector2Int4);
                                                if (!(entityController4 == null))
                                                {
                                                    entityController4.TriggerEnter(entityController3);
                                                }
                                            }
                                        }
                                        __instance.RemoveFromBuildingInProgressCells(cells);
                                        yield break;
                                    }
                                    yield return null;
                                }
                                ReleaseNetworkedGhostEntity(client_ghost_identifier);
                                __instance.EndThisDrop(info, cells, placementGhostForThisBuilding);
                                if (engineer && engineer.CommandChainExecutionIsPaused)
                                {
                                    engineer.ResumeCommandChainExecution();
                                }
                                foreach (EntityController entityController5 in unitsThatSteppedAside)
                                {
                                    if (entityController5)
                                    {
                                        if (entityController5.isHarvesting)
                                        {
                                            entityController5.ResumeHarvesting();
                                        }
                                        else
                                        {
                                            entityController5.ResumeCommandChainExecution();
                                        }
                                    }
                                }
                                yield break;
                            }
                            if (!engineer || !engineer.IsExecutingPlaceBuildingCommand(thisDropId))
                            {
                                ReleaseNetworkedGhostEntity(client_ghost_identifier);
                                __instance.EndThisDrop(info, cells, placementGhostForThisBuilding);
                                yield break;
                            }
                            yield return null;
                        }
                        ReleaseNetworkedGhostEntity(client_ghost_identifier);
                        __instance.EndThisDrop(info, cells, placementGhostForThisBuilding);
                        yield break;
                    }
                    if (engineer.HasPlaceBuildingCommandInCommandChain(thisDropId))
                    {
                        flag = true;
                    }
                }
                if (!flag)
                {
                    ReleaseNetworkedGhostEntity(client_ghost_identifier);
                    __instance.EndThisDrop(info, cells, placementGhostForThisBuilding);
                    yield break;
                }
                yield return null;
            }
            ReleaseNetworkedGhostEntity(client_ghost_identifier);
            __instance.EndThisDrop(info, cells, placementGhostForThisBuilding);
            yield break;
        }
        private static void ReleaseNetworkedGhostEntity(ushort client_ghost_identifier){
            if (client_ghost_identifier == 0xffff) return;
            SendServerInGamePacket(new ServerPlacementReleased(client_ghost_identifier));
        }
        [HarmonyPatch(typeof(PlaceBuildings), "InstantiateBuildingWithEffect")] public static class Patch_PlaceBuildings_InstantiateBuildingWithEffect{
            [HarmonyPrefix] public static bool Prefix(PlaceBuildings __instance, BlueprintInfo info, Vector3 placementPosition, List<Vector2Int> cells, GameObject placementGhostForThisBuilding, ref IEnumerator __result){
                EntityController engineer = ExistingControllers.Instance.Engineers().First<EntityController>();
                if (is_client){
                    // send packet
                    ushort desig_id = 0xffff;
                    if (placementGhostForThisBuilding != null){
                        desig_id = (ushort)UnityEngine.Random.Range(0, ushort.MaxValue);
                        placement_ghosts.Add(desig_id, placementGhostForThisBuilding);
                    }
                    SendClientInGamePacket(new ClientPlacementRequest(engineer, info.entityId, placementPosition, cells, desig_id));
                    __result = ClientStubRoutine();
                }
                else
                {
                    __result = CustomInstantiateBuildingWithEffect(__instance, engineer, info, placementPosition, cells, placementGhostForThisBuilding);
                }
                return false;
            }
        }




    }
}
