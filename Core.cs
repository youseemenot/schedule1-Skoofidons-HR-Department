using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Persistence;
using UnityEngine.Events;

[assembly: MelonInfo(typeof(SkoofidonsHRDepartment.Core), "Skoofidon's HR Department", "1.0.5", "Skoofidon")]
[assembly: System.Reflection.AssemblyVersion("1.0.5.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.5.0")]
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
            ModManagerIntegration.Initialize();
            LoggerInstance.Msg("HR Department 1.0.5 Handler parent-lookup fix test build (Schedule I 0.4.7f12).");
        }

        public override void OnDeinitializeMelon()
        {
            if (Singleton<LoadManager>.Instance != null)
            {
                Singleton<LoadManager>.Instance.onLoadComplete.RemoveListener(OnLoadCompleteAction);
            }
            ModManagerIntegration.Deinitialize();
            PackagerUIFix.ClearReferences();
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
            if (sceneName == "Main")
            {
                IsGameLoaded = false;
                GamePatches.ClearState();
                EmployeeController.ClearCache();
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
