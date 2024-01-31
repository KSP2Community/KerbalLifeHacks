using System.Collections;
using Castle.Core.Internal;
using I2.Loc;
using KSP.Messages;
using KSP.OAB;
using KSP.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KerbalLifeHacks.Hacks.VABExtendedColorManager;

[Hack("VAB Extended Color Manager")]
public class VABExtendedColorManager : BaseHack
{
    private enum ColorFormat
    {
        Hex,
        RGBA,
        sRGBA
    }

    private ObjectAssemblyColorPicker _oabColorPicker;
    
    private ColorFormat _selectedInputFormat = ColorFormat.Hex;
    
    private GameObject _hexColorInputContainer;
    private GameObject _rgbaColorInputContainer;
    private GameObject _srgbaColorInputContainer;
    
    private InputFieldExtended _hexColorInputField;
    private InputFieldExtended[] _rgbaColorInputFields;
    private InputFieldExtended[] _srgbaColorInputFields;
    
    public override void OnInitialized()
    {
        
        Messages.PersistentSubscribe<OABLoadFinalizedMessage>(OnOABLoadFinalizedMessage);
        Messages.PersistentSubscribe<OABUnloadedMessage>(OnOABUnloadedMessage);
    }
    
    private void OnOABLoadFinalizedMessage(MessageCenterMessage msg)
    {
        //Logger.LogInfo("OnOABLoadFinalizedMessagee");
        HackTheColorManager();
    }
    
    private void OnOABUnloadedMessage(MessageCenterMessage msg)
    {
        //Logger.LogInfo("OnOABUnloadedMessage");
        if (_oabColorPicker != null)
        {
            _oabColorPicker._baseColorValue.OnChangedValue -= UpdateInputFields;
            _oabColorPicker._accentColorValue.OnChangedValue -= UpdateInputFields;
        }

        if (_hexColorInputField != null)
        {
            _hexColorInputField.onValueChanged.RemoveAllListeners();
        }
        if (_rgbaColorInputFields != null && _rgbaColorInputFields.Length == 4)
        {
            foreach (var t in _rgbaColorInputFields)
            {
                t.onValueChanged.RemoveAllListeners();
            }
        }
        if (_srgbaColorInputFields != null && _srgbaColorInputFields.Length == 4)
        {
            foreach (var t in _srgbaColorInputFields)
            {
                t.onValueChanged.RemoveAllListeners();
            }
        }
    }

    private void HackTheColorManager()
    {
        GameObject colorPickerWindow = GameObject.Find("OAB(Clone)/HUDSpawner/HUD/UI-Editor_ColorPicker Window");
        _oabColorPicker = colorPickerWindow.GetComponent<ObjectAssemblyColorPicker>();
        _oabColorPicker._baseColorValue.OnChangedValue += UpdateInputFields;
        _oabColorPicker._accentColorValue.OnChangedValue += UpdateInputFields;
        
        GameObject oabColorManagerHUD = colorPickerWindow.transform.Find("Root/UIPanel/GRP-Body").gameObject;
        
        colorPickerWindow.GetComponent<RectTransform>().sizeDelta = new Vector2(330, 770);
        oabColorManagerHUD.transform.Find("Agency Colors Buttons").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -565);
        
        var baseColorButton = oabColorManagerHUD.transform.FindChildRecursive("BaseColorPreviewButton").GetComponent<ButtonExtended>();
        var accentColorButton = oabColorManagerHUD.transform.FindChildRecursive("DetailColorPreviewButton").GetComponent<ButtonExtended>();
        
        baseColorButton.onClick.AddListener(OnColorEditModeChanged);
        accentColorButton.onClick.AddListener(OnColorEditModeChanged);
        
        var colorInputPanel = Instantiate(new GameObject(), oabColorManagerHUD.transform);
        colorInputPanel.name = "GRP-ColorInput";
        colorInputPanel.transform.SetSiblingIndex(4);
        
        var horizontalLayoutGroup = colorInputPanel.AddComponent<HorizontalLayoutGroup>();
        horizontalLayoutGroup.spacing = 10;
        horizontalLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
        
