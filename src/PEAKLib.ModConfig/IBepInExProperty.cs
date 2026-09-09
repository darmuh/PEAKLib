using BepInEx;
using BepInEx.Configuration;

namespace PEAKLib.ModConfig;

internal interface IBepInExProperty
{
    //so we can refresh values from config and add functional section tabs
    internal ConfigEntryBase ConfigBase { get; }
    internal PluginInfo Pluginfo { get; }

    internal void RefreshValueFromConfig();
    internal void SetDefaultValue();

    internal string GetDisplayName();
    internal string GetCategory();

    internal T GetKeyValue<T>()
    {
        return (T)ConfigBase.BoxedValue;
    }

    internal T GetDefaultValue<T>()
    {
        return (T)ConfigBase.DefaultValue;
    }
}
