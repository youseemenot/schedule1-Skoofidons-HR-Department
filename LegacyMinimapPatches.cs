using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using HarmonyController=HarmonyLib.Harmony;

namespace SkoofidonsHRDepartment
{
    public sealed class LegacyMinimapPatches : IDisposable
    {
        public const string Owner="skoofidon.hr.legacy-minimap";
        private static LegacyMinimapPatches current;
        private readonly LegacyMinimapBinding binding;
        private HarmonyController harmony;
        private bool active;
        private LegacyMinimapPatches(LegacyMinimapBinding binding){this.binding=binding;}
        public static LegacyMinimapPatches Install(LegacyMinimapBinding binding)
        {
            if(current!=null)throw new InvalidOperationException("Legacy Minimap adapter is already installed.");
            foreach(var method in new[]{binding.UpdateMethod,binding.GuiMethod,binding.FlushMethod})
                if(HarmonyController.GetPatchInfo(method)?.Owners.Any()==true)
                    throw new InvalidOperationException("Legacy Minimap method already has a patch owner: "+method.Name);
            // Complete shape validation before touching any method.
            Rewrite(PatchProcessor.GetOriginalInstructions(binding.UpdateMethod),InputGetter).ToArray();
            var result=new LegacyMinimapPatches(binding);current=result;
            try
            {
                result.harmony=new HarmonyController(Owner);
                result.harmony.Patch(binding.UpdateMethod,transpiler:Patch(nameof(Transpiler)));
                result.harmony.Patch(binding.GuiMethod,prefix:Patch(nameof(GuiPrefix)));
                result.harmony.Patch(binding.FlushMethod,prefix:Patch(nameof(FlushPrefix)));
                binding.HideOldWindow();result.active=true;return result;
            }
            catch{result.Dispose();throw;}
        }
        private static MethodInfo InputGetter=>typeof(Input).GetMethod("GetKeyDown",new[]{typeof(KeyCode)});
        private static HarmonyMethod Patch(string name)=>new(typeof(LegacyMinimapPatches).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic));
        public static CodeInstruction[] Rewrite(IEnumerable<CodeInstruction> instructions,MethodInfo inputGetter)
        {
            var source=instructions.ToArray();var hits=new List<int>();
            for(int i=1;i<source.Length;i++)
                if(source[i].Calls(inputGetter)&&source[i-1].LoadsConstant((int)KeyCode.F3))hits.Add(i);
            if(hits.Count!=1)throw new InvalidOperationException("Expected exactly one legacy F3 key read, found "+hits.Count);
            var output=source.Select(c=>new CodeInstruction(c)).ToArray();
            output[hits[0]].opcode=OpCodes.Call;
            output[hits[0]].operand=typeof(LegacyMinimapPatches).GetMethod(nameof(ReadLegacyKey));
            return output;
        }
        public static bool ReadLegacyKey(KeyCode key)=>current?.active==true&&key==KeyCode.F3?false:Input.GetKeyDown(key);
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)=>Rewrite(instructions,InputGetter);
        private static bool GuiPrefix(object __instance)=>current?.active!=true||!ReferenceEquals(__instance,current.binding.Instance);
        private static bool FlushPrefix(object __instance)
        {
            if(current?.active!=true||!ReferenceEquals(__instance,current.binding.Instance))return true;
            current.binding.Flush(Time.realtimeSinceStartup,MelonLoader.MelonLogger.Warning);return false;
        }
        public void Dispose()
        {
            active=false;
            try{harmony?.UnpatchSelf();}
            finally{harmony=null;if(ReferenceEquals(current,this))current=null;}
        }
    }
}
