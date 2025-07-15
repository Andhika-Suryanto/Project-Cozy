using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class MusicCategory
{
    public string categoryName;           // e.g., "Menu", "Gameplay", "Victory"
    public List<AudioClip> musicTracks;   // List of music for this category
    public bool shuffleMode = false;      // Shuffle tracks in this category
    public bool loopCategory = true;      // Loop through the category when it ends
}

public class MusicManager : MonoBehaviour 
{
    [Header("Audio Components")]
    public AudioSource musicSource;       // Drag your Audio Source here
    
    [Header("Music Categories")]
    public List<MusicCategory> musicCategories = new List<MusicCategory>();
    
    [Header("Settings")]
    public float defaultVolume = 0.7f;
    
    [Header("Current Playback")]
    public string currentCategory = "";
    public int currentTrackIndex = 0;
    
    // Tracking
    private MusicCategory activeCategory;
    private List<int> shuffledIndices = new List<int>();
    private int shufflePosition = 0;
    
    void Start() 
    {
        // Validate audio source
        if (musicSource == null)
        {
            Debug.LogError("Please assign an Audio Source in the inspector!");
            return;
        }
        
        // Configure audio source
        ConfigureAudioSource(musicSource);
        
        // Play first category if available
        if (musicCategories.Count > 0)
        {
            PlayCategory(musicCategories[0].categoryName);
        }
    }
    
    void ConfigureAudioSource(AudioSource source)
    {
        source.loop = false;        // We handle looping manually
        source.playOnAwake = false;
        source.volume = defaultVolume;
        source.spatialBlend = 0f;   // 2D sound (not 3D positioned)
    }
    
    public void PlayCategory(string categoryName)
    {
        MusicCategory category = FindCategory(categoryName);
        if (category == null || category.musicTracks.Count == 0) 
        {
            Debug.LogWarning($"Music category '{categoryName}' not found or empty!");
            return;
        }
        
        activeCategory = category;
        currentCategory = categoryName;
        currentTrackIndex = 0;
        
        // Setup shuffle if needed
        if (category.shuffleMode)
        {
            SetupShuffle();
        }
        
        // Play first track
        PlayCurrentTrack();
    }
    
    public void PlaySpecificTrack(string categoryName, int trackIndex)
    {
        MusicCategory category = FindCategory(categoryName);
        if (category == null || trackIndex >= category.musicTracks.Count) 
        {
            Debug.LogWarning($"Track {trackIndex} in category '{categoryName}' not found!");
            return;
        }
        
        activeCategory = category;
        currentCategory = categoryName;
        currentTrackIndex = trackIndex;
        
        PlayCurrentTrack();
    }
    
    void PlayCurrentTrack()
    {
        if (activeCategory == null || activeCategory.musicTracks.Count == 0) return;
        
        int playIndex = activeCategory.shuffleMode ? shuffledIndices[shufflePosition] : currentTrackIndex;
        AudioClip trackToPlay = activeCategory.musicTracks[playIndex];
        
        if (trackToPlay == null) 
        {
            Debug.LogWarning("Music track is null!");
            return;
        }
        
        Debug.Log($"Playing: {trackToPlay.name} from {currentCategory}");
        
        // Stop current music and play new track
        musicSource.Stop();
        musicSource.clip = trackToPlay;
        musicSource.volume = defaultVolume;
        musicSource.Play();
        
        // Start checking for track end
        StartCoroutine(CheckTrackEnd());
    }
    
    IEnumerator CheckTrackEnd()
    {
        // Wait until track is nearly finished
        while (musicSource.isPlaying && musicSource.time < musicSource.clip.length - 0.1f)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Track ended, play next
        if (activeCategory != null)
        {
            NextTrack();
        }
    }
    
    public void NextTrack()
    {
        if (activeCategory == null || activeCategory.musicTracks.Count == 0) return;
        
        if (activeCategory.shuffleMode)
        {
            shufflePosition++;
            if (shufflePosition >= shuffledIndices.Count)
            {
                if (activeCategory.loopCategory)
                {
                    SetupShuffle(); // Reshuffle and start over
                }
                else
                {
                    StopMusic();
                    return;
                }
            }
        }
        else
        {
            currentTrackIndex++;
            if (currentTrackIndex >= activeCategory.musicTracks.Count)
            {
                if (activeCategory.loopCategory)
                {
                    currentTrackIndex = 0; // Loop back to first track
                }
                else
                {
                    StopMusic();
                    return;
                }
            }
        }
        
        PlayCurrentTrack();
    }
    
    public void PreviousTrack()
    {
        if (activeCategory == null || activeCategory.musicTracks.Count == 0) return;
        
        if (activeCategory.shuffleMode)
        {
            shufflePosition = Mathf.Max(0, shufflePosition - 1);
        }
        else
        {
            currentTrackIndex--;
            if (currentTrackIndex < 0)
            {
                currentTrackIndex = activeCategory.loopCategory ? activeCategory.musicTracks.Count - 1 : 0;
            }
        }
        
        PlayCurrentTrack();
    }
    
    void SetupShuffle()
    {
        shuffledIndices.Clear();
        for (int i = 0; i < activeCategory.musicTracks.Count; i++)
        {
            shuffledIndices.Add(i);
        }
        
        // Fisher-Yates shuffle
        for (int i = shuffledIndices.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = shuffledIndices[i];
            shuffledIndices[i] = shuffledIndices[randomIndex];
            shuffledIndices[randomIndex] = temp;
        }
        
        shufflePosition = 0;
    }
    
    MusicCategory FindCategory(string categoryName)
    {
        foreach (MusicCategory category in musicCategories)
        {
            if (category.categoryName == categoryName)
                return category;
        }
        return null;
    }
    
    // Public control methods
    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
        StopAllCoroutines();
    }
    
    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Pause();
    }
    
    public void ResumeMusic()
    {
        if (musicSource != null)
            musicSource.UnPause();
    }
    
    public void SetVolume(float volume)
    {
        defaultVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
            musicSource.volume = defaultVolume;
    }
    
    // Get current track info
    public string GetCurrentTrackName()
    {
        if (activeCategory != null && currentTrackIndex < activeCategory.musicTracks.Count)
        {
            int playIndex = activeCategory.shuffleMode ? shuffledIndices[shufflePosition] : currentTrackIndex;
            return activeCategory.musicTracks[playIndex].name;
        }
        return "No track playing";
    }
    
    public float GetCurrentTrackProgress()
    {
        if (musicSource != null && musicSource.clip != null && musicSource.isPlaying)
        {
            return musicSource.time / musicSource.clip.length;
        }
        return 0f;
    }
    
    // Add/Remove tracks at runtime
    public void AddTrackToCategory(string categoryName, AudioClip newTrack)
    {
        MusicCategory category = FindCategory(categoryName);
        if (category != null && newTrack != null)
        {
            category.musicTracks.Add(newTrack);
        }
    }
    
    public void CreateNewCategory(string categoryName, bool shuffle = false, bool loop = true)
    {
        MusicCategory newCategory = new MusicCategory();
        newCategory.categoryName = categoryName;
        newCategory.musicTracks = new List<AudioClip>();
        newCategory.shuffleMode = shuffle;
        newCategory.loopCategory = loop;
        
        musicCategories.Add(newCategory);
    }
}