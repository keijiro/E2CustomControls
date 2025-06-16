using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace E2Controls {

// A chord keyboard UI element that allows selecting up to 4 notes across 3 octaves
public sealed class ChordKeyboard : VisualElement
{
    public new class UxmlFactory : UxmlFactory<ChordKeyboard, UxmlTraits> { }
    public new class UxmlTraits : VisualElement.UxmlTraits { }

    // Events for chord and octave changes
    public event Action<int[]> OnChordChanged;
    public event Action<int> OnOctaveChanged;
    public event Action<int, bool> OnNoteToggled;

    // Note management
    Queue<int> noteOrder = new();
    readonly HashSet<int> activeNotes = new();
    int baseOctave = 3;
    
    // Constants
    const int MAX_NOTES = 4;
    const int OCTAVE_RANGE = 3;
    const int TOTAL_SEMITONES = OCTAVE_RANGE * 12;
    
    // UI elements
    Button leftShiftButton;
    Button rightShiftButton;
    VisualElement keyboardContainer;
    readonly List<PianoKey> pianoKeys = new();

    // Piano layout pattern (true = black key)
    static readonly bool[] BlackKeyPattern = { false, true, false, true, false, false, true, false, true, false, true, false };

    public ChordKeyboard()
    {
        // Setup basic styling
        AddToClassList("chord-keyboard");
        style.flexDirection = FlexDirection.Row;
        style.alignItems = Align.Center;
        style.height = 120;

        CreateUI();
        GenerateKeys();
        UpdateShiftButtons();
    }

    // Creates the main UI structure: left button, keyboard container, right button
    void CreateUI()
    {
        // Left octave shift button
        leftShiftButton = CreateShiftButton("<", "left-shift", -1);
        Add(leftShiftButton);

        // Piano keys container
        keyboardContainer = new VisualElement { name = "keyboard-container" };
        keyboardContainer.AddToClassList("keyboard-container");
        keyboardContainer.style.flexGrow = 1;
        keyboardContainer.style.flexDirection = FlexDirection.Row;
        keyboardContainer.style.position = Position.Relative;
        keyboardContainer.style.height = 100;
        Add(keyboardContainer);

        // Right octave shift button
        rightShiftButton = CreateShiftButton(">", "right-shift", 1);
        Add(rightShiftButton);
    }

    // Helper method to create octave shift buttons
    Button CreateShiftButton(string text, string name, int direction)
    {
        var button = new Button(() => ShiftOctave(direction))
        {
            text = text,
            name = name
        };
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
        
        blackKey.style.position = Position.Absolute;
        blackKey.style.left = Length.Percent(whiteKeyWidth * (whiteKeyIndex + 0.7f));
        blackKey.style.width = Length.Percent(whiteKeyWidth * 0.6f);
        blackKey.style.height = Length.Percent(60);
        blackKey.style.top = 0;
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

    // Toggles a note on/off, managing the 4-note limit with FIFO behavior
    void ToggleNote(int midiNote)
    {
        bool wasPressed = activeNotes.Contains(midiNote);
        
        if (wasPressed)
        {
            RemoveNote(midiNote);
        }
        else
        {
            AddNote(midiNote);
        }

        UpdateKeyStates();
        OnNoteToggled?.Invoke(midiNote, !wasPressed);
        OnChordChanged?.Invoke(GetCurrentChord());
    }

    // Adds a note, removing the oldest if at maximum capacity
    void AddNote(int midiNote)
    {
        if (activeNotes.Count >= MAX_NOTES)
        {
            int oldestNote = noteOrder.Dequeue();
            activeNotes.Remove(oldestNote);
        }
        
        activeNotes.Add(midiNote);
        noteOrder.Enqueue(midiNote);
    }

    // Removes a note from both active set and order queue
    void RemoveNote(int midiNote)
    {
        activeNotes.Remove(midiNote);
        noteOrder = new Queue<int>(noteOrder.Where(n => n != midiNote));
    }

    // Updates visual state of all piano keys based on active notes
    void UpdateKeyStates()
    {
        foreach (var key in pianoKeys)
        {
            key.IsPressed = activeNotes.Contains(key.Note);
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
        OnOctaveChanged?.Invoke(baseOctave);
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
    public int[] GetCurrentChord() => activeNotes.OrderBy(n => n).ToArray();

    // Clears all active notes
    public void ClearChord()
    {
        activeNotes.Clear();
        noteOrder.Clear();
        UpdateKeyStates();
        OnChordChanged?.Invoke(GetCurrentChord());
    }

    // Gets the current base octave (0-7)
    public int CurrentBaseOctave => baseOctave;
}

} // namespace E2Controls