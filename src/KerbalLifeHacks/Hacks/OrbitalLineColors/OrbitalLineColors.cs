using HarmonyLib;
using KSP.Map;
using KSP.Sim;
using SpaceWarp.API.Game;
using UnityEngine;

namespace KerbalLifeHacks.Hacks.OrbitalLineColors;

[Hack("Use various colors for orbital lines")]
public class OrbitalLineColors : BaseHack
{
    private static readonly List<Color> TrajectoryColors =
    [
        new Color(0, 0.5f, 1), // Light Blue
        new Color(0.5f, 1, 1), // Light Cyan
        new Color(0.5f, 0.5f, 1), // Light Blue
        new Color(0.5f, 1, 0.5f), // Light Green
        new Color(0.75f, 1, 0), // Lime Green
        new Color(0.25f, 1, 0.75f), // Aqua
        new Color(0, 0.75f, 1), // Sky Blue
        new Color(0, 0, 1), // Blue
    ];

    private static readonly List<Color> ManeuverColors =
    [
        new Color(1, 0, 0), // Red - first patch is the burn
        new Color(1, 0.5f, 0), // Orange
        new Color(1, 0.5f, 0.5f), // Light Red
        new Color(1, 0.75f, 0), // Light Orange
        new Color(1, 0.5f, 1), // Light Magenta
        new Color(1, 0, 0.5f), // Pink
        new Color(1, 0.5f, 0.75f), // Light Pink
        new Color(1, 1, 0.5f), // Light Yellow
        new Color(1, 0, 1), // Magenta
    ];

    public override void OnInitialized()
    {
        HarmonyInstance.PatchAll(typeof(OrbitalLineColors));
    }

    [HarmonyPatch(typeof(OrbitRenderer), nameof(OrbitRenderer.UpdateOrbitStyling))]
    [HarmonyPrefix]
    // ReSharper disable once InconsistentNaming
    private static bool UpdateOrbitStylingPrefix(ref OrbitRenderer __instance)
    {
        foreach (var value in __instance._orbitRenderData.Values)
        {
            var vessel = Vehicle.ActiveSimVessel;
            var isTarget = vessel?.TargetObjectId == value.Orbiter.SimulationObject.GlobalId;
            var isActiveVessel = vessel?.SimulationObject.GlobalId == value.Orbiter.SimulationObject.GlobalId;

            if (value.Segments == null)
            {
                continue;
            }

            if (value.IsCelestialBody)
            {
                UpdateCelestialBodyStyling(value, isTarget);
            }
            else if (value.IsManeuver)
            {
                UpdateManeuverStyling(value, isTarget);
            }
            else
            {
                UpdateTrajectoryStyling(value, isTarget, isActiveVessel);
            }
        }

        return false;
    }

    #region Celestial body orbit colors

    private static void UpdateCelestialBodyStyling(OrbitRenderer.OrbitRenderData data, bool isTarget)
    {
        foreach (var segment in data.Segments)
        {
            var startOrbitColor = isTarget ? MapMagicValues.TargetOrbitStartColor : data.DefaultOrbitColor;
            var endOrbitColor = isTarget
                ? MapMagicValues.TargetOrbitEndColor
                : data.DefaultOrbitColor * MapMagicValues.CelestialBodyOrbitEndColorBrightness;
            segment.SetColors(startOrbitColor, endOrbitColor);
            segment.OrbitRenderStyle = MapMagicValues.CelestialBodyRenderStyle;
            segment.DashStyling.size = MapMagicValues.CelestialBodyOrbitDashLength;
            segment.DashStyling.spacing = MapMagicValues.CelestialBodyOrbitDashGap;
        }

        data.OrbitThickness = MapMagicValues.CelestialBodyOrbitThickness;
        data.IsClosedLoop = data.Orbiter.PatchedConicsOrbit.eccentricity < 1.0;
    }

    #endregion

    #region Maneuver orbit colors

