using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToggleSwitch : MonoBehaviour
{
    [Header("Button Components")]
    public Button onButton;       // The ON button
    public Button offButton;      // The OFF button
    
    [Header("Visual Settings")]
    public Color activeColor = Color.white;     // Color when button is selected
    public Color inactiveColor = Color.gray;    // Color when button is not selected
    public Color textColor = Color.black;       // Always black text color
    
    [Header("Text Components (Optional)")]
    public TextMeshProUGUI onText;      // Text component of ON button (optional)
    public TextMeshProUGUI offText;     // Text component of OFF button (optional)
    
    [Header("Image Components (Optional)")]
    public Image onImage;       // Image component of ON button (optional)
    public Image offImage;      // Image component of OFF button (optional)
    
    [Header("Settings")]
    public bool isOn = true;            // Current state (true = ON, false = OFF)
    public bool useImageColor = true;   // Change image/background color only
    
    [Header("Animation (Optional)")]
    public bool animateTransition = true;
    public float animationSpeed = 5f;
    
    // Events
    public System.Action<bool> OnStateChanged; // Event when state changes (true = ON, false = OFF)
    
    // Animation variables
    private bool isAnimating = false;
    private float animationTimer = 0f;
    private Color onStartColor, onTargetColor;
    private Color offStartColor, offTargetColor;
    
    void Start()
    {
        // Setup button listeners
        if (onButton != null)
        {
            onButton.onClick.RemoveAllListeners(); // Clear existing listeners
            onButton.onClick.AddListener(() => SetState(true));
            
            // Ensure button stays interactable
            onButton.interactable = true;
        }
        
        if (offButton != null)
        {
            offButton.onClick.RemoveAllListeners(); // Clear existing listeners
            offButton.onClick.AddListener(() => SetState(false));
            
            // Ensure button stays interactable
            offButton.interactable = true;
        }
        
        // Get components automatically if not assigned
        AutoAssignComponents();
        
        // Set text to always be black
        SetTextColor();
        
        // Apply initial state
        UpdateVisualState();
    }
    
    void SetTextColor()
    {
        // Always set text to black
        if (onText != null) onText.color = textColor;
        if (offText != null) offText.color = textColor;
    }
    
    void AutoAssignComponents()
    {
        // Auto-assign text components if not set
        if (onText == null && onButton != null)
            onText = onButton.GetComponentInChildren<TextMeshProUGUI>();
        
        if (offText == null && offButton != null)
            offText = offButton.GetComponentInChildren<TextMeshProUGUI>();
        
        // Auto-assign image components if not set
        if (onImage == null && onButton != null)
            onImage = onButton.GetComponent<Image>();
        
        if (offImage == null && offButton != null)
            offImage = offButton.GetComponent<Image>();
    }
    
    void Update()
    {
        // Handle animation
        if (animateTransition && isAnimating)
        {
            HandleAnimation();
        }
        
        // Keep text always black (safety check)
        SetTextColor();
        
        // Safety check: ensure buttons stay interactable
        if (onButton != null && !onButton.interactable) onButton.interactable = true;
        if (offButton != null && !offButton.interactable) offButton.interactable = true;
    }
    
    public void SetState(bool newState)
    {
        
        if (isOn == newState) return; // No change needed
        
        isOn = newState;
        
        // Ensure both buttons remain interactable
        if (onButton != null) 
        {
            onButton.interactable = true;
        }
        if (offButton != null) 
        {
            offButton.interactable = true;
        }
        
        UpdateVisualState();
        TriggerStateChanged();
    }
    
    public void ToggleState()
    {
        SetState(!isOn);
    }
    
    void UpdateVisualState()
    {
        if (animateTransition && Application.isPlaying)
        {
            StartAnimation();
        }
        else
        {
            ApplyColors();
        }
    }
    
    void StartAnimation()
    {
        // Store current colors as start colors (only for images)
        if (useImageColor)
        {
            if (onImage != null) onStartColor = onImage.color;
            if (offImage != null) offStartColor = offImage.color;
        }
        
        // Set target colors
        if (isOn)
        {
            onTargetColor = activeColor;
            offTargetColor = inactiveColor;
        }
        else
        {
            onTargetColor = inactiveColor;
            offTargetColor = activeColor;
        }
        
        isAnimating = true;
        animationTimer = 0f;
    }
    
    void HandleAnimation()
    {
        animationTimer += Time.deltaTime * animationSpeed;
        float progress = Mathf.Clamp01(animationTimer);
        
        // Interpolate colors
        Color currentOnColor = Color.Lerp(onStartColor, onTargetColor, progress);
        Color currentOffColor = Color.Lerp(offStartColor, offTargetColor, progress);
        
        // Apply interpolated colors
        ApplyColors(currentOnColor, currentOffColor);
        
        // Check if animation is complete
        if (progress >= 1f)
        {
            isAnimating = false;
            ApplyColors(); // Ensure final colors are exact
        }
    }
    
    void ApplyColors()
    {
        if (isOn)
        {
            ApplyColors(activeColor, inactiveColor);
        }
        else
        {
            ApplyColors(inactiveColor, activeColor);
        }
    }
    
    void ApplyColors(Color onColor, Color offColor)
    {
        // Apply to image components only
        if (useImageColor)
        {
            if (onImage != null) 
            {
                onImage.color = onColor;
                // Ensure the button component stays interactable
                if (onButton != null) onButton.interactable = true;
            }
            if (offImage != null) 
            {
                offImage.color = offColor;
                // Ensure the button component stays interactable
                if (offButton != null) offButton.interactable = true;
            }
        }
        
        // Always keep text black
        SetTextColor();
    }
    
    void TriggerStateChanged()
    {
        OnStateChanged?.Invoke(isOn);
    }
    
    // Public methods for external access
    public bool IsOn()
    {
        return isOn;
    }
    
    public bool IsOff()
    {
        return !isOn;
    }
    
    public string GetCurrentState()
    {
        return isOn ? "ON" : "OFF";
    }
    
    // Methods to set colors at runtime
    public void SetActiveColor(Color color)
    {
        activeColor = color;
        UpdateVisualState();
    }
    
    public void SetInactiveColor(Color color)
    {
        inactiveColor = color;
        UpdateVisualState();
    }
    
    public void SetColors(Color active, Color inactive)
    {
        activeColor = active;
        inactiveColor = inactive;
        UpdateVisualState();
    }
    
    // Method to instantly set state without animation
    public void SetStateInstant(bool newState)
    {
        isOn = newState;
        ApplyColors();
        TriggerStateChanged();
    }
}