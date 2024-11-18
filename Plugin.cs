using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using IAmFuture.Data.Items;
using IAmFuture.Data.StorageItems;

namespace CustomStackSize;

[BepInPlugin(PluginInfo.PluginGuid, PluginInfo.PluginName, PluginInfo.PluginVersion)]
[BepInProcess("I Am Future.exe")]
public class Plugin : BaseUnityPlugin
{
    private const int GlobalMax = 1000000;

    private void Awake()
    {
        var globalItemStackSize = Config.Bind("Custom Global Item Stack Size",
            "globalItemStackSize",
            100f,
            "The custom stack size for every item in the game (MAX: " + GlobalMax + ")");
        CustomGlobalItemStack.MaxStackSize = Math.Abs(globalItemStackSize.Value);
        new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll(typeof(CustomGlobalItemStack));
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    [HarmonyPatch(typeof(ItemStack))]
    internal class CustomGlobalItemStack
    {
        public static float MaxStackSize;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ItemStack), MethodType.Constructor, typeof(int), typeof(ItemObject), typeof(float), typeof(float))]
        private static void Prefix(ref ItemStack __instance, int ID, ItemObject newObject, ref float count,
            ref float maxCount)
        {
            maxCount = Math.Min(MaxStackSize, GlobalMax);
        }
    }
}