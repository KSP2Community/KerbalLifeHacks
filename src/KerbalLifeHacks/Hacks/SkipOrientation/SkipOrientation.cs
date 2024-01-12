using HarmonyLib;
using KSP.Game;
using KSP.Messages;
using KSP.VFX;

namespace KerbalLifeHacks.Hacks.SkipOrientation;

[Hack("Skip Orientation")]
public class SkipOrientation: BaseHack
{
    public static SkipOrientation Instance;

    public override void OnInitialized()
    {
        Instance = this;
        HarmonyInstance.PatchAll(typeof(SkipOrientation));
    }
    
    /// <summary>
    /// By default KSP.Game.CreateCampaignMenu._isFTUEEnabled is set to true
    /// This patch will set it to false always, after the new campaign menu is opened.
    /// </summary>
    [HarmonyPatch(typeof(CreateCampaignMenu), nameof(CreateCampaignMenu.OnEnable), MethodType.Normal)]
    [HarmonyPostfix]
    public static void OrientationStartDisabled(CreateCampaignMenu __instance)
    {
        __instance._isFTUEEnabled.SetValue(false);
    }
}