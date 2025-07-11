using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance;
    
    [Header("Current Session Data")]
    public PlayerProfile currentPlayer;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPlayerProfile();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    #region Character Creation Save
    public void SaveCharacterCreation(string playerName, int characterIndex, string characterCardName)
    {
        // Create new player profile
        currentPlayer = new PlayerProfile();
        currentPlayer.playerName = playerName;
        currentPlayer.selectedCharacterIndex = characterIndex;
        currentPlayer.characterCardName = characterCardName;
        currentPlayer.creationDate = System.DateTime.Now.ToString();
        
        // Save to PlayerPrefs for quick access
        PlayerPrefs.SetString("CurrentPlayerName", playerName);
        PlayerPrefs.SetInt("CurrentCharacterIndex", characterIndex);
        PlayerPrefs.SetString("CurrentCharacterCard", characterCardName);
        PlayerPrefs.Save();
        
        // Save profile to file
        SavePlayerProfile();
        
        Debug.Log($"Character created: {playerName} using {characterCardName}");
    }
    
    public void LoadGameScene(string sceneName)
    {
        if (!HasValidPlayer())
        {
            Debug.LogError("No valid player data found! Create a character first.");
            return;
        }
        
        // Save current player data to ensure it persists
        PlayerPrefs.SetString("LoadingPlayerName", currentPlayer.playerName);
        PlayerPrefs.SetInt("LoadingCharacterIndex", currentPlayer.selectedCharacterIndex);
        PlayerPrefs.SetString("LoadingCharacterCard", currentPlayer.characterCardName);
        PlayerPrefs.Save();
        
        // Load the specified scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        
        Debug.Log($"Loading scene '{sceneName}' with character: {currentPlayer.playerName} ({currentPlayer.characterCardName})");
    }
    
    public void LoadGameSceneByIndex(int sceneIndex)
    {
        if (!HasValidPlayer())
        {
            Debug.LogError("No valid player data found! Create a character first.");
            return;
        }
        
        // Save current player data to ensure it persists
        PlayerPrefs.SetString("LoadingPlayerName", currentPlayer.playerName);
        PlayerPrefs.SetInt("LoadingCharacterIndex", currentPlayer.selectedCharacterIndex);
        PlayerPrefs.SetString("LoadingCharacterCard", currentPlayer.characterCardName);
        PlayerPrefs.Save();
        
        // Load the specified scene by index
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneIndex);
        
        Debug.Log($"Loading scene index {sceneIndex} with character: {currentPlayer.playerName} ({currentPlayer.characterCardName})");
    }
    #endregion
    
    #region Player Profile Management
    void SavePlayerProfile()
    {
        string json = JsonUtility.ToJson(currentPlayer, true);
        string fileName = SanitizeFileName(currentPlayer.playerName) + "_profile.json";
        string path = Path.Combine(Application.persistentDataPath, "Profiles", fileName);
        
        // Create directory if it doesn't exist
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        
        File.WriteAllText(path, json);
        Debug.Log($"Profile saved to: {path}");
    }
    
    void LoadPlayerProfile()
    {
        // Try to load from PlayerPrefs first (quick access)
        if (PlayerPrefs.HasKey("CurrentPlayerName"))
        {
            string playerName = PlayerPrefs.GetString("CurrentPlayerName");
            string fileName = SanitizeFileName(playerName) + "_profile.json";
            string path = Path.Combine(Application.persistentDataPath, "Profiles", fileName);
            
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                currentPlayer = JsonUtility.FromJson<PlayerProfile>(json);
                Debug.Log($"Loaded profile for: {currentPlayer.playerName}");
            }
        }
        
        // Check if we're loading from character creation
        if (PlayerPrefs.HasKey("LoadingPlayerName"))
        {
            currentPlayer = new PlayerProfile();
            currentPlayer.playerName = PlayerPrefs.GetString("LoadingPlayerName");
            currentPlayer.selectedCharacterIndex = PlayerPrefs.GetInt("LoadingCharacterIndex");
            currentPlayer.characterCardName = PlayerPrefs.GetString("LoadingCharacterCard");
            
            Debug.Log($"Loaded character from scene transition: {currentPlayer.playerName} ({currentPlayer.characterCardName})");
            
            // Clear loading data
            PlayerPrefs.DeleteKey("LoadingPlayerName");
            PlayerPrefs.DeleteKey("LoadingCharacterIndex");
            PlayerPrefs.DeleteKey("LoadingCharacterCard");
            PlayerPrefs.Save();
        }
    }
    #endregion
    
    #region Game Save Management
   public void SaveGameData(GameSaveData saveData)
{
    // Use character name and character index for consistent save file naming
    string baseFileName = $"{SanitizeFileName(currentPlayer.playerName)}_Char{currentPlayer.selectedCharacterIndex}";
    string fileName = $"{baseFileName}.save";
    string path = Path.Combine(Application.persistentDataPath, "SaveGames", fileName);
    
    // Add character info to save data
    saveData.playerName = currentPlayer.playerName;
    saveData.characterIndex = currentPlayer.selectedCharacterIndex;
    saveData.characterCardName = currentPlayer.characterCardName;
    
    // Create directory if it doesn't exist
    Directory.CreateDirectory(Path.GetDirectoryName(path));
    
    // Check if save file already exists
    bool isOverwriting = File.Exists(path);
    
    // Always update save date to current time
    saveData.saveDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    
    string json = JsonUtility.ToJson(saveData, true);
    File.WriteAllText(path, json);
    
    if (isOverwriting)
    {
        Debug.Log($"Game overwritten: {fileName} (Updated: {saveData.saveDate})");
    }
    else
    {
        Debug.Log($"Game saved: {fileName} (Created: {saveData.saveDate})");
    }
}

