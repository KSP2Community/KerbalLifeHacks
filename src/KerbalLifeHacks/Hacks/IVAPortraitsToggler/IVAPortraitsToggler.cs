using System.Collections;
using KSP.Game;
using KSP.Messages;
using KSP.Sim.impl;
using SpaceWarp.API.Assets;
using SpaceWarp.API.Game;
using UnityEngine;

namespace KerbalLifeHacks.Hacks.IVAPortraitsToggler;

[Hack("IVA Portraits Toggler")]
public class IVAPortraitsToggler : BaseHack
{
    private AppBarButton _buttonBar;

    // ReSharper disable once InconsistentNaming, IdentifierTypo
    private GameObject _ivaportraits;

    public override void OnInitialized()
    {
        Messages.PersistentSubscribe<FlightViewEnteredMessage>(OnFlightViewEnteredMessage);
        Messages.PersistentSubscribe<VesselChangedMessage>(OnVesselChangedMessage);
    }

    private void OnFlightViewEnteredMessage(MessageCenterMessage msg)
    {
        if (_ivaportraits == null)
        {
            var instruments = GameManager.Instance.Game.UI.FlightHud._instruments;
            // ReSharper disable once StringLiteralTypo
            instruments.TryGetValue("group_ivaportraits", out var ivaPortraits);
            if (ivaPortraits != null)
            {
                _ivaportraits = ivaPortraits._parentCanvas.gameObject;
            }
        }

        if (_buttonBar != null)
        {
            _buttonBar.SetActive();
            return;
        }

        _buttonBar = new AppBarButton(
            "IVA Portraits",
            "BTN-IVA-Portraits",
            AssetManager.GetAsset<Texture2D>($"KerbalLifeHacks/images/IVAPortraitsToggler-icon.png"),
            SetIVAPortraitsState,
            0
        );
        _buttonBar.SetActive();

        try
        {
            StartCoroutine(HandleVessel(Vehicle.ActiveSimVessel.GetControlOwner().GlobalId));
        }
        catch
        {
            // ignored
        }
    }

    private void OnVesselChangedMessage(MessageCenterMessage msg)
    {
        if (msg is not VesselChangedMessage { Vessel: { } vessel })
        {
            return;
        }

        var vesselGuid = vessel.GetControlOwner().GlobalId;
        StartCoroutine(HandleVessel(vesselGuid));
    }

    private IEnumerator HandleVessel(IGGuid vesselGuid)
    {
        yield return new WaitForUpdate();

        var allKerbalsInSimObject = GameManager.Instance.Game.KerbalManager._kerbalRosterManager
            ?.GetAllKerbalsInSimObject(vesselGuid);
        var state = allKerbalsInSimObject?.Count > 0;

        SetIVAPortraitsState(state);
        _buttonBar.SetState(state);
    }

    public void SetIVAPortraitsState(bool state)
    {
        _ivaportraits.SetActive(state);
    }
}