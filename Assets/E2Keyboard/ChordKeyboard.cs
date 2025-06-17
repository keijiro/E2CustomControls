using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace E2Controls {

[UxmlElement]
public sealed partial class ChordKeyboard : VisualElement
{
    // Note management - simplified to 4-note tuple
    (int note1, int note2, int note3, int note4) chord = (-1, -1, -1, -1);
    int baseOctave = 3;
    
    // Constants
    const int OCTAVE_RANGE = 3;
    const int TOTAL_SEMITONES = OCTAVE_RANGE * 12;
    
    // UI elements
    Button leftShiftButton;
    Button rightShiftButton;
    VisualElement keyboardContainer;
    List<PianoKey> pianoKeys = new();

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
        leftShiftButton = CreateShiftButton("<", "left-shift", -1);
        Add(leftShiftButton);

        // Piano keys container
        keyboardContainer = new VisualElement { name = "keyboard-container" };
        keyboardContainer.AddToClassList("keyboard-container");
        Add(keyboardContainer);

        // Right octave shift button
        rightShiftButton = CreateShiftButton(">", "right-shift", 1);
        Add(rightShiftButton);
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
        keyboardContainer.Clear();
        pianoKeys.Clear();

        int startNote = GetBaseNoteNumber();
        var (whiteKeys, blackKeys) = CreatePianoKeys(startNote);

        // Add white keys first (they form the background)
        whiteKeys.ForEach(keyboardContainer.Add);
        
        // Add black keys on top with positioning
        foreach (var blackKey in blackKeys)
        {
            keyboardContainer.Add(blackKey);
            PositionBlackKey(blackKey, startNote);
        }

        UpdateKeyStates();
    }

    // Creates and categorizes piano keys into white and black keys
    (List<PianoKey> whiteKeys, List<PianoKey> blackKeys) CreatePianoKeys(int startNote)
    {
        var whiteKeys = new List<PianoKey>();
        var blackKeys = new List<PianoKey>();

        for (int i = 0; i < TOTAL_SEMITONES; i++)
        {
            int midiNote = startNote + i;
            bool isBlack = BlackKeyPattern[i % 12];

            var key = new PianoKey(midiNote, isBlack);
            key.OnClicked += OnKeyClicked;
            pianoKeys.Add(key);

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
        int whiteKeyIndex = GetWhiteKeyIndex(blackKey.Note - startNote);
        float whiteKeyWidth = 100f / GetWhiteKeyCount();
        
        blackKey.AddToClassList("piano-key--black-positioned");
        blackKey.style.left = Length.Percent(whiteKeyWidth * (whiteKeyIndex + 0.7f));
        blackKey.style.width = Length.Percent(whiteKeyWidth * 0.6f);
        blackKey.style.height = Length.Percent(60);
    }

    // Calculates the white key index for a given semitone offset
    int GetWhiteKeyIndex(int semitoneFromStart)
    {
        int whiteKeyCount = 0;
        for (int i = 0; i < semitoneFromStart; i++)
        {
            if (!BlackKeyPattern[i % 12])
                whiteKeyCount++;
        }
        return whiteKeyCount;
    }

    // Gets total number of white keys in the current range
    int GetWhiteKeyCount()
    {
        return Enumerable.Range(0, TOTAL_SEMITONES)
            .Count(i => !BlackKeyPattern[i % 12]);
    }

    // Handles piano key click events
    void OnKeyClicked(int midiNote) => ToggleNote(midiNote);

    // Toggles a note on/off with FIFO behavior for 4-note limit
    void ToggleNote(int midiNote)
    {
        bool wasPressed = IsNoteActive(midiNote);
        
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

    // Adds a note with FIFO behavior
    void AddNote(int midiNote)
    {
        chord = (chord.note2, chord.note3, chord.note4, midiNote);
    }

    // Removes a specific note from chord
    void RemoveNote(int midiNote)
    {
        var (n1, n2, n3, n4) = chord;
        chord = (
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
        var activeNotes = new[] { chord.note1, chord.note2, chord.note3, chord.note4 }
            .Where(n => n != -1).ToArray();
        chord = (
            activeNotes.Length > 0 ? activeNotes[0] : -1,
            activeNotes.Length > 1 ? activeNotes[1] : -1,
            activeNotes.Length > 2 ? activeNotes[2] : -1,
            activeNotes.Length > 3 ? activeNotes[3] : -1
        );
    }

    // Checks if a note is currently active
    bool IsNoteActive(int midiNote) =>
        chord.note1 == midiNote || chord.note2 == midiNote || 
        chord.note3 == midiNote || chord.note4 == midiNote;

    // Updates visual state of all piano keys based on active notes
    void UpdateKeyStates()
    {
        foreach (var key in pianoKeys)
        {
            key.IsPressed = IsNoteActive(key.Note);
        }
    }

    // Shifts the keyboard octave range up or down
    void ShiftOctave(int direction)
    {
        int newOctave = baseOctave + direction;
        if (newOctave is < 0 or > 7) return;

        baseOctave = newOctave;
        GenerateKeys();
        UpdateShiftButtons();
    }

    // Updates octave shift button enabled states
    void UpdateShiftButtons()
    {
        leftShiftButton.SetEnabled(baseOctave > 0);
        rightShiftButton.SetEnabled(baseOctave < 7);
    }

    // Calculates the MIDI note number for the current base octave
    int GetBaseNoteNumber() => baseOctave * 12 + 12;

    // Gets the current chord as an ordered array of MIDI note numbers
    public int[] GetCurrentChord() => 
        new[] { chord.note1, chord.note2, chord.note3, chord.note4 }
            .Where(n => n != -1).OrderBy(n => n).ToArray();

    // Clears all active notes
    public void ClearChord()
    {
        chord = (-1, -1, -1, -1);
        UpdateKeyStates();
        SendChordChangedEvent();
    }

    // Gets the current base octave (0-7)
    public int CurrentBaseOctave => baseOctave;

    // Sends ChangeEvent with current chord as (int, int, int, int) tuple
    void SendChordChangedEvent()
    {
        using var evt = ChangeEvent<(int, int, int, int)>.GetPooled(default, chord);
        evt.target = this;
        SendEvent(evt);
    }
}

} // namespace E2Controls