// Also add this helper method to get existing save for a character
public GameSaveData GetExistingSaveForCharacter(string playerName, int characterIndex)
{
    string baseFileName = $"{SanitizeFileName(playerName)}_Char{characterIndex}";
    string fileName = $"{baseFileName}.save";
    string path = Path.Combine(Application.persistentDataPath, "SaveGames", fileName);
    
    if (File.Exists(path))
    {
        try
        {
            string json = File.ReadAllText(path);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            saveData.saveFilePath = path;
            return saveData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load existing save file {path}: {e.Message}");
        }
    }
    
    return null;
}

// Modified GetAllSaveGames method to handle the new naming convention
public List<GameSaveData> GetAllSaveGames()
{
    List<GameSaveData> saveGames = new List<GameSaveData>();
    string savePath = Path.Combine(Application.persistentDataPath, "SaveGames");
    
    if (Directory.Exists(savePath))
    {
        string[] saveFiles = Directory.GetFiles(savePath, "*.save");
        
        foreach (string file in saveFiles)
        {
            try
            {
                string json = File.ReadAllText(file);
                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
                saveData.saveFilePath = file; // Store file path for loading
                saveGames.Add(saveData);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load save file {file}: {e.Message}");
            }
        }
    }
    
    // Sort by save date (newest first)
    saveGames.Sort((a, b) => b.saveDate.CompareTo(a.saveDate));
    return saveGames;
}
    
    public void LoadGameData(string saveFilePath)
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            
            // Update current player data
            currentPlayer.playerName = saveData.playerName;
            currentPlayer.selectedCharacterIndex = saveData.characterIndex;
            currentPlayer.characterCardName = saveData.characterCardName;
            
            // Apply save data to game systems here
            ApplyLoadedGameData(saveData);
            
            Debug.Log($"Game loaded: {saveData.playerName} - {saveData.characterCardName}");
        }
    }
    #endregion
    
    #region Dialogue System Integration
    public string GetPlayerNameForDialogue()
    {
        return currentPlayer != null ? currentPlayer.playerName : "Player";
    }
    
    public string GetCharacterCardNameForDialogue()
    {
        return currentPlayer != null ? currentPlayer.characterCardName : "Unknown";
    }
    
    public int GetCharacterIndexForDialogue()
    {
        return currentPlayer != null ? currentPlayer.selectedCharacterIndex : 0;
    }
    #endregion
    
    #region Utility Methods
    string SanitizeFileName(string fileName)
    {
        // Remove invalid file name characters
        string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        foreach (char c in invalid)
        {
            fileName = fileName.Replace(c.ToString(), "");
        }
        return fileName.Trim();
    }
    
    void ApplyLoadedGameData(GameSaveData saveData)
    {
        // Apply race progress, car upgrades, etc.
        // This is where you'd integrate with your racing game systems
        
        // Example integrations:
        // RaceManager.Instance.SetCurrentTrack(saveData.currentTrack);
        // CarManager.Instance.SetCarUpgrades(saveData.carUpgrades);
        // ProgressManager.Instance.SetRaceProgress(saveData.raceProgress);
    }
    #endregion
    
    #region Public Accessors
    public PlayerProfile GetCurrentPlayer()
    {
        return currentPlayer;
    }
    
    public bool HasValidPlayer()
    {
        return currentPlayer != null && !string.IsNullOrEmpty(currentPlayer.playerName);
    }
    #endregion
}

