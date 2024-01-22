using HarmonyLib;
using I2.Loc;
using KSP.Api.CoreTypes;
using KSP.Audio;
using KSP.Map;
using UnityEngine;

namespace KerbalLifeHacks.Hacks.WarpToOrbitalPoint;

[Hack("Add buttons in map view to warp to orbital points")]
public class WarpToOrbitalPoint : BaseHack
{
    private const string ContainerName = "Container";
    private const string WarpToButtonName = "BTN-WarpTo-ContextPanelButton";

    private const string WarpToApButtonName = "BTN-WarpToAp-ContextPanelButton";
    private const string WarpToPeButtonName = "BTN-WarpToPe-ContextPanelButton";
    private const string WarpToSOIButtonName = "BTN-WarpToSOI-ContextPanelButton";

    private const string WarpToApButtonKey = "KerbalLifeHacks/Map/WarpToAp";
    private const string WarpToPeButtonKey = "KerbalLifeHacks/Map/WarpToPe";
    private const string WarpToSOIButtonKey = "KerbalLifeHacks/Map/WarpToSOI";

    public override void OnInitialized()
    {
        HarmonyInstance.PatchAll(typeof(WarpToOrbitalPoint));
    }

    [HarmonyPatch(typeof(Map3DManeuvers), nameof(Map3DManeuvers.Configure))]
    [HarmonyPrefix]
    private static void ConfigurePrefix(
        // ReSharper disable once InconsistentNaming
        Map3DManeuvers __instance,
        // ReSharper disable once InconsistentNaming
        ref bool __state
    )
    {
        // Save the state of the maneuver popup before the original method is called
        __state = __instance._maneuverPopupInstance != null;
    }

    private static int _nextSiblingIndex;

    [HarmonyPatch(typeof(Map3DManeuvers), nameof(Map3DManeuvers.Configure))]
    [HarmonyPostfix]
    private static void ConfigurePostfix(
        // ReSharper disable once InconsistentNaming
        Map3DManeuvers __instance,
        // ReSharper disable once InconsistentNaming
        bool __state
    )
    {
        // If the maneuver popup was already configured before the method was called, we don't need to do anything
        if (__state)
        {
            return;
        }

        _nextSiblingIndex = 4;

        var popupContainer = __instance._maneuverPopupInstance.gameObject.GetChild(ContainerName);
        var warpToButton = popupContainer.GetChild(WarpToButtonName);

        CreateButton(
            warpToButton,
            popupContainer.transform,
            WarpToApButtonName,
            WarpToApButtonKey,
            () => OnWarpToAp(__instance)
        );
        CreateButton(
            warpToButton,
            popupContainer.transform,
            WarpToPeButtonName,
            WarpToPeButtonKey,
            () => OnWarpToPe(__instance)
        );
        CreateButton(warpToButton,
            popupContainer.transform,
            WarpToSOIButtonName,
            WarpToSOIButtonKey,
            () => OnWarpToSOI(__instance)
        );
    }

    [HarmonyPatch(typeof(Map3DManeuvers), nameof(Map3DManeuvers.ShowManeuverPopup))]
    [HarmonyPostfix]
    // ReSharper disable once InconsistentNaming
    private static void ShowManeuverPopupPostfix(Map3DManeuvers __instance)
    {
        var popupContainer = __instance._maneuverPopupInstance.gameObject.GetChild(ContainerName);
        var warpToApButton = popupContainer.GetChild(WarpToApButtonName);
        var warpToPeButton = popupContainer.GetChild(WarpToPeButtonName);
        var warpToSOIButton = popupContainer.GetChild(WarpToSOIButtonName);

        var currentUT = __instance._game.UniverseModel.UniverseTime;
        var orbitPatch = __instance._representedRenderData.Segment.OrbitPatch;
        var timeAtAp = orbitPatch.StartUT + orbitPatch.TimeToAp;
        var timeAtPe = orbitPatch.StartUT + orbitPatch.TimeToPe;
        var timeAtSOI = orbitPatch.UniversalTimeAtSoiEncounter;

        warpToSOIButton.SetActive(timeAtSOI >= 0);
        warpToApButton.SetActive(timeAtAp < orbitPatch.EndUT);
        warpToPeButton.SetActive(timeAtPe < orbitPatch.EndUT && (timeAtPe > currentUT || timeAtSOI < 0));
    }

    private static void WarpTo(Map3DManeuvers instance, double time)
    {
        instance._game.ViewController.TimeWarp.WarpTo(time - 30);
        KSPAudioEventManager.OnMapModeWarpTo();
        instance.HideManeuverPopup();
    }

    private static void OnWarpToAp(Map3DManeuvers instance)
    {
        var orbitPatch = instance._representedRenderData.Segment.OrbitPatch;
        var apTime = orbitPatch.StartUT + orbitPatch.TimeToAp;
        WarpTo(instance, apTime);
    }

    private static void OnWarpToPe(Map3DManeuvers instance)
    {
        var orbitPatch = instance._representedRenderData.Segment.OrbitPatch;
        var peTime = orbitPatch.StartUT + orbitPatch.TimeToPe;
        WarpTo(instance, peTime);
    }

    private static void OnWarpToSOI(Map3DManeuvers instance)
    {
        var soiTime = instance._representedRenderData.Segment.OrbitPatch.UniversalTimeAtSoiEncounter;
        WarpTo(instance, soiTime);
    }

    private static void CreateButton(
        GameObject original,
        Transform parent,
        string name,
        string locKey,
        Action onClick
    )
    {
        var button = Instantiate(original, parent);
        button.name = name;
        button.transform.SetSiblingIndex(_nextSiblingIndex++);
        button.GetChild("Text (TMP)").GetComponent<Localize>().SetTerm(locKey);
        button.GetComponent<UIAction_Void_Button>().action = new DelegateAction(onClick);
    }
}