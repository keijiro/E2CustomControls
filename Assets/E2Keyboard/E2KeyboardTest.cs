using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace E2Controls {

public sealed class E2KeyboardTest : MonoBehaviour
{
    static readonly string[] NoteNames =
      { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    string GetNoteName(int note)
      => $"{NoteNames[note % 12]}{note / 12 - 1}";

    Label _label;

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        var chordKeyboard = new E2ChordKeyboard();
        chordKeyboard.RegisterCallback<ChangeEvent<(int, int, int, int)>>(OnChordChanged);
        root.Add(chordKeyboard);

        _label = new Label();
        root.Add(_label);
    }

    void OnChordChanged(ChangeEvent<(int, int, int, int)> evt)
    {
        var (note1, note2, note3, note4) = evt.newValue;
        var notes = new[]{ note1, note2, note3, note4 }.Where(n => n >= 0);
        _label.text = string.Join(", ", notes.Select(GetNoteName));
    }
}

} // namespace E2Controls