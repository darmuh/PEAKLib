using System;
using BepInEx.Configuration;
using PEAKLib.UI;
using PEAKLib.UI.Elements;
using TMPro;
using UnityEngine;
using Zorro.Core;
using Zorro.Settings;
using Zorro.Settings.UI;
using static PEAKLib.ModConfig.SettingsHandlerUtility;
using Object = UnityEngine.Object;

namespace PEAKLib.ModConfig.SettingOptions;

internal class BepInExString(
    ConfigEntryBase entryBase,
    string category = "Mods",
    Action<string>? saveCallback = null,
    Action<BepInExString>? onApply = null
) : StringSetting, IBepInExProperty, IExposedSetting
{
    ConfigEntryBase IBepInExProperty.ConfigBase
    {
        get => entryBase;
    }

    public string PlaceholderText { get; set; } = GetDefaultValue<string>(entryBase) ?? "";
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

                _settingUICell = Object.Instantiate(
                    SingletonAsset<InputCellMapper>.Instance.FloatSettingCell
                );
                _settingUICell.name = "BepInExStringCell";

                var oldFloatSetting = _settingUICell.GetComponent<FloatSettingUI>();
                var newStringSetting = _settingUICell.AddComponent<StringSettingUI>();
                newStringSetting.inputField = oldFloatSetting.inputField;

                Object.DestroyImmediate(oldFloatSetting.slider.gameObject);
                Object.DestroyImmediate(oldFloatSetting);

                newStringSetting.inputField.characterValidation = TMP_InputField
                    .CharacterValidation
                    .None;
                var inputRectTransform = newStringSetting.inputField.GetComponent<RectTransform>();
                inputRectTransform.pivot = new Vector2(0.5f, 0.5f);
                inputRectTransform.offsetMin = new Vector2(20, -25);
                inputRectTransform.offsetMax = new Vector2(380, 25);

                var texts = newStringSetting.inputField.GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var text in texts)
                {
                    text.fontSize = text.fontSizeMin = text.fontSizeMax = 22;
                    text.alignment = TextAlignmentOptions.MidlineLeft;
                }

                Object.DontDestroyOnLoad(_settingUICell);
            }

            return _settingUICell;
        }
    }

    public override void Load(ISettingsSaveLoad loader) =>
        Value = GetCurrentValue<string>(entryBase);

    public override void Save(ISettingsSaveLoad saver) => saveCallback?.Invoke(Value);

    public override void ApplyValue() => onApply?.Invoke(this);

    public override GameObject? GetSettingUICell() => SettingUICell;

    public string GetCategory() => category;

    public string GetDisplayName() => entryBase.Definition.Key;

    protected override string GetDefaultValue() => GetDefaultValue<string>(entryBase);

    public void RefreshValueFromConfig() => Value = GetCurrentValue<string>(entryBase);

    public void SetDefaultValue()
    {
        Value = GetDefaultValue();
        SetBoxedValue(entryBase, Value);
        OnSettingChangedExternal();
    }

    public void ClearValue()
    {
        Value = string.Empty;
        SetBoxedValue(entryBase, Value);
        OnSettingChangedExternal();
    }
}

public class StringSettingUI : SettingInputUICell
{
    public PeakMenuButton SetDefaultButton { get; internal set; } = null!;
    public PeakMenuButton ClearBindButton { get; internal set; } = null!;
    public Setting KeySetting { get; set; } = null!;
    public TMP_InputField? inputField;

    public override void Setup(Setting setting, ISettingHandler settingHandler)
    {
        if (inputField == null || setting == null || setting is not BepInExString stringSetting)
            return;

        KeySetting = setting;
        AddDefaultButton();
        AddClearButton();

        RegisterSettingListener(setting);

        inputField.SetTextWithoutNotify(stringSetting.Value);
        inputField.onValueChanged.AddListener(OnChanged);

        void OnChanged(string str)
        {
            inputField.SetTextWithoutNotify(str);
            stringSetting.SetValue(str, settingHandler);
        }

        var texts = inputField.GetComponentsInChildren<TextMeshProUGUI>();

        foreach (var text in texts)
            if (text.name == "Placeholder")
                text.text = stringSetting.PlaceholderText;
    }

    private void AddDefaultButton()
    {
        SetDefaultButton = MenuAPI
            .CreateMenuButton("DefaultsButton")
            .ParentTo(transform)
            .SetSize(new(90f, 20f))
            .SetAnchorMin(new(0.5f, 0.5f))
            .SetAnchorMax(new(0.5f, 0.5f))
            .SetPosition(new(186f, 12f))
            .OnClick(SetDefaultValue)
            .SetText("DEFAULT")
            .SetBorderColor(Color.white)
            .SetColor(Color.dodgerBlue);

        SetDefaultButton.Text.rectTransform.offsetMin = new(10f, 10f);
        SetDefaultButton.Text.rectTransform.offsetMax = new(-10f, -10f);
        SetDefaultButton.Text.fontSizeMax = 16f;
        SetDefaultButton.Text.fontSizeMin = 16f;
    }

    private void AddClearButton()
    {
        ClearBindButton = MenuAPI
            .CreateMenuButton("ClearButton")
            .ParentTo(transform)
            .SetSize(new(90f, 20f))
            .SetAnchorMin(new(0.5f, 0.5f))
            .SetAnchorMax(new(0.5f, 0.5f))
            .SetPosition(new(186f, -14f))
            .OnClick(ClearValue)
            .SetText("CLEAR");

        ClearBindButton.Text.rectTransform.offsetMin = new(10f, 10f);
        ClearBindButton.Text.rectTransform.offsetMax = new(-10f, -10f);
        ClearBindButton.Text.fontSizeMax = 16f;
        ClearBindButton.Text.fontSizeMin = 16f;
    }

    private void ClearValue()
    {
        if (KeySetting is not BepInExString bepString)
            return;

        bepString.ClearValue();
    }

    private void SetDefaultValue()
    {
        if (KeySetting is not IBepInExProperty bep)
            return;

        bep.SetDefaultValue();
        OnSettingChangedExternal(KeySetting);
    }

    protected override void OnSettingChangedExternal(Setting setting)
    {
        base.OnSettingChangedExternal(setting);

        if (inputField != null && setting is BepInExString stringSetting)
            inputField.SetTextWithoutNotify(stringSetting.Value);
    }
}
