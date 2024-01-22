using HarmonyLib;
using KSP.Map;
using KSP.Sim;
using UnityEngine;
using Random = UnityEngine.Random;

namespace KerbalLifeHacks.Hacks.OrbitalLineColors;

[Hack("Use various colors for orbital lines")]
public class OrbitalLineColors : BaseHack
{
    private static OrbitalLineColors _instance;

    public override void OnInitialized()
    {
        _instance = this;
        HarmonyInstance.PatchAll(typeof(OrbitalLineColors));
    }

    private static readonly List<Color> PossibleColors =
    [
        new Color(1, 0, 0), // Red
        new Color(0, 1, 0), // Green
        new Color(0, 0, 1), // Blue
        new Color(1, 1, 0), // Yellow
        new Color(1, 0, 1), // Magenta
        new Color(0, 1, 1), // Cyan
        new Color(1, 0.5f, 0), // Orange
        new Color(0.5f, 1, 0), // Light Green
        new Color(0, 0.5f, 1), // Light Blue
        new Color(1, 0, 0.5f), // Pink
        new Color(0.5f, 0, 1), // Purple
        new Color(1, 0.5f, 0.5f), // Light Red
        new Color(0.5f, 1, 0.5f), // Light Green
        new Color(0.5f, 0.5f, 1), // Light Blue
        new Color(1, 1, 0.5f), // Light Yellow
        new Color(1, 0.5f, 1), // Light Magenta
        new Color(0.5f, 1, 1), // Light Cyan
        new Color(1, 0.75f, 0), // Light Orange
        new Color(0.75f, 1, 0), // Lime Green
        new Color(0, 0.75f, 1), // Sky Blue
    ];

    private static Dictionary<OrbitRenderSegment, Color> _segmentColors = new();

