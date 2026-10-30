using HarmonyLib;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.NPCs.CharacterClasses;
using Il2CppScheduleOne.Variables;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne;
using Il2CppScheduleOne.Management;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SkoofidonsHRDepartment
{
    [HarmonyPatch]
    public static class GamePatches
    {
        private static bool _isScrollBlocked = false;

        // --- 1. Перехват спавна работников ---
        [HarmonyPatch(typeof(Employee), "NetworkInitialize___Early")]
        [HarmonyPostfix]
        public static void EmployeeSpawnPostfix(Employee __instance)
        {
            if (__instance != null)
            {
                EmployeeController.AddEmployee(__instance);
            }
        }

        // Start runs after configuration/movement initialization, also for new hires.
        [HarmonyPatch(typeof(Employee), "Start")]
        [HarmonyPostfix]
        public static void EmployeeStartPostfix(Employee __instance)
        {
            if (__instance == null) return;
            EmployeeController.AddEmployee(__instance);
            EmployeeController.ApplyStats(__instance);
        }

        // --- 2. Перерасчет комиссии Фиксера ---
        [HarmonyPatch(typeof(Fixer), "GetAdditionalSigningFee")]
        [HarmonyPostfix]
        public static void FixerFeePostfix(ref float __result)
        {
            int recruited = Mathf.RoundToInt(NetworkSingleton<VariableDatabase>.Instance.GetValue<float>("LifetimeEmployeesRecruited"));
            float totalFee = 0f;

            for (int i = 0; i < recruited; i++)
            {
                totalFee += (i > 5) ? Config.FixerFee5Plus.Value : Config.FixerFeeLess5.Value;
            }

            __result = Mathf.Min(totalFee, Config.FixerFeeLimit.Value);
        }

        // --- 3. Блокировка скролла для интерфейса Фасовщика ---
        [HarmonyPatch(typeof(ManagementInterface), "Open")]
        [HarmonyPostfix]
        public static void MenuOpenPostfix() => _isScrollBlocked = true;

        [HarmonyPatch(typeof(ManagementInterface), "Close")]
        [HarmonyPostfix]
        public static void MenuClosePostfix() => _isScrollBlocked = false;

        public static void ClearState() => _isScrollBlocked = false;

        [HarmonyPatch(typeof(GameInput), "OnScrollWheel")]
        [HarmonyPrefix]
        public static bool DisableScrollWheel()
        {
            return !_isScrollBlocked;
        }
    }
}
