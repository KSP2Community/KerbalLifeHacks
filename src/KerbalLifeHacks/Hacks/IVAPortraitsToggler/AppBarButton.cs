using KSP.Api.CoreTypes;
using KSP.UI;
using KSP.UI.Binding;
using SpaceWarp.API.UI.Appbar;
using UnityEngine;
using UnityEngine.UI;
using UnityObject = UnityEngine.Object;
namespace KerbalLifeHacks.Hacks.IVAPortraitsToggler;

internal class AppBarButton
{
    private UIValue_WriteBool_Toggle _buttonState;
    public GameObject AddButton(string buttonTooltip, string buttonId, Texture2D buttonIcon, Action<bool> function, int siblingIndex = -1)
    {
        // Find 'ButtonBar' gameobject
        var list = GameObject.Find("GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Scaled Popup Canvas/Container/ButtonBar");
        // Get 'NonStageable-Resources' button
        var nonStageableResources = list != null ? list.GetChild("BTN-NonStageable-Resources") : null;
        
        if (list == null || nonStageableResources == null)
        {
            return null;
        }
        
        // Clone 'NonStageable-Resources' button.
        
        var barButton = UnityObject.Instantiate(nonStageableResources, list.transform);
        if(siblingIndex >= 0 && siblingIndex < list.transform.childCount-1)
            barButton.transform.SetSiblingIndex(siblingIndex);
        
        barButton.name = buttonId;
        
        // Change the tooltip.
        barButton.GetComponent<BasicTextTooltipData>()._tooltipTitleKey = buttonTooltip;
        
        // Change the icon.
        var sprite = Appbar.GetAppBarIconFromTexture(buttonIcon);
        var icon = barButton.GetChild("Content").GetChild("GRP-icon");
        var image = icon.GetChild("ICO-asset").GetComponent<Image>();
        image.sprite = sprite;

        // Add our function call to the toggle.
        var utoggle = barButton.GetComponent<ToggleExtended>();
        utoggle.onValueChanged.AddListener(state => function(state));
        utoggle.onValueChanged.AddListener(SetButtonState);
        
        // Set the initial state of the button.
        _buttonState = barButton.GetComponent<UIValue_WriteBool_Toggle>();
        _buttonState.valueKey = $"Is{buttonId}Visible";
        _buttonState.BindValue(new Property<bool>(false));
        return barButton;
    }
    
    public void SetButtonState(bool state)
    {
        if (_buttonState == null) return;
        _buttonState.SetValue(state);
    }
}