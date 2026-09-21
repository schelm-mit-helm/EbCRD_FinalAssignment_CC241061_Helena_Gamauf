using UnityEngine;
using TMPro;

// Attach to a UI object with a TextMeshProUGUI component (or assign one
// elsewhere in the inspector), then drop it anywhere in the scene.
[RequireComponent(typeof(TextMeshProUGUI))]
public class LevelDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private string format = "Level: {0}";

    private void Reset()
    {
        label = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (label == null) label = GetComponent<TextMeshProUGUI>();

        // Show the current value immediately, then keep it in sync.
        UpdateLabel(LevelService.Instance.Level);
        LevelService.Instance.OnLevelChanged += UpdateLabel;
    }

    private void OnDisable()
    {
        if (LevelService.Instance != null)
            LevelService.Instance.OnLevelChanged -= UpdateLabel;
    }

    private void UpdateLabel(int level)
    {
        label.text = string.Format(format, level);
    }
}