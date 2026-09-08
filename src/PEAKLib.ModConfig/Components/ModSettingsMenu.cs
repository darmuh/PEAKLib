using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PEAKLib.ModConfig.SettingOptions;
using PEAKLib.ModConfig.SettingOptions.SettingUI;
using PEAKLib.UI.Elements;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Zorro.Settings;

namespace PEAKLib.ModConfig.Components;

internal class ModSettingsMenu : MonoBehaviour
{
    private void OnEnable()
    {
        RefreshSettings();

        if (ModTabs != null && ModTabs.selectedButton != null)
            ModTabs.Select(ModTabs.selectedButton);
    }

    internal static ModSettingsMenu Instance { get; private set; } = null!;

    public ModdedSettingsTABS ModTabs { get; set; } = null!;
    public ModdedSettingsSectionTABS SectionTabs { get; set; } = null!;

    public Transform Content { get; set; } = null!;

    private List<IBepInExProperty>? settings;

    private readonly List<SettingsUICell> m_spawnedCells = [];

    private Coroutine? m_fadeInCoroutine;

    private string search = "";
    private string selectedSection = "";
    private string selectedMod = "";
    public PeakHorizontalTabs SectionTabController { get; set; } = null!;
    public PeakHorizontalTabs ModTabController { get; set; } = null!;

    internal PeakChildPage MainPage = null!;
    internal PeakDropdown FilterDropdown { get; set; } = null!;

    public int FilterValue = 31; // all options (bits) selected to be shown

    private void Awake()
    {
        ModConfigPlugin.Log.LogDebug("ModdedSettingsMenu Awake");
        Instance = this;
    }

    public void SetSearch(string query)
    {
        search = query.ToLower();
        ShowSettings(); // just to update the settings
    }

    public void SetFilter(int value)
    {
        // set all tabs to active that may have been disabled
        SetAllTabsActive();
        FilterValue = value;
        ShowSettings(); // just to update the settings
    }

    public void SetSection(string section)
    {
        selectedSection = section;
        ShowSettings(); // just to update the settings
    }

    public void ShowSettings()
    {
        if (m_fadeInCoroutine != null)
        {
            StopCoroutine(m_fadeInCoroutine);
            m_fadeInCoroutine = null;
        }

        foreach (SettingsUICell spawnedCell in m_spawnedCells)
            Destroy(spawnedCell.gameObject);

        m_spawnedCells.Clear();
        RefreshSettings();

        if (settings == null)
            return;

        var isSearching = !string.IsNullOrEmpty(search);

        var listing = settings.Where(setting =>
            setting is not IConditionalSetting conditionalSetting || conditionalSetting.ShouldShow()
        );

        // cull items not matching search
        if (isSearching)
            listing = listing.Where(setting =>
                setting.GetDisplayName()?.ToLower()?.Contains(search) == true
            );

        if (!ShouldUseFilterResults(listing, out var beplisting))
            return;

        foreach (IBepInExProperty item in beplisting)
        {
            if (Templates.SettingsCellPrefab == null)
            {
                ModConfigPlugin.Log.LogError("SettingsCellPrefab has not been loaded.");
                return;
            }

            if (item is not Setting setting)
            {
                ModConfigPlugin.Log.LogError("Invalid IExposedSetting");
                continue;
            }

            if (!string.IsNullOrEmpty(selectedSection)) //skip if selected section is empty/null
            {
                //update assigned value from configbase
                item.RefreshValueFromConfig();
            }

            SettingsUICell component = Instantiate(Templates.SettingsCellPrefab, Content)
                .GetComponent<SettingsUICell>();
            m_spawnedCells.Add(component);

            // component.Setup(item as Setting);
            // temporary fix - uncomment component.Setup and remove the region when they set printDebug default to false in LocalizedText.GetText(string id, bool printDebug = true)

            #region temporary fix
            component.m_text.text = item.GetDisplayName();
            component.m_canvasGroup = component.GetComponent<CanvasGroup>();
            component.m_canvasGroup.alpha = 0f;

            Instantiate(setting.GetSettingUICell(), component.m_settingsContentParent)
                .GetComponent<SettingInputUICell>()
                .Setup(setting, GameHandler.Instance.SettingsHandler);
            #endregion
        }

        // get duplicate bindings
        InputBindingSettingUI.RefreshDuplicates();

        m_fadeInCoroutine = StartCoroutine(FadeInCells());
    }

    private void SetAllTabsActive()
    {
        foreach (var tab in ModTabController.Tabs)
            tab.SetActive(true);

        foreach (var tab in SectionTabController.Tabs)
            tab.SetActive(true);
    }

