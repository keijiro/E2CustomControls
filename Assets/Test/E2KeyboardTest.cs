using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace E2Controls {

public sealed class E2KeyboardTest : MonoBehaviour
{
    void Start()
      => GetComponent<UIDocument>().rootVisualElement.Q<E2ChordKeyboard>()
           .RegisterCallback<ChangeEvent<(int, int, int, int)>>(OnChordChanged);

    static readonly string[] NoteNames =
      { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    static string GetNoteName(int note)
      => $"{NoteNames[note % 12]}{note / 12 - 1}";

    void OnChordChanged(ChangeEvent<(int, int, int, int)> evt)
    {
        var (note1, note2, note3, note4) = evt.newValue;
        var notes = new[]{ note1, note2, note3, note4 }.Where(n => n >= 0);
        GetComponent<UIDocument>().rootVisualElement.Q<Label>("chord-label")
          .text = string.Join(", ", notes.Select(GetNoteName));
    }
}

} // namespace E2Controls