using System.Collections.Generic;
using PEAKLib.Core;
using PEAKLib.ModConfig.Components;
using PEAKLib.UI;
using PEAKLib.UI.Elements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zorro.Core;
using Zorro.Settings;
using Zorro.Settings.UI;
using Object = UnityEngine.Object;

namespace PEAKLib.ModConfig.SettingOptions.SettingUI;

internal abstract class InputBindingSettingUI : SettingInputUICell
{
    internal static List<InputBindingSettingUI> Cells = [];
    public TextMeshProUGUI? DuplicateWarningText { get; set; }
    public Setting KeySetting { get; set; } = null!;
    public Button KeyButton { get; set; } = null!;
    public PeakMenuButton SetDefaultButton { get; internal set; } = null!;
    public PeakMenuButton ClearBindButton { get; internal set; } = null!;
    public TextMeshProUGUI KeyText { get; set; } = null!;

    protected void SetupBinding(Setting setting)
    {
        Cells.Add(this);
        KeySetting = setting;

        KeyButton = gameObject.GetComponent<Button>();
        KeyText = KeyButton.GetComponentInChildren<TextMeshProUGUI>();

        SetupDefaultButton();
        SetupClearButton();
        SetupDuplicateBindText();

        ThrowHelper.ThrowIfArgumentNull(KeySetting);
        ThrowHelper.ThrowIfArgumentNull(KeyButton);
        ThrowHelper.ThrowIfArgumentNull(KeyText);
        ThrowHelper.ThrowIfArgumentNull(SetDefaultButton);
        ThrowHelper.ThrowIfArgumentNull(ClearBindButton);

        RegisterSettingListener(setting);
    }

    private void SetupDefaultButton()
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

    private void SetupClearButton()
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

    private void SetupDuplicateBindText()
    {
        var component = transform.parent.parent.Find("OnlyOnMainMenu").gameObject;
        var rect = component.GetComponent<RectTransform>();
        rect.anchoredPosition = new(-1060f, -25f);
        rect.sizeDelta = new(600f, 0);
        DuplicateWarningText = component.GetComponent<TextMeshProUGUI>();
    }

    internal static void RefreshDuplicates()
    {
        foreach (var item in Cells)
        {
            item.GetDuplicateControls();
        }
    }

    internal void GetDuplicateControls()
    {
        if (DuplicateWarningText == null)
            return;

        if (KeySetting is IBepInExProperty bep)
        {
            string match = ModSettingsMenu.GetMatchingValues(bep);

            // build warning string
            if (match != string.Empty)
            {
                match = match.TrimEnd().TrimEnd(',');
                DuplicateWarningText.gameObject.SetActive(true);
                DuplicateWarningText.text = $"This key is bound to: {match}";
            }
            else
                DuplicateWarningText.gameObject.SetActive(false);
        }
    }

    protected void ShowCapturePrompt() => KeyText.text = "SELECT A KEY";

    protected virtual void OnDisable() => CancelCapture();

    protected override void OnDestroy()
    {
        CancelCapture();
        Cells.Remove(this);
        base.OnDestroy();
    }

    public abstract void SetDefaultValue();
    public abstract void ClearValue();

    private void CancelCapture()
    {
        if (
            ModConfigPlugin.instance != null
            && ModConfigPlugin.instance.InputBindingCapture != null
        )
            ModConfigPlugin.instance.InputBindingCapture.Cancel(this);
    }
}

internal static class InputBindingSettingCellFactory
{
    public static GameObject? Create<TSettingUI>(string name)
        where TSettingUI : InputBindingSettingUI
    {
        InputCellMapper? mapper = SingletonAsset<InputCellMapper>.Instance;
        if (mapper == null || mapper.FloatSettingCell == null)
            return null;

        GameObject cell = Object.Instantiate(mapper.FloatSettingCell);
        cell.name = name;

        FloatSettingUI? oldFloatSetting = cell.GetComponent<FloatSettingUI>();
        if (oldFloatSetting == null)
        {
            ModConfigPlugin.Log.LogError("FloatSettingCell is missing FloatSettingUI.");
            Object.Destroy(cell);
            return null;
        }

        cell.AddComponent<TSettingUI>();
        RectTransform inputRectTransform = oldFloatSetting.inputField.GetComponent<RectTransform>();
        inputRectTransform.pivot = new Vector2(0.5f, 0.5f);
        inputRectTransform.offsetMin = new Vector2(20, -25);
        inputRectTransform.offsetMax = new Vector2(380, 25);

        var keybutton = cell.AddComponent<Button>();
        oldFloatSetting.inputField.name = "Key Button Input";
        Object.DestroyImmediate(oldFloatSetting.inputField.placeholder.gameObject);
        Object.Destroy(oldFloatSetting.inputField);
        Object.DestroyImmediate(oldFloatSetting.slider.gameObject);
        Object.DestroyImmediate(oldFloatSetting);

        var text = keybutton.GetComponentInChildren<TextMeshProUGUI>();
        text.enableAutoSizing = true;
        text.fontSize = text.fontSizeMax = 28;
        text.fontSizeMin = 18;
        text.alignment = TextAlignmentOptions.Midline;

        Object.DontDestroyOnLoad(cell);
        return cell;
    }
}
