using System;
using System.Collections.Generic;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.Persistence.Datas;

namespace SkoofidonsHRDepartment
{
    public static partial class MachineTimerController
    {
        private const string SaveKey = "SkoofidonsHR.MachineTimers.v1";

        private static List<MachineTimerSnapshot> Snapshots(BuildableItem owner)
        {
            var result = new List<MachineTimerSnapshot>();
            void Add(MachineTimerSnapshot snapshot) { if (snapshot != null) result.Add(snapshot); }
            if (owner.TryCast<ChemistryStation>() is { } chemistry) Add(ChemistrySnapshot(chemistry));
            else if (owner.TryCast<LabOven>() is { } oven) Add(OvenSnapshot(oven));
            else if (owner.TryCast<MixingStation>() is { } mixing) Add(MixingSnapshot(mixing));
            else if (owner.TryCast<Cauldron>() is { } cauldron) Add(CauldronSnapshot(cauldron));
            else if (owner.TryCast<DryingRack>() is { } rack)
            {
                var operations = rack.DryingOperations;
                for (int i = 0; i < operations.Count; i++) Add(DryingSnapshot(rack, operations[i], i));
            }
            return result;
        }

        private static void SavePostfix(BuildableItem __instance, DynamicSaveData __result)
        {
            if (!_active || __result == null || __instance == null) return;
            try
            {
                string payload = State.Serialize(Snapshots(__instance));
                if (!string.IsNullOrEmpty(payload)) __result.AddData(SaveKey, payload);
            }
            catch (Exception ex) { Warn("saving captured machine rates", ex); }
        }

        private static MachineTimerPersistence.LoadScope BeginLoad(MachineTimerKind kind, string json, string payload = null)
        {
            if (!_active) return null;
            try { return State.BeginLoad(kind, json, payload); }
            catch (Exception ex) { Warn("loading captured machine rates", ex); return State.BeginLoad(kind, "{}", null); }
        }

        private static MachineTimerPersistence.LoadScope BeginLoad(MachineTimerKind kind, DynamicSaveData data)
        {
            if (!_active) return null;
            try
            {
                string payload = null;
                if (data != null) data.TryGetData(SaveKey, out payload);
                return BeginLoad(kind, data?.BaseData, payload);
            }
            catch (Exception ex) { Warn("reading captured machine rates", ex); return BeginLoad(kind, "{}"); }
        }

        private static Exception LoadFinalizer(Exception __exception, MachineTimerPersistence.LoadScope __state)
        {
            if (__state != null)
            {
                try { State.EndLoad(__state, __exception != null); }
                catch (Exception ex) { Warn("restoring load scope", ex); }
            }
            return __exception;
        }

        private static void ChemistryLoadDynamicPrefix(DynamicSaveData __0, out MachineTimerPersistence.LoadScope __state) => __state = BeginLoad(MachineTimerKind.Chemistry, __0);
        private static void ChemistryLoadLegacyPrefix(string __0, out MachineTimerPersistence.LoadScope __state) => __state = BeginLoad(MachineTimerKind.Chemistry, __0);
        private static void OvenLoadDynamicPrefix(DynamicSaveData __0, out MachineTimerPersistence.LoadScope __state) => __state = BeginLoad(MachineTimerKind.Oven, __0);
        private static void OvenLoadLegacyPrefix(string __0, out MachineTimerPersistence.LoadScope __state) => __state = BeginLoad(MachineTimerKind.Oven, __0);
        private static void MixingLoadDynamicPrefix(DynamicSaveData __0, out MachineTimerPersistence.LoadScope __state) => __state = BeginLoad(MachineTimerKind.Mixing, __0);
        private static void MixingLoadLegacyPrefix(string __0, out MachineTimerPersistence.LoadScope __state) => __state = BeginLoad(MachineTimerKind.Mixing, __0);
        private static void CauldronLoadDynamicPrefix(DynamicSaveData __0, out MachineTimerPersistence.LoadScope __state) => __state = BeginLoad(MachineTimerKind.Cauldron, __0);
        private static void CauldronLoadLegacyPrefix(string __0, out MachineTimerPersistence.LoadScope __state) => __state = BeginLoad(MachineTimerKind.Cauldron, __0);
        private static void DryingLoadDynamicPrefix(DynamicSaveData __0, out MachineTimerPersistence.LoadScope __state) => __state = BeginLoad(MachineTimerKind.Drying, __0);
        private static void DryingLoadLegacyPrefix(string __0, out MachineTimerPersistence.LoadScope __state) => __state = BeginLoad(MachineTimerKind.Drying, __0);
    }
}
