using System;
using BepInEx;
using BepInEx.Configuration;
using PEAKLib.ModConfig.SettingOptions.SettingUI;
using Unity.Mathematics;
using UnityEngine;
using Zorro.Core;
using Zorro.Settings;
using Zorro.Settings.UI;
using static PEAKLib.ModConfig.SettingsHandlerUtility;

namespace PEAKLib.ModConfig.SettingOptions;

internal class BepInExFloat(
    ConfigEntryBase entryBase,
    PluginInfo plugin,
    string categoryName = "Mods",
    Action<float>? saveCallback = null,
    Action<BepInExFloat>? onApply = null
) : FloatSetting, IBepInExProperty, IExposedSetting
{
    ConfigEntryBase IBepInExProperty.ConfigBase => entryBase;

    PluginInfo IBepInExProperty.Pluginfo => plugin;

    private static GameObject? _settingUICell = null;
    public static GameObject? SettingUICell
    {
        get
        {
            if (_settingUICell == null)
            {
                if (
                    SingletonAsset<InputCellMapper>.Instance == null
                    || SingletonAsset<InputCellMapper>.Instance.FloatSettingCell == null
                )
                    return null;

                _settingUICell = UnityEngine.Object.Instantiate(
                    SingletonAsset<InputCellMapper>.Instance.FloatSettingCell
                );
                _settingUICell.name = "BepInExFloatCell";

                var original = _settingUICell.GetComponent<FloatSettingUI>();
                var replace = _settingUICell.AddComponent<BepInExFloat_SettingUI>();

                replace.slider = original.slider;
                replace.inputField = original.inputField;

                UnityEngine.Object.DestroyImmediate(original);
                UnityEngine.Object.DontDestroyOnLoad(_settingUICell);
            }

            return _settingUICell;
        }
    }

    public override GameObject? GetSettingUICell() => SettingUICell;

    public override void Load(ISettingsSaveLoad loader)
    {
        Value = GetCurrentValue<float>(entryBase);

        float2 minMaxValue = GetMinMaxValue();
        MinValue = minMaxValue.x;
        MaxValue = minMaxValue.y;
    }

    public override void Save(ISettingsSaveLoad saver) => saveCallback?.Invoke(Value);

    public override void ApplyValue() => onApply?.Invoke(this);

    public void RefreshValueFromConfig() => Value = GetCurrentValue<float>(entryBase);

    public string GetDisplayName() => entryBase.Definition.Key;

    public string GetCategory() => categoryName;

    protected override float GetDefaultValue() => GetDefaultValue<float>(entryBase);

    protected override float2 GetMinMaxValue()
    {
        if (TryGetMinMaxValue(entryBase, out float minValue, out float maxValue))
            return new(minValue, maxValue);

        return new(0f, 1000f);
    }

    public void SetDefaultValue()
    {
        SetBoxedValue(entryBase, GetDefaultValue());
        RefreshValueFromConfig();
    }
}