    private static void UpdateManeuverStyling(OrbitRenderer.OrbitRenderData data, bool isTarget)
    {
        var (startUT, endUT) = GetTrajectoryUT(data.Orbiter.ManeuverPlanSolver.ManeuverTrajectory);

        for (var index = 0; index < data.Orbiter.ManeuverPlanSolver.ManeuverTrajectory.Count; index++)
        {
            var patchedOrbit = data.Orbiter.ManeuverPlanSolver.ManeuverTrajectory[index];
            var segment = data.Segments[index];

            if (segment.IsHighlighted)
            {
                SetManeuverHighlightedColors(data, index, startUT, endUT);
            }
            else if (patchedOrbit.PatchStartTransition == PatchTransitionType.Maneuver ||
                     patchedOrbit.PatchEndTransition == PatchTransitionType.EndThrust ||
                     patchedOrbit.PatchEndTransition == PatchTransitionType.PartialOutOfFuel ||
                     patchedOrbit.PatchEndTransition == PatchTransitionType.CompletelyOutOfFuel)
            {
                SetManeuverBurnColors(data, index, startUT, endUT, isTarget);
            }
            else
            {
                SetManeuverGeneralColors(data, index, startUT, endUT, isTarget);
            }

            segment.NeedRenderConnector = patchedOrbit.PatchEndTransition == PatchTransitionType.Escape;
            if (!segment.NeedRenderConnector)
            {
                continue;
            }

            segment.ConnectorDashStyling.size = MapMagicValues.TrajectoryConnectorDashLength;
            segment.ConnectorDashStyling.spacing = MapMagicValues.TrajectoryConnectorDashGap;
        }

        data.OrbitThickness = MapMagicValues.ManeuverOrbitThickness;
        data.IsClosedLoop = false;
    }

    private static void SetManeuverHighlightedColors(
        OrbitRenderer.OrbitRenderData data,
        int index,
        double startUT,
        double endUT
    )
    {
        Color startOrbitColor;
        Color endOrbitColor;

        var (initialStartColor, initialEndColor) = GetColorsForSegmentIndex(index, SegmentType.Maneuver, true);

        var time = endUT - startUT;
        var patchedOrbit = data.Orbiter.ManeuverPlanSolver.ManeuverTrajectory[index];
        var segment = data.Segments[index];

        if (!MapMagicValues.PerPatchGradients)
        {
            startOrbitColor = Color.Lerp(
                initialStartColor,
                initialEndColor,
                (float)((patchedOrbit.StartUT - startUT) / time)
            );
            endOrbitColor = Color.Lerp(
                initialStartColor,
                initialEndColor,
                (float)((patchedOrbit.EndUT - startUT) / time)
            );
        }
        else
        {
            startOrbitColor = initialStartColor;
            endOrbitColor = initialEndColor;
        }

        segment.SetColors(startOrbitColor, endOrbitColor);
    }

    private static void SetManeuverBurnColors(
        OrbitRenderer.OrbitRenderData data,
        int index,
        double startUT,
        double endUT,
        bool isTarget
    )
    {
        Color startOrbitColor;
        Color endOrbitColor;

        var time = endUT - startUT;
        var patchedOrbit = data.Orbiter.ManeuverPlanSolver.ManeuverTrajectory[index];
        var segment = data.Segments[index];

        if (isTarget)
        {
            if (MapMagicValues.PerPatchGradients)
            {
                startOrbitColor = MapMagicValues.TargetOrbitStartColor;
                endOrbitColor = MapMagicValues.TargetOrbitEndColor;
            }
            else
            {
                startOrbitColor = Color.Lerp(
                    MapMagicValues.TargetOrbitStartColor,
                    MapMagicValues.TargetOrbitEndColor,
                    (float)((patchedOrbit.StartUT - startUT) / time)
                );
                endOrbitColor = Color.Lerp(
                    MapMagicValues.TargetOrbitStartColor,
                    MapMagicValues.TargetOrbitEndColor,
                    (float)((patchedOrbit.EndUT - startUT) / time)
                );
            }
        }
        else if (data.Orbiter.IsLocallyOwned)
        {
            if (MapMagicValues.PerPatchGradients)
            {
                startOrbitColor = MapMagicValues.ManeuverNonImpulseStartColor;
                endOrbitColor = MapMagicValues.ManeuverNonImpulseEndColor;
            }
            else
            {
                startOrbitColor = Color.Lerp(
                    MapMagicValues.ManeuverNonImpulseStartColor,
                    MapMagicValues.ManeuverNonImpulseEndColor,
                    (float)((patchedOrbit.StartUT - startUT) / time)
                );
                endOrbitColor = Color.Lerp(
                    MapMagicValues.ManeuverNonImpulseStartColor,
                    MapMagicValues.ManeuverNonImpulseEndColor,
                    (float)((patchedOrbit.EndUT - startUT) / time)
                );
            }
        }
        else if (MapMagicValues.PerPatchGradients)
        {
            startOrbitColor = MapMagicValues.NonLocallyOwnedOrbitStartColor;
            endOrbitColor = MapMagicValues.NonLocallyOwnedOrbitEndColor;
        }
        else
        {
            startOrbitColor = Color.Lerp(
                MapMagicValues.NonLocallyOwnedOrbitStartColor,
                MapMagicValues.NonLocallyOwnedOrbitEndColor,
                (float)((patchedOrbit.StartUT - startUT) / time)
            );
            endOrbitColor = Color.Lerp(
                MapMagicValues.NonLocallyOwnedOrbitStartColor,
                MapMagicValues.NonLocallyOwnedOrbitEndColor,
                (float)((patchedOrbit.EndUT - startUT) / time)
            );
        }

        segment.OrbitRenderStyle = MapMagicValues.ManeuverNonImpulseRenderStyle;
        segment.DashStyling.size = MapMagicValues.ManeuverNonImpulseDashLength;
        segment.DashStyling.spacing = MapMagicValues.ManeuverNonImpulseDashGap;

        segment.SetColors(startOrbitColor, endOrbitColor);
    }

