using KSP.Game;
using KSP.Messages;
using SpaceWarp.API.Assets;
using UnityEngine;

namespace KerbalLifeHacks.Hacks.IVAPortraitsToggler;

[Hack("IVA Portraits Toggler")]
public class IVAPortraitsToggler : BaseHack
{
    private AppBarButton _buttonBar;

    // ReSharper disable once InconsistentNaming, IdentifierTypo
    private Canvas _ivaportraits_canvas;

    public override void OnInitialized()
    {
        Messages.PersistentSubscribe<FlightViewEnteredMessage>(OnFlightViewEnteredMessage);
        Messages.PersistentSubscribe<VesselChangedMessage>(OnVesselChangedMessage);
    }

    private void OnFlightViewEnteredMessage(MessageCenterMessage msg)
    {
        if (_ivaportraits_canvas == null)
        {
            var instruments = GameManager.Instance.Game.UI.FlightHud._instruments;
            // ReSharper disable once StringLiteralTypo
            instruments.TryGetValue("group_ivaportraits", out var ivaPortraits);
            if (ivaPortraits != null)
            {
                _ivaportraits_canvas = ivaPortraits._parentCanvas;
            }
        }

        if (_buttonBar != null)
        {
            return;
        }

        _buttonBar = new AppBarButton(
            "IVA Portraits",
            "BTN-IVA-Portraits",
            AssetManager.GetAsset<Texture2D>($"KerbalLifeHacks/images/IVAPortraitsToggler-icon.png"),
            ToggleIVAPortraitsCanvas,
            0
        );
    }

    private void OnVesselChangedMessage(MessageCenterMessage msg)
    {
        if (msg is not VesselChangedMessage { Vessel: { } vessel })
        {
            return;
        }

        var vesselGuid = vessel.GetControlOwner().GlobalId;
        var allKerbalsInSimObject = GameManager.Instance.Game.KerbalManager._kerbalRosterManager
            ?.GetAllKerbalsInSimObject(vesselGuid);
        var state = allKerbalsInSimObject?.Count > 0;

        ToggleIVAPortraitsCanvas(state);
        _buttonBar.SetButtonState(state);
    }

    public void ToggleIVAPortraitsCanvas(bool state)
    {
        _ivaportraits_canvas.enabled = state;
    }
}