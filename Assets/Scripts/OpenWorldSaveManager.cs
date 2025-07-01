using UnityEngine;
using UnityEngine.UI;
// Note: Remove RCC_Settings using if causing issues - we'll reference RCC directly

// Open World Save System for RCC Racing Game - Save Anywhere
public class OpenWorldSaveManager : MonoBehaviour
{
    [Header("RCC Car Reference")]
    public MonoBehaviour playerCar;        // Your RCC car (using MonoBehaviour to avoid type errors)
    
    [Header("Car Management")]
    public GameObject[] availableCarPrefabs;   // All car models in your game
    public string[] carModelNames;             // Names corresponding to prefabs
    public Transform carSpawnPoint;            // Where to spawn cars when loading
    
    [Header("UI")]
    public Button saveGameButton;              // Manual save button
    public Button loadGameButton;              // Manual load button  
    public TMPro.TextMeshProUGUI saveStatusText; // "Game Saved!" feedback
    public KeyCode saveHotkey = KeyCode.F5;    // Quick save key
    
    [Header("Open World State")]
    public string currentZone = "Downtown";    // Current area/district
    public int playerLevel = 1;
    public int totalMoney = 1000;
    public int totalRacesWon = 0;
    public int totalRacesCompleted = 0;
    
    private bool isInRace = false;
    private OpenWorldSaveData currentSave;
    
    void Start()
    {
        // Setup save/load buttons
        if (saveGameButton != null)
            saveGameButton.onClick.AddListener(ManualSaveGame);
            
        if (loadGameButton != null)
            loadGameButton.onClick.AddListener(ManualLoadGame);
        
        // Auto-detect player car if not assigned
        if (playerCar == null)
        {
            AutoDetectPlayerCar();
        }
        
        // Auto-load existing progress
        AutoLoadOpenWorldProgress();
    }
    
    void Update()
    {
        // Quick save hotkey (only in open world)
        if (!isInRace && Input.GetKeyDown(saveHotkey))
        {
            ManualSaveGame();
        }
    }
    
    void AutoDetectPlayerCar()
    {
        // Find car with player tag
        GameObject carWithPlayerTag = GameObject.FindGameObjectWithTag("Player");
        if (carWithPlayerTag != null)
        {
            MonoBehaviour carController = carWithPlayerTag.GetComponent<MonoBehaviour>();
            if (carController != null && carController.GetType().Name.Contains("RCC"))
            {
                playerCar = carController;
                Debug.Log($"Auto-detected player car: {playerCar.name}");
                return;
            }
        }
        
        // Fallback: Find any RCC car in scene
        MonoBehaviour[] allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var component in allComponents)
        {
            if (component.GetType().Name.Contains("RCC_CarController"))
            {
                playerCar = component;
                Debug.Log($"Auto-assigned RCC car found: {playerCar.name}");
                return;
            }
        }
        
