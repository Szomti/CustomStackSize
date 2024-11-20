using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace CustomStackSize;

public class SeparateItemStackHandler
{
    private static SeparateItemStackHandler _instance;
    private readonly List<SimplifiedSeparateItemStack> _items = [];

    private SeparateItemStackHandler()
    {
    }

    public static SeparateItemStackHandler GetInstance()
    {
        return _instance ??= new SeparateItemStackHandler();
    }

    public void LoadSeparateItemStacksConfigs(ConfigFile configFile)
    {
        foreach (var separateItemStack in SeparateItemStack.AllItemStacks)
        {
            var result = separateItemStack.GetFromConfig(configFile);
            if (result.Value == 0) continue;
            _items.Add(result);
        }
    }

    public int CustomValueForItemStack(string gameObjectId)
    {
        foreach (var simplified in _items.Where(simplified => gameObjectId.ToLower().Contains(simplified.GameObjectId)))
        {
            return simplified.Value;
        }

        return SeparateItemStack.GlobalValueBase;
    }
}