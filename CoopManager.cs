

using System;
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
using Shapes;
using SmartTutorial;
using TestMod;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using static RCM_Coop.Network.GameProtocols;
namespace RCM_Coop{

    [BepInDependency(RCMManager.IDENTIFIER, BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(IDENTIFIER, "Co-op Plugin", "1.0.0.0")]
    internal class CoopManager : BaseUnityPlugin{
        const string IDENTIFIER = "RCM.plugins.coop";
        static RCMModUI mod;
        public static CoopManager coop;
        private void Awake(){
            new Harmony(IDENTIFIER).PatchAll();
            coop = this;
            DontDestroyOnLoad(this.gameObject);
            Chainloader.ManagerObject.hideFlags = HideFlags.HideAndDontSave;

            RCMManager.ConnectMod("Co-op").ContinueWith(t => {
                mod = t.Result;

                UpdateUI();

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        private void Update(){
            UnityMainThreadDispatcher.Update();
            EntitiesManager.Update();
        }

        static void UpdateUI(){
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

        static GameServer game_server = null;
        static GameClient game_client = null;

        static async void BeginConnect(){
            is_connecting = true;
            UpdateUI();
            RCMManager.Log("beginning connect");
            session = await Session.StartAutoAsync();
            if (session.is_server)
            {
                game_server = new(session);
            }
            else
            {
                game_client = new(session);
            }
            is_connecting = false;
            UpdateUI();
        }
        static void BeginDisconnect()
        {
            if (session != null)
            {
                game_server = null;
                game_client = null;
                session.Terminate();
                session = null;
                UpdateUI();
            }
        }
        static void RequestEntityData()
        {
            RCMManager.Log("entity button pressed");
            if (game_client != null)
                game_client.SendMapLoadedRequest();
        }

        public static bool IsServerUp() => (game_server != null & session != null);
        public static void SendServerInGamePacket(SerializablePacket packet){
            if (IsServerUp())
            {
                game_server.SendPacketToInGame(packet);
            }
        }


        public class ree : Exception{
            public ree() { }
            public ree(string message) : base(message) { }
            public ree(string message, Exception inner) : base(message, inner) { }
        }


        #region map seeds
        // force seed
        [HarmonyPatch(typeof(InitMap), "StartInit")]
        public static class InitMapPatch_StartInit{
            [HarmonyPrefix]
            public static bool Prefix(ref LandscapeGenerator landscapeGenerator,ref LandscapeGenerator fallbackLandscapeGenerator,ref DefaultAiBehaviour ai,ref bool asCoroutine,ref int? landscapeGeneratorSeed,ref bool landscapeGeneratorWasInstantiated){
                landscapeGeneratorSeed = 1;
                RCMManager.Log($"[Co-op] StartInit Prefix: landscapeGeneratorSeed set to {landscapeGeneratorSeed}");
                return true;
            }
        }
        #endregion

        #region entity spawning stubs
        // stub out entity instantiation
        [HarmonyPatch(typeof(EntityFactory), "InstantiateEntity")]
        public static class Patch_InstantiateEntity_Stub{
            [HarmonyPrefix]
            public static bool Prefix(string entityId,Vector3 position,EntityController originEntity,string tag,string name,Transform parentTransform,UnitRole additionalRoles,bool hasBeenCalledFromAbove,string instantiationInfo,ref EntityController __result){
                if (is_client){
                    __result = null;
                    return false;
                }
                return true;
            }
            [HarmonyReversePatch]
            public static EntityController Original(string entityId, Vector3 position, EntityController originEntity, string tag, string name, Transform parentTransform, UnitRole additionalRoles, bool hasBeenCalledFromAbove, string instantiationInfo){
                throw new NotImplementedException("Stub for reverse patch");
            }
        }
        // this stubs out all entities created at map generation
        [HarmonyPatch(typeof(EntityFactory), "InstantiateEntityFromPrefab")]
        public static class Patch_InstantiateEntityFromPrefab_Stub{
            [HarmonyPrefix]
            public static bool Prefix(GameObject prefab,Vector3 position,Quaternion rotation,Vector3 scale,string tag,string instantiationInfo){
                if (is_client)
                    return false;
                return true;
            }
            [HarmonyReversePatch]
            public static void Original(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale, string tag, string instantiationInfo){
                throw new NotImplementedException("Stub for reverse patch");
            }
        }


        // this stubs out engineer and entities spawned at start via hacks
        [HarmonyPatch(typeof(SetupPlayerStart), "SpawnStartEntities")]
        public static class Patch_SetupPlayerStart_SpawnStartEntities{
            [HarmonyPrefix]
            public static bool Prefix(){
                if (is_client)
                    return false;
                return true;
        }}
        // stub out extra object spawning
        [HarmonyPatch(typeof(SpawnObject), "RunForEveryIdentifiedEntity")]
        public static class Patch_SpawnObject_RunForEveryIdentifiedEntity{
            [HarmonyPrefix]
            public static bool Prefix(SpawnObject __instance, EntityController entity, EventPayload payload, int index){
                // if the action wants to spawn an entity then we say no since network will sync it anyway...
                if (__instance.initEntityController && is_client) return false;
                return true;
            }
        }
        #endregion
        
        #region game over stubs
        // patch out game losing conditions
        [HarmonyPatch(typeof(Game), "Lose")]
        public static class Patch_Game_Lose{
            [HarmonyPrefix]
            public static bool Prefix(){
                RCMManager.Log($"Game.Lose hit:  {new StackTrace(true).ToString()}");
                if (is_client){
                    return false;
                }
                return true;
        }}
        [HarmonyPatch(typeof(FinishLevel), "Lose_Static")]
        public static class Patch_FinishLevel_Lose_Static{
            [HarmonyPrefix]
            public static bool Prefix(){
                RCMManager.Log($"FinishLevel.Lose_Static hit:  {new StackTrace(true).ToString()}");
                if (is_client){
                    return false;
                }
                return true;
        }}
        [HarmonyPatch(typeof(FinishLevel), "Win_Static")]
        public static class Patch_FinishLevel_Win_Static{
            [HarmonyPrefix]
            public static bool Prefix(){
                RCMManager.Log($"FinishLevel.Win_Static hit:  {new StackTrace(true).ToString()}");
                if (is_client){
                    return false;
                }
                return true;
        }}
        #endregion



        [HarmonyPatch(typeof(EntityController), "OnHasBeenInstantiated")] public static class Patch_EntityController_OnHasBeenInstantiated{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, bool hasBeenCalledFromAbove){
                // skip entity management if not server, as client recieves all relevant stuff via the sync'd session data
                if (!is_client) EntitiesManager.EntitySpawned(__instance, hasBeenCalledFromAbove);
                return true;
        }}
        [HarmonyPatch(typeof(EntityController), "Destroy")] public static class Patch_EntityController_Destroy{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, bool withoutTriggeringDestructionActions, EntityController originator){
                if (is_client) return false; EntitiesManager.EntityDestroyed(__instance, withoutTriggeringDestructionActions, originator); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, bool withoutTriggeringDestructionActions, EntityController originator) {throw new ree("err");}
        }

        [HarmonyPatch(typeof(EntityController), "UpdateCachedPosition")] public static class Patch_EntityController_UpdateCachedPosition{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance){
                // cleints dont need to track this
                if (!is_client) EntitiesManager.NotifyEntityMoved(__instance);
                return true;
            }
        }


        #region ENTITY STATE PATCHES
        [HarmonyPatch(typeof(EntityController), "ActivateSkill", new Type[] { typeof(Vector3) })] public static class Patch_EntityController_ActivateSkill_Vector3{
            [HarmonyPrefix]public static bool Prefix(EntityController __instance, Vector3 position){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitActivateSkillPosition(__instance, position)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, Vector3 position) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "ActivateSkill", new Type[] { typeof(EntityController) })] public static class Patch_EntityController_ActivateSkill_Target{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, EntityController target){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitActivateSkillTarget(__instance, target)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, EntityController target) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "ActivateSkill", new Type[] { typeof(int?) })] public static class Patch_EntityController_ActivateSkill_Int{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, int? numberOfUnactivatedStatusFlagsInGroup){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitActivateSkill(__instance, numberOfUnactivatedStatusFlagsInGroup)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, int? numberOfUnactivatedStatusFlagsInGroup) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "AttackMove", new Type[] { typeof(Vector3) })] public static class Patch_EntityController_AttackMove_Vector3{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, Vector3 position){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitAttackMovePosition(__instance, position)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, Vector3 position) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "AttackMove", new Type[] { typeof(EntityController) })] public static class Patch_EntityController_AttackMove_Target{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, EntityController entity){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitAttackMoveTarget(__instance, entity)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, EntityController entity) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "Attack")] public static class Patch_EntityController_Attack{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, EntityController entity){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitAttack(__instance, entity)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, EntityController entity) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "OnReadyToShootOnTarget")] public static class Patch_EntityController_OnReadyToShootOnTarget{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, EntityController target){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitOnReadyToShoot(__instance, target)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, EntityController target) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "Follow", new Type[] { typeof(EntityController) })] public static class Patch_EntityController_Follow_Target{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, EntityController entity){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitFollowTarget(__instance, entity)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, EntityController entity) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "Follow", new Type[] { typeof(Vector3), typeof(float) })] public static class Patch_EntityController_Follow_Position{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, Vector3 position, float distance){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitFollowPosition(__instance, position, distance)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, Vector3 position, float distance) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "Stop")] public static class Patch_EntityController_Stop{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitStop(__instance)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "Teleport")] public static class Patch_EntityController_Teleport{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, Vector3 destination, bool doNotTriggerTeleportEvents){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitTeleport(__instance, destination, doNotTriggerTeleportEvents)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, Vector3 destination, bool doNotTriggerTeleportEvents) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "MoveTo", new Type[]{ typeof(Vector3), typeof(bool), typeof(HeightLayer?), typeof(Vector2Int?)})] public static class Patch_EntityController_MoveTo{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, Vector3 destination, bool countsAsMoveCommand, HeightLayer? restrictedToHeightLayer, Vector2Int? clickPositionCell){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitMoveTo(__instance, destination, countsAsMoveCommand, restrictedToHeightLayer, clickPositionCell)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, Vector3 destination, bool countsAsMoveCommand, HeightLayer? restrictedToHeightLayer, Vector2Int? clickPositionCell) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "RepairArmor")] public static class Patch_EntityController_RepairArmor{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, float amount, EntityController originator, bool doNotFireOnHasRepairedArmor){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitRepairArmor(__instance, amount, originator, doNotFireOnHasRepairedArmor)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, float amount, EntityController originator, bool doNotFireOnHasRepairedArmor) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "Heal")] public static class Patch_EntityController_Heal{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, float amount, EntityController originator, bool doNotFireOnHasHealed, bool doNotFireOnBeingHealed){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitHeal(__instance, amount, originator, doNotFireOnHasHealed, doNotFireOnBeingHealed)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, float amount, EntityController originator, bool doNotFireOnHasHealed, bool doNotFireOnBeingHealed) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "ChargeShield")] public static class Patch_EntityController_ChargeShield{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, float amount, bool displayDeltaInBar){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitChargeShield(__instance, amount)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, float amount, bool displayDeltaInBar) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "TakeDamage")] public static class Patch_EntityController_TakeDamage{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, float amount, EntityController originator, bool doNotFireOnHasDealtDamage, bool ignoreArmor){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitTakeDamage(__instance, amount, originator, doNotFireOnHasDealtDamage, ignoreArmor)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, float amount, EntityController originator, bool doNotFireOnHasDealtDamage, bool ignoreArmor) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "Produce")] public static class Patch_EntityController_Produce{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, bool instantProduction, bool forFree, bool doNotTriggerHasProducedEvent){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitProduce(__instance, instantProduction, forFree, doNotTriggerHasProducedEvent)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, bool instantProduction, bool forFree, bool doNotTriggerHasProducedEvent) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "AbortProduction")] public static class Patch_EntityController_AbortProduction{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitAbortProduction(__instance)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "ChargeMana")] public static class Patch_EntityController_ChargeMana{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, float amount, bool displayDeltaInBar){
                if (is_client) return false; 
                // if increment is small, only send if it ticks over to the next whole number
                if (amount >= 0.5f || Math.Floor(__instance.CurrentMana) < Math.Floor(__instance.CurrentMana + amount))
                {
                    SendServerInGamePacket(new ServerUnitChargeMana(__instance, amount, displayDeltaInBar));
                }
                return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, float amount, bool displayDeltaInBar) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "SetActiveStatusEffect")] public static class Patch_EntityController_SetActiveStatusEffect{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, StatusEffect statusEffect, SetStatusEffect.DurationType durationType, float duration){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitSetStatusEffect(__instance, statusEffect, durationType, duration)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, StatusEffect statusEffect, SetStatusEffect.DurationType durationType, float duration) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "RemoveStatusEffectFromActiveStatusEffects")] public static class Patch_EntityController_RemoveStatusEffectFromActiveStatusEffects{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, StatusEffect statusEffect){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitRemoveActiveStatus(__instance, statusEffect)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, StatusEffect statusEffect) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "RemoveStatusEffect")] public static class Patch_EntityController_RemoveStatusEffect{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, StatusEffect statusEffect){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitRemoveStatusEffect(__instance, statusEffect)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, StatusEffect statusEffect) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(EntityController), "RankUp")] public static class Patch_EntityController_RankUp{
            [HarmonyPrefix] public static bool Prefix(EntityController __instance, int amount){
                if (is_client) return false; SendServerInGamePacket(new ServerUnitRankUp(__instance, amount)); return true;
            }
            [HarmonyReversePatch] public static void Original(EntityController __instance, int amount) { throw new ree("err"); }
        }
        #endregion

        #region PAUSE GAME PATCHES
        [HarmonyPatch(typeof(Navigator), "SlowDown")] public static class Patch_Navigator_SlowDown{
            [HarmonyPrefix] public static bool Prefix(bool withMessage){
                if (is_client) return false; SendServerInGamePacket(new ServerTimeSlow()); return true;
            }
            [HarmonyReversePatch] public static void Original(bool withMessage) { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(Navigator), "ResetToDefaultSpeed")] public static class Patch_Navigator_ResetToDefaultSpeed{
            [HarmonyPrefix] public static bool Prefix(){
                if (is_client) return false; SendServerInGamePacket(new ServerTimeNormal()); return true;
            }
            [HarmonyReversePatch] public static void Original() { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(Navigator), "Pause")] public static class Patch_Navigator_Pause{
            [HarmonyPrefix] public static bool Prefix(){
                if (is_client) return false; SendServerInGamePacket(new ServerTimePaused()); return true;
            }
            [HarmonyReversePatch] public static void Original() { throw new ree("err"); }
        }
        [HarmonyPatch(typeof(Navigator), "Unpause")] public static class Patch_Navigator_Unpause{
            [HarmonyPrefix] public static bool Prefix(){
                if (is_client) return false; SendServerInGamePacket(new ServerTimeUnpaused()); return true;
            }
            [HarmonyReversePatch] public static void Original() { throw new ree("err"); }
        }
        #endregion

    }
}