    private static void SetManeuverGeneralColors(
        OrbitRenderer.OrbitRenderData data,
        int index,
        double startUT,
        double endUT,
        bool isTarget
    )
    {
        Color startOrbitColor;
        Color endOrbitColor;

        var time = endUT - startUT;
        var patchedOrbit = data.Orbiter.ManeuverPlanSolver.ManeuverTrajectory[index];
        var segment = data.Segments[index];

        if (isTarget)
        {
            if (MapMagicValues.PerPatchGradients)
            {
                startOrbitColor = MapMagicValues.TargetOrbitStartColor;
                endOrbitColor = MapMagicValues.TargetOrbitEndColor;
            }
            else
            {
                startOrbitColor = Color.Lerp(
                    MapMagicValues.TargetOrbitStartColor,
                    MapMagicValues.TargetOrbitEndColor,
                    (float)((patchedOrbit.StartUT - startUT) / time)
                );
                endOrbitColor = Color.Lerp(MapMagicValues.TargetOrbitStartColor,
                    MapMagicValues.TargetOrbitEndColor,
                    (float)((patchedOrbit.EndUT - startUT) / time)
                );
            }
        }
        else if (data.Orbiter.IsLocallyOwned)
        {
            var (initialStartColor, initialEndColor) = GetColorsForSegmentIndex(index, SegmentType.Maneuver);

            if (MapMagicValues.PerPatchGradients)
            {
                startOrbitColor = initialStartColor;
                endOrbitColor = initialEndColor;
            }
            else
            {
                startOrbitColor = Color.Lerp(
                    initialStartColor,
                    initialEndColor,
                    (float)((patchedOrbit.StartUT - startUT) / time)
                );
                endOrbitColor = Color.Lerp(
                    initialStartColor,
                    initialEndColor,
                    (float)((patchedOrbit.EndUT - startUT) / time)
                );
            }
        }
        else if (MapMagicValues.PerPatchGradients)
        {
            startOrbitColor = MapMagicValues.NonLocallyOwnedOrbitStartColor;
            endOrbitColor = MapMagicValues.NonLocallyOwnedOrbitEndColor;
        }
        else
        {
            startOrbitColor = Color.Lerp(
                MapMagicValues.NonLocallyOwnedOrbitStartColor,
                MapMagicValues.NonLocallyOwnedOrbitEndColor,
                (float)((patchedOrbit.StartUT - startUT) / time)
            );
            endOrbitColor = Color.Lerp(
                MapMagicValues.NonLocallyOwnedOrbitStartColor,
                MapMagicValues.NonLocallyOwnedOrbitEndColor,
                (float)((patchedOrbit.EndUT - startUT) / time)
            );
        }

        segment.OrbitRenderStyle = MapMagicValues.ManeuverOrbitRenderStyle;
        segment.DashStyling.size = MapMagicValues.ManeuverOrbitDashLength;
        segment.DashStyling.spacing = MapMagicValues.ManeuverOrbitDashGap;

        segment.SetColors(startOrbitColor, endOrbitColor);
    }

    #endregion

    #region Trajectory orbit colors

