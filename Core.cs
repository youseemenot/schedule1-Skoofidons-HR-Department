using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Persistence;
using UnityEngine.Events;
using Skoofidons.SharedSettings.V1;

[assembly: MelonInfo(typeof(SkoofidonsHRDepartment.Core), "Skoofidon's HR Department", "1.3.0-beta.3", "Skoofidon")]
[assembly: System.Reflection.AssemblyVersion("1.3.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.3.0.0")]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: MelonColor(255, 0, 200, 0)]

namespace SkoofidonsHRDepartment
{
    public class Core : MelonMod
    {
        public static Core Instance { get; private set; }
        public static bool IsGameLoaded { get; private set; }

        private static readonly System.Action OnLoadCompleteAction = OnLoadComplete;
        private bool _eventsHooked;

        public override void OnInitializeMelon()
        {
            Instance = this;
            Config.Initialize();
            CleanerActionSpeedController.Initialize();
            MachineTimerController.Initialize();
            ModManagerIntegration.Initialize();
            HrSettingsPanel.Initialize();
            LoggerInstance.Msg("HR Department 1.3.0-beta.3 initialized. F3: independent shared settings (Schedule I 0.4.7f12).");
        }

        public override void OnDeinitializeMelon()
        {
            try{SharedMenuRuntime.Shutdown();}finally{LegacyMinimapIntegration.Shutdown();}
            if (Singleton<LoadManager>.Instance != null)
            {
                Singleton<LoadManager>.Instance.onLoadComplete.RemoveListener(OnLoadCompleteAction);
            }
            ModManagerIntegration.Deinitialize();
            CleanerActionSpeedController.Deinitialize();
            MachineTimerController.Deinitialize();
            EmployeeSpeedController.RestoreAndClear();
            PackagerUIFix.ClearReferences();
        }

        public override void OnLateInitializeMelon()
        {
            LegacyMinimapIntegration.Initialize(MelonBase.RegisteredMelons);
            try{SharedMenuRuntime.LateInitialize();}
            catch{try{SharedMenuRuntime.Shutdown();}finally{LegacyMinimapIntegration.Shutdown();}throw;}
        }
        public override void OnGUI() => SharedMenuRuntime.Draw();
        public override void OnApplicationQuit(){try{SharedMenuRuntime.Shutdown();}finally{LegacyMinimapIntegration.Shutdown();}}
        public override void OnUpdate()
        {
            SharedMenuRuntime.Tick();
            ClipboardLayoutController.RefreshTracked();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "Menu")
            {
                IsGameLoaded = false;
                if (!_eventsHooked)
                {
                    Singleton<LoadManager>.Instance.onLoadComplete.AddListener(OnLoadCompleteAction);
                    _eventsHooked = true;
                }
            }
            if (sceneName == "Main")
            {
                EmployeeController.ApplyToPrefabs();
                PackagerUIFix.InitializeRouteOverride();
            }
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            SharedMenuRuntime.SceneChanged();
            if (sceneName == "Main")
            {
                IsGameLoaded = false;
                GamePatches.ClearState();
                EmployeeController.ClearCache();
                CleanerActionSpeedController.ClearState();
                MachineTimerController.ClearState();
                PackagerUIFix.ClearReferences();
            }
        }

        private static void OnLoadComplete()
        {
            EmployeeController.ApplyToSpawnedEmployees();
            IsGameLoaded = true;
        }
    }
}
