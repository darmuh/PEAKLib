using Md.PauseMenuMainPage;
using MonoDetour;
using MonoDetour.HookGen;
using PEAKLib.UI.Elements;

namespace PEAKLib.UI.Hooks;

[MonoDetourTargets(typeof(PauseMenuMainPage))]
static class PauseMenuMainPageHooks
{
    internal static bool RunOnce = false;

    [MonoDetourHookInitialize]
    static void Init()
    {
        Start.Postfix(PostfixEnable);
    }

    private static void PostfixEnable(PauseMenuMainPage self)
    {
        MenuAPI.pauseMenuBuilderDelegate?.Invoke(self.transform);

        var controls = self.transform.parent.Find("ControlsPage");
        Templates.InitializeControlsPageTemplates(controls.GetComponent<PauseMenuControlsPage>());
        MenuAPI.controlsMenuBuilderDelegate?.Invoke(controls);
    }
}
