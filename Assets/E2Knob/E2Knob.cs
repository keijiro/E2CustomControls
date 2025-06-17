using UnityEngine;
using UnityEngine.UIElements;

namespace E2Controls {

[UxmlElement]
public sealed partial class E2Knob : BaseField<int>
{
    #region UI attributes

    [UxmlAttribute]
    public int lowValue { get => _lowValue; set => SetLowValue(value); }

    [UxmlAttribute]
    public int highValue { get => _highValue; set => SetHighValue(value); }

    [UxmlAttribute]
    public bool isRelative { get => _isRelative; set => SetIsRelative(value); }

    [UxmlAttribute]
    public float sensitivity { get; set; } = 1;

    #endregion

    #region Runtime public properties

    public bool showOverlay { get => _showOverlay; set => SetShowOverlay(value); }

    #endregion

    #region Property backend

    int _lowValue = 0;
    int _highValue = 100;
    bool _isRelative;
    bool _showOverlay;

    void SetLowValue(int value)
    {
        _lowValue = value;
        SetValueWithoutNotify(this.value);
    }

    void SetHighValue(int value)
    {
        _highValue = value;
        SetValueWithoutNotify(this.value);
    }

    void SetIsRelative(bool value)
    {
        _isRelative = value;
        SetValueWithoutNotify(this.value);
    }

    void SetShowOverlay(bool value)
    {
        _showOverlay = value;
        _overlay.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
    }

    #endregion

    #region USS class names

    public static readonly new string ussClassName = "e2-knob";
    public static readonly new string labelUssClassName = "e2-knob__label";
    public static readonly string overlayLabelUssClassName = "e2-knob__overlay-label";

    #endregion

    #region Visual element implementation

    E2KnobInput _input;
    Label _overlay;

    public E2Knob() : this(null) {}

    public E2Knob(string label) : base(label, new E2KnobInput())
    {
        AddToClassList(ussClassName);
        labelElement.AddToClassList(labelUssClassName);

        // Knob input control
        _input = (E2KnobInput)this.Q(className: E2KnobInput.ussClassName);
        _input.AddManipulator(new E2Dragger(this));

        // Value overlay label
        _overlay = new();
        _overlay.AddToClassList(overlayLabelUssClassName);
        _input.Add(_overlay);
    }

    public override void SetValueWithoutNotify(int newValue)
    {
        newValue = Mathf.Clamp(newValue, lowValue, highValue);
        base.SetValueWithoutNotify(newValue);

        // Value overlay label
        _overlay.text = newValue.ToString();

        // Knob input control
        _input.NormalizedValue = (float)(newValue - lowValue) / (highValue - lowValue);
        _input.IsRelative = isRelative;
        _input.MarkDirtyRepaint();
    }

    #endregion
}

} // namespace E2Controls
