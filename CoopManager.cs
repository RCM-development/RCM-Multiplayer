

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
        }}
        // this stubs out all entities created at map generation
        [HarmonyPatch(typeof(EntityFactory), "InstantiateEntityFromPrefab")]
        public static class Patch_InstantiateEntityFromPrefab_Stub{
            [HarmonyPrefix]
            public static bool Prefix(GameObject prefab,Vector3 position,Quaternion rotation,Vector3 scale,string tag,string instantiationInfo){
                if (is_client)
                    return false;
                return true;
            }
        }
        // methods for clients to call to bypass client restrictions !!
        [HarmonyPatch(typeof(EntityFactory), "InstantiateEntity")]
        public static class Reverse_InstantiateEntity{
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(EntityFactory), "InstantiateEntity")]
            public static EntityController Original(string entityId, Vector3 position, EntityController originEntity, string tag, string name, Transform parentTransform, UnitRole additionalRoles, bool hasBeenCalledFromAbove, string instantiationInfo){
                throw new NotImplementedException("Stub for reverse patch");
            }
        }
        [HarmonyPatch(typeof(EntityFactory), "InstantiateEntityFromPrefab")]
        public static class Reverse_InstantiateEntityFromPrefab{
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(EntityFactory), "InstantiateEntityFromPrefab")]
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



        [HarmonyPatch(typeof(EntityController), "OnHasBeenInstantiated")]
        public static class Patch_EntityController_OnHasBeenInstantiated{
            [HarmonyPrefix]
            public static bool Prefix(EntityController __instance, bool hasBeenCalledFromAbove){
                // skip entity management if not server, as client recieves all relevant stuff via the sync'd session data
                if (!is_client) EntitiesManager.EntitySpawned(__instance, hasBeenCalledFromAbove);
                return true;
        }}
        
        [HarmonyPatch(typeof(EntityController), "Destroy")]
        public static class Patch_EntityController_Destroy{
            [HarmonyPrefix]
            public static bool Prefix(EntityController __instance, bool withoutTriggeringDestructionActions, EntityController originator){
                // we dont want clients killing units on their own as this would desync a lot of stuff
                if (is_client) return false;
                EntitiesManager.EntityDestroyed(__instance, withoutTriggeringDestructionActions, originator);
                return true;
        }}
        [HarmonyPatch(typeof(EntityController), "Destroy")]
        public static class Reverse_EntityController_Destroy{
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(EntityController), "Destroy")]
            public static void Original(EntityController __instance, bool withoutTriggeringDestructionActions, EntityController originator){
                // Harmony replaces this with the original method body
                throw new NotImplementedException("Stub for reverse patch");
            }
        }




    }
}
