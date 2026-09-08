using Zorro.Settings;

namespace PEAKLib.ModConfig.SettingOptions.SettingUI;

internal class BepInExKeyPath_SettingUI : InputBindingSettingUI
{
    public override void ClearValue()
    {
        if (KeySetting is not BepInExKeyPath keyPathSetting)
            return;

        if (keyPathSetting.Value == "None")
            return;

        keyPathSetting.SetValue("None", SettingsHandler.Instance);
        OnSettingChangedExternal(KeySetting);
    }

    public override void SetDefaultValue()
    {
        if (KeySetting is not BepInExKeyPath keyPathSetting)
            return;

        if (keyPathSetting.Value == keyPathSetting.DefaultValue)
            return;

        keyPathSetting.SetValue(keyPathSetting.DefaultValue, SettingsHandler.Instance);
        OnSettingChangedExternal(KeySetting);
    }

    public override void Setup(Setting setting, ISettingHandler settingHandler)
    {
        if (setting is not BepInExKeyPath keyPathSetting)
            return;

        SetupBinding(setting);
        InputBindingDisplay.SetText(KeyText, keyPathSetting.Value);
        KeyButton.onClick.AddListener(() => StartKeybindCapture(keyPathSetting, settingHandler));
    }

    protected override void OnSettingChangedExternal(Setting setting)
    {
        base.OnSettingChangedExternal(setting);

        if (KeyText != null && setting is BepInExKeyPath keyPathSetting)
            InputBindingDisplay.SetText(KeyText, keyPathSetting.Value);

        // refresh warnings
        InputBindingSettingUI.RefreshDuplicates();
    }

    private void StartKeybindCapture(BepInExKeyPath setting, ISettingHandler settingHandler)
    {
        bool started = ModConfigPlugin.instance.InputBindingCapture.TryCapturePath(
            this,
            path =>
            {
                try
                {
                    setting.SetValue(path, settingHandler);
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
