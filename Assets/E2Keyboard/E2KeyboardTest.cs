using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace E2Controls {

public sealed class E2KeyboardTest : MonoBehaviour
{
    [SerializeField] UIDocument uiDocument;
    
    ChordKeyboard chordKeyboard;
    Label chordDisplay;

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
        
        chordKeyboard.RegisterCallback<ChangeEvent<(int, int, int, int)>>(OnChordChanged);
        
        chordDisplay = new Label("Current Chord: None")
        {
            name = "chord-display"
        };
        chordDisplay.style.fontSize = 16;
        chordDisplay.style.marginBottom = 10;
        
        var clearButton = new Button(() => chordKeyboard.ClearChord())
        {
            text = "Clear Chord"
        };
        clearButton.style.marginTop = 10;
        clearButton.style.width = 100;
        
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

    void OnChordChanged(ChangeEvent<(int, int, int, int)> evt)
    {
        var (note1, note2, note3, note4) = evt.newValue;
        var activeNotes = new[] { note1, note2, note3, note4 }.Where(n => n != -1).ToArray();
        
        if (activeNotes.Length == 0)
        {
            chordDisplay.text = "Current Chord: None";
        }
        else
        {
            string chordText = "Current Chord: ";
            for (int i = 0; i < activeNotes.Length; i++)
            {
                chordText += GetNoteName(activeNotes[i]);
                if (i < activeNotes.Length - 1)
                    chordText += ", ";
            }
            chordDisplay.text = chordText;
        }
        
        string midiText = activeNotes.Length > 0 ? 
            $" (MIDI: {string.Join(", ", activeNotes)})" : "";
        chordDisplay.text += midiText;
        
        Debug.Log($"Chord changed: {string.Join(", ", activeNotes)}");
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