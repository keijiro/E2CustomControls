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
        AddToClassList("e2-piano-key");
        AddToClassList(IsBlackKey ? "e2-piano-key__black" : "e2-piano-key__white");
        if (!IsBlackKey && relativeNote > 0) AddToClassList("e2-piano-key--no-left");
        RegisterCallback<ClickEvent>(OnClick);
    }

    void OnClick(ClickEvent evt)
      => OnClicked?.Invoke(RelativeNote);

    void SetPressed(bool pressed)
    {
        _isPressed = pressed;
        if (pressed)
            AddToClassList("e2-piano-key--pressed");
        else
            RemoveFromClassList("e2-piano-key--pressed");
    }
}

} // namespace E2Controls