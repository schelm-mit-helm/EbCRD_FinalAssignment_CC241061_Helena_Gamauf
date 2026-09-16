using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "RumpledCode/Item", order = 1)]
public class Item : ScriptableObject
{
    public string id;
    public string description;
    public Sprite icon;
    public GameObject prefab;

    [Tooltip("Multiplier applied on top of auto-scaled held/dropped size.")]
    public float displayScale = 1f;

    public float GetDisplayScale() => displayScale <= 0f ? 1f : displayScale;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (displayScale <= 0f)
        {
            displayScale = 1f;
        }
    }
#endif
}