    private static void UpdateTrajectoryStyling(OrbitRenderer.OrbitRenderData data, bool isTarget, bool isActiveVessel)
    {
        var (startUt, endUt) = GetTrajectoryUT(data.Orbiter.PatchedConicSolver.CurrentTrajectory);

        for (var index = 0; index < data.Orbiter.PatchedConicSolver.CurrentTrajectory.Count; index++)
        {
            var patchedConicsOrbit = data.Orbiter.PatchedConicSolver.CurrentTrajectory[index];
            var segment = data.Segments[index];

            if (segment.IsHighlighted)
            {
                SetVesselHighlightedColors(data, index, startUt, endUt, isActiveVessel);
            }
            else if (isTarget)
            {
                SetVesselTargetColors(data, index, startUt, endUt);
            }
            else if (data.Orbiter.IsLocallyOwned)
            {
                SetVesselLocalPlayerColors(data, index, startUt, endUt, isActiveVessel);
            }
            else if (MapMagicValues.PerPatchGradients)
            {
                var startOrbitColor = MapMagicValues.NonLocallyOwnedOrbitStartColor;
                var endOrbitColor = MapMagicValues.NonLocallyOwnedOrbitEndColor;
                segment.SetColors(startOrbitColor, endOrbitColor);
            }
            else
            {
                var time = endUt - startUt;

                var startOrbitColor = Color.Lerp(
                    MapMagicValues.NonLocallyOwnedOrbitStartColor,
                    MapMagicValues.NonLocallyOwnedOrbitEndColor,
                    (float)((patchedConicsOrbit.StartUT - startUt) / time)
                );
                var endOrbitColor = Color.Lerp(
                    MapMagicValues.NonLocallyOwnedOrbitStartColor,
                    MapMagicValues.NonLocallyOwnedOrbitEndColor,
                    (float)((patchedConicsOrbit.EndUT - startUt) / time)
                );

                segment.SetColors(startOrbitColor, endOrbitColor);
            }

            segment.OrbitRenderStyle = MapMagicValues.TrajectoryOrbitRenderStyle;
            segment.DashStyling.size = MapMagicValues.TrajectoryOrbitDashLength;
            segment.DashStyling.spacing = MapMagicValues.TrajectoryOrbitDashGap;
            segment.NeedRenderConnector = patchedConicsOrbit.PatchEndTransition == PatchTransitionType.Escape;

            if (segment.NeedRenderConnector)
            {
                segment.ConnectorDashStyling.size = MapMagicValues.TrajectoryConnectorDashLength;
                segment.ConnectorDashStyling.spacing = MapMagicValues.TrajectoryConnectorDashGap;
            }
        }

        data.OrbitThickness = MapMagicValues.TrajectoryOrbitThickness;
        data.IsClosedLoop = data.Orbiter.PatchedConicsOrbit.eccentricity < 1.0;
    }

    private static void SetVesselHighlightedColors(
        OrbitRenderer.OrbitRenderData data,
        int index,
        double startUt,
        double endUt,
        bool isActiveVessel
    )
    {
        Color startOrbitColor;
        Color endOrbitColor;

        var initialStartColor = MapMagicValues.HighlightedOrbitStartColor;
        var initialEndColor = MapMagicValues.HighlightedOrbitEndColor;

        var time = endUt - startUt;
        var patchedConicsOrbit = data.Orbiter.PatchedConicSolver.CurrentTrajectory[index];
        var segment = data.Segments[index];

        if (data.Orbiter.IsLocallyOwned && isActiveVessel)
        {
            (initialStartColor, initialEndColor) = GetColorsForSegmentIndex(index, SegmentType.Trajectory, true);
        }

        if (!MapMagicValues.PerPatchGradients)
        {
            startOrbitColor = Color.Lerp(
                initialStartColor,
                initialEndColor,
                (float)((patchedConicsOrbit.StartUT - startUt) / time)
            );
            endOrbitColor = Color.Lerp(
                initialStartColor,
                initialEndColor,
                (float)((patchedConicsOrbit.EndUT - startUt) / time)
            );
        }
        else
        {
            startOrbitColor = initialStartColor;
            endOrbitColor = initialEndColor;
        }

        segment.SetColors(startOrbitColor, endOrbitColor);
    }

