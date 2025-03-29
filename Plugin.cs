using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using IAmFuture.Data;
using IAmFuture.Data.Items;
using IAmFuture.Data.StorageItems;
using IAmFuture.Gameplay.Storages;
using IAmFuture.UserInterface;
using IAmFuture.UserInterface.GameplayMenu;
using IAmFuture.UserInterface.Buildings;
using IAmFuture.Gameplay.Buildings;
using TMPro;
using UnityEngine.UI;
using System.Linq;

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
        SeparateItemStackHandler.MaxStackSize = Math.Abs(globalItemStackSize.Value);
        SeparateItemStackHandler.LoadSeparateItemStacksConfigs(Config);
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll(typeof(RestoreStorageTranspiler));
        harmony.PatchAll(typeof(ResolveOnApplyTranspiler));
        harmony.PatchAll(typeof(CustomGlobalItemStack));
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    [HarmonyPatch(typeof(GUI_StackDivider))]
    internal class ResolveOnApplyTranspiler
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GUI_StackDivider), "ResolveOnApply")]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            var idGetter = typeof(ItemObject).GetMethod("get_ID", BindingFlags.Instance | BindingFlags.Public);
            var customStackValueMethod = typeof(SeparateItemStackHandler)
                .GetMethod("CustomValueForItemStack", BindingFlags.Public | BindingFlags.Static);
            var stackCapacityGetter =
                typeof(ItemObject).GetMethod("get_StackCapacity", BindingFlags.Instance | BindingFlags.Public);
            if (idGetter == null || customStackValueMethod == null)
            {
                return codes;
            }

            Label skipAssignmentLabel = generator.DefineLabel();

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Ldloc_3) continue;
                if (codes[i + 1].opcode == OpCodes.Brfalse || codes[i + 1].opcode == OpCodes.Brfalse_S) continue;
                codes.RemoveRange(i, 3);
                codes.InsertRange(i, [
                    new CodeInstruction(OpCodes.Ldloc_3),
                    new CodeInstruction(OpCodes.Callvirt, idGetter),
                    new CodeInstruction(OpCodes.Callvirt, customStackValueMethod),
                    new CodeInstruction(OpCodes.Stloc_2),
                    new CodeInstruction(OpCodes.Ldloc_2),
                    new CodeInstruction(OpCodes.Ldc_I4_1),
                    new CodeInstruction(OpCodes.Bge_S, skipAssignmentLabel),
                    new CodeInstruction(OpCodes.Ldloc_3),
                    new CodeInstruction(OpCodes.Callvirt, stackCapacityGetter),
                    new CodeInstruction(OpCodes.Stloc_2),
                    new CodeInstruction(OpCodes.Nop).WithLabels(skipAssignmentLabel),
                ]);

                break;
            }

            return codes;
        }
    }

    [HarmonyPatch(typeof(Storage))]
    internal class RestoreStorageTranspiler
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Storage), "Restore")]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
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

    [HarmonyPatch]
    internal class CustomGlobalItemStack
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ItemStack), MethodType.Constructor, typeof(int), typeof(ItemObject), typeof(float),
            typeof(float))]
        private static void Prefix(ref ItemStack __instance, int ID, ItemObject newObject, ref float count,
            ref float maxCount)
        {
            if (ID < 0) return;
            var newValue = SeparateItemStackHandler.CustomValueForItemStack(newObject.ID);
            if (newValue == SeparateItemStack.GameDefaultBase) return;
            maxCount = newValue;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GUI_HugeLootStoragePopover), "UpdateView")]
        public static bool GUI_HugeLootStoragePopover_UpdateView_Prefix(ref GUI_HugeLootStoragePopover __instance)
        {
            TextMeshProUGUI capacityText = (TextMeshProUGUI)AccessTools.Field(typeof(GUI_HugeLootStoragePopover), "capacityText").GetValue(__instance);
            Image fillBar = (Image)AccessTools.Field(typeof(GUI_HugeLootStoragePopover), "fillBar").GetValue(__instance);
            HugeItemStorageBuilding storage = (HugeItemStorageBuilding)AccessTools.Field(typeof(GUI_HugeLootStoragePopover), "storage").GetValue(__instance);
            int configuredValue = SeparateItemStackHandler.CustomValueForItemStack(storage.StoredItemType.ID);
            int stackSize = configuredValue == SeparateItemStack.GameDefaultBase ? 1 : configuredValue;
            fillBar.fillAmount = storage.PercentageFilled;
            capacityText.text = storage.Storage.Stacks.Sum(stack => stack.Count).ToString() + "/" + (storage.Storage.Capacity * stackSize).ToString();
            return false;
        }
    }
}
