using PEAKLib.UI;
using PEAKLib.UI.Elements;
using TMPro;
using UnityEngine;
using Zorro.Settings;

namespace PEAKLib.ModConfig.SettingOptions.SettingUI;

internal class BepInExInt_SettingUI : SettingInputUICell
{
    public PeakMenuButton SetDefaultButton { get; internal set; } = null!;
    public Setting KeySetting { get; set; } = null!;
    public TMP_InputField? inputField;

    public override void Setup(Setting setting, ISettingHandler settingHandler)
    {
        if (inputField == null || setting == null || setting is not BepInExInt intSetting)
            return;

        KeySetting = setting;
        AddDefaultButton();

        RegisterSettingListener(setting);
        inputField.SetTextWithoutNotify(intSetting.Expose(intSetting.Value));
        inputField.onValueChanged.AddListener(OnChanged);

        void OnChanged(string str)
        {
            if (int.TryParse(str, out var result))
            {
                inputField.SetTextWithoutNotify(intSetting.Expose(result));
                intSetting.SetValue(result, settingHandler);
            }
        }
    }

    private void AddDefaultButton()
    {
        SetDefaultButton = MenuAPI.CreateMenuButton("DefaultsButton")
        .ParentTo(transform)
        .SetSize(new(90f, 30f))
        .SetAnchorMin(new(0.5f, 0.5f))
        .SetAnchorMax(new(0.5f, 0.5f))
        .SetPosition(new(186f, 0f))
        .OnClick(SetDefaultValue)
        .SetText("DEFAULT")
        .SetBorderColor(Color.white)
        .SetColor(Color.dodgerBlue);

        SetDefaultButton.Text.rectTransform.offsetMin = new(10f, 10f);
        SetDefaultButton.Text.rectTransform.offsetMax = new(-10f, -10f);
        SetDefaultButton.Text.fontSizeMax = 16f;
        SetDefaultButton.Text.fontSizeMin = 16f;
    }

    protected override void OnSettingChangedExternal(Setting setting)
    {
        base.OnSettingChangedExternal(setting);

        if (inputField != null && setting is BepInExInt intSetting)
            inputField.SetTextWithoutNotify(intSetting.Expose(intSetting.Value));
    }

    private void SetDefaultValue()
    {
        if (KeySetting is not IBepInExProperty bep)
            return;

        bep.SetDefaultValue();
        OnSettingChangedExternal(KeySetting);
    }
}