        var panelRectTransform = colorInputPanel.GetComponent<RectTransform>();
        panelRectTransform.anchoredPosition = new Vector2(0, -179);
        panelRectTransform.sizeDelta = new Vector2(270, 30);
        
        CreateInputFormatDropdown(colorInputPanel);
        
        var searchBar = GameObject.Find("OAB(Clone)/HUDSpawner/HUD/widget_PartsPicker/mask_PartsPicker/GRP-Search-Filter-Sort/GRP-Search-Field");
        
        CreateHexInput(colorInputPanel, searchBar);
        Create_RGBAInputs(colorInputPanel, searchBar);
        Create_sRGBAInputs(colorInputPanel, searchBar);
        
        UpdateInputFields(_oabColorPicker._baseColorValue.GetValue());
    }

    private void CreateInputFormatDropdown(GameObject parent)
    {
        var dropDown = GameObject.Find("OAB(Clone)/HUDSpawner/HUD/widget_PartsPicker/mask_PartsPicker/GRP-Search-Filter-Sort/widget_part_filters/BTN-Dropdown");
        var colorInputFormatDropdownGO = Instantiate(dropDown, parent.transform);
        var colorInputFormatLayoutElement = colorInputFormatDropdownGO.GetComponent<LayoutElement>();
        colorInputFormatLayoutElement.preferredWidth = 0;
        colorInputFormatLayoutElement.enabled = true;
        var colorInputFormatDropdown = colorInputFormatDropdownGO.GetComponent<DropdownExtended>();
        colorInputFormatDropdownGO.transform.Find("title-sort by").gameObject.SetActive(false);
        
        var formatOptions = new List<TMP_Dropdown.OptionData>();
        var enumType = _selectedInputFormat.GetType();
        for(int i = 0; i < Enum.GetNames(enumType).Length; i++)
        {
            formatOptions.Add(new TMP_Dropdown.OptionData(Enum.GetName(enumType, i)));
        }
        colorInputFormatDropdown.options = formatOptions;
        colorInputFormatDropdown.onValueChanged.AddListener(value => OnColorFormatChanged((ColorFormat)value));
    }

    private void CreateHexInput(GameObject parent, GameObject inputField)
    {
        _hexColorInputContainer = Instantiate(inputField, parent.transform);
        _hexColorInputContainer.name = "HexColorInputContainer";
        var hexLayoutElement = _hexColorInputContainer.GetComponent<LayoutElement>();
        hexLayoutElement.minWidth = 130;
        hexLayoutElement.enabled = true;
        
        var hexColorInputOuter = _hexColorInputContainer.GetChild("UI-Editor_Parts-Search-Bar");
        hexColorInputOuter.name = "HexColorInputOuter";
        
        DestroyImmediate(hexColorInputOuter.GetChild("TGL-Search-Button"));
        DestroyImmediate(hexColorInputOuter.GetChild("BTN-Clear-Search"));
        DestroyImmediate(hexColorInputOuter.GetChild("UI-Editor_Parts-Search-TextArea"));
        
        var hashSymbol = Instantiate(new GameObject(), hexColorInputOuter.transform);
        var hashSymbolText = hashSymbol.AddComponent<TextMeshProUGUI>();
        hashSymbolText.text = "#";
        hashSymbolText.fontSize = 24;
        hashSymbolText.alignment = TextAlignmentOptions.MidlineLeft;
        hashSymbolText.color = new Color(0.4784f, 0.5216f, 0.6f);
        hashSymbolText.font = hexColorInputOuter.transform.GetComponentInChildren<TextMeshProUGUI>().font;
        var hashSymbolTextRect = hashSymbolText.GetComponent<RectTransform>();
        hashSymbolTextRect.sizeDelta = new Vector2(30, 50);
        hashSymbolTextRect.pivot = new Vector2(0, 0.5f);
        hashSymbolTextRect.anchoredPosition = new Vector2(-83, 0);
        
        var hexColorInputInner = hexColorInputOuter.GetChild("FIELD-Part-Search");
        hexColorInputInner.name = "HexColorInputInner";
        
        _hexColorInputField = hexColorInputInner.GetComponentInChildren<InputFieldExtended>();
        _hexColorInputField.contentType = TMP_InputField.ContentType.Alphanumeric;
        _hexColorInputField.characterLimit = 8;
        _hexColorInputField.placeholder.GetComponent<Localize>().enabled = false;
        _hexColorInputField.placeholder.GetComponent<TMP_Text>().SetText("Enter Hex");
        _hexColorInputField.onValueChanged.AddListener(OnHexValueChanged);
    }

    private void Create_RGBAInputs(GameObject parent, GameObject inputField)
    {
        _rgbaColorInputContainer = Instantiate(inputField, parent.transform);
        _rgbaColorInputContainer.SetActive(false);
        _rgbaColorInputContainer.name = "RGBAColorInputContainer";
        var rgbaColorInputContainerLayoutElement = _rgbaColorInputContainer.GetComponent<LayoutElement>();
        rgbaColorInputContainerLayoutElement.minWidth = 130;
        rgbaColorInputContainerLayoutElement.enabled = true;
        
        var rgbaColorInputOuter = _rgbaColorInputContainer.GetChild("UI-Editor_Parts-Search-Bar");
        rgbaColorInputOuter.name = "RGBAColorInputOuter";
        
        DestroyImmediate(rgbaColorInputOuter.GetChild("BTN-Clear-Search"));
        DestroyImmediate(rgbaColorInputOuter.GetChild("UI-Editor_Parts-Search-TextArea"));
        DestroyImmediate(rgbaColorInputOuter.GetChild("TGL-Search-Button"));
        DestroyImmediate(rgbaColorInputOuter.GetChild("FIELD-Part-Search"));

        var rgbaHorizontalLayout = rgbaColorInputOuter.AddComponent<HorizontalLayoutGroup>();
        rgbaHorizontalLayout.childForceExpandWidth = false;
        rgbaHorizontalLayout.padding = new RectOffset(10, 10, 0, 0);

        var clonedInputField = inputField.transform.Find("UI-Editor_Parts-Search-Bar/FIELD-Part-Search").gameObject;
        _rgbaColorInputFields = new InputFieldExtended[4];
        _rgbaColorInputFields[0] = CreateInputField(clonedInputField, rgbaColorInputOuter, OnRGBAValueChanged, Color.red, TMP_InputField.ContentType.IntegerNumber, 3, "RGBAInputField_R", "R");
        _rgbaColorInputFields[1] = CreateInputField(clonedInputField, rgbaColorInputOuter, OnRGBAValueChanged, Color.green, TMP_InputField.ContentType.IntegerNumber, 3, "RGBAInputField_G", "G");
        _rgbaColorInputFields[2] = CreateInputField(clonedInputField, rgbaColorInputOuter, OnRGBAValueChanged, new Color(0,0.4f,1,1), TMP_InputField.ContentType.IntegerNumber, 3, "RGBAInputField_B", "B");
        _rgbaColorInputFields[3] = CreateInputField(clonedInputField, rgbaColorInputOuter, OnRGBAValueChanged, Color.gray, TMP_InputField.ContentType.IntegerNumber, 3, "RGBAInputField_A", "A");
    }
    
    private void Create_sRGBAInputs(GameObject parent, GameObject inputField)
    {
        _srgbaColorInputContainer = Instantiate(inputField, parent.transform);
        _srgbaColorInputContainer.SetActive(false);
        _srgbaColorInputContainer.name = "sRGBAColorInputContainer";
        var srgbaColorInputContainerLayoutElement = _srgbaColorInputContainer.GetComponent<LayoutElement>();
        srgbaColorInputContainerLayoutElement.minWidth = 130;
        srgbaColorInputContainerLayoutElement.enabled = true;
        
        var srgbaColorInputOuter = _srgbaColorInputContainer.GetChild("UI-Editor_Parts-Search-Bar");
        srgbaColorInputOuter.name = "sRGBAColorInputOuter";
        
        DestroyImmediate(srgbaColorInputOuter.GetChild("BTN-Clear-Search"));
        DestroyImmediate(srgbaColorInputOuter.GetChild("UI-Editor_Parts-Search-TextArea"));
        DestroyImmediate(srgbaColorInputOuter.GetChild("TGL-Search-Button"));
        DestroyImmediate(srgbaColorInputOuter.GetChild("FIELD-Part-Search"));

        var srgbaHorizontalLayout = srgbaColorInputOuter.AddComponent<HorizontalLayoutGroup>();
        srgbaHorizontalLayout.childForceExpandWidth = false;
        srgbaHorizontalLayout.padding = new RectOffset(10, 10, 0, 0);

        var clonedInputField = inputField.transform.Find("UI-Editor_Parts-Search-Bar/FIELD-Part-Search").gameObject;
        _srgbaColorInputFields = new InputFieldExtended[4];
        _srgbaColorInputFields[0] = CreateInputField(clonedInputField, srgbaColorInputOuter, OnsRGBAValueChanged, Color.red, TMP_InputField.ContentType.DecimalNumber, 5, "sRGBAInputField_R", "R");
        _srgbaColorInputFields[1] = CreateInputField(clonedInputField, srgbaColorInputOuter, OnsRGBAValueChanged, Color.green, TMP_InputField.ContentType.DecimalNumber, 5, "sRGBAInputField_G", "G");
        _srgbaColorInputFields[2] = CreateInputField(clonedInputField, srgbaColorInputOuter, OnsRGBAValueChanged, new Color(0,0.4f,1,1), TMP_InputField.ContentType.DecimalNumber, 5, "sRGBAInputField_B", "B");
        _srgbaColorInputFields[3] = CreateInputField(clonedInputField, srgbaColorInputOuter, OnsRGBAValueChanged, Color.gray, TMP_InputField.ContentType.DecimalNumber, 5, "sRGBAInputField_A", "A");
    }
    
    private InputFieldExtended CreateInputField(GameObject toClone, GameObject parent, UnityAction<string> onValueChanged, Color textColor, TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Alphanumeric, int characterLimit = 8, string goName = "InputField", string placeholderText = "")
    {
        var inputInner = Instantiate(toClone, parent.transform);
        inputInner.name = goName;
        inputInner.AddComponent<LayoutElement>().minWidth = 45;
        var inputField = inputInner.GetComponentInChildren<InputFieldExtended>();
        inputField.contentType = contentType;
        inputField.characterLimit = characterLimit;
        inputField.textComponent.color = textColor;
        inputField.placeholder.GetComponent<Localize>().enabled = false;
        inputField.placeholder.GetComponent<TMP_Text>().text = placeholderText;
        //inputField.placeholder.gameObject.SetActive(false);
        inputField.onValueChanged.AddListener(onValueChanged);
        return inputField;
    }

    private void UpdateInputFields(Color color)
    {
        switch (_selectedInputFormat)
        {
            case ColorFormat.Hex:
                var colorString = ColorUtility.ToHtmlStringRGBA(color);
                if(!_hexColorInputField.isFocused)
                    _hexColorInputField.SetTextWithoutNotify(colorString);
                break;
            case ColorFormat.RGBA:
                if(!_rgbaColorInputFields[0].isFocused)
                    _rgbaColorInputFields[0].SetTextWithoutNotify(((Color32)color).r.ToString());
                if(!_rgbaColorInputFields[1].isFocused)
                    _rgbaColorInputFields[1].SetTextWithoutNotify(((Color32)color).g.ToString());
                if(!_rgbaColorInputFields[2].isFocused)
                    _rgbaColorInputFields[2].SetTextWithoutNotify(((Color32)color).b.ToString());
                if(!_rgbaColorInputFields[3].isFocused)
                    _rgbaColorInputFields[3].SetTextWithoutNotify(((Color32)color).a.ToString());
                break;            
            case ColorFormat.sRGBA:
                if(!_srgbaColorInputFields[0].isFocused)
                    _srgbaColorInputFields[0].SetTextWithoutNotify(color.r.ToString3F());
                if(!_srgbaColorInputFields[1].isFocused)
                    _srgbaColorInputFields[1].SetTextWithoutNotify(color.g.ToString3F());
                if(!_srgbaColorInputFields[2].isFocused)
                    _srgbaColorInputFields[2].SetTextWithoutNotify(color.b.ToString3F());
                if(!_srgbaColorInputFields[3].isFocused)
                    _srgbaColorInputFields[3].SetTextWithoutNotify(color.a.ToString3F());
                break;
        }
    }
    
    private void OnColorFormatChanged(ColorFormat format)
    {
        _selectedInputFormat = format;
        _hexColorInputContainer.SetActive(format == ColorFormat.Hex);
        _rgbaColorInputContainer.SetActive(format == ColorFormat.RGBA);
        _srgbaColorInputContainer.SetActive(format == ColorFormat.sRGBA);

        switch (_oabColorPicker._colorEditMode)
        {
            case ObjectAssemblyColorPicker.ColorEditMode.Base:
                UpdateInputFields(_oabColorPicker._baseColorValue.GetValue());
                break;
            case ObjectAssemblyColorPicker.ColorEditMode.Accent:
                UpdateInputFields(_oabColorPicker._accentColorValue.GetValue());
                break;
        }
    }

    private void OnColorEditModeChanged()
    {
        switch (_oabColorPicker._colorEditMode)
        {
            case ObjectAssemblyColorPicker.ColorEditMode.Base:
                UpdateInputFields(_oabColorPicker._baseColorValue.GetValue());
                break;
            case ObjectAssemblyColorPicker.ColorEditMode.Accent:
                UpdateInputFields(_oabColorPicker._accentColorValue.GetValue());
                break;
        }
    }
    
    private void OnHexValueChanged(string value)
    {
        var colorString = $"#{value}";
        if (ColorUtility.TryParseHtmlString(colorString, out Color color))
        {
            SetOABColor(color);
        }
        else
        {
            //Logger.LogInfo("Color parse failed");
        }
    }
    
    private void OnRGBAValueChanged(string value)
    {
        Color color = new Color32(
            _rgbaColorInputFields[0].text.ToByte(),
            _rgbaColorInputFields[1].text.ToByte(),
            _rgbaColorInputFields[2].text.ToByte(),
            _rgbaColorInputFields[3].text.ToByte()
        );
        SetOABColor(color);
    }
    
    private void OnsRGBAValueChanged(string value)
    {
        Color color = new Color(
            _srgbaColorInputFields[0].text.ToFloat(),
            _srgbaColorInputFields[1].text.ToFloat(),
            _srgbaColorInputFields[2].text.ToFloat(),
            _srgbaColorInputFields[3].text.ToFloat()
        );
        SetOABColor(color);
    }
    
    private void SetOABColor(Color color)
    {
        switch (_oabColorPicker._colorEditMode)
        {
            case ObjectAssemblyColorPicker.ColorEditMode.Base:
                _oabColorPicker._baseColorValue.SetValueInternal(color);
                break;
            case ObjectAssemblyColorPicker.ColorEditMode.Accent:
                _oabColorPicker._accentColorValue.SetValueInternal(color);
                break;
        }
    }
}

internal static class StringExtensions
{
    public static byte ToByte(this string value)
    {
        if (!value.IsNullOrEmpty() && int.TryParse(value, out int intValue))
        {
            return (byte)intValue;
        }
        return 0;
    }
    
    public static float ToFloat(this string value)
    {
        if (!value.IsNullOrEmpty() && float.TryParse(value, out float floatValue))
        {
            return Mathf.Abs(floatValue);
        }
        return 0;
    }
}

internal static class FloatExtensions
{
    public static string ToString3F(this float value)
    {
        value = (float)Math.Round(value, 3);
        string formatSpecifier = value % 1 == 0 ? "F0" : "F3";
        return value.ToString(formatSpecifier);
    }
}