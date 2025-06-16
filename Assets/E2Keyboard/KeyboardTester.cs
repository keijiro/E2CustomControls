using UnityEngine;
using UnityEngine.UIElements;

namespace E2Controls {

public sealed class KeyboardTester : MonoBehaviour
{
    [SerializeField] UIDocument uiDocument;
    
    ChordKeyboard chordKeyboard;
    Label chordDisplay;
    Label octaveDisplay;

    void Start()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
            
        SetupUI();
    }

    void SetupUI()
    {
        var root = uiDocument.rootVisualElement;
        
        chordKeyboard = new ChordKeyboard();
        
        chordKeyboard.OnChordChanged += OnChordChanged;
        chordKeyboard.OnOctaveChanged += OnOctaveChanged;
        chordKeyboard.OnNoteToggled += OnNoteToggled;
        
        chordDisplay = new Label("Current Chord: None")
        {
            name = "chord-display"
        };
        chordDisplay.style.fontSize = 16;
        chordDisplay.style.marginBottom = 10;
        
        octaveDisplay = new Label($"Base Octave: C{chordKeyboard.CurrentBaseOctave}")
        {
            name = "octave-display"
        };
        octaveDisplay.style.fontSize = 14;
        octaveDisplay.style.marginBottom = 10;
        
        var clearButton = new Button(() => chordKeyboard.ClearChord())
        {
            text = "Clear Chord"
        };
        clearButton.style.marginTop = 10;
        clearButton.style.width = 100;
        
        root.Add(octaveDisplay);
        root.Add(chordDisplay);
        root.Add(chordKeyboard);
        root.Add(clearButton);
        
        var debugInfo = new Label("Click keys to build chords (max 4 notes). Use < > to shift octaves.")
        {
            name = "debug-info"
        };
        debugInfo.style.fontSize = 12;
        debugInfo.style.color = new Color(0.6f, 0.6f, 0.6f);
        debugInfo.style.marginTop = 10;
        root.Add(debugInfo);
    }

    void OnChordChanged(int[] midiNotes)
    {
        if (midiNotes.Length == 0)
        {
            chordDisplay.text = "Current Chord: None";
        }
        else
        {
            string chordText = "Current Chord: ";
            for (int i = 0; i < midiNotes.Length; i++)
            {
                chordText += GetNoteName(midiNotes[i]);
                if (i < midiNotes.Length - 1)
                    chordText += ", ";
            }
            chordDisplay.text = chordText;
        }
        
        string midiText = midiNotes.Length > 0 ? 
            $" (MIDI: {string.Join(", ", midiNotes)})" : "";
        chordDisplay.text += midiText;
        
        Debug.Log($"Chord changed: {string.Join(", ", midiNotes)}");
    }

    void OnOctaveChanged(int baseOctave)
    {
        octaveDisplay.text = $"Base Octave: C{baseOctave}";
        Debug.Log($"Octave changed to: C{baseOctave}");
    }

    void OnNoteToggled(int midiNote, bool isPressed)
    {
        string action = isPressed ? "pressed" : "released";
        Debug.Log($"Note {GetNoteName(midiNote)} (MIDI: {midiNote}) {action}");
    }

    string GetNoteName(int midiNote)
    {
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int noteIndex = midiNote % 12;
        int octave = midiNote / 12 - 1; // MIDI規格でC4=60
        return $"{noteNames[noteIndex]}{octave}";
    }
}

} // namespace E2Controls