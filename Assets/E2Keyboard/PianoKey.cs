using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace E2Controls {

public class PianoKey : VisualElement
{
    public event Action<int> OnClicked;
    
    int midiNote;
    bool isBlackKey;
    bool isPressed;

    public int MidiNote => midiNote;
    public bool IsBlackKey => isBlackKey;
    public bool IsPressed => isPressed;

    public PianoKey(int midiNote, bool isBlackKey)
    {
        this.midiNote = midiNote;
        this.isBlackKey = isBlackKey;
        
        Initialize();
    }

    void Initialize()
    {
        AddToClassList("piano-key");
        AddToClassList(isBlackKey ? "piano-key--black" : "piano-key--white");
        
        this.RegisterCallback<ClickEvent>(OnClick);
        this.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
        this.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
    }


    void OnClick(ClickEvent evt)
    {
        OnClicked?.Invoke(midiNote);
    }

    void OnMouseEnter(MouseEnterEvent evt)
    {
        if (!isPressed)
        {
            AddToClassList("piano-key--hover");
        }
    }

    void OnMouseLeave(MouseLeaveEvent evt)
    {
        if (!isPressed)
        {
            RemoveFromClassList("piano-key--hover");
        }
    }

    public void SetPressed(bool pressed)
    {
        if (isPressed == pressed) return;
        
        isPressed = pressed;
        
        if (pressed)
        {
            AddToClassList("piano-key--pressed");
            RemoveFromClassList("piano-key--hover");
        }
        else
        {
            RemoveFromClassList("piano-key--pressed");
        }
    }

    public string GetNoteName()
    {
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int noteIndex = midiNote % 12;
        int octave = midiNote / 12 - 1;
        return $"{noteNames[noteIndex]}{octave}";
    }
}

} // namespace E2Controls