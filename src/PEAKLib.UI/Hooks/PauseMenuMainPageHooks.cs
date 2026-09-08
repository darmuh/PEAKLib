using Md.PauseMenuMainPage;
using MonoDetour;
using MonoDetour.HookGen;
using PEAKLib.UI.Elements;

namespace PEAKLib.UI.Hooks;

[MonoDetourTargets(typeof(PauseMenuMainPage))]
static class PauseMenuMainPageHooks
{
    [MonoDetourHookInitialize]
    static void Init()
    {
        Awake.Postfix(PostfixAwake);
    }

    private static void PostfixAwake(PauseMenuMainPage self)
    {
        MenuAPI.pauseMenuBuilderDelegate?.Invoke(self.transform);

        var settings = self.transform.parent.Find("SettingsPage");
        MenuAPI.settingsMenuBuilderDelegateNEW?.Invoke(settings);

        var controls = self.transform.parent.Find("ControlsPage");
        Templates.InitializeControlsPageTemplates(controls.GetComponent<PauseMenuControlsPage>());
        MenuAPI.controlsMenuBuilderDelegate?.Invoke(controls);
    }
}
