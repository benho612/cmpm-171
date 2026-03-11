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
        // Singleton pattern: Ensures only one AudioManager exists in the scene
        if (Instance == null)
        {
            Instance = this;
            // Keeps the AudioManager alive when loading new scenes, we can tst this when/if we get the level transition done for the boss level.
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
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
        }
    }

    // The method you will call from other scripts
    public void Play(string name)
    {
        // Finds the sound in the array that matches the requested name
        Sound s = Array.Find(sounds, sound => sound.name == name);

        if (s == null)
        {
            Debug.LogWarning("AudioManager: Sound '" + name + "' not found!");
            return;
        }

        // Using PlayOneShot instead of Play() ensures that rapid sounds 
        // overlap naturally instead of cutting each other off.
        s.source.PlayOneShot(s.clip);
    }

    // Use this for your continuous looping sounds (like walking/running)
    public void PlayLoop(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) return;

        // Checks if it's already playing so we don't restart the audio clip every single frame
        if (!s.source.isPlaying)
        {
            s.source.loop = true; // Tells the AudioSource to loop
            s.source.clip = s.clip;
            s.source.Play(); // Use standard Play() instead of PlayOneShot() so it doesn't spam the audio
        }
    }

    // Use this to stop a looping sound when the player stops moving
    public void Stop(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) return;

        s.source.Stop();
        s.source.loop = false; // Reset the loop flag just in case
    }
}