    [HarmonyPatch(typeof(OrbitRenderer), nameof(OrbitRenderer.UpdateOrbitStyling))]
    [HarmonyPrefix]
    // ReSharper disable once InconsistentNaming
    private static bool UpdateOrbitStylingPrefix(ref OrbitRenderer __instance)
    {
        foreach (var value in __instance._orbitRenderData.Values)
        {
            var activeSimVessel = _instance.Game.ViewController.GetActiveSimVessel();
            var isTarget = activeSimVessel != null &&
                       activeSimVessel.TargetObjectId == value.Orbiter.SimulationObject.GlobalId;
            var isActiveVessel = activeSimVessel != null &&
                        activeSimVessel.SimulationObject.GlobalId == value.Orbiter.SimulationObject.GlobalId;
            if (value.Segments == null)
            {
                continue;
            }

            if (!value.IsCelestialBody)
            {
                if (value.IsManeuver)
                {
                    var startUt = value.Orbiter.ManeuverPlanSolver.ManeuverTrajectory[0].StartUT;
                    var endUt = value.Orbiter.ManeuverPlanSolver.ManeuverTrajectory[0].EndUT;
                    if (!MapMagicValues.PerPatchGradients)
                    {
                        foreach (var patchedOrbit in value.Orbiter.ManeuverPlanSolver.ManeuverTrajectory)
                        {
                            if (patchedOrbit.PatchEndTransition == PatchTransitionType.Final)
                            {
                                endUt = patchedOrbit.EndUT;
                                break;
                            }
                        }
                    }

                    var time = endUt - startUt;
                    value.OrbitThickness = MapMagicValues.ManeuverOrbitThickness;
                    for (var j = 0; j < value.Orbiter.ManeuverPlanSolver.ManeuverTrajectory.Count; j++)
                    {
                        var patchedOrbit2 = value.Orbiter.ManeuverPlanSolver.ManeuverTrajectory[j];
                        var orbitRenderSegment = value.Segments[j];
                        if (orbitRenderSegment.IsHighlighted)
                        {
                            Color startOrbitColor;
                            Color endOrbitColor;
                            if (MapMagicValues.PerPatchGradients)
                            {
                                startOrbitColor = MapMagicValues.HighlightedManeuverStartColor;
                                endOrbitColor = MapMagicValues.HighlightedManeuverEndColor;
                            }
                            else
                            {
                                startOrbitColor = Color.Lerp(MapMagicValues.HighlightedManeuverStartColor,
                                    MapMagicValues.HighlightedManeuverEndColor,
                                    (float)((patchedOrbit2.StartUT - startUt) / time));
                                endOrbitColor = Color.Lerp(MapMagicValues.HighlightedManeuverStartColor,
                                    MapMagicValues.HighlightedManeuverEndColor,
                                    (float)((patchedOrbit2.EndUT - startUt) / time));
                            }

                            orbitRenderSegment.SetColors(startOrbitColor, endOrbitColor);
                        }
                        else if (patchedOrbit2.PatchStartTransition == PatchTransitionType.Maneuver ||
                                 patchedOrbit2.PatchEndTransition == PatchTransitionType.EndThrust ||
                                 patchedOrbit2.PatchEndTransition == PatchTransitionType.PartialOutOfFuel ||
                                 patchedOrbit2.PatchEndTransition == PatchTransitionType.CompletelyOutOfFuel)
                        {
                            Color startOrbitColor;
                            Color endOrbitColor;
                            if (isTarget)
                            {
                                if (MapMagicValues.PerPatchGradients)
                                {
                                    startOrbitColor = MapMagicValues.TargetOrbitStartColor;
                                    endOrbitColor = MapMagicValues.TargetOrbitEndColor;
                                }
                                else
                                {
                                    startOrbitColor = Color.Lerp(MapMagicValues.TargetOrbitStartColor,
                                        MapMagicValues.TargetOrbitEndColor,
                                        (float)((patchedOrbit2.StartUT - startUt) / time));
                                    endOrbitColor = Color.Lerp(MapMagicValues.TargetOrbitStartColor,
                                        MapMagicValues.TargetOrbitEndColor,
                                        (float)((patchedOrbit2.EndUT - startUt) / time));
                                }
                            }
                            else if (value.Orbiter.IsLocallyOwned)
                            {
                                if (MapMagicValues.PerPatchGradients)
                                {
                                    startOrbitColor = MapMagicValues.ManeuverNonImpulseStartColor;
                                    endOrbitColor = MapMagicValues.ManeuverNonImpulseEndColor;
                                }
                                else
                                {
                                    startOrbitColor = Color.Lerp(MapMagicValues.ManeuverNonImpulseStartColor,
                                        MapMagicValues.ManeuverNonImpulseEndColor,
                                        (float)((patchedOrbit2.StartUT - startUt) / time));
                                    endOrbitColor = Color.Lerp(MapMagicValues.ManeuverNonImpulseStartColor,
                                        MapMagicValues.ManeuverNonImpulseEndColor,
                                        (float)((patchedOrbit2.EndUT - startUt) / time));
                                }
                            }
                            else if (MapMagicValues.PerPatchGradients)
                            {
                                startOrbitColor = MapMagicValues.NonLocallyOwnedOrbitStartColor;
                                endOrbitColor = MapMagicValues.NonLocallyOwnedOrbitEndColor;
                            }
                            else
                            {
                                startOrbitColor = Color.Lerp(MapMagicValues.NonLocallyOwnedOrbitStartColor,
                                    MapMagicValues.NonLocallyOwnedOrbitEndColor,
                                    (float)((patchedOrbit2.StartUT - startUt) / time));
                                endOrbitColor = Color.Lerp(MapMagicValues.NonLocallyOwnedOrbitStartColor,
                                    MapMagicValues.NonLocallyOwnedOrbitEndColor,
                                    (float)((patchedOrbit2.EndUT - startUt) / time));
                            }

                            orbitRenderSegment.SetColors(startOrbitColor, endOrbitColor);
                            orbitRenderSegment.OrbitRenderStyle = MapMagicValues.ManeuverNonImpulseRenderStyle;
                            orbitRenderSegment.DashStyling.size = MapMagicValues.ManeuverNonImpulseDashLength;
                            orbitRenderSegment.DashStyling.spacing = MapMagicValues.ManeuverNonImpulseDashGap;
                        }
                        else
                        {
                            Color startOrbitColor;
                            Color endOrbitColor;
                            if (isTarget)
                            {
                                if (MapMagicValues.PerPatchGradients)
                                {
                                    startOrbitColor = MapMagicValues.TargetOrbitStartColor;
                                    endOrbitColor = MapMagicValues.TargetOrbitEndColor;
                                }
                                else
                                {
                                    startOrbitColor = Color.Lerp(MapMagicValues.TargetOrbitStartColor,
                                        MapMagicValues.TargetOrbitEndColor,
                                        (float)((patchedOrbit2.StartUT - startUt) / time));
                                    endOrbitColor = Color.Lerp(MapMagicValues.TargetOrbitStartColor,
                                        MapMagicValues.TargetOrbitEndColor,
                                        (float)((patchedOrbit2.EndUT - startUt) / time));
                                }
                            }
                            else if (value.Orbiter.IsLocallyOwned)
                            {
                                if (MapMagicValues.PerPatchGradients)
                                {
                                    startOrbitColor = MapMagicValues.ManeuverCoastingStartColor;
                                    endOrbitColor = MapMagicValues.ManeuverCoastingEndColor;
                                }
                                else
                                {
                                    startOrbitColor = Color.Lerp(MapMagicValues.ManeuverCoastingStartColor,
                                        MapMagicValues.ManeuverCoastingEndColor,
                                        (float)((patchedOrbit2.StartUT - startUt) / time));
                                    endOrbitColor = Color.Lerp(MapMagicValues.ManeuverCoastingStartColor,
                                        MapMagicValues.ManeuverCoastingEndColor,
                                        (float)((patchedOrbit2.EndUT - startUt) / time));
                                }
                            }
                            else if (MapMagicValues.PerPatchGradients)
                            {
                                startOrbitColor = MapMagicValues.NonLocallyOwnedOrbitStartColor;
                                endOrbitColor = MapMagicValues.NonLocallyOwnedOrbitEndColor;
                            }
                            else
                            {
                                startOrbitColor = Color.Lerp(MapMagicValues.NonLocallyOwnedOrbitStartColor,
                                    MapMagicValues.NonLocallyOwnedOrbitEndColor,
                                    (float)((patchedOrbit2.StartUT - startUt) / time));
                                endOrbitColor = Color.Lerp(MapMagicValues.NonLocallyOwnedOrbitStartColor,
                                    MapMagicValues.NonLocallyOwnedOrbitEndColor,
                                    (float)((patchedOrbit2.EndUT - startUt) / time));
                            }

                            orbitRenderSegment.SetColors(startOrbitColor, endOrbitColor);
                            orbitRenderSegment.OrbitRenderStyle = MapMagicValues.ManeuverOrbitRenderStyle;
                            orbitRenderSegment.DashStyling.size = MapMagicValues.ManeuverOrbitDashLength;
                            orbitRenderSegment.DashStyling.spacing = MapMagicValues.ManeuverOrbitDashGap;
                        }

                        orbitRenderSegment.NeedRenderConnector =
                            patchedOrbit2.PatchEndTransition == PatchTransitionType.Escape;
                        if (orbitRenderSegment.NeedRenderConnector)
                        {
                            orbitRenderSegment.ConnectorDashStyling.size = MapMagicValues.TrajectoryConnectorDashLength;
                            orbitRenderSegment.ConnectorDashStyling.spacing = MapMagicValues.TrajectoryConnectorDashGap;
                        }
                    }

                    value.IsClosedLoop = false;
                    continue;
                }

                var startUt2 = value.Orbiter.PatchedConicSolver.CurrentTrajectory[0].StartUT;
                var endUt2 = value.Orbiter.PatchedConicSolver.CurrentTrajectory[0].EndUT;
                if (!MapMagicValues.PerPatchGradients)
                {
                    foreach (IPatchedOrbit patchedOrbit3 in value.Orbiter.PatchedConicSolver.CurrentTrajectory)
                    {
                        if (patchedOrbit3.PatchEndTransition == PatchTransitionType.Final)
                        {
                            endUt2 = patchedOrbit3.EndUT;
                            break;
                        }
                    }
                }

                var time2 = endUt2 - startUt2;
                value.OrbitThickness = MapMagicValues.TrajectoryOrbitThickness;
                for (var l = 0; l < value.Orbiter.PatchedConicSolver.CurrentTrajectory.Count; l++)
                {
                    var patchedConicsOrbit = value.Orbiter.PatchedConicSolver.CurrentTrajectory[l];
                    var orbitRenderSegment2 = value.Segments[l];
                    Color startOrbitColor;
                    Color endOrbitColor;
                    if (orbitRenderSegment2.IsHighlighted)
                    {
                        var startColor = MapMagicValues.HighlightedOrbitStartColor;
                        var endColor = MapMagicValues.HighlightedOrbitEndColor;

                        if (value.Orbiter.IsLocallyOwned && isActiveVessel)
                        {
                            (startColor, endColor) = GetColorsForSegment(orbitRenderSegment2);
                            startColor = startColor with { a = startColor.a * 0.5f };
                            endColor = endColor with { a = endColor.a * 0.5f };
                        }

                        if (MapMagicValues.PerPatchGradients)
                        {
                            startOrbitColor = startColor;
                            endOrbitColor = endColor;
                        }
                        else
                        {
                            startOrbitColor = Color.Lerp(startColor,
                                endColor,
                                (float)((patchedConicsOrbit.StartUT - startUt2) / time2));
                            endOrbitColor = Color.Lerp(startColor,
                                endColor,
                                (float)((patchedConicsOrbit.EndUT - startUt2) / time2));
                        }

                        orbitRenderSegment2.SetColors(startOrbitColor, endOrbitColor);
                    }
                    else if (isTarget)
                    {
                        if (MapMagicValues.PerPatchGradients)
                        {
                            startOrbitColor = MapMagicValues.TargetOrbitStartColor;
                            endOrbitColor = MapMagicValues.TargetOrbitEndColor;
                        }
                        else
                        {
                            startOrbitColor = Color.Lerp(MapMagicValues.TargetOrbitStartColor,
                                MapMagicValues.TargetOrbitEndColor,
                                (float)((patchedConicsOrbit.StartUT - startUt2) / time2));
                            endOrbitColor = Color.Lerp(MapMagicValues.TargetOrbitStartColor,
                                MapMagicValues.TargetOrbitEndColor,
                                (float)((patchedConicsOrbit.EndUT - startUt2) / time2));
                        }
                    }
                    else if (value.Orbiter.IsLocallyOwned)
                    {
                        if (isActiveVessel)
                        {
                            var (startColor, endColor) = GetColorsForSegment(orbitRenderSegment2);

                            if (MapMagicValues.PerPatchGradients)
                            {
                                startOrbitColor = startColor;
                                endOrbitColor = endColor;
                            }
                            else
                            {
                                startOrbitColor = Color.Lerp(startColor,
                                    endColor,
                                    (float)((patchedConicsOrbit.StartUT - startUt2) / time2));
                                endOrbitColor = Color.Lerp(startColor,
                                    endColor,
                                    (float)((patchedConicsOrbit.EndUT - startUt2) / time2));
                            }
                        }
                        else if (MapMagicValues.PerPatchGradients)
                        {
                            startOrbitColor = MapMagicValues.NonActiveVesselOrbitStartColor;
                            endOrbitColor = MapMagicValues.NonActiveVesselOrbitEndColor;
                        }
                        else
                        {
                            startOrbitColor = Color.Lerp(MapMagicValues.NonActiveVesselOrbitStartColor,
                                MapMagicValues.NonActiveVesselOrbitEndColor,
                                (float)((patchedConicsOrbit.StartUT - startUt2) / time2));
                            endOrbitColor = Color.Lerp(MapMagicValues.NonActiveVesselOrbitStartColor,
                                MapMagicValues.NonActiveVesselOrbitEndColor,
                                (float)((patchedConicsOrbit.EndUT - startUt2) / time2));
                        }
                    }
                    else if (MapMagicValues.PerPatchGradients)
                    {
                        startOrbitColor = MapMagicValues.NonLocallyOwnedOrbitStartColor;
                        endOrbitColor = MapMagicValues.NonLocallyOwnedOrbitEndColor;
                    }
                    else
                    {
                        startOrbitColor = Color.Lerp(MapMagicValues.NonLocallyOwnedOrbitStartColor,
                            MapMagicValues.NonLocallyOwnedOrbitEndColor,
                            (float)((patchedConicsOrbit.StartUT - startUt2) / time2));
                        endOrbitColor = Color.Lerp(MapMagicValues.NonLocallyOwnedOrbitStartColor,
                            MapMagicValues.NonLocallyOwnedOrbitEndColor,
                            (float)((patchedConicsOrbit.EndUT - startUt2) / time2));
                    }

                    orbitRenderSegment2.SetColors(startOrbitColor, endOrbitColor);
                    orbitRenderSegment2.OrbitRenderStyle = MapMagicValues.TrajectoryOrbitRenderStyle;
                    orbitRenderSegment2.DashStyling.size = MapMagicValues.TrajectoryOrbitDashLength;
                    orbitRenderSegment2.DashStyling.spacing = MapMagicValues.TrajectoryOrbitDashGap;
                    orbitRenderSegment2.NeedRenderConnector =
                        patchedConicsOrbit.PatchEndTransition == PatchTransitionType.Escape;
                    if (orbitRenderSegment2.NeedRenderConnector)
                    {
                        orbitRenderSegment2.ConnectorDashStyling.size = MapMagicValues.TrajectoryConnectorDashLength;
                        orbitRenderSegment2.ConnectorDashStyling.spacing = MapMagicValues.TrajectoryConnectorDashGap;
                    }
                }

                value.IsClosedLoop = value.Orbiter.PatchedConicsOrbit.eccentricity < 1.0;
                continue;
            }

            value.OrbitThickness = MapMagicValues.CelestialBodyOrbitThickness;
            foreach (var segment in value.Segments)
            {
                var startOrbitColor = isTarget ? MapMagicValues.TargetOrbitStartColor : value.DefaultOrbitColor;
                var endOrbitColor =
                    isTarget
                        ? MapMagicValues.TargetOrbitEndColor
                        : value.DefaultOrbitColor * MapMagicValues.CelestialBodyOrbitEndColorBrightness;
                segment.SetColors(startOrbitColor, endOrbitColor);
                segment.OrbitRenderStyle = MapMagicValues.CelestialBodyRenderStyle;
                segment.DashStyling.size = MapMagicValues.CelestialBodyOrbitDashLength;
                segment.DashStyling.spacing = MapMagicValues.CelestialBodyOrbitDashGap;
            }

            value.IsClosedLoop = value.Orbiter.PatchedConicsOrbit.eccentricity < 1.0;
        }

        return false;
    }

    private static (Color startColor, Color endColor) GetColorsForSegment(OrbitRenderSegment segment)
    {
        if (!_segmentColors.TryGetValue(segment, out var startColor))
        {
            startColor = PossibleColors[Random.Range(0, PossibleColors.Count)];
            _segmentColors[segment] = startColor;
        }

        var endColor = new Color(startColor.r, startColor.g, startColor.b, 0.5f);

        return (startColor, endColor);
    }
}