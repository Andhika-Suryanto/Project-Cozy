using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class LoadGameManager : MonoBehaviour
{
    [Header("Save Slots")]
    public SaveSlotUI[] saveSlots = new SaveSlotUI[4]; // 4 save slots
    
    [Header("UI")]
    public Button backButton;                    // Back to main menu
    public TextMeshProUGUI noSavesMessage;       // "No save files found"
    
    [Header("Hover Animation")]
    public float hoverScale = 1.1f;              // How much bigger on hover
    public float animationSpeed = 10f;           // Speed of scale animation
    
    private List<GameSaveData> allSaveFiles;
    
    void Start()
    {
        SetupUI();
        LoadAllSaveFiles();
        PopulateSaveSlots();
    }
    
    void SetupUI()
    {
        // Setup back button
        if (backButton != null)
            backButton.onClick.AddListener(BackToMainMenu);
        
        // Setup save slot buttons and hover effects
        for (int i = 0; i < saveSlots.Length; i++)
        {
            int slotIndex = i; // Capture for closure
            
            if (saveSlots[i].slotButton != null)
            {
                // Add click listener
                saveSlots[i].slotButton.onClick.AddListener(() => LoadSaveSlot(slotIndex));
                
                // Add hover effects
                AddHoverEffects(saveSlots[i]);
            }
        }
    }
    
    void AddHoverEffects(SaveSlotUI saveSlot)
    {
        // Add EventTrigger component for hover effects
        EventTrigger trigger = saveSlot.slotButton.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = saveSlot.slotButton.gameObject.AddComponent<EventTrigger>();
        }
        
        // Hover enter
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => OnSlotHoverEnter(saveSlot));
        trigger.triggers.Add(enterEntry);
        
        // Hover exit  
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => OnSlotHoverExit(saveSlot));
        trigger.triggers.Add(exitEntry);
    }
    
    void OnSlotHoverEnter(SaveSlotUI saveSlot)
    {
        if (saveSlot.isEmpty) return; // Don't animate empty slots
        
        // Start hover animation
        StopCoroutine(nameof(AnimateSlotScale));
        StartCoroutine(AnimateSlotScale(saveSlot.slotButton.transform, hoverScale));
        
        // Optional: Increase font size
        if (saveSlot.playerNameText != null)
        {
            saveSlot.playerNameText.fontSize = saveSlot.originalFontSize * hoverScale;
        }
    }
    
    void OnSlotHoverExit(SaveSlotUI saveSlot)
    {
        if (saveSlot.isEmpty) return;
        
        // Return to normal size
        StopCoroutine(nameof(AnimateSlotScale));
        StartCoroutine(AnimateSlotScale(saveSlot.slotButton.transform, 1f));
        
        // Reset font size
        if (saveSlot.playerNameText != null)
        {
            saveSlot.playerNameText.fontSize = saveSlot.originalFontSize;
        }
    }
    
    System.Collections.IEnumerator AnimateSlotScale(Transform target, float targetScale)
    {
        Vector3 startScale = target.localScale;
        Vector3 endScale = Vector3.one * targetScale;
        float elapsed = 0f;
        float duration = 1f / animationSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Smooth animation curve
            progress = Mathf.SmoothStep(0f, 1f, progress);
            
            target.localScale = Vector3.Lerp(startScale, endScale, progress);
            yield return null;
        }
        
        target.localScale = endScale;
    }
    
    void LoadAllSaveFiles()
    {
        if (GameSaveManager.Instance != null)
        {
            allSaveFiles = GameSaveManager.Instance.GetAllSaveGames();
        }
        else
        {
            // Fallback: Load from PlayerPrefs or manual file search
            allSaveFiles = LoadSaveFilesFromDisk();
        }
        
        Debug.Log($"Found {allSaveFiles.Count} save files");
    }
    
    List<GameSaveData> LoadSaveFilesFromDisk()
    {
        // Fallback method if GameSaveManager doesn't exist
        List<GameSaveData> saves = new List<GameSaveData>();
        
        // Add manual file loading here if needed
        // This is a backup method
        
        return saves;
    }
    
    void PopulateSaveSlots()
    {
        // Reset all slots first
        for (int i = 0; i < saveSlots.Length; i++)
        {
            SetSlotEmpty(saveSlots[i], i + 1);
        }
        
        // Populate slots with save data
        for (int i = 0; i < allSaveFiles.Count && i < saveSlots.Length; i++)
        {
            PopulateSaveSlot(saveSlots[i], allSaveFiles[i], i + 1);
        }
        
        // Show no saves message if no saves found
        if (noSavesMessage != null)
        {
            noSavesMessage.gameObject.SetActive(allSaveFiles.Count == 0);
        }
    }
    
    void SetSlotEmpty(SaveSlotUI slot, int slotNumber)
    {
        slot.isEmpty = true;
        slot.saveData = null;
        
        if (slot.playerNameText != null)
        {
            slot.playerNameText.text = $"Empty Slot {slotNumber}";
            slot.originalFontSize = slot.playerNameText.fontSize;
        }
        
        if (slot.saveInfoText != null)
        {
            slot.saveInfoText.text = "No save data";
        }
        
        if (slot.characterTypeText != null)
        {
            slot.characterTypeText.text = "";
        }
        
        // Make button less prominent for empty slots
        if (slot.slotButton != null)
        {
            slot.slotButton.interactable = false;
            
            // Optional: Change appearance of empty slots
            Image buttonImage = slot.slotButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color color = buttonImage.color;
                color.a = 0.5f; // Make transparent
                buttonImage.color = color;
            }
        }
    }
    
    void PopulateSaveSlot(SaveSlotUI slot, GameSaveData saveData, int slotNumber)
    {
        slot.isEmpty = false;
        slot.saveData = saveData;
        
        if (slot.playerNameText != null)
        {
            slot.playerNameText.text = saveData.playerName;
            slot.originalFontSize = slot.playerNameText.fontSize;
        }
        
        if (slot.saveInfoText != null)
        {
            slot.saveInfoText.text = $"Saved: {saveData.saveDate}";
        }
        
        if (slot.characterTypeText != null)
        {
            slot.characterTypeText.text = saveData.characterCardName;
        }
        
        // Make button interactive
        if (slot.slotButton != null)
        {
            slot.slotButton.interactable = true;
            
            // Reset button appearance
            Image buttonImage = slot.slotButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color color = buttonImage.color;
                color.a = 1f; // Full opacity
                buttonImage.color = color;
            }
        }
    }
    
    void LoadSaveSlot(int slotIndex)
    {
        if (slotIndex >= saveSlots.Length || saveSlots[slotIndex].isEmpty)
        {
            Debug.LogWarning($"Slot {slotIndex + 1} is empty or invalid!");
            return;
        }
        
        GameSaveData saveToLoad = saveSlots[slotIndex].saveData;
        
        if (saveToLoad == null)
        {
            Debug.LogError($"No save data in slot {slotIndex + 1}!");
            return;
        }
        
        // Load the save file
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.LoadGameData(saveToLoad.saveFilePath);
        }
        
        // Load the appropriate scene based on save data
        LoadGameScene(saveToLoad);
        
        Debug.Log($"Loading save from slot {slotIndex + 1}: {saveToLoad.playerName}");
    }
    
    void LoadGameScene(GameSaveData saveData)
    {
        // Load the scene where the save was created
        string sceneToLoad = "GameWorld"; // Default scene
        
        // Check if save data has scene info
        if (saveData is OpenWorldSaveData worldSave && !string.IsNullOrEmpty(worldSave.currentSceneName))
        {
            sceneToLoad = worldSave.currentSceneName;
        }
        
        // Load the scene
        SceneManager.LoadScene(sceneToLoad);
    }
    
    void BackToMainMenu()
    {
        // Load main menu scene
        SceneManager.LoadScene("MainMenu"); // Change to your main menu scene name
    }
    
    // Public method to refresh save slots (call after deleting saves, etc.)
    public void RefreshSaveSlots()
    {
        LoadAllSaveFiles();
        PopulateSaveSlots();
    }
}

