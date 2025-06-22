using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace E2Controls {

[UxmlElement]
public sealed partial class E2ChordKeyboard : VisualElement
{
    #region Public members

    [UxmlAttribute, CreateProperty] public int note1 { get => _note1; set => SetNote1(value); }
    [UxmlAttribute, CreateProperty] public int note2 { get => _note2; set => SetNote2(value); }
    [UxmlAttribute, CreateProperty] public int note3 { get => _note3; set => SetNote3(value); }
    [UxmlAttribute, CreateProperty] public int note4 { get => _note4; set => SetNote4(value); }

    public void ClearChord()
    {
        _note1 = _note2 = _note3 = _note4 = -1;
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

    // Binding IDs for properties
    static readonly BindingId Note1Property = nameof(note1);
    static readonly BindingId Note2Property = nameof(note2);
    static readonly BindingId Note3Property = nameof(note3);
    static readonly BindingId Note4Property = nameof(note4);

    // Property backing fields
    int _note1 = -1;
    int _note2 = -1;
    int _note3 = -1;
    int _note4 = -1;

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

    void SetNote1(int value)
    {
        _note1 = value;
        UpdateKeyStates();
        SendChordChangedEvent();
    }

    void SetNote2(int value)
    {
        _note2 = value;
        UpdateKeyStates();
        SendChordChangedEvent();
    }

    void SetNote3(int value)
    {
        _note3 = value;
        UpdateKeyStates();
        SendChordChangedEvent();
    }

    void SetNote4(int value)
    {
        _note4 = value;
        UpdateKeyStates();
        SendChordChangedEvent();
    }

    #endregion

    #region Chord operations

    // Checks if a note is currently active
    bool IsNoteActive(int note)
        => _note1 == note || _note2 == note || 
           _note3 == note || _note4 == note;

    // Adds a note with FIFO behavior (only when all 4 slots are filled)
    void AddNote(int note)
    {
        // Find first empty slot
        if (_note1 == -1)
            _note1 = note;
        else if (_note2 == -1)
            _note2 = note;
        else if (_note3 == -1)
            _note3 = note;
        else if (_note4 == -1)
            _note4 = note;
        else
        {
            // All slots filled, use FIFO
            _note1 = _note2;
            _note2 = _note3;
            _note3 = _note4;
            _note4 = note;
        }
    }

    // Removes a specific note from chord
    void RemoveNote(int note)
    {
        if (_note1 == note) _note1 = -1;
        if (_note2 == note) _note2 = -1;
        if (_note3 == note) _note3 = -1;
        if (_note4 == note) _note4 = -1;
        CompactChord();
    }

    // Compacts chord by moving active notes to the front
    void CompactChord()
    {
        var i = 0;
        Span<int> chord = stackalloc int[]{-1, -1, -1, -1};
        if (_note1 != -1) chord[i++] = _note1;
        if (_note2 != -1) chord[i++] = _note2;
        if (_note3 != -1) chord[i++] = _note3;
        if (_note4 != -1) chord[i++] = _note4;
        _note1 = chord[0];
        _note2 = chord[1];
        _note3 = chord[2];
        _note4 = chord[3];
    }

    #endregion

    #region UI callbacks and event handlers

    void OnKeyClicked(int relativeNote)
    {
        var note = BaseNote + relativeNote;
        if (IsNoteActive(note)) RemoveNote(note); else AddNote(note);
        UpdateKeyStates();
        SendChordChangedEvent();
        NotifyPropertyChanged(Note1Property);
        NotifyPropertyChanged(Note2Property);
        NotifyPropertyChanged(Note3Property);
        NotifyPropertyChanged(Note4Property);
    }

    void SendChordChangedEvent()
    {
        using var evt = ChangeEvent<(int, int, int, int)>
          .GetPooled(default, (_note1, _note2, _note3, _note4));
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