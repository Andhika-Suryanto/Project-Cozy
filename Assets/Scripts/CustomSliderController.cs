using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class CustomSliderController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Slider Components")]
    public RectTransform greyBar;        // The background grey bar
    public RectTransform whiteBar;       // The white fill bar that expands/shrinks
    public TextMeshProUGUI valueText;    // The TMP text showing current value
    
    [Header("Slider Settings")]
    public float minValue = 60f;         // Minimum value (left side)
    public float maxValue = 120f;        // Maximum value (right side)
    public float currentValue = 90f;     // Current value
    public bool useIntegerValues = false; // If true, rounds to whole numbers
    public string textPrefix = "FOV: ";  // Text prefix (e.g., "FOV: ", "Volume: ")
    public string textSuffix = "";       // Text suffix (e.g., "%", "°")
    
    [Header("Visual Settings")]
    public bool animateTransition = true; // Smooth transition when clicking
    public float animationSpeed = 10f;    // Speed of the animation
    
    private bool isDragging = false;
    private float targetValue;
    
    void Start()
    {
        // Initialize the slider
        targetValue = currentValue;
        currentValue = Mathf.Clamp(currentValue, minValue, maxValue);
        
        // Setup white bar initial position and size
        SetupWhiteBar();
        UpdateSliderVisual();
        UpdateText();
    }
    
    void SetupWhiteBar()
    {
        if (whiteBar == null || greyBar == null) return;
        
        // Set white bar anchors to left edge
        whiteBar.anchorMin = new Vector2(0f, 0f);
        whiteBar.anchorMax = new Vector2(0f, 1f);
        whiteBar.pivot = new Vector2(0f, 0.5f);
        
        // Position at left edge
        whiteBar.anchoredPosition = new Vector2(0f, 0f);
        
        // Restore the original height by setting sizeDelta.y
        Vector2 sizeDelta = whiteBar.sizeDelta;
        sizeDelta.y = 0f; // Set to 0 because anchors handle the height stretching
        whiteBar.sizeDelta = sizeDelta;
    }
    
    void Update()
    {
        // Smooth animation if enabled
        if (animateTransition && !isDragging)
        {
            if (Mathf.Abs(currentValue - targetValue) > 0.1f)
            {
                currentValue = Mathf.Lerp(currentValue, targetValue, Time.deltaTime * animationSpeed);
                UpdateSliderVisual();
                UpdateText();
            }
            else
            {
                currentValue = targetValue;
            }
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        HandleInput(eventData);
        isDragging = true;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            HandleInput(eventData);
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }
    
    private void HandleInput(PointerEventData eventData)
    {
        if (greyBar == null) return;
        
        // Convert screen point to local point within the GREY BAR (the clickable area)
        Vector2 localPoint;
        bool validPoint = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            greyBar, 
            eventData.position, 
            eventData.pressEventCamera, 
            out localPoint);
        
        if (!validPoint) return;
        
        // Get the width of the grey bar
        float sliderWidth = greyBar.rect.width;
        
        // Calculate the position as a percentage (0 to 1)
        // localPoint.x ranges from -width/2 to +width/2, so we need to convert it to 0-1
        float percentage = (localPoint.x + sliderWidth * 0.5f) / sliderWidth;
        percentage = Mathf.Clamp01(percentage);
        
        // Convert percentage to value
        float newValue = Mathf.Lerp(minValue, maxValue, percentage);
        
        // Round to integer if needed
        if (useIntegerValues)
        {
            newValue = Mathf.Round(newValue);
        }
        
        // Set the target value
        targetValue = newValue;
        
        // If not animating, set immediately
        if (!animateTransition || isDragging)
        {
            currentValue = targetValue;
            UpdateSliderVisual();
            UpdateText();
        }
    }
    
    private void UpdateSliderVisual()
    {
        if (whiteBar == null || greyBar == null) return;
        
        // Calculate the fill percentage
        float fillPercentage = (currentValue - minValue) / (maxValue - minValue);
        fillPercentage = Mathf.Clamp01(fillPercentage);
        
        // Get the grey bar's width
        float greyBarWidth = greyBar.rect.width;
        
        // Calculate the new width for white bar
        float newWidth = greyBarWidth * fillPercentage;
        
        // Update ONLY the width, keep height unchanged
        Vector2 currentSize = whiteBar.sizeDelta;
        whiteBar.sizeDelta = new Vector2(newWidth, currentSize.y);
    }
    
    private void UpdateText()
    {
        if (valueText == null) return;
        
        // Format the value based on integer setting
        string valueString;
        if (useIntegerValues)
        {
            valueString = Mathf.RoundToInt(currentValue).ToString();
        }
        else
        {
            valueString = currentValue.ToString("F1"); // One decimal place
        }
        
        // Update the text with prefix and suffix
        valueText.text = textPrefix + valueString + textSuffix;
    }
    
    // Public method to set value from code
    public void SetValue(float value)
    {
        targetValue = Mathf.Clamp(value, minValue, maxValue);
        
        if (!animateTransition)
        {
            currentValue = targetValue;
            UpdateSliderVisual();
            UpdateText();
        }
    }
    
    // Public method to get current value
    public float GetValue()
    {
        return currentValue;
    }
    
    // Method to set min/max values
    public void SetRange(float min, float max)
    {
        minValue = min;
        maxValue = max;
        currentValue = Mathf.Clamp(currentValue, minValue, maxValue);
        targetValue = currentValue;
        UpdateSliderVisual();
        UpdateText();
    }
}