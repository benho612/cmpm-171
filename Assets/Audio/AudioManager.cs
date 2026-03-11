using UnityEngine;
using System;

public class AudioManager : MonoBehaviour
{
    // The Singleton instance
    public static AudioManager Instance;

    // Array to hold all our custom Sound objects
    public Sound[] sounds;

    void Awake()
    {
        // Singleton pattern: Ensure only one AudioManager exists in the scene
        if (Instance == null)
        {
            Instance = this;
            // Optional: Keeps the AudioManager alive when loading new scenes
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Loop through the array and create an AudioSource for each sound
        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip; // Reverted to s.clip
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
        }
    }

    // The method you will call from other scripts
    public void Play(string name)
    {
        // Find the sound in the array that matches the requested name
        Sound s = Array.Find(sounds, sound => sound.name == name);

        if (s == null)
        {
            Debug.LogWarning("AudioManager: Sound '" + name + "' not found!");
            return;
        }

        // Using PlayOneShot instead of Play() ensures that rapid sounds 
        // overlap naturally instead of cutting each other off.
        s.source.PlayOneShot(s.clip); // Reverted to s.clip
    }
}