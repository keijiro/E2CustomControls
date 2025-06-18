using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace E2Controls {

[UxmlElement]
public sealed partial class E2ChordKeyboard : VisualElement
{
    #region Public members

    public (int note1, int note2, int note3, int note4)
        CurrentChord { get; private set; } = (-1, -1, -1, -1);

    public void ClearChord()
    {
        CurrentChord = (-1, -1, -1, -1);
        UpdateKeyStates();
        SendChordChangedEvent();
    }

    #endregion

    #region Constructor

    public E2ChordKeyboard()
    {
        AddToClassList("chord-keyboard");

        // Octave shift buttons
        _leftShiftButton = CreateShiftButton("<", -1);
        _rightShiftButton = CreateShiftButton(">", 1);

        // Piano keys container
        _keyboardContainer = new VisualElement();
        _keyboardContainer.AddToClassList("keyboard-container");

        // Base layout
        Add(_leftShiftButton);
        Add(_keyboardContainer);
        Add(_rightShiftButton);

        // Piano keys
        CreatePianoKeys(_keyboardContainer);
    }

    #endregion

    #region Private members

    // Constants
    const int TotalOctaves = 3;
    const int TotalKeys = TotalOctaves * 12;

    // Active range
    int _baseOctave = 3;
    int BaseNote => _baseOctave * 12 + 12;
    
    // UI elements
    Button _leftShiftButton;
    Button _rightShiftButton;
    VisualElement _keyboardContainer;
    List<E2PianoKey> _pianoKeys = new();

    static int GetWhiteKeyIndex(int note)
    {
        var (o, i) = (note / 12, note % 12);
        return o * 7 + (i < 5 ? i / 2 : (i - 5) / 2 + 3);
    }

    #endregion

    #region Chord tuple operations

    // Checks if a note is currently active
    bool IsNoteActive(int note)
        => CurrentChord.note1 == note || CurrentChord.note2 == note || 
           CurrentChord.note3 == note || CurrentChord.note4 == note;

    // Adds a note with FIFO behavior (only when all 4 slots are filled)
    void AddNote(int note)
    {
        var (n1, n2, n3, n4) = CurrentChord;
        // Find first empty slot
        if (n1 == -1)
            CurrentChord = (note, n2, n3, n4);
        else if (n2 == -1)
            CurrentChord = (n1, note, n3, n4);
        else if (n3 == -1)
            CurrentChord = (n1, n2, note, n4);
        else if (n4 == -1)
            CurrentChord = (n1, n2, n3, note);
        else
            // All slots filled, use FIFO
            CurrentChord = (n2, n3, n4, note);
    }

    // Removes a specific note from chord
    void RemoveNote(int note)
    {
        var (n1, n2, n3, n4) = CurrentChord;
        CurrentChord = (n1 == note ? -1 : n1, n2 == note ? -1 : n2,
                        n3 == note ? -1 : n3, n4 == note ? -1 : n4);
        CompactChord();
    }

    // Compacts chord by moving active notes to the front
    void CompactChord()
    {
        var i = 0;
        Span<int> chord = stackalloc int[]{-1, -1, -1, -1};
        if (CurrentChord.note1 != -1) chord[i++] = CurrentChord.note1;
        if (CurrentChord.note2 != -1) chord[i++] = CurrentChord.note2;
        if (CurrentChord.note3 != -1) chord[i++] = CurrentChord.note3;
        if (CurrentChord.note4 != -1) chord[i++] = CurrentChord.note4;
        CurrentChord = (chord[0], chord[1], chord[2], chord[3]);
    }

    #endregion

    #region UI callbacks and event handlers

    void OnKeyClicked(int relativeNote)
    {
        var note = BaseNote + relativeNote;
        if (IsNoteActive(note)) RemoveNote(note); else AddNote(note);
        UpdateKeyStates();
        SendChordChangedEvent();
    }

    void SendChordChangedEvent()
    {
        using var evt = ChangeEvent<(int, int, int, int)>.GetPooled(default, CurrentChord);
        evt.target = this;
        SendEvent(evt);
    }

    void ShiftOctave(int direction)
    {
        _baseOctave = Math.Clamp(_baseOctave + direction, 0, 7);
        UpdateKeyStates();
        _leftShiftButton.SetEnabled(_baseOctave > 0);
        _rightShiftButton.SetEnabled(_baseOctave < 7);
    }

    void UpdateKeyStates()
    {
        foreach (var key in _pianoKeys)
            key.IsPressed = IsNoteActive(BaseNote + key.RelativeNote);
    }

    #endregion

    #region UI factory methods

    Button CreateShiftButton(string text, int direction)
    {
        var button = new Button(() => ShiftOctave(direction)){ text = text };
        button.AddToClassList("octave-shift-button");
        return button;
    }

    void CreatePianoKeys(VisualElement container)
    {
        // Key instantiation
        for (var i = 0; i < TotalKeys; i++)
        {
            var key = new E2PianoKey(i);
            key.OnClicked += OnKeyClicked;
            _pianoKeys.Add(key);
        }

        // First pass: white keys (background layer)
        foreach (var key in _pianoKeys)
            if (!key.IsBlackKey) container.Add(key);
        
        // Second pass: black keys with positioning
        foreach (var key in _pianoKeys)
        {
            if (!key.IsBlackKey) continue;
            container.Add(key);
            PositionBlackKey(key);
        }
    }

    void PositionBlackKey(E2PianoKey key)
    {
        var index = GetWhiteKeyIndex(key.RelativeNote);
        var width = 100.0f / (TotalOctaves * 7);
        key.AddToClassList("piano-key--black-positioned");
        key.style.left = Length.Percent(width * (index + 0.7f));
        key.style.width = Length.Percent(width * 0.6f);
        key.style.height = Length.Percent(60);
    }

    #endregion
}

} // namespace E2Controls