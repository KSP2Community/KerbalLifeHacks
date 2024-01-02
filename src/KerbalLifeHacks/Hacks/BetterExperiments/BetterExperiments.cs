using System.Reflection.Emit;
using HarmonyLib;
using KSP.Sim.impl;
using System.Reflection;
using KSP.Modules;
using KSP.Game.Science;

namespace KerbalLifeHacks.Hacks.BetterExperiments;

using PCMSE = PartComponentModule_ScienceExperiment;

[Hack("Automatically resume paused experiments when they return to the appropriate region, and progress without waiting for animations", true)]
public class BetterExperiments : BaseHack
{
    public static BetterExperiments Instance;

    public override void OnInitialized()
    {
        Instance = this;
        HarmonyInstance.PatchAll(typeof(BetterExperiments));
    }

    /// <summary>
    /// Harmony transpiler to ignore part deployment state when running an
    /// experiment, allowing it to progress during part animations.
    /// </summary>
    [HarmonyPatch(typeof(PCMSE), nameof(PCMSE.OnUpdate))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> IgnorePartAnimationState(IEnumerable<CodeInstruction> instructions)
    {
        FieldInfo dataScienceExpField = typeof(PCMSE)
            .GetField(nameof(PCMSE.dataScienceExperiment), AccessTools.all);
        FieldInfo isPartDeployedField = typeof(Data_ScienceExperiment)
            .GetField(nameof(Data_ScienceExperiment.PartIsDeployed), AccessTools.all);

        return new CodeMatcher(instructions)
            .MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, dataScienceExpField),
                new CodeMatch(OpCodes.Ldfld, isPartDeployedField)
            )
            .Repeat(
                matcher => matcher
                    // Rather than removing the instructions outright, we carefully
                    // replace them with nop/ldc to preserve labels.
                    .SetAndAdvance(OpCodes.Nop, null)
                    .SetAndAdvance(OpCodes.Nop, null)
                    .SetAndAdvance(OpCodes.Ldc_I4_1, null),
                notFoundAction: message =>
                    Instance.Logger.LogWarning($"did not find experiment animation code! {message}")
            )
            .InstructionEnumeration();
    }

    /// <summary>
    /// Automatically resume experiments when re-entering the correct research
    /// location for a paused experiment.
    /// </summary>
    [HarmonyPatch(typeof(PCMSE), nameof(PCMSE.OnScienceSituationChanged))]
    [HarmonyPostfix]
    public static void AutomaticallyResumeExperiment(PCMSE __instance)
    {
        var newLocation = new ResearchLocation(
            requiresRegion: __instance._currentLocation.RequiresRegion,
            bodyName: __instance._currentLocation.BodyName,
            scienceSituation: __instance._currentLocation.ScienceSituation,
            scienceRegion: __instance._currentLocation.ScienceRegion
        );

        ref var standings = ref __instance.dataScienceExperiment.ExperimentStandings;
        if (standings.Any(exp => exp.CurrentExperimentState == ExperimentState.RUNNING))
        {
            // Only one experiment can run at a time, so bail out
            return;
        }

        for (int i = 0; i < standings.Count; i++)
        {
            newLocation.RequiresRegion = standings[i].RegionRequired;
            if (standings[i].CurrentExperimentState == ExperimentState.PAUSED && standings[i].ExperimentLocation.Equals(newLocation))
            {
                Instance.Logger.LogInfo($"Resuming experiment {__instance.Part.PartName}/{standings[i].ExperimentID}");
                // Experiment runtime can dip into negative values during high time warp,
                // which causes RunExperiment to reset the experiment progress
                standings[i].CurrentRunningTime = Math.Max(standings[i].CurrentRunningTime, 0.01);
                __instance.RunExperiment(standings[i].ExperimentID);
                return;
            }
        }
    }
}