    private static void SetVesselTargetColors(
        OrbitRenderer.OrbitRenderData data,
        int index,
        double startUt,
        double endUt
    )
    {
        Color startOrbitColor;
        Color endOrbitColor;

        var time = endUt - startUt;
        var patchedConicsOrbit = data.Orbiter.PatchedConicSolver.CurrentTrajectory[index];
        var segment = data.Segments[index];

        if (MapMagicValues.PerPatchGradients)
        {
            startOrbitColor = MapMagicValues.TargetOrbitStartColor;
            endOrbitColor = MapMagicValues.TargetOrbitEndColor;
        }
        else
        {
            startOrbitColor = Color.Lerp(
                MapMagicValues.TargetOrbitStartColor,
                MapMagicValues.TargetOrbitEndColor,
                (float)((patchedConicsOrbit.StartUT - startUt) / time)
            );
            endOrbitColor = Color.Lerp(
                MapMagicValues.TargetOrbitStartColor,
                MapMagicValues.TargetOrbitEndColor,
                (float)((patchedConicsOrbit.EndUT - startUt) / time)
            );
        }

        segment.SetColors(startOrbitColor, endOrbitColor);
    }

    private static void SetVesselLocalPlayerColors(OrbitRenderer.OrbitRenderData data,
        int index,
        double startUt,
        double endUt,
        bool isActiveVessel)
    {
        Color startOrbitColor;
        Color endOrbitColor;

        var time = endUt - startUt;
        var patchedConicsOrbit = data.Orbiter.PatchedConicSolver.CurrentTrajectory[index];
        var segment = data.Segments[index];

        if (isActiveVessel)
        {
            var (startColor, endColor) = GetColorsForSegmentIndex(index, SegmentType.Trajectory);

            if (MapMagicValues.PerPatchGradients)
            {
                startOrbitColor = startColor;
                endOrbitColor = endColor;
            }
            else
            {
                startOrbitColor = Color.Lerp(
                    startColor,
                    endColor,
                    (float)((patchedConicsOrbit.StartUT - startUt) / time)
                );
                endOrbitColor = Color.Lerp(
                    startColor,
                    endColor,
                    (float)((patchedConicsOrbit.EndUT - startUt) / time)
                );
            }
        }
        else if (MapMagicValues.PerPatchGradients)
        {
            startOrbitColor = MapMagicValues.NonActiveVesselOrbitStartColor;
            endOrbitColor = MapMagicValues.NonActiveVesselOrbitEndColor;
        }
        else
        {
            startOrbitColor = Color.Lerp(
                MapMagicValues.NonActiveVesselOrbitStartColor,
                MapMagicValues.NonActiveVesselOrbitEndColor,
                (float)((patchedConicsOrbit.StartUT - startUt) / time)
            );
            endOrbitColor = Color.Lerp(
                MapMagicValues.NonActiveVesselOrbitStartColor,
                MapMagicValues.NonActiveVesselOrbitEndColor,
                (float)((patchedConicsOrbit.EndUT - startUt) / time)
            );
        }

        segment.SetColors(startOrbitColor, endOrbitColor);
    }

    #endregion

    #region Helper methods

    private static (double startUT, double endUT) GetTrajectoryUT<T>(List<T> trajectory) where T : IPatchedOrbit
    {
        var startUT = trajectory[0].StartUT;
        var endUT = trajectory[0].EndUT;
        if (MapMagicValues.PerPatchGradients)
        {
            return (startUT, endUT);
        }

        foreach (var patchedOrbit in trajectory)
        {
            if (patchedOrbit.PatchEndTransition != PatchTransitionType.Final)
            {
                continue;
            }

            endUT = patchedOrbit.EndUT;
            break;
        }

        return (startUT, endUT);
    }

    private static (Color startColor, Color endColor) GetColorsForSegmentIndex(
        int segmentIndex,
        SegmentType type,
        bool isHighlighted = false
    )
    {
        var alphaFactor = isHighlighted ? 0.5f : 1.0f;

        var startColor = GetSegmentColors(type)[segmentIndex % TrajectoryColors.Count]  with {a = alphaFactor};
        var endColor = startColor * 0.75f;

        return (startColor, endColor);
    }

    private static List<Color> GetSegmentColors(SegmentType type) => type switch
    {
        SegmentType.Trajectory => TrajectoryColors,
        SegmentType.Maneuver => ManeuverColors,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    #endregion

    private enum SegmentType
    {
        Trajectory,
        Maneuver,
    }
}