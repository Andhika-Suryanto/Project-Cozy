using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class CharacterCreationManager : MonoBehaviour
{
    [Header("Character Cards")]
    public CharacterCardData[] characterCards; // Each card with its own TMP
    
    [Header("Navigation")]
    public Button leftArrowButton;
    public Button rightArrowButton;
    
    [Header("Start Game Button")]
    public Button startGameButton;
    
    [Header("Card Positions")]
    public Transform leftPosition;      // Empty object - where left card sits
    public Transform centerPosition;    // Empty object - where active card sits  
    public Transform rightPosition;     // Empty object - where right card sits
    
    [Header("Scene Loading")]
    public UnityEngine.Object gameSceneAsset;   // Drag scene from Project window here
    public string gameSceneName = "GameScene";  // Fallback: type scene name manually
    public int gameSceneIndex = -1;             // Fallback: use build settings index
    
    [Header("Card Animation Settings")]
    [Range(0f, 1f)]
    public float inactiveCardOpacity = 0.3f; // Opacity for side cards
    [Range(0f, 1f)]
    public float disabledArrowOpacity = 0.3f; // Opacity for disabled arrows
    public float animationSpeed = 5f; // How fast cards fade
    public float moveSpeed = 5f; // How fast cards move between positions
    
    // Private variables
    private int currentCardIndex = 0;
    private bool hasValidName = false;
    private CanvasGroup[] cardCanvasGroups; // For opacity control
    private CanvasGroup leftArrowCanvasGroup;
    private CanvasGroup rightArrowCanvasGroup;
    
    void Start()
    {
        SetupUI();
    }
    
    void SetupUI()
    {
        // Setup canvas groups for opacity control
        SetupCanvasGroups();
        
        // Setup arrow buttons
        leftArrowButton.onClick.AddListener(() => ChangeCard(-1));
        rightArrowButton.onClick.AddListener(() => ChangeCard(1));
        
        // Setup input fields for each card
        SetupInputFields();
        
        // Setup start game button
        startGameButton.onClick.AddListener(StartGame);
        
        // Display initial card
        DisplayCurrentCard();
        
        // Update all UI states
        UpdateArrowStates();
        UpdateStartButtonState();
    }
    
    void SetupCanvasGroups()
    {
        cardCanvasGroups = new CanvasGroup[characterCards.Length];
        
        for (int i = 0; i < characterCards.Length; i++)
        {
            // Add CanvasGroup to cards if they don't exist
            cardCanvasGroups[i] = characterCards[i].cardObject.GetComponent<CanvasGroup>();
            if (cardCanvasGroups[i] == null)
            {
                cardCanvasGroups[i] = characterCards[i].cardObject.AddComponent<CanvasGroup>();
            }
            
            // Set initial opacity
            cardCanvasGroups[i].alpha = (i == currentCardIndex) ? 1f : inactiveCardOpacity;
        }
        
        // Setup arrow canvas groups
        leftArrowCanvasGroup = leftArrowButton.GetComponent<CanvasGroup>();
        if (leftArrowCanvasGroup == null)
        {
            leftArrowCanvasGroup = leftArrowButton.gameObject.AddComponent<CanvasGroup>();
        }
        
        rightArrowCanvasGroup = rightArrowButton.GetComponent<CanvasGroup>();
        if (rightArrowCanvasGroup == null)
        {
            rightArrowCanvasGroup = rightArrowButton.gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    void SetupInputFields()
    {
        for (int i = 0; i < characterCards.Length; i++)
        {
            int cardIndex = i; // Capture for closure
            
            if (characterCards[i].nameInputField != null)
            {
                // Set character limit
                characterCards[i].nameInputField.characterLimit = 20;
                
                // Listen to name changes on this specific card
                characterCards[i].nameInputField.onValueChanged.AddListener((name) => OnNameChanged(name, cardIndex));
            }
        }
    }
    
    void ChangeCard(int direction)
    {
        // Check if movement is allowed
        if (!CanMoveInDirection(direction)) return;
        
        // Change card index
        currentCardIndex += direction;
        
        // Update display
        DisplayCurrentCard();
        
        // Update arrow states
        UpdateArrowStates();
        
        Debug.Log($"Selected Card: {currentCardIndex + 1}");
    }
    
    bool CanMoveInDirection(int direction)
    {
        if (direction < 0) // Moving left
        {
            return currentCardIndex > 0;
        }
        else if (direction > 0) // Moving right
        {
            return currentCardIndex < characterCards.Length - 1;
        }
        return false;
    }
    
    void DisplayCurrentCard()
    {
        // Update card positions and opacities
        UpdateCardPositionsAndOpacity();
        
        // Update button state based on current card's name
        UpdateStartButtonState();
        
        // Focus on current card's input field
        if (characterCards[currentCardIndex].nameInputField != null)
        {
            characterCards[currentCardIndex].nameInputField.Select();
        }
    }
    
    void UpdateCardPositionsAndOpacity()
    {
        for (int i = 0; i < characterCards.Length; i++)
        {
            GameObject card = characterCards[i].cardObject;
            CanvasGroup canvasGroup = cardCanvasGroups[i];
            
            // Determine target position and opacity
            Transform targetPosition = null;
            float targetOpacity = inactiveCardOpacity;
            
            if (i == currentCardIndex)
            {
                // Current card - center position, full opacity
                targetPosition = centerPosition;
                targetOpacity = 1f;
            }
            else if (i == currentCardIndex - 1)
            {
                // Previous card - left position, reduced opacity
                targetPosition = leftPosition;
            }
            else if (i == currentCardIndex + 1)
            {
                // Next card - right position, reduced opacity
                targetPosition = rightPosition;
            }
            else
            {
                // Cards further away - hide completely or keep at last position
                targetOpacity = 0f;
                // Don't move cards that are too far away
                continue;
            }
            
            // Animate position if target position exists
            if (targetPosition != null)
            {
                card.SetActive(true);
                if (moveSpeed > 0)
                {
                    StartCoroutine(AnimateToPosition(card, targetPosition.position, targetOpacity, canvasGroup));
                }
                else
                {
                    card.transform.position = targetPosition.position;
                    canvasGroup.alpha = targetOpacity;
                }
            }
            else
            {
                // Hide cards that are too far away
                if (animationSpeed > 0)
                {
                    StartCoroutine(AnimateOpacity(canvasGroup, 0f));
                }
                else
                {
                    canvasGroup.alpha = 0f;
                }
            }
        }
    }
    
    System.Collections.IEnumerator AnimateToPosition(GameObject card, Vector3 targetPosition, float targetOpacity, CanvasGroup canvasGroup)
    {
        Vector3 startPosition = card.transform.position;
        float startOpacity = canvasGroup.alpha;
        float elapsed = 0f;
        float duration = 1f / moveSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Animate position
            card.transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            
            // Animate opacity
            canvasGroup.alpha = Mathf.Lerp(startOpacity, targetOpacity, progress);
            
            yield return null;
        }
        
        // Ensure final values
        card.transform.position = targetPosition;
        canvasGroup.alpha = targetOpacity;
    }
    
    void UpdateArrowStates()
    {
        // Check if left arrow should be enabled
        bool canMoveLeft = CanMoveInDirection(-1);
        leftArrowButton.interactable = canMoveLeft;
        SetArrowOpacity(leftArrowCanvasGroup, canMoveLeft);
        
        // Check if right arrow should be enabled
        bool canMoveRight = CanMoveInDirection(1);
        rightArrowButton.interactable = canMoveRight;
        SetArrowOpacity(rightArrowCanvasGroup, canMoveRight);
    }
    
    void SetArrowOpacity(CanvasGroup arrowCanvasGroup, bool isEnabled)
    {
        float targetOpacity = isEnabled ? 1f : disabledArrowOpacity;
        
        if (animationSpeed > 0)
        {
            StartCoroutine(AnimateOpacity(arrowCanvasGroup, targetOpacity));
        }
        else
        {
            arrowCanvasGroup.alpha = targetOpacity;
        }
    }
    
    System.Collections.IEnumerator AnimateOpacity(CanvasGroup canvasGroup, float targetOpacity)
    {
        float startOpacity = canvasGroup.alpha;
        float elapsed = 0f;
        float duration = 1f / animationSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            canvasGroup.alpha = Mathf.Lerp(startOpacity, targetOpacity, progress);
            yield return null;
        }
        
        canvasGroup.alpha = targetOpacity;
    }
    
    void OnNameChanged(string newName, int cardIndex)
    {
        // Only update if this is the currently displayed card
        if (cardIndex != currentCardIndex) return;
        
        // Check if name is valid
        hasValidName = !string.IsNullOrWhiteSpace(newName);
        
        // Update start button state
        UpdateStartButtonState();
        
        Debug.Log($"Card {cardIndex + 1} name changed to: '{newName}' - Valid: {hasValidName}");
    }
    
    void UpdateStartButtonState()
    {
        // Check current card's name validity
        if (characterCards[currentCardIndex].nameInputField != null)
        {
            string currentName = characterCards[currentCardIndex].nameInputField.text;
            hasValidName = !string.IsNullOrWhiteSpace(currentName);
        }
        else
        {
            hasValidName = false;
        }
        
        // Update button interactability (sprite swap handles visuals)
        startGameButton.interactable = hasValidName;
        
        // Backup: Also control opacity if sprite swap isn't working
        SetStartButtonOpacity(hasValidName ? 1f : 0.33f);
    }
    
    void SetStartButtonOpacity(float alpha)
    {
        // Change button image opacity
        Image buttonImage = startGameButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color color = buttonImage.color;
            color.a = alpha;
            buttonImage.color = color;
        }
        
        // Change button text opacity if it exists
        TextMeshProUGUI buttonText = startGameButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            Color textColor = buttonText.color;
            textColor.a = alpha;
            buttonText.color = textColor;
        }
    }
    
    void StartGame()
    {
        if (!hasValidName) return; // Safety check
        
        // Get current card data
        CharacterCardData selectedCard = characterCards[currentCardIndex];
        string playerName = selectedCard.nameInputField.text.Trim();
        string characterCardName = selectedCard.cardName;
        
        // Save character creation data using GameSaveManager
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SaveCharacterCreation(playerName, currentCardIndex, characterCardName);
            
            // Determine which scene to load
            string sceneToLoad = GetSceneNameToLoad();
            
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                GameSaveManager.Instance.LoadGameScene(sceneToLoad);
            }
            else if (gameSceneIndex >= 0)
            {
                GameSaveManager.Instance.LoadGameSceneByIndex(gameSceneIndex);
            }
            else
            {
                Debug.LogError("No valid scene specified! Please drag a scene asset, enter a scene name, or set scene index.");
            }
        }
        else
        {
            // Fallback if GameSaveManager doesn't exist
            SaveCharacterDataFallback(playerName, currentCardIndex);
            LoadSceneFallback();
        }
        
        Debug.Log($"Starting game with Card {currentCardIndex + 1}, Name: {playerName}");
    }
    
    string GetSceneNameToLoad()
    {
        // Priority 1: Scene asset dragged in inspector
        if (gameSceneAsset != null)
        {
            return gameSceneAsset.name;
        }
        
        // Priority 2: Manually typed scene name
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            return gameSceneName;
        }
        
        return null;
    }
    
    void SaveCharacterDataFallback(string playerName, int cardIndex)
    {
        // Fallback save method if GameSaveManager doesn't exist
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.SetInt("SelectedCharacter", cardIndex);
        PlayerPrefs.Save();
    }
    
    void LoadSceneFallback()
    {
        // Fallback scene loading if GameSaveManager doesn't exist
        string sceneToLoad = GetSceneNameToLoad();
        
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else if (gameSceneIndex >= 0)
        {
            SceneManager.LoadScene(gameSceneIndex);
        }
        else
        {
            SceneManager.LoadScene("GameScene");
        }
    }
    
    // Optional: Load saved data
    public void LoadCharacterData()
    {
        if (PlayerPrefs.HasKey("SelectedCharacter"))
        {
            int savedIndex = PlayerPrefs.GetInt("SelectedCharacter");
            if (savedIndex >= 0 && savedIndex < characterCards.Length)
            {
                currentCardIndex = savedIndex;
                
                if (PlayerPrefs.HasKey("PlayerName"))
                {
                    string savedName = PlayerPrefs.GetString("PlayerName");
                    if (characterCards[currentCardIndex].nameInputField != null)
                    {
                        characterCards[currentCardIndex].nameInputField.text = savedName;
                    }
                }
                
                DisplayCurrentCard();
                UpdateArrowStates();
            }
        }
    }
    
    // Public methods for external use
    public int GetSelectedCardIndex()
    {
        return currentCardIndex;
    }
    
    public string GetCurrentPlayerName()
    {
        if (characterCards[currentCardIndex].nameInputField != null)
        {
            return characterCards[currentCardIndex].nameInputField.text.Trim();
        }
        return "";
    }
    
    public CharacterCardData GetSelectedCard()
    {
        return characterCards[currentCardIndex];
    }
    
    // Editor utility - call this to refresh canvas groups in play mode
    [ContextMenu("Refresh Canvas Groups")]
    public void RefreshCanvasGroups()
    {
        SetupCanvasGroups();
        UpdateCardPositionsAndOpacity();
        UpdateArrowStates();
    }
    
    // Editor utility - setup initial card positions
    [ContextMenu("Setup Initial Positions")]
    public void SetupInitialPositions()
    {
        if (centerPosition == null || leftPosition == null || rightPosition == null)
        {
            Debug.LogWarning("Please assign Left, Center, and Right position objects in the inspector!");
            return;
        }
        
        for (int i = 0; i < characterCards.Length; i++)
        {
            GameObject card = characterCards[i].cardObject;
            if (card == null) continue;
            
            if (i == 0)
            {
                // First card starts in center
                card.transform.position = centerPosition.position;
            }
            else if (i == 1)
            {
                // Second card starts on right
                card.transform.position = rightPosition.position;
            }
            else
            {
                // Other cards start hidden/off-screen
                card.transform.position = rightPosition.position;
            }
        }
        
        Debug.Log("Initial card positions set up!");
    }
    
    // Editor utility to validate scene reference
    #if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-fill scene name when scene asset is dragged
        if (gameSceneAsset != null)
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(gameSceneAsset);
            if (assetPath.EndsWith(".unity"))
            {
                gameSceneName = gameSceneAsset.name;
            }
            else
            {
                Debug.LogWarning("Please drag a .unity scene file!");
                gameSceneAsset = null;
            }
        }
    }
    #endif
}

[System.Serializable]
public class CharacterCardData
{
    [Header("Card Elements")]
    public GameObject cardObject;           // The card GameObject
    public TMP_InputField nameInputField;   // This card's input field
    
    [Header("Optional Info")]
    public string cardName;                 // "Warrior", "Mage", etc. (for reference)
}