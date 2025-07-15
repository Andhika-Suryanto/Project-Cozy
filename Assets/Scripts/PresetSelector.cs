using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class Preset
{
    public string presetName;
    public string displayText; // Optional: different text to show than the name
    
    // Constructor for easy setup
    public Preset(string name)
    {
        presetName = name;
        displayText = name;
    }
    
    public Preset(string name, string display)
    {
        presetName = name;
        displayText = display;
    }
}

public class PresetSelector : MonoBehaviour, IPointerDownHandler
{
    [Header("UI Components")]
    public Button leftArrow;           // Left arrow button
    public Button rightArrow;          // Right arrow button
    public TextMeshProUGUI presetText; // Text showing current preset
    
    [Header("Preset Settings")]
    public List<Preset> presets = new List<Preset>(); // List of all presets
    public int currentPresetIndex = 0;                 // Currently selected preset index
    public bool loopAround = true;                     // Loop from last to first preset
    
    [Header("Visual Settings")]
    public bool animateTransition = true;  // Smooth text transition
    public float animationSpeed = 5f;      // Speed of text fade
    public Color normalTextColor = Color.white;
    public Color transitionColor = Color.gray;
    
    [Header("Input Settings")]
    public bool useKeyboardInput = true;   // Allow arrow key input
    public KeyCode leftKey = KeyCode.LeftArrow;
    public KeyCode rightKey = KeyCode.RightArrow;
    
    // Events for when preset changes
    public System.Action<int, string> OnPresetChanged; // (index, presetName)
    
    private bool isTransitioning = false;
    private float transitionTimer = 0f;
    private string targetPresetText = "";
    
    void Start()
    {
        // Setup button listeners
        if (leftArrow != null)
            leftArrow.onClick.AddListener(PreviousPreset);
        
        if (rightArrow != null)
            rightArrow.onClick.AddListener(NextPreset);
        
        // Initialize with default presets if none are set
        if (presets.Count == 0)
        {
            SetupDefaultPresets();
        }
        
        // Clamp current index
        currentPresetIndex = Mathf.Clamp(currentPresetIndex, 0, presets.Count - 1);
        
        // Update display
        UpdatePresetDisplay();
    }
    
    void SetupDefaultPresets()
    {
        presets.Add(new Preset("Low", "Low Quality"));
        presets.Add(new Preset("Medium", "Medium Quality"));
        presets.Add(new Preset("High", "High Quality"));
        presets.Add(new Preset("Ultra", "Ultra Quality"));
        presets.Add(new Preset("Custom", "Custom Settings"));
    }
    
    void Update()
    {
        // Handle keyboard input
        if (useKeyboardInput && !isTransitioning)
        {
            if (Input.GetKeyDown(leftKey))
                PreviousPreset();
            
            if (Input.GetKeyDown(rightKey))
                NextPreset();
        }
        
        // Handle text animation
        if (animateTransition && isTransitioning)
        {
            HandleTextTransition();
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        // Optional: Click on the text area to cycle through presets
        if (eventData.button == PointerEventData.InputButton.Left)
            NextPreset();
        else if (eventData.button == PointerEventData.InputButton.Right)
            PreviousPreset();
    }
    
    public void NextPreset()
    {
        if (presets.Count == 0) return;
        
        currentPresetIndex++;
        
        if (currentPresetIndex >= presets.Count)
        {
            if (loopAround)
                currentPresetIndex = 0;
            else
                currentPresetIndex = presets.Count - 1;
        }
        
        UpdatePresetDisplay();
        TriggerPresetChanged();
    }
    
    public void PreviousPreset()
    {
        if (presets.Count == 0) return;
        
        currentPresetIndex--;
        
        if (currentPresetIndex < 0)
        {
            if (loopAround)
                currentPresetIndex = presets.Count - 1;
            else
                currentPresetIndex = 0;
        }
        
        UpdatePresetDisplay();
        TriggerPresetChanged();
    }
    
    void UpdatePresetDisplay()
    {
        if (presetText == null || presets.Count == 0) return;
        
        string displayText = presets[currentPresetIndex].displayText;
        
        if (animateTransition && Application.isPlaying)
        {
            StartTextTransition(displayText);
        }
        else
        {
            presetText.text = displayText;
            presetText.color = normalTextColor;
        }
    }
    
    void StartTextTransition(string newText)
    {
        targetPresetText = newText;
        isTransitioning = true;
        transitionTimer = 0f;
    }
    
    void HandleTextTransition()
    {
        transitionTimer += Time.deltaTime * animationSpeed;
        
        if (transitionTimer <= 1f)
        {
            // Fade out current text
            presetText.color = Color.Lerp(normalTextColor, transitionColor, transitionTimer);
        }
        else if (transitionTimer <= 2f)
        {
            // Change text and fade in
            if (presetText.text != targetPresetText)
                presetText.text = targetPresetText;
            
            float fadeInProgress = transitionTimer - 1f;
            presetText.color = Color.Lerp(transitionColor, normalTextColor, fadeInProgress);
        }
        else
        {
            // Transition complete
            presetText.text = targetPresetText;
            presetText.color = normalTextColor;
            isTransitioning = false;
        }
    }
    
    void TriggerPresetChanged()
    {
        if (OnPresetChanged != null && presets.Count > 0)
        {
            OnPresetChanged.Invoke(currentPresetIndex, presets[currentPresetIndex].presetName);
        }
    }
    
    // Public methods for external control
    public void SetPreset(int index)
    {
        if (index >= 0 && index < presets.Count)
        {
            currentPresetIndex = index;
            UpdatePresetDisplay();
            TriggerPresetChanged();
        }
    }
    
    public void SetPreset(string presetName)
    {
        for (int i = 0; i < presets.Count; i++)
        {
            if (presets[i].presetName == presetName)
            {
                SetPreset(i);
                break;
            }
        }
    }
    
    public string GetCurrentPresetName()
    {
        if (presets.Count > 0 && currentPresetIndex >= 0 && currentPresetIndex < presets.Count)
            return presets[currentPresetIndex].presetName;
        
        return "";
    }
    
    public int GetCurrentPresetIndex()
    {
        return currentPresetIndex;
    }
    
    public void AddPreset(string name, string displayText = "")
    {
        if (string.IsNullOrEmpty(displayText))
            displayText = name;
        
        presets.Add(new Preset(name, displayText));
    }
    
    public void RemovePreset(int index)
    {
        if (index >= 0 && index < presets.Count)
        {
            presets.RemoveAt(index);
            
            // Adjust current index if needed
            if (currentPresetIndex >= presets.Count)
                currentPresetIndex = Mathf.Max(0, presets.Count - 1);
            
            UpdatePresetDisplay();
        }
    }
    
    public void ClearPresets()
    {
        presets.Clear();
        currentPresetIndex = 0;
        if (presetText != null)
            presetText.text = "";
    }
    
    // Method to get all preset names
    public string[] GetAllPresetNames()
    {
        string[] names = new string[presets.Count];
        for (int i = 0; i < presets.Count; i++)
        {
            names[i] = presets[i].presetName;
        }
        return names;
    }
}