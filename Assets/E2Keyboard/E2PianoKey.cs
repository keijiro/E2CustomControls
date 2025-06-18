using System;
using UnityEngine.UIElements;

namespace E2Controls {

public sealed class E2PianoKey : VisualElement
{
    public event Action<int> OnClicked;

    public int RelativeNote { get; private set; }

    public bool IsBlackKey
      => (RelativeNote & 1) == (RelativeNote % 12 < 5 ? 1 : 0);

    public bool IsPressed { get => _isPressed; set => SetPressed(value); }

    bool _isPressed;

    public E2PianoKey(int relativeNote)
    {
        RelativeNote = relativeNote;
        AddToClassList("piano-key");
        AddToClassList(IsBlackKey ? "piano-key--black" : "piano-key--white");
        RegisterCallback<ClickEvent>(OnClick);
    }

    void OnClick(ClickEvent evt)
      => OnClicked?.Invoke(RelativeNote);

    void SetPressed(bool pressed)
    {
        _isPressed = pressed;
        if (pressed)
            AddToClassList("piano-key--pressed");
        else
            RemoveFromClassList("piano-key--pressed");
    }
}

} // namespace E2Controls