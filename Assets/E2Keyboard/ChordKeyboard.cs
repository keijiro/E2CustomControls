using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace E2Controls {

[UxmlElement]
public sealed partial class ChordKeyboard : VisualElement
{
    // Constants
    const int TotalKeys = 3 * 12;

    // Note management - simplified to 4-note tuple
    (int note1, int note2, int note3, int note4) _chord = (-1, -1, -1, -1);
    int _baseOctave = 3;
    
    // UI elements
    Button _leftShiftButton;
    Button _rightShiftButton;
    VisualElement _keyboardContainer;
    List<PianoKey> _pianoKeys = new();

    // Piano layout pattern (true = black key)
    static readonly bool[] BlackKeyPattern =
      { false, true, false, true, false, false, true, false, true, false, true, false };

    public ChordKeyboard()
    {
        AddToClassList("chord-keyboard");
        CreateUI();
        GenerateKeys();
        UpdateShiftButtons();
    }

    void CreateUI()
    {
        // Left octave shift button
        _leftShiftButton = CreateShiftButton("<", "left-shift", -1);
        Add(_leftShiftButton);

        // Piano keys container
        _keyboardContainer = new VisualElement { name = "keyboard-container" };
        _keyboardContainer.AddToClassList("keyboard-container");
        Add(_keyboardContainer);

        // Right octave shift button
        _rightShiftButton = CreateShiftButton(">", "right-shift", 1);
        Add(_rightShiftButton);
    }

    // Helper method to create octave shift buttons
    Button CreateShiftButton(string text, string name, int direction)
    {
        var button = new Button(() => ShiftOctave(direction))
            { text = text, name = name };
        button.AddToClassList("octave-shift-button");
        return button;
    }

    // Generates piano keys for the current octave range
    void GenerateKeys()
    {
        _keyboardContainer.Clear();
        _pianoKeys.Clear();

        var startNote = GetBaseNoteNumber();
        var (whiteKeys, blackKeys) = CreatePianoKeys(startNote);

        // Add white keys first (they form the background)
        whiteKeys.ForEach(_keyboardContainer.Add);
        
        // Add black keys on top with positioning
        foreach (var blackKey in blackKeys)
        {
            _keyboardContainer.Add(blackKey);
            PositionBlackKey(blackKey, startNote);
        }

        UpdateKeyStates();
    }

    // Creates and categorizes piano keys into white and black keys
    (List<PianoKey> whiteKeys, List<PianoKey> blackKeys) CreatePianoKeys(int startNote)
    {
        var whiteKeys = new List<PianoKey>();
        var blackKeys = new List<PianoKey>();

        for (var i = 0; i < TotalKeys; i++)
        {
            var isBlack = BlackKeyPattern[i % 12];

            var key = new PianoKey(i, isBlack);
            key.OnClicked += OnKeyClicked;
            _pianoKeys.Add(key);

            if (isBlack)
                blackKeys.Add(key);
            else
                whiteKeys.Add(key);
        }

        return (whiteKeys, blackKeys);
    }

    // Positions a black key relative to white keys
    void PositionBlackKey(PianoKey blackKey, int startNote)
    {
        var whiteKeyIndex = GetWhiteKeyIndex(blackKey.RelativeNote);
        var whiteKeyWidth = 100f / GetWhiteKeyCount();
        
        blackKey.AddToClassList("piano-key--black-positioned");
        blackKey.style.left = Length.Percent(whiteKeyWidth * (whiteKeyIndex - 1 + 0.7f));
        blackKey.style.width = Length.Percent(whiteKeyWidth * 0.6f);
        blackKey.style.height = Length.Percent(60);
    }

    // Calculates the white key index for a given semitone offset
    int GetWhiteKeyIndex(int semitoneFromStart)
    {
        var whiteKeyCount = 0;
        for (var i = 0; i < semitoneFromStart; i++)
        {
            if (!BlackKeyPattern[i % 12])
                whiteKeyCount++;
        }
        return whiteKeyCount;
    }

    // Gets total number of white keys in the current range
    int GetWhiteKeyCount()
    {
        return Enumerable.Range(0, TotalKeys)
            .Count(i => !BlackKeyPattern[i % 12]);
    }

    // Handles piano key click events
    void OnKeyClicked(int relativeNote)
    {
        var midiNote = GetBaseNoteNumber() + relativeNote;
        ToggleNote(midiNote);
    }

    // Toggles a note on/off with FIFO behavior for 4-note limit
    void ToggleNote(int midiNote)
    {
        var wasPressed = IsNoteActive(midiNote);
        
        if (wasPressed)
        {
            RemoveNote(midiNote);
        }
        else
        {
            AddNote(midiNote);
        }

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
        var baseNote = GetBaseNoteNumber();
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
    int GetBaseNoteNumber() => _baseOctave * 12 + 12;

    // Gets the current chord as an ordered array of MIDI note numbers
    public int[] GetCurrentChord() => 
        new[] { _chord.note1, _chord.note2, _chord.note3, _chord.note4 }
            .Where(n => n != -1).OrderBy(n => n).ToArray();

    // Clears all active notes
    public void ClearChord()
    {
        _chord = (-1, -1, -1, -1);
        UpdateKeyStates();
        SendChordChangedEvent();
    }

    // Gets the current base octave (0-7)
    public int CurrentBaseOctave => _baseOctave;

    // Sends ChangeEvent with current chord as (int, int, int, int) tuple
    void SendChordChangedEvent()
    {
        using var evt = ChangeEvent<(int, int, int, int)>.GetPooled(default, _chord);
        evt.target = this;
        SendEvent(evt);
    }
}

} // namespace E2Controls