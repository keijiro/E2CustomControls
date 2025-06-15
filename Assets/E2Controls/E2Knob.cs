using UnityEngine;
using UnityEngine.UIElements;

namespace E2Controls {

[UxmlElement]
public partial class E2Knob : BaseField<int>
{
    #region Public UI properties

    [UxmlAttribute]
    public int lowValue { get => _lowValue; set => SetLowValue(value); }

    [UxmlAttribute]
    public int highValue { get => _highValue; set => SetHighValue(value); }

    #endregion

    #region Property backend

    int _lowValue = 0;
    int _highValue = 100;

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

    #endregion

    #region USS class names

    public static readonly new string ussClassName = "e2-knob";
    public static readonly new string labelUssClassName = "e2-knob__label";

    #endregion

    #region Visual element implementation

    E2KnobInput _input;

    public E2Knob() : this(null) {}

    public E2Knob(string label) : base(label, new E2KnobInput())
    {
        AddToClassList(ussClassName);
        labelElement.AddToClassList(labelUssClassName);
        _input = (E2KnobInput)this.Q(className: E2KnobInput.ussClassName);
        _input.AddManipulator(new E2Dragger(this));
    }

    public override void SetValueWithoutNotify(int newValue)
    {
        newValue = Mathf.Clamp(newValue, lowValue, highValue);
        base.SetValueWithoutNotify(newValue);
        _input.NormalizedValue = (float)(newValue - lowValue) / (highValue - lowValue);
        _input.MarkDirtyRepaint();
    }

    #endregion
}

} // namespace E2Controls
