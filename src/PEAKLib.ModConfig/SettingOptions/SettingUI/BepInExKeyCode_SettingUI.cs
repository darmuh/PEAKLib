using UnityEngine;
using Zorro.Settings;

namespace PEAKLib.ModConfig.SettingOptions.SettingUI;

internal class BepInExKeyCode_SettingUI : InputBindingSettingUI
{
    public override void ClearValue()
    {
        if (KeySetting is not BepInExKeyCode keyCodeSetting)
            return;

        if (keyCodeSetting.Value == KeyCode.None)
            return;

        keyCodeSetting.SetValue(KeyCode.None, SettingsHandler.Instance);
        OnSettingChangedExternal(KeySetting);
    }

    public override void SetDefaultValue()
    {
        if (KeySetting is not BepInExKeyCode keyCodeSetting)
            return;

        if (keyCodeSetting.Value == keyCodeSetting.DefaultValue)
            return;

        keyCodeSetting.SetValue(keyCodeSetting.DefaultValue, SettingsHandler.Instance);
        OnSettingChangedExternal(KeySetting);
    }

    public override void Setup(Setting setting, ISettingHandler settingHandler)
    {
        if (setting is not BepInExKeyCode keyCodeSetting)
            return;

        SetupBinding(setting);
        InputBindingDisplay.SetText(KeyText, keyCodeSetting.Value);
        KeyButton.onClick.AddListener(() => StartKeybindCapture(keyCodeSetting, settingHandler));
    }

    protected override void OnSettingChangedExternal(Setting setting)
    {
        base.OnSettingChangedExternal(setting);

        if (KeyText != null && setting is BepInExKeyCode keyCode)
            InputBindingDisplay.SetText(KeyText, keyCode.Value);

        // refresh warnings
        InputBindingSettingUI.RefreshDuplicates();
    }

    private void StartKeybindCapture(BepInExKeyCode setting, ISettingHandler settingHandler)
    {
        bool started = ModConfigPlugin.instance.InputBindingCapture.TryCaptureKeyCode(
            this,
            keyCode =>
            {
                try
                {
                    setting.SetValue(keyCode, settingHandler);
                }
                finally
                {
                    OnSettingChangedExternal(setting);
                }
            },
            () => OnSettingChangedExternal(setting)
        );

        if (started)
            ShowCapturePrompt();
    }
}