    private bool ShouldUseFilterResults(
        IEnumerable<IBepInExProperty> listing,
        out IEnumerable<IBepInExProperty> outListing
    )
    {
        // set to initially provided listing
        outListing = listing;

        // Filter set to nothing, refresh empty handed
        if (FilterValue == 0)
        {
            m_fadeInCoroutine = StartCoroutine(FadeInCells());
            return false;
        }

        // --- Filter Start

        ModConfigPlugin.Log.LogDebug($"FilterValue: {FilterValue}");
        // check filter flag values
        var boolsEnabled = ((FilterValue & (1 << 0)) != 0);
        var stringsEnabled = ((FilterValue & (1 << 1)) != 0);
        var numbersEnabled = ((FilterValue & (1 << 2)) != 0);
        var enumsEnabled = ((FilterValue & (1 << 3)) != 0);
        var controlsEnabled = ((FilterValue & (1 << 4)) != 0);
        ModConfigPlugin.Log.LogDebug(
            $"bools:{boolsEnabled},strings:{stringsEnabled},numbers:{numbersEnabled},enums:{enumsEnabled},controls:{controlsEnabled}"
        );

        // cull items not matching each filter item
        if (!boolsEnabled)
            outListing = outListing.Where(setting =>
                setting.ConfigBase.SettingType != typeof(bool)
            );

        // typeof(string) will include strings with acceptable values, so use BepInExString type instead
        if (!stringsEnabled)
            outListing = outListing.Where(setting => setting is not BepInExString);

        System.Type[] nums = [typeof(int), typeof(float), typeof(double)];
        if (!numbersEnabled)
            outListing = outListing.Where(setting =>
                !nums.Any(x => setting.ConfigBase.SettingType == x)
            );

        if (!enumsEnabled)
            outListing = outListing.Where(setting => setting is not BepInExEnum);

        if (!controlsEnabled)
            outListing = outListing.Where(setting =>
                setting is not BepInExKeyPath && setting.ConfigBase.SettingType != typeof(KeyCode)
            );

        // --- Filter End

        // If full listing is empty, end here
        if (!outListing.Any())
        {
            m_fadeInCoroutine = StartCoroutine(FadeInCells());
            return false;
        }

        // get only the current mod's settings
        var fullListing = outListing;
        var thisModListing = fullListing.Where(b => b.GetCategory() == selectedMod);

        // get section settings, don't update beplisting yet so we can compare this to it later
        var thisSection = thisModListing.Where(setting =>
            setting.ConfigBase.Definition.Section.Equals(
                selectedSection,
                StringComparison.InvariantCultureIgnoreCase
            )
        );

        // get tabs that do not contain any settings
        var TabsWithoutSetting = SectionTabController.Tabs.FindAll(x => 
            !thisModListing.Any(bep => bep.ConfigBase.Definition.Section == x.name));

        // only hide tabs if setting exists in any other tab
        if (SectionTabController.Tabs.Count != TabsWithoutSetting.Count)
        {
            foreach (var tab in TabsWithoutSetting)
                tab.SetActive(false);
        }

        // current section does not have any items
        if (!thisSection.Any())
        {
            // try to select another section that contains filtered items
            var newSection = SectionTabController.Tabs.FirstOrDefault(x =>
                thisModListing.Any(bep => bep.ConfigBase.Definition.Section == x.name)
            );
            if (newSection != null)
            {
                // hide current tab
                if (SectionTabController.TryGetTab(selectedSection, out GameObject currentTab))
                    currentTab.SetActive(false);

                ModConfigPlugin.Log.LogDebug($"Updating mod section to {newSection.name}");
                SectionTabs.Select(newSection.GetComponent<ModdedTABSButton>());
            }
            else
            {
                var ModsWithSetting = ModTabController.Tabs.FindAll(x => fullListing.Any(bep => bep.GetCategory() == x.name));
                var ModsWithoutSetting = ModTabController.Tabs.Where(x => !ModsWithSetting.Contains(x));
                if (ModsWithSetting.Count == 0 || !ModTabController.TryGetTab(selectedMod, out GameObject currentMod))
                    return false;

                // last resort, try to select a mod that contains the setting
                GameObject? newMod = null!;

                var currentIndex = ModTabController.Tabs.IndexOf(currentMod);

                // set to next closest tab to the left
                if (ModsWithSetting.All(m => ModTabController.Tabs.IndexOf(m) < currentIndex))
                {
                    ModConfigPlugin.Log.LogDebug($"All tabs have a lower index than {currentIndex}");
                    int nextIndex = 0; 
                    foreach (var tab in ModsWithSetting)
                    {
                        int modIndex = ModTabController.Tabs.IndexOf(tab);
                        ModConfigPlugin.Log.LogDebug($"{nextIndex} > {modIndex}");
                        // get closest (highest) value to current index
                        if (nextIndex < modIndex)
                        {
                            newMod = tab;
                            nextIndex = modIndex;
                        }
                    }
                }
                else
                {
                    ModConfigPlugin.Log.LogDebug($"Finding next lowest index after {currentIndex}");
                    // set to next closest tab to the right
                    int nextIndex = ModTabController.Tabs.Count;
                    foreach (var tab in ModsWithSetting)
                    {
                        int modIndex = ModTabController.Tabs.IndexOf(tab);
                        ModConfigPlugin.Log.LogDebug($"{nextIndex} < {modIndex} && {modIndex} > {currentIndex}");
                        // get lowest index above current index
                        if (nextIndex > modIndex && modIndex > currentIndex) 
                        {
                            newMod = tab;
                            nextIndex = modIndex;
                        }
                    }
                }

                if (newMod == null)
                    return false;

                // hide mods without setting
                foreach(var mod in ModsWithoutSetting)
                    mod.SetActive(false);

                ModConfigPlugin.Log.LogDebug($"Updating selected mod to {newMod.name}");
                ModTabs.Select(newMod.GetComponent<ModdedTABSButton>());
            }

            // do not continue with original ShowSettings, regardless of the above results
            return false;
        }

        // return just this section
        outListing = thisSection;
        return true;
    }

