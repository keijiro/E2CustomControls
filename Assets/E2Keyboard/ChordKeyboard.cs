using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace E2Controls {

public sealed class ChordKeyboard : VisualElement
{
    public new class UxmlFactory : UxmlFactory<ChordKeyboard, UxmlTraits> { }
    public new class UxmlTraits : VisualElement.UxmlTraits { }

    public event Action<int[]> OnChordChanged;
    public event Action<int> OnOctaveChanged;
    public event Action<int, bool> OnNoteToggled;

    Queue<int> noteOrder = new Queue<int>();
    HashSet<int> activeNotes = new HashSet<int>();
    int baseOctave = 3; // C3開始
    const int MAX_NOTES = 4;
    const int OCTAVE_RANGE = 3; // 3オクターブ表示
    const int TOTAL_SEMITONES = OCTAVE_RANGE * 12; // 36半音

    Button leftShiftButton;
    Button rightShiftButton;
    VisualElement keyboardContainer;
    List<PianoKey> pianoKeys = new List<PianoKey>();

    readonly bool[] blackKeyPattern = { false, true, false, true, false, false, true, false, true, false, true, false };

    public ChordKeyboard()
    {
        Initialize();
    }

    void Initialize()
    {
        AddToClassList("chord-keyboard");
        style.flexDirection = FlexDirection.Row;
        style.alignItems = Align.Center;
        style.height = 120;

        CreateUI();
        GenerateKeys();
        UpdateShiftButtons();
    }

    void CreateUI()
    {
        leftShiftButton = new Button(() => ShiftOctave(-1))
        {
            text = "<",
            name = "left-shift"
        };
        leftShiftButton.AddToClassList("octave-shift-button");
        Add(leftShiftButton);

        keyboardContainer = new VisualElement
        {
            name = "keyboard-container"
        };
        keyboardContainer.AddToClassList("keyboard-container");
        keyboardContainer.style.flexGrow = 1;
        keyboardContainer.style.flexDirection = FlexDirection.Row;
        keyboardContainer.style.position = Position.Relative;
        keyboardContainer.style.height = 100;
        Add(keyboardContainer);

        rightShiftButton = new Button(() => ShiftOctave(1))
        {
            text = ">",
            name = "right-shift"
        };
        rightShiftButton.AddToClassList("octave-shift-button");
        Add(rightShiftButton);
    }

    void GenerateKeys()
    {
        keyboardContainer.Clear();
        pianoKeys.Clear();

        int startNote = GetBaseNoteNumber();
        List<PianoKey> whiteKeys = new List<PianoKey>();
        List<PianoKey> blackKeys = new List<PianoKey>();

        for (int i = 0; i < TOTAL_SEMITONES; i++)
        {
            int midiNote = startNote + i;
            int semitone = i % 12;
            bool isBlack = blackKeyPattern[semitone];

            var key = new PianoKey(midiNote, isBlack);
            key.OnClicked += OnKeyClicked;
            
            pianoKeys.Add(key);
            
            if (isBlack)
                blackKeys.Add(key);
            else
                whiteKeys.Add(key);
        }

        foreach (var whiteKey in whiteKeys)
        {
            keyboardContainer.Add(whiteKey);
        }

        foreach (var blackKey in blackKeys)
        {
            keyboardContainer.Add(blackKey);
            PositionBlackKey(blackKey);
        }

        UpdateKeyStates();
    }

    void PositionBlackKey(PianoKey blackKey)
    {
        int midiNote = blackKey.MidiNote;
        int startNote = GetBaseNoteNumber();
        int semitone = (midiNote - startNote) % 12;
        
        int whiteKeyIndex = GetWhiteKeyIndex(midiNote - startNote);
        float whiteKeyWidth = 100f / GetWhiteKeyCount();
        
        blackKey.style.position = Position.Absolute;
        blackKey.style.left = Length.Percent(whiteKeyWidth * whiteKeyIndex + whiteKeyWidth * 0.7f);
        blackKey.style.width = Length.Percent(whiteKeyWidth * 0.6f);
        blackKey.style.height = Length.Percent(60);
        blackKey.style.top = 0;
    }

    int GetWhiteKeyIndex(int semitoneFromStart)
    {
        int whiteKeyCount = 0;
        for (int i = 0; i < semitoneFromStart; i++)
        {
            if (!blackKeyPattern[i % 12])
                whiteKeyCount++;
        }
        return whiteKeyCount;
    }

    int GetWhiteKeyCount()
    {
        int count = 0;
        for (int i = 0; i < TOTAL_SEMITONES; i++)
        {
            if (!blackKeyPattern[i % 12])
                count++;
        }
        return count;
    }

    void OnKeyClicked(int midiNote)
    {
        ToggleNote(midiNote);
    }

    void ToggleNote(int midiNote)
    {
        bool wasPressed = activeNotes.Contains(midiNote);
        
        if (wasPressed)
        {
            activeNotes.Remove(midiNote);
            RemoveFromQueue(midiNote);
        }
        else
        {
            if (activeNotes.Count >= MAX_NOTES)
            {
                int oldestNote = noteOrder.Dequeue();
                activeNotes.Remove(oldestNote);
            }
            
            activeNotes.Add(midiNote);
            noteOrder.Enqueue(midiNote);
        }

        UpdateKeyStates();
        OnNoteToggled?.Invoke(midiNote, !wasPressed);
        OnChordChanged?.Invoke(GetCurrentChord());
    }

    void RemoveFromQueue(int midiNote)
    {
        var tempQueue = new Queue<int>();
        while (noteOrder.Count > 0)
        {
            int note = noteOrder.Dequeue();
            if (note != midiNote)
                tempQueue.Enqueue(note);
        }
        noteOrder = tempQueue;
    }

    void UpdateKeyStates()
    {
        foreach (var key in pianoKeys)
        {
            key.SetPressed(activeNotes.Contains(key.MidiNote));
        }
    }

    void ShiftOctave(int direction)
    {
        int newOctave = baseOctave + direction;
        if (newOctave < 0 || newOctave > 7) return;

        baseOctave = newOctave;
        GenerateKeys();
        UpdateShiftButtons();
        OnOctaveChanged?.Invoke(baseOctave);
    }

    void UpdateShiftButtons()
    {
        leftShiftButton.SetEnabled(baseOctave > 0);
        rightShiftButton.SetEnabled(baseOctave < 7);
    }

    int GetBaseNoteNumber()
    {
        return baseOctave * 12 + 12;
    }

    public int[] GetCurrentChord()
    {
        return activeNotes.OrderBy(n => n).ToArray();
    }

    public void ClearChord()
    {
        activeNotes.Clear();
        noteOrder.Clear();
        UpdateKeyStates();
        OnChordChanged?.Invoke(GetCurrentChord());
    }

    public int CurrentBaseOctave => baseOctave;
}

} // namespace E2Controls