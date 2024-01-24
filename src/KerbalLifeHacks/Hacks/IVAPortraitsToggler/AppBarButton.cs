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
    private readonly UIValue_WriteBool_Toggle _buttonState;
    private readonly GameObject _button;

    public AppBarButton(
        string buttonTooltip,
        string buttonId,
        Texture2D buttonIcon,
        Action<bool> function,
        int siblingIndex = -1
    )
    {
        // Find 'ButtonBar' game object
        var list = GameObject.Find(
            "GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Scaled Popup Canvas/Container/ButtonBar"
        );

        // Get 'NonStageable-Resources' button
        var nonStageableResources = list != null ? list.GetChild("BTN-NonStageable-Resources") : null;

        if (list == null || nonStageableResources == null)
        {
            return;
        }

        // Clone 'NonStageable-Resources' button

        _button = UnityObject.Instantiate(nonStageableResources, list.transform);
        if (siblingIndex >= 0 && siblingIndex < list.transform.childCount - 1)
        {
            _button.transform.SetSiblingIndex(siblingIndex);
        }

        _button.name = buttonId;

        // Change the tooltip
        _button.GetComponent<BasicTextTooltipData>()._tooltipTitleKey = buttonTooltip;

        // Change the icon
        var sprite = Appbar.GetAppBarIconFromTexture(buttonIcon);
        var icon = _button.GetChild("Content").GetChild("GRP-icon");
        var image = icon.GetChild("ICO-asset").GetComponent<Image>();
        image.sprite = sprite;

        // Add our function call to the toggle
        var toggle = _button.GetComponent<ToggleExtended>();
        toggle.onValueChanged.AddListener(state => function(state));
        toggle.onValueChanged.AddListener(SetState);

        // Set the initial state of the button
        _buttonState = _button.GetComponent<UIValue_WriteBool_Toggle>();
        _buttonState.valueKey = $"Is{buttonId}Visible";
        _buttonState.BindValue(new Property<bool>(false));
    }

    public void SetState(bool state)
    {
        if (_buttonState == null)
        {
            return;
        }

        _buttonState.SetValue(state);
    }

    public void SetActive(bool isActive = true)
    {
        if (_button == null)
        {
            return;
        }

        _button.SetActive(isActive);
    }
}