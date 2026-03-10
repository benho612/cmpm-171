using UnityEngine;
using UnityEngine.Rendering.Universal;

public class HighContrastToggle : MonoBehaviour
{
    [Header("URP Settings")]
    public UniversalRendererData rendererData;

    [Tooltip("List the index numbers for your Render Features (e.g., 1 for Player, 2 for Enemy)")]
    public int[] featureIndices;

    // This method will be called automatically by the UI Toggle
    public void SetHighContrastMode(bool isOn)
    {
        if (rendererData == null) return;

        // Loop through every index you provided and turn them on/off
        foreach (int index in featureIndices)
        {
            if (rendererData.rendererFeatures.Count > index)
            {
                rendererData.rendererFeatures[index].SetActive(isOn);
            }
        }
    }
}