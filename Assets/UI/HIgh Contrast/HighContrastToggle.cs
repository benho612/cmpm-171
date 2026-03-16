using UnityEngine;
using UnityEngine.UI; // Needed to control the Toggle component
using UnityEngine.Rendering.Universal;

public class HighContrastToggle : MonoBehaviour
{
    [Header("URP Settings")]
    public UniversalRendererData rendererData;
    public int[] featureIndices;

    [Header("UI Reference")]
    public Toggle highContrastToggle; // Drag your Toggle here in the inspector

    // Static variable persists across scene changes
    public static bool IsHighContrastEnabled = false;

    private void Start()
    {
        // 1. Apply the saved state to the URP renderer features
        ApplyHighContrast(IsHighContrastEnabled);

        // 2. Sync the UI Toggle checkmark so it doesn't look "Off" when it's "On"
        if (highContrastToggle != null)
        {
            highContrastToggle.isOn = IsHighContrastEnabled;
        }
    }

    public void SetHighContrastMode(bool isOn)
    {
        IsHighContrastEnabled = isOn;
        ApplyHighContrast(isOn);
    }

    private void ApplyHighContrast(bool isOn)
    {
        if (rendererData == null) return;

        foreach (int index in featureIndices)
        {
            if (rendererData.rendererFeatures.Count > index)
            {
                rendererData.rendererFeatures[index].SetActive(isOn);
            }
        }
    }
}