[System.Serializable]
public class SaveSlotUI
{
    [Header("UI Components")]
    public Button slotButton;               // The clickable button
    public TextMeshProUGUI playerNameText;  // Character name display
    public TextMeshProUGUI saveInfoText;    // Save date/time info
    public TextMeshProUGUI characterTypeText; // Character type (Warrior, Mage, etc.)
    
    [Header("Slot State")]
    public bool isEmpty = true;             // Is this slot empty?
    public GameSaveData saveData;           // The save data for this slot
    public float originalFontSize;          // Original font size for animations
}

// Alternative simpler version for just character names
public class SimpleLoadGameManager : MonoBehaviour
{
    [Header("Simple Save Slots")]
    public Button[] saveSlotButtons = new Button[4];
    public TextMeshProUGUI[] saveSlotTexts = new TextMeshProUGUI[4];
    
    [Header("Hover Settings")]
    public float hoverScale = 1.2f;
    public float animationSpeed = 8f;
    
    private GameSaveData[] slotSaveData = new GameSaveData[4];
    
    void Start()
    {
        LoadAndDisplaySaves();
    }
    
    void LoadAndDisplaySaves()
    {
        if (GameSaveManager.Instance == null) return;
        
        List<GameSaveData> allSaves = GameSaveManager.Instance.GetAllSaveGames();
        
        for (int i = 0; i < saveSlotButtons.Length; i++)
        {
            int slotIndex = i;
            
            if (i < allSaves.Count)
            {
                // Slot has save data
                slotSaveData[i] = allSaves[i];
                saveSlotTexts[i].text = allSaves[i].playerName;
                saveSlotButtons[i].interactable = true;
                saveSlotButtons[i].onClick.AddListener(() => LoadSlot(slotIndex));
            }
            else
            {
                // Empty slot
                slotSaveData[i] = null;
                saveSlotTexts[i].text = $"Empty Slot {i + 1}";
                saveSlotButtons[i].interactable = false;
            }
            
            // Add hover effects
            AddSimpleHoverEffect(saveSlotButtons[i]);
        }
    }
    
    void AddSimpleHoverEffect(Button button)
    {
        if (!button.interactable) return;
        
        EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();
        
        EventTrigger.Entry enter = new EventTrigger.Entry();
        enter.eventID = EventTriggerType.PointerEnter;
        enter.callback.AddListener((data) => {
            button.transform.localScale = Vector3.one * hoverScale;
        });
        trigger.triggers.Add(enter);
        
        EventTrigger.Entry exit = new EventTrigger.Entry();
        exit.eventID = EventTriggerType.PointerExit;
        exit.callback.AddListener((data) => {
            button.transform.localScale = Vector3.one;
        });
        trigger.triggers.Add(exit);
    }
    
    void LoadSlot(int slotIndex)
    {
        if (slotSaveData[slotIndex] != null)
        {
            GameSaveManager.Instance.LoadGameData(slotSaveData[slotIndex].saveFilePath);
            SceneManager.LoadScene("GameWorld");
        }
    }
}