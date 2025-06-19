using UnityEngine;
using UnityEngine.UIElements;
using Unity.Properties;

public sealed class E2KnobTest : MonoBehaviour
{
    [SerializeField] string[] _typeNames = null;
    [CreateProperty] public int TypeValue { get; set; }
    [CreateProperty] public string TypeName => _typeNames[TypeValue];

    void Start()
      => GetComponent<UIDocument>().rootVisualElement.dataSource = this;
}
