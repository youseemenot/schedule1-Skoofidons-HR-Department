using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MelonLoader;
using ModManagerPhoneApp;
using HarmonyController = HarmonyLib.Harmony;

namespace SkoofidonsHRDepartment
{
    public static class PreferenceCategoryOrderIntegration
    {
        private const string Owner = "SkoofidonsHRDepartment.PreferenceCategoryOrder";
        private static readonly string[] Selectors = {
            "<PopulateMenuConfigPanel>b__80_1", "<PopulateAllSettings>b__96_0", "<PopulateModSettings>b__97_0"
        };
        private static HarmonyController _harmony;

        private static MethodInfo[] Resolve()
        {
            var type = typeof(ModSettingsEvents).Assembly.GetType("ModManagerPhoneApp.ModSettingsAppCreator+<>c", throwOnError: true);
            return Selectors.Select(name => type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                ?? throw new MissingMethodException("Phone App category selector changed: " + name)).ToArray();
        }

        public static int VerifyManagedBindings()
        {
            foreach (var method in Resolve())
            {
                var parameters = method.GetParameters();
                var il = method.GetMethodBody()?.GetILAsByteArray();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(MelonPreferences_Category) || method.ReturnType != typeof(string) ||
                    il == null || il.Length != 7 || il[0] != 0x03 || il[1] != 0x6f || il[6] != 0x2a)
                    throw new InvalidOperationException("Phone App category sort implementation changed.");
                var getter = method.Module.ResolveMethod(BitConverter.ToInt32(il, 2));
                if (getter?.DeclaringType != typeof(MelonPreferences_Category) || getter.Name != "get_Identifier")
                    throw new InvalidOperationException("Phone App no longer sorts category identifiers.");
                RuntimeHelpers.PrepareMethod(method.MethodHandle);
            }
            return Selectors.Length;
        }

        public static void Initialize()
        {
            try
            {
                VerifyManagedBindings();
                var methods = Resolve();
                foreach (var method in methods)
                    if (HarmonyController.GetPatchInfo(method)?.Owners.Any() == true)
                        throw new InvalidOperationException("Phone App category selector has another patch owner.");
                _harmony = new HarmonyController(Owner);
                var postfix = new HarmonyMethod(typeof(PreferenceCategoryOrderIntegration).GetMethod(nameof(SortKeyPostfix), BindingFlags.NonPublic | BindingFlags.Static));
                foreach (var method in methods) _harmony.Patch(method, postfix: postfix);
                MelonLogger.Msg("[HR SETTINGS ORDER READY] Botanist, Chemist, Cleaner, Handler, Fixer, Machine Timers.");
            }
            catch (Exception exception)
            {
                Deinitialize();
                MelonLogger.Warning("[HR SETTINGS ORDER INACTIVE] " + exception.Message);
            }
        }

        private static void SortKeyPostfix(MelonPreferences_Category __0, ref string __result)
        {
            if (__0 != null) __result = PreferenceCategoryOrder.GetSortKey(__0.Identifier, __result);
        }

        public static void Deinitialize()
        {
            try { _harmony?.UnpatchSelf(); }
            catch (Exception exception) { MelonLogger.Warning("[HR SETTINGS ORDER] Cleanup: " + exception.Message); }
            _harmony = null;
        }
    }
}