    private static string GetKeyValue(IBepInExProperty item)
    {
        if (item is BepInExKeyPath keyCodePath)
            return keyCodePath.Value;

        if (item is BepInExKeyCode keyCode)
            return keyCode.Value.ToString();

        return string.Empty;
    }

    public static string GetMatchingValues(IBepInExProperty item)
    {
        string keyvalue = GetKeyValue(item);

        if (keyvalue == "None" || Instance == null || Instance.settings == null)
            return string.Empty;

        bool itemIsKeyCode = (item.ConfigBase.SettingType == typeof(KeyCode));
        bool itemIsPath = (item.ConfigBase.SettingType == typeof(string));

        // assume a control is kbm unless otherwise found
        InputBindingDevice deviceType = InputBindingDevice.Keyboard;

        if (itemIsKeyCode)
        {
            KeyCode key = item.GetKeyValue<KeyCode>();
            keyvalue = InputBindingDisplay.GetSpriteTagText(key);
            if (key.ToString().StartsWith("Joystick", StringComparison.Ordinal))
                deviceType = InputBindingDevice.Gamepad; // this is the only time keycode changes from kbm
        }

        if (itemIsPath)
        {
            string keyPath = item.GetKeyValue<string>();
            keyvalue = InputBindingDisplay.GetSpriteTagText(keyPath);
            deviceType = InputBindingPath.GetDevice(keyPath);
        }

        ModConfigPlugin.Log.LogDebug($"comparing {keyvalue}");

        string matchingControls = string.Empty;

        foreach (var key in Instance.settings)
        {
            bool match = false;

            if (key != item && key is BepInExKeyCode keyCode)
            {
                match = keyvalue == InputBindingDisplay.GetSpriteTagText(keyCode.Value);

                ModConfigPlugin.Log.LogDebug($"{keyCode.Value} - {match}");
            }

            if (key != item && key is BepInExKeyPath keyPath)
            {
                match = keyvalue == InputBindingDisplay.GetSpriteTagText(keyPath.Value);

                ModConfigPlugin.Log.LogDebug($"{keyPath.Value} - {match}");
            }

            if (match)
                matchingControls += $"({key.GetCategory()}) {key.ConfigBase.Definition.Key}, ";
        }

        // This should just be vanilla controls, modders don't need to insert controls in here usually
        List<string> matchingvanilla = [];
        foreach (InputAction action in InputSystem.actions)
        {
            foreach (var bind in action.bindings)
            {
                if (bind.action.Equals("AnyKey", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (
                    InputBindingDisplay.CompareVanillaToSprite(
                        keyvalue,
                        bind.effectivePath,
                        deviceType
                    )
                )
                    matchingvanilla.Add(bind.action);
            }
        }

        matchingvanilla = [.. matchingvanilla.Distinct()];

        foreach (string match in matchingvanilla)
            matchingControls += $"{match}, ";

        return matchingControls;
    }

    public void RefreshSettings()
    {
        if (GameHandler.Instance != null)
        {
            var exposedsettings =
                GameHandler.Instance.SettingsHandler.GetSettingsThatImplements<IBepInExProperty>();
            settings = [.. exposedsettings.Where(setting => setting.ConfigBase != null)]; // fix for autoreload mods?
        }
    }

    public void UpdateSectionTabs(string modName)
    {
        selectedMod = modName;
        if (ModSectionNames.TryGetModSections(modName, out List<string> sections))
        {
            if (SectionTabController.Tabs.Count > 0)
            {
                //Remove existing tabs
                for (int i = SectionTabController.Tabs.Count - 1; i >= 0; i--)
                    SectionTabController.DeleteTab(SectionTabController.Tabs[i].name);
            }

            List<ModdedTABSButton> sectionButtons = [];
            foreach (string section in sections)
            {
                GameObject tab = SectionTabController.AddTab(section);
                var sectionButton = tab.AddComponent<ModdedTABSButton>();
                sectionButton.category = section;
                sectionButton.text = tab.GetComponentInChildren<TextMeshProUGUI>();
                sectionButton.SelectedGraphic = tab.transform.Find("Selected").gameObject;
                sectionButtons.Add(sectionButton);
            }

            var sectionTab = sectionButtons[0].GetComponent<ModdedTABSButton>();
            SectionTabs.Select(sectionTab);
        }
    }

    private IEnumerator FadeInCells()
    {
        int i = 0;
        foreach (SettingsUICell spawnedCell in m_spawnedCells)
        {
            spawnedCell.FadeIn();
            yield return new WaitForSecondsRealtime(0.05f);
            i++;
        }

        m_fadeInCoroutine = null;
    }
}