[System.Serializable]
public class PlayerProfile
{
    public string playerName;
    public int selectedCharacterIndex;
    public string characterCardName;
    public string creationDate;
    
    // Additional profile data
    public int totalRacesWon;
    public float bestLapTime;
    public int coinsEarned;
    public string favoriteTrack;
}

[System.Serializable]
public class GameSaveData
{
    [Header("Character Data")]
    public string playerName;
    public int characterIndex;
    public string characterCardName;
    
    [Header("Game Progress")]
    public string currentTrack;
    public int raceProgress;
    public float[] carUpgrades;
    public int playerLevel;
    public int coinsAmount;
    
    [Header("Save Info")]
    public string saveDate;
    public string saveFilePath; // Not saved to JSON, used for loading
    
    public GameSaveData()
    {
        saveDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

// Game scene character loader - Add this script to your game scene
public class GameSceneCharacterLoader : MonoBehaviour
{
    [Header("Character Display")]
    public Transform characterSpawnPoint;       // Where to spawn the character in game scene
    public GameObject[] characterPrefabs;       // Array of character prefabs matching creation indices
    
    [Header("UI References")]
    public TMPro.TextMeshProUGUI playerNameUI;  // UI text to show player name
    public TMPro.TextMeshProUGUI characterTypeUI; // UI text to show character type
    public UnityEngine.UI.Image characterIconUI;  // UI image for character icon
    
    private GameObject spawnedCharacter;
    
    void Start()
    {
        LoadPlayerCharacter();
    }
    
    void LoadPlayerCharacter()
    {
        if (!GameSaveManager.Instance.HasValidPlayer())
        {
            Debug.LogError("No valid player data found in game scene!");
            return;
        }
        
        PlayerProfile player = GameSaveManager.Instance.GetCurrentPlayer();
        
        // Update UI with player info
        UpdatePlayerUI(player);
        
        // Spawn character model
        SpawnCharacterModel(player);
        
        Debug.Log($"Loaded player in game scene: {player.playerName} as {player.characterCardName}");
    }
    
    void UpdatePlayerUI(PlayerProfile player)
    {
        // Update player name in UI
        if (playerNameUI != null)
        {
            playerNameUI.text = player.playerName;
        }
        
        // Update character type in UI
        if (characterTypeUI != null)
        {
            characterTypeUI.text = player.characterCardName;
        }
        
        // You can add character icon loading here if needed
        // if (characterIconUI != null)
        // {
        //     characterIconUI.sprite = GetCharacterIcon(player.selectedCharacterIndex);
        // }
    }
    
    void SpawnCharacterModel(PlayerProfile player)
    {
        // Check if we have character prefabs and valid index
        if (characterPrefabs == null || characterPrefabs.Length == 0)
        {
            Debug.LogWarning("No character prefabs assigned!");
            return;
        }
        
        if (player.selectedCharacterIndex < 0 || player.selectedCharacterIndex >= characterPrefabs.Length)
        {
            Debug.LogError($"Invalid character index: {player.selectedCharacterIndex}");
            return;
        }
        
        // Destroy existing character if any
        if (spawnedCharacter != null)
        {
            DestroyImmediate(spawnedCharacter);
        }
        
        // Spawn the selected character
        GameObject characterPrefab = characterPrefabs[player.selectedCharacterIndex];
        Vector3 spawnPosition = characterSpawnPoint != null ? characterSpawnPoint.position : Vector3.zero;
        Quaternion spawnRotation = characterSpawnPoint != null ? characterSpawnPoint.rotation : Quaternion.identity;
        
        spawnedCharacter = Instantiate(characterPrefab, spawnPosition, spawnRotation);
        
        Debug.Log($"Spawned character: {player.characterCardName} at {spawnPosition}");
    }
    
    // Public method to get current player data for other scripts
    public PlayerProfile GetCurrentPlayer()
    {
        return GameSaveManager.Instance.GetCurrentPlayer();
    }
    
    // Example method for dialogue system integration
    public void ShowCharacterDialogue(string dialogue)
    {
        PlayerProfile player = GameSaveManager.Instance.GetCurrentPlayer();
        
        // Replace placeholders
        dialogue = dialogue.Replace("{PLAYER_NAME}", player.playerName);
        dialogue = dialogue.Replace("{CHARACTER_TYPE}", player.characterCardName);
        
        // Display dialogue (integrate with your dialogue system)
        Debug.Log($"Character Dialogue: {dialogue}");
    }
}

// Example dialogue system integration
public class DialogueManager : MonoBehaviour
{
    public void ShowDialogue(string dialogueText)
    {
        // Replace placeholders with player data
        string playerName = GameSaveManager.Instance.GetPlayerNameForDialogue();
        string characterCard = GameSaveManager.Instance.GetCharacterCardNameForDialogue();
        
        dialogueText = dialogueText.Replace("{PLAYER_NAME}", playerName);
        dialogueText = dialogueText.Replace("{CHARACTER_TYPE}", characterCard);
        
        // Example: "Hello {PLAYER_NAME}! Nice to see a {CHARACTER_TYPE} around here!"
        // Becomes: "Hello Alex! Nice to see a Warrior around here!"
        
        DisplayDialogue(dialogueText);
    }
    
    void DisplayDialogue(string text)
    {
        // Your dialogue display logic here
        Debug.Log($"Dialogue: {text}");
    }
}

// Example load game UI
public class LoadGameUI : MonoBehaviour
{
    public Transform saveGameListParent;
    public GameObject saveGameButtonPrefab;
    
    void Start()
    {
        PopulateSaveGameList();
    }
    
    void PopulateSaveGameList()
    {
        List<GameSaveData> saveGames = GameSaveManager.Instance.GetAllSaveGames();
        
        foreach (GameSaveData save in saveGames)
        {
            GameObject button = Instantiate(saveGameButtonPrefab, saveGameListParent);
            
            // Set button text to show character name and save info
            TMPro.TextMeshProUGUI buttonText = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            buttonText.text = $"{save.playerName} ({save.characterCardName})\n{save.saveDate}";
            
            // Set button click to load this save
            UnityEngine.UI.Button btn = button.GetComponent<UnityEngine.UI.Button>();
            btn.onClick.AddListener(() => GameSaveManager.Instance.LoadGameData(save.saveFilePath));
        }
    }
}