using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace CustomStackSize;

public class SeparateItemStackHandler
{
    public static int MaxStackSize;
    private static readonly List<SimplifiedSeparateItemStack> Items = [];

    private SeparateItemStackHandler()
    {
    }

    public static void LoadSeparateItemStacksConfigs(ConfigFile configFile)
    {
        foreach (var separateItemStack in SeparateItemStack.AllItemStacks)
        {
            var result = separateItemStack.GetFromConfig(configFile);
            if (result.Value == 0) continue;
            Items.Add(result);
        }
    }

    public static int CustomValueForItemStack(string gameObjectId)
    {
        foreach (var simplified in Items.Where(simplified => gameObjectId.ToLower().Contains(simplified.GameObjectId)))
        {
            return simplified.Value;
        }

        return MaxStackSize;
    }
}