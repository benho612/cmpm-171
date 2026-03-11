using UnityEngine;

// This tag makes the custom class visible in the Unity Inspector
[System.Serializable]
public class Sound
{
    // Keep these generic! You will type "Swoosh" in the Inspector later.
    public string name;
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(0.1f, 3f)]
    public float pitch = 1f;

    // We hide this in the inspector because the AudioManager will assign it via code
    [HideInInspector]
    public AudioSource source;
}