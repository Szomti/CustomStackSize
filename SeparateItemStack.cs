using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace CustomStackSize;

public class SimplifiedSeparateItemStack(string gameObjectId, int value)
{
    public readonly string GameObjectId = gameObjectId.ToLower();
    public readonly int Value = value;
}

public abstract class SeparateItemStack
{
    private const string Explanation = "\nSet custom value for item, additionally -> [0 = global value; -1 = game default]";
    public const int GlobalValueBase = 0;
    public const int GameDefaultBase = -1;
    public const int GlobalMax = 1000000;
    public static readonly List<SeparateItemStack> AllItemStacks =
    [
        new WoodBeamSeparate(),
        new IronSeparate(),
        new GlassSeparate(),
        new DigitalStorageSeparate(),
    ];
    
    protected abstract string GameObjectId { get; }
    protected abstract string Section { get; }
    protected abstract string Key { get; }
    protected abstract int CustomStackSize { get; }
    protected abstract string Description { get; }

    public SimplifiedSeparateItemStack GetFromConfig(ConfigFile configFile)
    {
        ConfigEntry<int> entry = configFile.Bind(Section, Key, CustomStackSize, Description + Explanation);
        return new SimplifiedSeparateItemStack(GameObjectId, Math.Min(entry.Value, GlobalMax));
    }

    private class WoodBeamSeparate : SeparateItemStack
    {
        protected override string GameObjectId => "Wood Beam (Huge)";
        protected override string Section => "Separate Custom";
        protected override string Key => "planksStack";
        protected override int CustomStackSize => GameDefaultBase;
        protected override string Description => "Setting custom value multiplies the storage capacity (it will still say it's max 10)";
    }

    private class IronSeparate : SeparateItemStack
    {
        protected override string GameObjectId => "Iron (Huge)";
        protected override string Section => "Separate Custom";
        protected override string Key => "sheetMetalStack";
        protected override int CustomStackSize => GameDefaultBase;
        protected override string Description => "Setting custom value multiplies the storage capacity (it will still say it's max 10)";
    }

    private class GlassSeparate : SeparateItemStack
    {
        protected override string GameObjectId => "Glass (Huge)";
        protected override string Section => "Separate Custom";
        protected override string Key => "plexiGlassStack";
        protected override int CustomStackSize => GameDefaultBase;
        protected override string Description => "Setting custom value multiplies the storage capacity (it will still say it's max 10)";
    }
    
    private class DigitalStorageSeparate : SeparateItemStack
    {
        protected override string GameObjectId => "DigitalStorage";
        protected override string Section => "Critical";
        protected override string Key => "digitalStorageStack";
        protected override int CustomStackSize => GameDefaultBase;
        protected override string Description => "Setting custom value not recommended as decoder uses entire stack for 1 reward";
    }
}