        Debug.LogWarning("No RCC car found in scene! Please assign manually or ensure car is spawned.");
    }
    
    #region Race State Management
    public void SetRaceState(bool inRace)
    {
        isInRace = inRace;
        
        // Disable save options during races
        if (saveGameButton != null)
            saveGameButton.interactable = !inRace;
    }
    
    public void OnRaceCompleted(bool won, int prize)
    {
        totalRacesCompleted++;
        if (won)
        {
            totalRacesWon++;
            totalMoney += prize;
        }
        
        // Auto-save after race completion (when back in open world)
        SetRaceState(false);
        Invoke(nameof(AutoSaveAfterRace), 2f); // Wait a bit then auto-save
    }
    
    void AutoSaveAfterRace()
    {
        SaveOpenWorldState();
        ShowSaveMessage("Progress Auto-Saved After Race!");
    }
    #endregion
    
    #region Manual Save/Load
    public void ManualSaveGame()
    {
        if (isInRace)
        {
            Debug.LogWarning("[SAVE] Cannot save during race!");
            ShowSaveMessage("Cannot save during race!");
            return;
        }
        
        Debug.Log("[SAVE] Starting manual save...");
        SaveOpenWorldState();
        ShowSaveMessage($"Game Saved! ({saveHotkey} to quick save)");
        Debug.Log("[SAVE] Manual save completed successfully!");
    }
    
    public void ManualLoadGame()
    {
        if (isInRace)
        {
            Debug.LogWarning("[LOAD] Cannot load during race!");
            ShowSaveMessage("Cannot load during race!");
            return;
        }
        
        Debug.Log("[LOAD] Starting manual load...");
        LoadOpenWorldState();
        ShowSaveMessage("Game Loaded Successfully!");
        Debug.Log("[LOAD] Manual load completed successfully!");
    }
    #endregion
    
    #region Save Open World State
    void SaveOpenWorldState()
    {
        Debug.Log("[SAVE] ======== STARTING SAVE PROCESS ========");
        
        if (playerCar == null)
        {
            Debug.LogError("[SAVE] FAILED: No RCC car assigned for saving!");
            return;
        }
        
        Debug.Log($"[SAVE] Player car found: {playerCar.name}");
        
        // Create save data
        OpenWorldSaveData saveData = new OpenWorldSaveData();
        Debug.Log("[SAVE] Created new save data object");
        
        // Character info (from GameSaveManager)
        if (GameSaveManager.Instance != null)
        {
            PlayerProfile player = GameSaveManager.Instance.GetCurrentPlayer();
            saveData.playerName = player.playerName;
            saveData.characterIndex = player.selectedCharacterIndex;
            saveData.characterCardName = player.characterCardName;
            Debug.Log($"[SAVE] Character data: {player.playerName} as {player.characterCardName}");
        }
        else
        {
            Debug.LogWarning("[SAVE] GameSaveManager not found - using default character data");
        }
        
        // Current scene info - IMPORTANT for multi-scene open worlds
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        
        saveData.currentSceneName = currentSceneName;
        saveData.currentSceneIndex = currentSceneIndex;
        
        Debug.Log($"[SAVE] Scene data: '{saveData.currentSceneName}' (Index: {saveData.currentSceneIndex})");
        Debug.Log($"[SAVE] Scene in Build Settings: {IsSceneInBuildSettings(currentSceneName)}");
        
        // Open world position - save exactly where player is
        saveData.playerPosition = playerCar.transform.position;
        saveData.playerRotation = playerCar.transform.rotation;
        saveData.currentZone = currentZone;
        Debug.Log($"[SAVE] Position: {saveData.playerPosition}, Zone: {saveData.currentZone}");
        
        // Car data
        saveData.currentCarModel = GetCarModelName(playerCar.gameObject);
        saveData.currentCarPrefabIndex = GetCarPrefabIndex(playerCar.gameObject);
        Debug.Log($"[SAVE] Car: {saveData.currentCarModel} (Index: {saveData.currentCarPrefabIndex})");
        
        SaveCarModifications(saveData);
        
        // Player progress
        saveData.playerLevel = playerLevel;
        saveData.totalMoney = totalMoney;
        saveData.totalRacesWon = totalRacesWon;
        saveData.totalRacesCompleted = totalRacesCompleted;
        Debug.Log($"[SAVE] Progress: Level {saveData.playerLevel}, Money ${saveData.totalMoney}, Races {saveData.totalRacesWon}/{saveData.totalRacesCompleted}");
        
        // Game time
        saveData.playTime = Time.time;
        Debug.Log($"[SAVE] Play time: {saveData.playTime} seconds");
        
        // Save using GameSaveManager
        if (GameSaveManager.Instance != null)
        {
            Debug.Log("[SAVE] Saving through GameSaveManager...");
            GameSaveManager.Instance.SaveGameData(saveData);
            Debug.Log("[SAVE] GameSaveManager save completed");
        }
        else
        {
            Debug.LogWarning("[SAVE] GameSaveManager not found - using PlayerPrefs fallback");
            SaveToPlayerPrefs(saveData);
        }
        
        currentSave = saveData;
        Debug.Log($"[SAVE] ======== SAVE COMPLETED SUCCESSFULLY ========");
        Debug.Log($"[SAVE] Summary: {saveData.playerName} at {saveData.playerPosition} in {saveData.currentSceneName}");
    }
    
    void SaveCarModifications(OpenWorldSaveData saveData)
    {
        if (playerCar == null) 
        {
            Debug.LogError("[SAVE CAR] Player car is null!");
            return;
        }
        
        Debug.Log("[SAVE CAR] Starting car data save...");
        
        // Save complete car state using component scanning
        SaveCompleteCarState(saveData);
        
        Debug.Log($"[SAVE CAR] Completed car save for: {saveData.currentCarModel}");
        Debug.Log($"[SAVE CAR] Car stats - Speed: {saveData.carMaxSpeed}, Health: {saveData.carHealth}%, Color: {saveData.carColor}");
    }
    
    void SaveCompleteCarState(OpenWorldSaveData saveData)
    {
        Debug.Log("[SAVE CAR] Scanning car components...");
        
        // Save all RCC components automatically
        saveData.carComponentData = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>();
        
        // Save basic car settings using reflection to avoid RCC type dependencies
        try
        {
            var carType = playerCar.GetType();
            Debug.Log($"[SAVE CAR] Car type: {carType.Name}");
            
            // Debug: List all fields in the car component
            var allFields = carType.GetFields();
            Debug.Log($"[SAVE CAR] Available fields in {carType.Name}:");
            foreach (var field in allFields)
            {
                if (field.FieldType == typeof(float) || field.FieldType == typeof(int))
                {
                    Debug.Log($"[SAVE CAR]   - {field.Name} ({field.FieldType.Name})");
                }
            }
            
            // Get max speed
            var maxspeedField = carType.GetField("maxspeed") ?? carType.GetField("maxSpeed") ?? carType.GetField("topSpeed");
            if (maxspeedField != null)
            {
                saveData.carMaxSpeed = (float)maxspeedField.GetValue(playerCar);
                Debug.Log($"[SAVE CAR] Max Speed: {saveData.carMaxSpeed} (from field: {maxspeedField.Name})");
            }
            else
            {
                Debug.LogWarning("[SAVE CAR] Could not find speed field (tried: maxspeed, maxSpeed, topSpeed)");
                saveData.carMaxSpeed = 240f; // Default
            }
            
            // Get max torque (try multiple field names)
            var maxtorqueField = carType.GetField("maxTorque") ?? carType.GetField("maxMotorTorque") ?? carType.GetField("motorTorque");
            if (maxtorqueField != null)
            {
                saveData.carMaxTorque = (float)maxtorqueField.GetValue(playerCar);
                Debug.Log($"[SAVE CAR] Max Torque: {saveData.carMaxTorque}");
            }
            else
            {
                Debug.LogWarning("[SAVE CAR] Could not find torque field (tried: maxTorque, maxMotorTorque, motorTorque)");
                saveData.carMaxTorque = 2500f; // Default
            }
            
            // Get max brake torque (try multiple field names)
            var maxbrakeField = carType.GetField("maxBrakeTorque") ?? carType.GetField("brakeTorque") ?? carType.GetField("maxBrakeForce");
            if (maxbrakeField != null)
            {
                saveData.carMaxBrakeTorque = (float)maxbrakeField.GetValue(playerCar);
                Debug.Log($"[SAVE CAR] Max Brake Torque: {saveData.carMaxBrakeTorque}");
            }
            else
            {
                Debug.LogWarning("[SAVE CAR] Could not find brake field (tried: maxBrakeTorque, brakeTorque, maxBrakeForce)");
                saveData.carMaxBrakeTorque = 3000f; // Default
            }
            
            // Get max steer angle (try multiple field names)
            var maxsteerField = carType.GetField("maxsteerAngle") ?? carType.GetField("maxSteerAngle") ?? carType.GetField("steerAngle");
            if (maxsteerField != null)
            {
                saveData.carMaxSteerAngle = (float)maxsteerField.GetValue(playerCar);
                Debug.Log($"[SAVE CAR] Max Steer Angle: {saveData.carMaxSteerAngle}");
            }
            else
            {
                Debug.LogWarning("[SAVE CAR] Could not find steer field (tried: maxsteerAngle, maxSteerAngle, steerAngle)");
                saveData.carMaxSteerAngle = 30f; // Default
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SAVE CAR] Error saving car settings: {e.Message}");
            // Set defaults
            saveData.carMaxSpeed = 240f;
            saveData.carMaxTorque = 2500f;
            saveData.carMaxBrakeTorque = 3000f;
            saveData.carMaxSteerAngle = 30f;
        }
        
        // Save car health if damage component exists
        var damageComponent = playerCar.GetComponent<MonoBehaviour>();
        bool foundDamage = false;
        if (damageComponent != null && damageComponent.GetType().Name.Contains("RCC_Damage"))
        {
            try
            {
                var healthField = damageComponent.GetType().GetField("health");
                if (healthField != null)
                {
                    saveData.carHealth = (float)healthField.GetValue(damageComponent);
                    foundDamage = true;
                    Debug.Log($"[SAVE CAR] Car Health: {saveData.carHealth}%");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SAVE CAR] Could not read car health: {e.Message}");
            }
        }
        
        if (!foundDamage)
        {
            saveData.carHealth = 100f; // Default full health
            Debug.Log("[SAVE CAR] No damage component found - using default health (100%)");
        }
        
        Debug.Log($"[SAVE CAR] Car component scan completed successfully");
    }
    #endregion
    
    #region Load Open World State
    void AutoLoadOpenWorldProgress()
    {
        // Check if there's existing save data for this character
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.HasValidPlayer())
        {
            LoadOpenWorldState();
        }
    }
    
    void LoadOpenWorldState()
    {
        Debug.Log("[LOAD] ======== STARTING LOAD PROCESS ========");
        
        OpenWorldSaveData saveData = null;
        
        // Try to load from GameSaveManager first
        if (GameSaveManager.Instance != null)
        {
            Debug.Log("[LOAD] GameSaveManager found - searching for save files...");
            var allSaves = GameSaveManager.Instance.GetAllSaveGames();
            PlayerProfile currentPlayer = GameSaveManager.Instance.GetCurrentPlayer();
            
            Debug.Log($"[LOAD] Found {allSaves.Count} total save files");
            Debug.Log($"[LOAD] Looking for saves for: {currentPlayer.playerName} (Character {currentPlayer.selectedCharacterIndex})");
            
            foreach (var save in allSaves)
            {
                Debug.Log($"[LOAD] Checking save: {save.playerName} (Character {save.characterIndex})");
                if (save.playerName == currentPlayer.playerName && 
                    save.characterIndex == currentPlayer.selectedCharacterIndex)
                {
                    saveData = save as OpenWorldSaveData;
                    Debug.Log($"[LOAD] ✓ Found matching save file!");
                    break;
                }
            }
            
            if (saveData == null)
            {
                Debug.LogWarning("[LOAD] No matching save file found for current character");
            }
        }
        else
        {
            Debug.LogWarning("[LOAD] GameSaveManager not found - trying PlayerPrefs fallback");
        }
        
        // Fallback: Load from PlayerPrefs
        if (saveData == null)
        {
            Debug.Log("[LOAD] Attempting PlayerPrefs fallback...");
            saveData = LoadFromPlayerPrefs();
            if (saveData != null)
            {
                Debug.Log("[LOAD] ✓ Loaded from PlayerPrefs successfully");
            }
            else
            {
                Debug.Log("[LOAD] No PlayerPrefs save data found");
            }
        }
        
        // Apply loaded data
        if (saveData != null)
        {
            Debug.Log("[LOAD] Applying loaded save data...");
            ApplyOpenWorldState(saveData);
            Debug.Log("[LOAD] ======== LOAD COMPLETED SUCCESSFULLY ========");
        }
        else
        {
            Debug.Log("[LOAD] No save data found - setting up new game");
            SetDefaultStartingPosition();
            Debug.Log("[LOAD] ======== NEW GAME SETUP COMPLETED ========");
        }
    }
    
    void ApplyOpenWorldState(OpenWorldSaveData saveData)
    {
        if (playerCar == null) return;
        
        // Check if we need to load a different scene
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (saveData.currentSceneName != currentScene)
        {
            Debug.Log($"Player was in different scene '{saveData.currentSceneName}', current scene is '{currentScene}'");
        }
        
        // Restore position exactly where saved
        playerCar.transform.position = saveData.playerPosition;
        playerCar.transform.rotation = saveData.playerRotation;
        
        // Stop car movement to prevent physics issues
        Rigidbody carRigid = playerCar.GetComponent<Rigidbody>();
        if (carRigid != null)
        {
            carRigid.linearVelocity = Vector3.zero;
            carRigid.angularVelocity = Vector3.zero;
        }
        
        // Restore zone
        currentZone = saveData.currentZone;
        
        // Restore car modifications
        LoadCarModifications(saveData);
        
        // Restore player progress
        playerLevel = saveData.playerLevel;
        totalMoney = saveData.totalMoney;
        totalRacesWon = saveData.totalRacesWon;
        totalRacesCompleted = saveData.totalRacesCompleted;
        
        Debug.Log($"Loaded open world state: Scene '{saveData.currentSceneName}', Zone '{saveData.currentZone}' at {saveData.playerPosition}");
        Debug.Log($"Player stats: Level {playerLevel}, Money ${totalMoney}, Races won {totalRacesWon}/{totalRacesCompleted}");
    }
    
    void LoadCarModifications(OpenWorldSaveData saveData)
    {
        if (playerCar == null) return;
        
        // Restore car performance using reflection
        try
        {
            var carType = playerCar.GetType();
            
            // Set max speed
            var maxspeedField = carType.GetField("maxspeed");
            if (maxspeedField != null)
                maxspeedField.SetValue(playerCar, saveData.carMaxSpeed);
            
            // Set max torque
            var maxtorqueField = carType.GetField("maxTorque");
            if (maxtorqueField != null)
                maxtorqueField.SetValue(playerCar, saveData.carMaxTorque);
            
            // Set max brake torque
            var maxbrakeField = carType.GetField("maxBrakeTorque");
            if (maxbrakeField != null)
                maxbrakeField.SetValue(playerCar, saveData.carMaxBrakeTorque);
            
            // Set max steer angle
            var maxsteerField = carType.GetField("maxsteerAngle");
            if (maxsteerField != null)
                maxsteerField.SetValue(playerCar, saveData.carMaxSteerAngle);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not restore car settings: {e.Message}");
        }
        
        // Restore car health
        var damageComponent = playerCar.GetComponent<MonoBehaviour>();
        if (damageComponent != null && damageComponent.GetType().Name.Contains("RCC_Damage"))
        {
            try
            {
                var healthField = damageComponent.GetType().GetField("health");
                if (healthField != null)
                    healthField.SetValue(damageComponent, saveData.carHealth);
            }
            catch
            {
                Debug.LogWarning("Could not restore car health");
            }
        }
        
        // Stop car movement to prevent physics issues
        Rigidbody carRigid = playerCar.GetComponent<Rigidbody>();
        if (carRigid != null)
        {
            carRigid.linearVelocity = Vector3.zero;
            carRigid.angularVelocity = Vector3.zero;
        }
        
        Debug.Log($"Loaded car modifications");
    }
    
    void SetDefaultStartingPosition()
    {
        // Set default starting position for new players
        if (playerCar != null)
        {
            playerCar.transform.position = Vector3.zero + Vector3.up * 2f;
            playerCar.transform.rotation = Quaternion.identity;
        }
        
        Debug.Log("Set default starting position for new game");
    }
    #endregion
    
    #region Fallback Save/Load (PlayerPrefs)
    void SaveToPlayerPrefs(OpenWorldSaveData saveData)
    {
        PlayerPrefs.SetFloat("PlayerPosX", saveData.playerPosition.x);
        PlayerPrefs.SetFloat("PlayerPosY", saveData.playerPosition.y);
        PlayerPrefs.SetFloat("PlayerPosZ", saveData.playerPosition.z);
        
        PlayerPrefs.SetString("CurrentZone", saveData.currentZone);
        PlayerPrefs.SetInt("PlayerLevel", saveData.playerLevel);
        PlayerPrefs.SetInt("TotalMoney", saveData.totalMoney);
        PlayerPrefs.SetInt("RacesWon", saveData.totalRacesWon);
        PlayerPrefs.SetInt("RacesCompleted", saveData.totalRacesCompleted);
        
        PlayerPrefs.Save();
    }
    
    OpenWorldSaveData LoadFromPlayerPrefs()
    {
        if (!PlayerPrefs.HasKey("PlayerPosX")) return null;
        
        OpenWorldSaveData saveData = new OpenWorldSaveData();
        
        saveData.playerPosition = new Vector3(
            PlayerPrefs.GetFloat("PlayerPosX"),
            PlayerPrefs.GetFloat("PlayerPosY"),
            PlayerPrefs.GetFloat("PlayerPosZ")
        );
        
        saveData.currentZone = PlayerPrefs.GetString("CurrentZone", "Downtown");
        saveData.playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        saveData.totalMoney = PlayerPrefs.GetInt("TotalMoney", 1000);
        saveData.totalRacesWon = PlayerPrefs.GetInt("RacesWon", 0);
        saveData.totalRacesCompleted = PlayerPrefs.GetInt("RacesCompleted", 0);
        
        return saveData;
    }
    #endregion
    
    #region Utility Methods
    void ShowSaveMessage(string message)
    {
        if (saveStatusText != null)
        {
            saveStatusText.text = message;
            Invoke(nameof(ClearSaveMessage), 3f);
        }
    }
    
    void ClearSaveMessage()
    {
        if (saveStatusText != null)
            saveStatusText.text = "";
    }
    
    public void AddMoney(int amount)
    {
        totalMoney += amount;
        Debug.Log($"Added ${amount}. Total: ${totalMoney}");
    }
    
    public bool SpendMoney(int amount)
    {
        if (totalMoney >= amount)
        {
            totalMoney -= amount;
            Debug.Log($"Spent ${amount}. Remaining: ${totalMoney}");
            return true;
        }
        return false;
    }
    
    public void SetZone(string zone)
    {
        currentZone = zone;
        Debug.Log($"Entered zone: {zone}");
    }
    
    string GetCarModelName(GameObject carObject)
    {
        return carObject.name.Replace("(Clone)", "").Trim();
    }
    
    int GetCarPrefabIndex(GameObject carObject)
    {
        string carName = GetCarModelName(carObject);
        for (int i = 0; i < carModelNames.Length; i++)
        {
            if (carModelNames[i] == carName)
                return i;
        }
        return 0;
    }
    
    bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            if (sceneNameFromPath == sceneName)
            {
                return true;
            }
        }
        return false;
    }
    #endregion
    
    #region Public Accessors
    public bool CanSave => !isInRace;
    public int GetMoney() => totalMoney;
    public int GetLevel() => playerLevel;
    public int GetRacesWon() => totalRacesWon;
    public string GetCurrentZone() => currentZone;
    public string GetCurrentCarModel() => playerCar?.name ?? "Unknown";
    #endregion
}

