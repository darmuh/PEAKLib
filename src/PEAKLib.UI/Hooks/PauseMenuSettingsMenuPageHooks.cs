using Md.PauseMenuSettingsMenuPage;
using MonoDetour;
using MonoDetour.HookGen;

namespace PEAKLib.UI.Hooks;

[MonoDetourTargets(typeof(PauseMenuSettingsMenuPage))]
static class PauseMenuSettingsMenuPageHooks
{
    [MonoDetourHookInitialize]
    static void Init()
    {
        Start.Prefix(Prefix_Start);
    }

    static void Prefix_Start(PauseMenuSettingsMenuPage self)
    {
        // keep compatibility with old method
        MenuAPI.settingsMenuBuilderDelegate?.Invoke(self.gameObject.transform);
    }
}
