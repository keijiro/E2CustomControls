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
        
        SetupStyle();
    }

    void SetupStyle()
    {
        if (isBlackKey)
        {
            style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            style.borderTopWidth = 1;
            style.borderBottomWidth = 1;
            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;
            style.borderTopColor = Color.black;
            style.borderBottomColor = Color.black;
            style.borderLeftColor = Color.black;
            style.borderRightColor = Color.black;
        }
        else
        {
            style.backgroundColor = Color.white;
            style.borderTopWidth = 1;
            style.borderBottomWidth = 1;
            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;
            style.borderTopColor = new Color(0.7f, 0.7f, 0.7f);
            style.borderBottomColor = new Color(0.7f, 0.7f, 0.7f);
            style.borderLeftColor = new Color(0.7f, 0.7f, 0.7f);
            style.borderRightColor = new Color(0.7f, 0.7f, 0.7f);
            
            style.flexGrow = 1;
            style.height = Length.Percent(100);
        }
        
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
            if (isBlackKey)
            {
                style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            }
            else
            {
                style.backgroundColor = new Color(0.95f, 0.95f, 0.95f);
            }
        }
    }

    void OnMouseLeave(MouseLeaveEvent evt)
    {
        if (!isPressed)
        {
            if (isBlackKey)
            {
                style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            }
            else
            {
                style.backgroundColor = Color.white;
            }
        }
    }

    public void SetPressed(bool pressed)
    {
        if (isPressed == pressed) return;
        
        isPressed = pressed;
        
        if (pressed)
        {
            AddToClassList("piano-key--pressed");
            if (isBlackKey)
            {
                style.backgroundColor = new Color(0.4f, 0.6f, 1.0f); // 青系
            }
            else
            {
                style.backgroundColor = new Color(0.6f, 0.8f, 1.0f); // 明るい青系
            }
        }
        else
        {
            RemoveFromClassList("piano-key--pressed");
            if (isBlackKey)
            {
                style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            }
            else
            {
                style.backgroundColor = Color.white;
            }
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