// Extended save data for open world
[System.Serializable]
public class OpenWorldSaveData : GameSaveData
{
    [Header("Scene Data")]
    public string currentSceneName;       // Which scene player was in
    public int currentSceneIndex;         // Scene build index
    
    [Header("Open World Data")]
    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public string currentZone;
    
    [Header("Car Auto-Save Data")]
    public string currentCarModel;         // Name of the car model
    public int currentCarPrefabIndex;      // Index in prefabs array
    public float carMaxSpeed;
    public float carMaxTorque;
    public float carMaxBrakeTorque;
    public float carMaxSteerAngle;
    public float carHealth = 100f;
    public Color carColor = Color.white;
    public System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>> carComponentData;
    
    [Header("Player Progress")]
    public new int playerLevel;        // Using 'new' to hide inherited member intentionally
    public int totalMoney;
    public int totalRacesWon;
    public int totalRacesCompleted;
    public float playTime;
    
    public OpenWorldSaveData() : base()
    {
        currentSceneName = "";
        currentSceneIndex = -1;
        
        playerPosition = Vector3.zero;
        playerRotation = Quaternion.identity;
        currentZone = "Downtown";
        currentCarModel = "Default";
        currentCarPrefabIndex = 0;
        
        // Default RCC car stats
        carMaxSpeed = 240f;
        carMaxTorque = 2500f;
        carMaxBrakeTorque = 3000f;
        carMaxSteerAngle = 30f;
        carHealth = 100f;
        carColor = Color.white;
        carComponentData = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>();
        
        playerLevel = 1;
        totalMoney = 1000;
        totalRacesWon = 0;
        totalRacesCompleted = 0;
        playTime = 0f;
    }
}