using HarmonyLib;
using KSP.Game;
using KSP.Messages;
using KSP.UI.Binding;
using SpaceWarp.API.UI.Appbar;
using UnityEngine;
using UnityEngine.UI;

namespace KerbalLifeHacks.Hacks.VabMissionTracker;

[Hack("Enable Mission Tracker in VAB", true)]
// ReSharper disable once InconsistentNaming
public class VABMissionTracker : BaseHack
{
    private const string IconPath = "GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Scaled Popup Canvas/" +
                                    "Container/ButtonBar/BTN-Mission-Tracker/Content/GRP-icon/ICO-asset";

    private const string ToolbarOabButtonID = "BTN-VABMissionTracker";

    public override void OnInitialized()
    {
        HarmonyInstance.PatchAll(typeof(VABMissionTracker));
        Messages.PersistentSubscribe<GameStateEnteredMessage>(OnGameStateEntered);
    }

    private void OnGameStateEntered(MessageCenterMessage msg)
    {
        if (((GameStateEnteredMessage)msg).StateBeingEntered != GameState.VehicleAssemblyBuilder)
        {
            return;
        }

        var icon = GameObject.Find(IconPath).GetComponent<Image>().sprite;

        Appbar.RegisterOABAppButton("Mission Tracker", ToolbarOabButtonID, icon, isOpen =>
        {
            GameObject.Find(ToolbarOabButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(isOpen);
            Game.MissionControlManager.MissionTracker.SetVisible(isOpen);
        });

        Messages.Unsubscribe<GameStateEnteredMessage>(OnGameStateEntered);
    }

    [HarmonyPatch(typeof(MissionTracker), nameof(MissionTracker.SetVisible))]
    [HarmonyPostfix]
    private static void OnMissionTrackerSetVisible(bool isVisible = true)
    {
        if (GameManager.Instance.Game.GlobalGameState.GetState() == GameState.VehicleAssemblyBuilder)
        {
            GameObject.Find(ToolbarOabButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(isVisible);
        }
    }
}