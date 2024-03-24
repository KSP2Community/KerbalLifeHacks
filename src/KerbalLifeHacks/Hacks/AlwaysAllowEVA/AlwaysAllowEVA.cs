using HarmonyLib;
using KSP.Sim.impl;

namespace KerbalLifeHacks.Hacks.AlwaysAllowEVA;

/// <summary>
/// Ported over from EVA ANARCHY by ThunderousEcho, with blessing from the original author
/// Original repository: https://github.com/ThunderousEcho/EvaAnarchy
/// </summary>
[Hack(name: "Allows EVA even when EVA is disabled due to an obstacle.", isEnabledByDefault: false)]
public class AlwaysAllowEVA : BaseHack
{
    public override void OnInitialized()
    {
        HarmonyInstance.PatchAll(typeof(AlwaysAllowEVA));
    }
    
    [HarmonyPatch(typeof(IVAPortraitEVAObstacleDetector), nameof(IVAPortraitEVAObstacleDetector.IsEVADisabledByObstacle))]
    [HarmonyPrefix]
    public static bool IVAPortraitEVAObstacleDetector_IsEVADisabledByObstacle(ref bool __result)
    {
        __result = false;
        return false;
    }
}