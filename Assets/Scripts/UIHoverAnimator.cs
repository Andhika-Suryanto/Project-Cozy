using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

/// <summary>
/// Universal hover animation component that can be added to any UI element
/// Provides smooth scaling, color changes, and text size animations on hover
/// </summary>
public class UIHoverAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale Animation")]
    [Tooltip("Scale multiplier when hovering")]
    [Range(0.8f, 2f)]
    public float hoverScale = 1.1f;
    
    [Tooltip("Speed of scale animation")]
    [Range(1f, 20f)]
    public float animationSpeed = 10f;
    
    [Tooltip("Use smooth animation curve")]
    public bool useSmoothCurve = true;
    
    [Header("Color Animation")]
    [Tooltip("Enable color change on hover")]
    public bool animateColor = false;
    
    [Tooltip("Color when hovering (for Image or Text components)")]
    public Color hoverColor = Color.white;
    
    [Tooltip("Color animation speed")]
    [Range(1f, 20f)]
    public float colorAnimationSpeed = 5f;
    
    [Header("Text Animation")]
    [Tooltip("Enable text size animation")]
    public bool animateTextSize = false;
    
    [Tooltip("Text size multiplier on hover")]
    [Range(0.8f, 2f)]
    public float textHoverScale = 1.2f;
    
    [Tooltip("Target TextMeshPro component (auto-detected if null)")]
    public TextMeshProUGUI targetText;
    
    [Header("Advanced Settings")]
    [Tooltip("Only animate if button is interactable")]
    public bool respectInteractableState = true;
    
    [Tooltip("Disable hover animation")]
    public bool disableAnimation = false;
    
    [Tooltip("Custom target transform to animate (uses this transform if null)")]
    public Transform customTarget;
    
    // Private variables
    private Vector3 originalScale;
    private Color originalColor;
    private float originalTextSize;
    private bool isHovering = false;
    private bool isAnimating = false;
    
    // Component references
    private Button button;
    private Image image;
    private Text legacyText;
    private Coroutine scaleCoroutine;
    private Coroutine colorCoroutine;
    private Transform animationTarget;
    
    void Awake()
    {
        InitializeComponents();
        StoreOriginalValues();
    }
    
    void Start()
    {
        // Validate setup
        if (animationTarget == null)
        {
            Debug.LogWarning($"UIHoverAnimator on {gameObject.name}: No animation target found!");
        }
    }
    
    void InitializeComponents()
    {
        // Set animation target
        animationTarget = customTarget != null ? customTarget : transform;
        
        // Get component references
        button = GetComponent<Button>();
        image = GetComponent<Image>();
        legacyText = GetComponent<Text>();
        
        // Auto-detect TextMeshPro if not assigned
        if (targetText == null)
        {
            targetText = GetComponent<TextMeshProUGUI>();
            if (targetText == null)
            {
                targetText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }
    }
    
    void StoreOriginalValues()
    {
        // Store original scale
        originalScale = animationTarget.localScale;
        
        // Store original color
        if (animateColor)
        {
            if (image != null)
            {
                originalColor = image.color;
            }
            else if (legacyText != null)
            {
                originalColor = legacyText.color;
            }
            else if (targetText != null)
            {
                originalColor = targetText.color;
            }
        }
        
        // Store original text size
        if (animateTextSize && targetText != null)
        {
            originalTextSize = targetText.fontSize;
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ShouldAnimateHover())
        {
            StartHoverAnimation();
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (ShouldAnimateHover())
        {
            StartExitAnimation();
        }
    }
    
    bool ShouldAnimateHover()
    {
        // Check if animation is disabled
        if (disableAnimation) return false;
        
        // Check if button is interactable (if required)
        if (respectInteractableState && button != null && !button.interactable)
            return false;
        
        return true;
    }
    
    void StartHoverAnimation()
    {
        isHovering = true;
        
        // Start scale animation
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(AnimateScale(hoverScale));
        
        // Start color animation
        if (animateColor)
        {
            if (colorCoroutine != null) StopCoroutine(colorCoroutine);
            colorCoroutine = StartCoroutine(AnimateColor(hoverColor));
        }
        
        // Animate text size
        if (animateTextSize && targetText != null)
        {
            AnimateTextSize(originalTextSize * textHoverScale);
        }
    }
    
    void StartExitAnimation()
    {
        isHovering = false;
        
        // Return to original scale
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(AnimateScale(1f));
        
        // Return to original color
        if (animateColor)
        {
            if (colorCoroutine != null) StopCoroutine(colorCoroutine);
            colorCoroutine = StartCoroutine(AnimateColor(originalColor));
        }
        
        // Return text to original size
        if (animateTextSize && targetText != null)
        {
            AnimateTextSize(originalTextSize);
        }
    }
    
    IEnumerator AnimateScale(float targetScale)
    {
        isAnimating = true;
        
        Vector3 startScale = animationTarget.localScale;
        Vector3 endScale = originalScale * targetScale;
        float elapsed = 0f;
        float duration = 1f / animationSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time for UI
            float progress = elapsed / duration;
            
            // Apply animation curve
            if (useSmoothCurve)
            {
                progress = Mathf.SmoothStep(0f, 1f, progress);
            }
            
            animationTarget.localScale = Vector3.Lerp(startScale, endScale, progress);
            yield return null;
        }
        
        animationTarget.localScale = endScale;
        isAnimating = false;
    }
    
    IEnumerator AnimateColor(Color targetColor)
    {
        Color startColor = GetCurrentColor();
        float elapsed = 0f;
        float duration = 1f / colorAnimationSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            
            if (useSmoothCurve)
            {
                progress = Mathf.SmoothStep(0f, 1f, progress);
            }
            
            Color currentColor = Color.Lerp(startColor, targetColor, progress);
            SetCurrentColor(currentColor);
            
            yield return null;
        }
        
        SetCurrentColor(targetColor);
    }
    
    void AnimateTextSize(float targetSize)
    {
        if (targetText == null) return;
        
        // Immediate text size change (can be made smoother with coroutine if needed)
        targetText.fontSize = targetSize;
    }
    
    Color GetCurrentColor()
    {
        if (image != null) return image.color;
        if (legacyText != null) return legacyText.color;
        if (targetText != null) return targetText.color;
        return Color.white;
    }
    
    void SetCurrentColor(Color color)
    {
        if (image != null) image.color = color;
        if (legacyText != null) legacyText.color = color;
        if (targetText != null) targetText.color = color;
    }
    
    // Public methods for external control
    public void SetHoverScale(float scale)
    {
        hoverScale = scale;
    }
    
    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = speed;
    }
    
    public void EnableAnimation(bool enable)
    {
        disableAnimation = !enable;
    }
    
    public void ForceHoverState(bool hover)
    {
        if (hover)
            StartHoverAnimation();
        else
            StartExitAnimation();
    }
    
    // Reset to original values
    public void ResetToOriginal()
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        
        animationTarget.localScale = originalScale;
        SetCurrentColor(originalColor);
        
        if (targetText != null)
            targetText.fontSize = originalTextSize;
    }
    
    void OnDisable()
    {
        // Clean up coroutines
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        
        // Reset to original state
        if (animationTarget != null)
        {
            animationTarget.localScale = originalScale;
        }
    }
    
    // Utility method to add hover animation to any GameObject
    public static UIHoverAnimator AddHoverAnimation(GameObject target, float hoverScale = 1.1f, float speed = 10f)
    {
        UIHoverAnimator animator = target.GetComponent<UIHoverAnimator>();
        if (animator == null)
        {
            animator = target.AddComponent<UIHoverAnimator>();
        }
        
        animator.hoverScale = hoverScale;
        animator.animationSpeed = speed;
        
        return animator;
    }
}

