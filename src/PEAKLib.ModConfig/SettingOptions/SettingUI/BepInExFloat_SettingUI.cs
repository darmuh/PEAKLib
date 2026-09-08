using System;
using System.Collections.Generic;
using System.Text;
using PEAKLib.UI;
using PEAKLib.UI.Elements;
using UnityEngine;
using Zorro.Settings;
using Zorro.Settings.UI;

namespace PEAKLib.ModConfig.SettingOptions.SettingUI
{
    public class BepInExFloat_SettingUI : FloatSettingUI
    {
        public PeakMenuButton SetDefaultButton { get; internal set; } = null!;
        public Setting KeySetting { get; set; } = null!;

        public override void Setup(Setting setting, ISettingHandler settingHandler)
        {
            if (setting == null || setting is not BepInExFloat)
                return;

            KeySetting = setting;
            ScootAndScaleSlider();
            AddDefaultButton();

            base.Setup(setting, settingHandler);
        }

        private void ScootAndScaleSlider()
        {
            var slider = base.slider.gameObject.GetComponent<RectTransform>();
            slider.anchoredPosition = new(16f, 0f);
            slider.sizeDelta = new(-220f, 20f);
        }

        private void AddDefaultButton()
        {
            SetDefaultButton = MenuAPI
                .CreateMenuButton("DefaultsButton")
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

        public void SetDefaultValue()
        {
            if (KeySetting is not IBepInExProperty bep)
                return;

            bep.SetDefaultValue();
            OnSettingChangedExternal(KeySetting);
        }
    }
}
