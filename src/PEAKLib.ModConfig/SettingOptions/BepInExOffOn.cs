using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using PEAKLib.ModConfig.SettingOptions.SettingUI;
using UnityEngine;
using UnityEngine.Localization;
using Zorro.Core;
using Zorro.Settings;
using Zorro.Settings.UI;
using static PEAKLib.ModConfig.SettingsHandlerUtility;

namespace PEAKLib.ModConfig.SettingOptions;

internal class BepInExOffOn(
    ConfigEntryBase entryBase,
    string category = "Mods",
    Action<bool>? saveCallback = null,
    Action<BepInExOffOn>? onApply = null
) : OffOnSetting, IBepInExProperty, IExposedSetting
{
    ConfigEntryBase IBepInExProperty.ConfigBase
    {
        get => entryBase;
    }

    private static GameObject? _settingUICell = null;
    public static GameObject? SettingUICell
    {
        get
        {
            if (_settingUICell == null)
            {
                if (
                    SingletonAsset<InputCellMapper>.Instance == null
                    || SingletonAsset<InputCellMapper>.Instance.EnumSettingCell == null
                )
                    return null;

                _settingUICell = UnityEngine.Object.Instantiate(
                    SingletonAsset<InputCellMapper>.Instance.EnumSettingCell
                );
                _settingUICell.name = "BepInExOnOffCell";

                var original = _settingUICell.GetComponent<EnumSettingUI>();
                var replace = _settingUICell.AddComponent<BepInExEnum_SettingUI>();

                replace.dropdown = original.dropdown;

                UnityEngine.Object.DestroyImmediate(original);
                UnityEngine.Object.DontDestroyOnLoad(_settingUICell);
            }

            return _settingUICell;
        }
    }

    public override GameObject? GetSettingUICell() => SettingUICell;

    public override void Load(ISettingsSaveLoad loader) =>
        Value = GetCurrentValue<bool>(entryBase) ? OffOnMode.ON : OffOnMode.OFF;

    public override void Save(ISettingsSaveLoad saver) =>
        saveCallback?.Invoke(Value == OffOnMode.ON);

    public void RefreshValueFromConfig() =>
        Value = GetCurrentValue<bool>(entryBase) ? OffOnMode.ON : OffOnMode.OFF;

    public override void ApplyValue() => onApply?.Invoke(this);

    protected override OffOnMode GetDefaultValue() =>
        GetDefaultValue<bool>(entryBase) == true ? OffOnMode.ON : OffOnMode.OFF;

    public string GetDisplayName() => entryBase.Definition.Key;

    public string GetCategory() => category;

    public override List<LocalizedString>? GetLocalizedChoices() => null;

    public void SetDefaultValue()
    {
        SetBoxedValue(entryBase, GetDefaultValue<bool>(entryBase));
        RefreshValueFromConfig();
    }
}
