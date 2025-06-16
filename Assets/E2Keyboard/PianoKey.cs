using System;
using UnityEngine.UIElements;

namespace E2Controls {

public sealed class PianoKey : VisualElement
{
    public event Action<int> OnClicked;

    public int Note { get; private set; }

    public bool IsPressed { get => _pressed; set => SetPressed(value); }

    bool _pressed;

    public PianoKey(int note, bool isBlackKey)
    {
        Note = note;
        AddToClassList("piano-key");
        AddToClassList(isBlackKey ? "piano-key--black" : "piano-key--white");
        RegisterCallback<ClickEvent>(OnClick);
    }

    void OnClick(ClickEvent evt)
      => OnClicked?.Invoke(Note);

    void SetPressed(bool pressed)
    {
        _pressed = pressed;
        if (pressed)
            AddToClassList("piano-key--pressed");
        else
            RemoveFromClassList("piano-key--pressed");
    }
}

} // namespace E2Controls