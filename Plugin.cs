using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using IAmFuture.Data.Items;
using IAmFuture.Data.StorageItems;
using IAmFuture.Gameplay.Storages;

namespace CustomStackSize;

[BepInPlugin(PluginInfo.PluginGuid, PluginInfo.PluginName, PluginInfo.PluginVersion)]
[BepInProcess("I Am Future.exe")]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        var globalItemStackSize = Config.Bind("Custom Global Item Stack Size",
            "globalItemStackSize",
            100,
            "The custom stack size for every item in the game (MAX: " + SeparateItemStack.GlobalMax + ")");
        CustomGlobalItemStack.MaxStackSize = Math.Abs(globalItemStackSize.Value);
        SeparateItemStackHandler.GetInstance().LoadSeparateItemStacksConfigs(Config);
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll(typeof(RestoreStorageTranspiler));
        harmony.PatchAll(typeof(CustomGlobalItemStack));
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    [HarmonyPatch(typeof(Storage))]
    internal class RestoreStorageTranspiler
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Storage), "Restore")]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            var prevConstructor = typeof(ItemStack).GetConstructor([typeof(ItemObject), typeof(float), typeof(float)]);
            var getIdMethod = typeof(ItemStack).GetMethod("get_ID",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var constructor =
                typeof(ItemStack).GetConstructor([typeof(int), typeof(ItemObject), typeof(float), typeof(float)]);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Newobj) continue;
                if (codes[i].operand as ConstructorInfo != prevConstructor) continue;
                if (constructor != null)
                {
                    codes[i] = new CodeInstruction(OpCodes.Newobj, constructor);
                }

                int found = 0;
                for (var j = i; j > 0; j--)
                {
                    if (codes[j].opcode != OpCodes.Ldloc_1) continue;
                    found++;
                    if (found == 3)
                    {
                        var targetLabel = codes[j].labels[0];
                        codes[j].labels.Remove(targetLabel);
                        codes.Insert(j, new CodeInstruction(OpCodes.Callvirt, getIdMethod));
                        codes.Insert(j, new CodeInstruction(OpCodes.Ldloc_1));
                        codes[j].labels.Add(targetLabel);
                        break;
                    }
                }

                break;
            }

            return codes;
        }
    }

    [HarmonyPatch(typeof(ItemStack))]
    internal class CustomGlobalItemStack
    {
        public static float MaxStackSize;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ItemStack), MethodType.Constructor, typeof(int), typeof(ItemObject), typeof(float), typeof(float))]
        private static void Prefix(ref ItemStack __instance, int ID, ItemObject newObject, ref float count, ref float maxCount)
        {
            if (ID < 0) return;
            var newValue = SeparateItemStackHandler.GetInstance().CustomValueForItemStack(newObject.ID);
            if (newValue == SeparateItemStack.GameDefaultBase) return;
            if (newValue == SeparateItemStack.GlobalValueBase)
            {
                maxCount = MaxStackSize;
            }
            else
            {
                maxCount = newValue;
            }
        }
    }
}