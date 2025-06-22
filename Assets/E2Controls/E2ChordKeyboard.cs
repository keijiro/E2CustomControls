using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace E2Controls {

[UxmlElement]
public sealed partial class E2ChordKeyboard : VisualElement
{
    #region Public members

    public (int note1, int note2, int note3, int note4) CurrentChord 
      { get => _chord; set  => SetCurrentChord(value); }

    public int Note1 
    { 
        get => _chord.note1; 
        set => SetCurrentChord((value, _chord.note2, _chord.note3, _chord.note4)); 
    }

    public int Note2 
    { 
        get => _chord.note2; 
        set => SetCurrentChord((_chord.note1, value, _chord.note3, _chord.note4)); 
    }

    public int Note3 
    { 
        get => _chord.note3; 
        set => SetCurrentChord((_chord.note1, _chord.note2, value, _chord.note4)); 
    }

    public int Note4 
    { 
        get => _chord.note4; 
        set => SetCurrentChord((_chord.note1, _chord.note2, _chord.note3, value)); 
    }

    public void ClearChord()
    {
        _chord = (-1, -1, -1, -1);
        UpdateKeyStates();
        SendChordChangedEvent();
    }

    #endregion

    #region Constructor

    public E2ChordKeyboard()
    {
        AddToClassList("e2-chord-keyboard");

        // Toolbar
        var toolbar = new VisualElement();
        toolbar.AddToClassList("e2-chord-keyboard__toolbar");
        Add(toolbar);

        // Octave label
        _octaveLabel = new Label("C3");
        _octaveLabel.AddToClassList("e2-chord-keyboard__octave-label");
        toolbar.Add(_octaveLabel);

        // Octave shift buttons
        _leftShiftButton = CreateShiftButton("<", -1);
        _rightShiftButton = CreateShiftButton(">", 1);
        toolbar.Add(_leftShiftButton);
        toolbar.Add(_rightShiftButton);

        // Piano keys container
        _keyboardContainer = new VisualElement();
        _keyboardContainer.AddToClassList("e2-chord-keyboard-container");
        Add(_keyboardContainer);

        // Piano keys
        CreatePianoKeys(_keyboardContainer);
    }

    #endregion

    #region Private members

    // Constants
    const int TotalOctaves = 3;
    const int TotalKeys = TotalOctaves * 12;

    // Property backing fields
    (int note1, int note2, int note3, int note4)
        _chord = (-1, -1, -1, -1);

    // Active range
    int _baseOctave = 3;
    int BaseNote => _baseOctave * 12 + 12;
    
    // UI elements
    Label _octaveLabel;
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
        => _chord.note1 == note || _chord.note2 == note || 
           _chord.note3 == note || _chord.note4 == note;

    void SetCurrentChord((int, int, int, int) chord)
    {
        _chord = chord;
        UpdateKeyStates();
        SendChordChangedEvent();
    }

    // Adds a note with FIFO behavior (only when all 4 slots are filled)
    void AddNote(int note)
    {
        var (n1, n2, n3, n4) = _chord;
        // Find first empty slot
        if (n1 == -1)
            _chord = (note, n2, n3, n4);
        else if (n2 == -1)
            _chord = (n1, note, n3, n4);
        else if (n3 == -1)
            _chord = (n1, n2, note, n4);
        else if (n4 == -1)
            _chord = (n1, n2, n3, note);
        else
            // All slots filled, use FIFO
            _chord = (n2, n3, n4, note);
    }

    // Removes a specific note from chord
    void RemoveNote(int note)
    {
        var (n1, n2, n3, n4) = _chord;
        _chord = (n1 == note ? -1 : n1, n2 == note ? -1 : n2,
                  n3 == note ? -1 : n3, n4 == note ? -1 : n4);
        CompactChord();
    }

    // Compacts chord by moving active notes to the front
    void CompactChord()
    {
        var i = 0;
        Span<int> chord = stackalloc int[]{-1, -1, -1, -1};
        if (_chord.note1 != -1) chord[i++] = _chord.note1;
        if (_chord.note2 != -1) chord[i++] = _chord.note2;
        if (_chord.note3 != -1) chord[i++] = _chord.note3;
        if (_chord.note4 != -1) chord[i++] = _chord.note4;
        _chord = (chord[0], chord[1], chord[2], chord[3]);
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
        using var evt = ChangeEvent<(int, int, int, int)>.GetPooled(default, _chord);
        evt.target = this;
        SendEvent(evt);
    }

    void ShiftOctave(int direction)
    {
        _baseOctave = Math.Clamp(_baseOctave + direction, 0, 7);
        UpdateKeyStates();
        _octaveLabel.text = $"C{_baseOctave}";
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
        key.style.left = Length.Percent(width * (index + 0.7f));
        key.style.width = Length.Percent(width * 0.6f);
    }

    #endregion
}

} // namespace E2Controls