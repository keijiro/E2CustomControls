using System.Linq;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace E2Controls {

public sealed class E2KeyboardTest : MonoBehaviour
{
    [CreateProperty] public int Note1 { get; set; } = -1;
    [CreateProperty] public int Note2 { get; set; } = -1;
    [CreateProperty] public int Note3 { get; set; } = -1;
    [CreateProperty] public int Note4 { get; set; } = -1;

    [CreateProperty] public string Note1Label => GetNoteName(Note1);
    [CreateProperty] public string Note2Label => GetNoteName(Note2);
    [CreateProperty] public string Note3Label => GetNoteName(Note3);
    [CreateProperty] public string Note4Label => GetNoteName(Note4);

    static readonly string[] NoteNames =
      { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    static string GetNoteName(int note)
      => note >= 0 ? $"{NoteNames[note % 12]}{note / 12 - 1}" : "";

    void Start()
      => GetComponent<UIDocument>().rootVisualElement.Q("keyboard").dataSource = this;
}

} // namespace E2Controls