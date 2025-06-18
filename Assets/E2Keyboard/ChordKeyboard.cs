using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace E2Controls {

[UxmlElement]
public sealed partial class ChordKeyboard : VisualElement
{
    #region Public members

    public (int, int, int, int) CurrentChord => _chord;

    public void ClearChord()
    {
        _chord = (-1, -1, -1, -1);
        UpdateKeyStates();
        SendChordChangedEvent();
    }

    #endregion

    #region Constructor

    public ChordKeyboard()
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

    // Note management - simplified to 4-note tuple
    (int note1, int note2, int note3, int note4) _chord = (-1, -1, -1, -1);
    int _baseOctave = 3;
    
    // UI elements
    Button _leftShiftButton;
    Button _rightShiftButton;
    VisualElement _keyboardContainer;
    List<PianoKey> _pianoKeys = new();

    static bool IsBlackKey(int noteInOctave)
      => (noteInOctave & 1) == (noteInOctave % 12 < 5 ? 1 : 0);

    static int GetWhiteKeyIndex(int note)
    {
        var (o, i) = (note / 12, note % 12);
        return o * 7 + (i < 5 ? i / 2 : (i - 5) / 2 + 3);
    }

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
            var key = new PianoKey(i);
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

    void PositionBlackKey(PianoKey key)
    {
        var index = GetWhiteKeyIndex(key.RelativeNote);
        var width = 100.0f / (TotalOctaves * 7);
        key.AddToClassList("piano-key--black-positioned");
        key.style.left = Length.Percent(width * (index + 0.7f));
        key.style.width = Length.Percent(width * 0.6f);
        key.style.height = Length.Percent(60);
    }

    void OnKeyClicked(int relativeNote)
    {
        var note = BaseNote + relativeNote;
        if (IsNoteActive(note)) RemoveNote(note); else AddNote(note);
        UpdateKeyStates();
        SendChordChangedEvent();
    }

    // Adds a note with FIFO behavior (only when all 4 slots are filled)
    void AddNote(int midiNote)
    {
        var (n1, n2, n3, n4) = _chord;
        
        // Find first empty slot
        if (n1 == -1)
            _chord = (midiNote, n2, n3, n4);
        else if (n2 == -1)
            _chord = (n1, midiNote, n3, n4);
        else if (n3 == -1)
            _chord = (n1, n2, midiNote, n4);
        else if (n4 == -1)
            _chord = (n1, n2, n3, midiNote);
        else
            // All slots filled, use FIFO
            _chord = (n2, n3, n4, midiNote);
    }

    // Removes a specific note from chord
    void RemoveNote(int midiNote)
    {
        var (n1, n2, n3, n4) = _chord;
        _chord = (
            n1 == midiNote ? -1 : n1,
            n2 == midiNote ? -1 : n2,
            n3 == midiNote ? -1 : n3,
            n4 == midiNote ? -1 : n4
        );
        CompactChord();
    }

    // Compacts chord by moving active notes to the front
    void CompactChord()
    {
        var activeNotes = new[] { _chord.note1, _chord.note2, _chord.note3, _chord.note4 }
            .Where(n => n != -1).ToArray();
        _chord = (
            activeNotes.Length > 0 ? activeNotes[0] : -1,
            activeNotes.Length > 1 ? activeNotes[1] : -1,
            activeNotes.Length > 2 ? activeNotes[2] : -1,
            activeNotes.Length > 3 ? activeNotes[3] : -1
        );
    }

    // Checks if a note is currently active
    bool IsNoteActive(int midiNote) =>
        _chord.note1 == midiNote || _chord.note2 == midiNote || 
        _chord.note3 == midiNote || _chord.note4 == midiNote;

    // Updates visual state of all piano keys based on active notes
    void UpdateKeyStates()
    {
        var baseNote = BaseNote;
        foreach (var key in _pianoKeys)
        {
            var midiNote = baseNote + key.RelativeNote;
            key.IsPressed = IsNoteActive(midiNote);
        }
    }

    // Shifts the keyboard octave range up or down
    void ShiftOctave(int direction)
    {
        var newOctave = _baseOctave + direction;
        if (newOctave is < 0 or > 7) return;

        _baseOctave = newOctave;
        UpdateKeyStates();
        UpdateShiftButtons();
    }

    // Updates octave shift button enabled states
    void UpdateShiftButtons()
    {
        _leftShiftButton.SetEnabled(_baseOctave > 0);
        _rightShiftButton.SetEnabled(_baseOctave < 7);
    }

    // Calculates the MIDI note number for the current base octave
    int BaseNote => _baseOctave * 12 + 12;

    // Sends ChangeEvent with current chord as (int, int, int, int) tuple
    void SendChordChangedEvent()
    {
        using var evt = ChangeEvent<(int, int, int, int)>.GetPooled(default, _chord);
        evt.target = this;
        SendEvent(evt);
    }

    #endregion
}

} // namespace E2Controls