/// <summary>
/// Helper class to easily add hover animations to multiple UI elements
/// </summary>
public class UIHoverAnimationManager : MonoBehaviour
{
    [Header("Bulk Animation Settings")]
    [Tooltip("Apply hover animation to all buttons in children")]
    public bool animateAllButtons = true;
    
    [Tooltip("Apply hover animation to all images in children")]
    public bool animateAllImages = false;
    
    [Tooltip("Default hover scale for all elements")]
    [Range(0.8f, 2f)]
    public float defaultHoverScale = 1.1f;
    
    [Tooltip("Default animation speed for all elements")]
    [Range(1f, 20f)]
    public float defaultAnimationSpeed = 10f;
    
    [Tooltip("Enable color animation for all elements")]
    public bool enableColorAnimation = false;
    
    [Tooltip("Default hover color")]
    public Color defaultHoverColor = Color.white;
    
    [Header("Manual Targets")]
    [Tooltip("Manually assigned GameObjects to animate")]
    public GameObject[] manualTargets;
    
    void Start()
    {
        ApplyHoverAnimations();
    }
    
    void ApplyHoverAnimations()
    {
        // Animate all buttons
        if (animateAllButtons)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                AddAnimationToTarget(button.gameObject);
            }
        }
        
        // Animate all images
        if (animateAllImages)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                // Skip if it's already a button (to avoid duplicate animations)
                if (image.GetComponent<Button>() == null)
                {
                    AddAnimationToTarget(image.gameObject);
                }
            }
        }
        
        // Animate manual targets
        foreach (GameObject target in manualTargets)
        {
            if (target != null)
            {
                AddAnimationToTarget(target);
            }
        }
    }
    
    void AddAnimationToTarget(GameObject target)
    {
        UIHoverAnimator animator = UIHoverAnimator.AddHoverAnimation(target, defaultHoverScale, defaultAnimationSpeed);
        
        if (enableColorAnimation)
        {
            animator.animateColor = true;
            animator.hoverColor = defaultHoverColor;
        }
    }
    
    // Public method to refresh animations (call after UI changes)
    public void RefreshAnimations()
    {
        // Remove existing animations
        UIHoverAnimator[] existingAnimators = GetComponentsInChildren<UIHoverAnimator>(true);
        for (int i = existingAnimators.Length - 1; i >= 0; i--)
        {
            if (existingAnimators[i] != null)
            {
                DestroyImmediate(existingAnimators[i]);
            }
        }
        
        // Re-apply animations
        ApplyHoverAnimations();
    }
}

/// <summary>
/// Preset configurations for common hover animation styles
/// </summary>
public static class UIHoverPresets
{
    public static void ApplyGentleHover(UIHoverAnimator animator)
    {
        animator.hoverScale = 1.05f;
        animator.animationSpeed = 8f;
        animator.useSmoothCurve = true;
    }
    
    public static void ApplyBouncyHover(UIHoverAnimator animator)
    {
        animator.hoverScale = 1.2f;
        animator.animationSpeed = 15f;
        animator.useSmoothCurve = false;
    }
    
    public static void ApplySubtleHover(UIHoverAnimator animator)
    {
        animator.hoverScale = 1.02f;
        animator.animationSpeed = 12f;
        animator.useSmoothCurve = true;
    }
    
    public static void ApplyCozyRoadSignHover(UIHoverAnimator animator)
    {
        animator.hoverScale = 1.1f;
        animator.animationSpeed = 10f;
        animator.useSmoothCurve = true;
        animator.animateColor = true;
        animator.hoverColor = new Color(1f, 1f, 0.8f, 1f); // Warm tint
    }
}