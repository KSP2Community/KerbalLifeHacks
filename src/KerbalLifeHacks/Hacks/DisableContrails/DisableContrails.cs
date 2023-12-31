using KSP.Game;
using KSP.Messages;
using KSP.VFX;

namespace KerbalLifeHacks.Hacks.DisableContrails;

[Hack("Disable Contrails", false)]
public class DisableContrails : BaseHack
{
    public override void OnInitialized()
    {
        Messages.PersistentSubscribe<GameStateEnteredMessage>(msg =>
        {
            if (((GameStateEnteredMessage)msg).StateBeingEntered != GameState.FlightView)
            {
                return;
            }
            
            CFXSystem.SetVFXTypeEnabled(VFXEventType.Contrail, false);
            CFXSystem.SetVFXTypeEnabled(VFXEventType.WingipVortex, false);
        });
    }
}