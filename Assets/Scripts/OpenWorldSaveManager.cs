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
    
    [Header("Loading Settings")]
    public float loadDelay = 1f;               // Delay before loading save data
    public bool waitForCarInitialization = true; // Wait for car to be fully initialized
    
    [Header("Open World State")]
    public string currentZone = "Downtown";    // Current area/district
    public int playerLevel = 1;
    public int totalMoney = 1000;
    public int totalRacesWon = 0;
    public int totalRacesCompleted = 0;
    
    private bool isInRace = false;
    private OpenWorldSaveData currentSave;
    private bool hasLoadedSaveData = false;
    private Camera originalCamera;
    
    void Start()
    {
        // Store original camera reference
        originalCamera = Camera.main;
        
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
        
        // Delay the auto-load to let everything initialize first
        if (waitForCarInitialization)
        {
            Invoke(nameof(DelayedAutoLoad), loadDelay);
        }
        else
        {
            AutoLoadOpenWorldProgress();
        }
    }
    
    void DelayedAutoLoad()
    {
        // Re-detect car in case it wasn't ready in Start()
        if (playerCar == null)
        {
            AutoDetectPlayerCar();
        }
        
        // Now auto-load
        AutoLoadOpenWorldProgress();
    }
    
    void Update()
    {
        // Quick save hotkey (only in open world)
        if (!isInRace && Input.GetKeyDown(saveHotkey))
        {
            ManualSaveGame();
        }
        
        // Re-detect car if it's missing (in case it gets spawned later)
        if (playerCar == null && Time.time > 2f) // Wait 2 seconds before trying again
        {
            AutoDetectPlayerCar();
        }
    }
    
    void AutoDetectPlayerCar()
    {
        Debug.Log("[CAR DETECT] Starting car detection...");
        
        // Method 1: Find car with player tag
        GameObject carWithPlayerTag = GameObject.FindGameObjectWithTag("Player");
        if (carWithPlayerTag != null)
        {
            Debug.Log($"[CAR DETECT] Found GameObject with Player tag: {carWithPlayerTag.name}");
            
            // Check if it has RCC component
            MonoBehaviour[] components = carWithPlayerTag.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component.GetType().Name.Contains("RCC"))
                {
                    playerCar = component;
                    Debug.Log($"[CAR DETECT] ✓ Auto-detected player car: {playerCar.name} (Component: {component.GetType().Name})");
                    return;
                }
            }
            
            // If no RCC component on the tagged object, check children
            MonoBehaviour[] childComponents = carWithPlayerTag.GetComponentsInChildren<MonoBehaviour>();
            foreach (var component in childComponents)
            {
                if (component.GetType().Name.Contains("RCC"))
                {
                    playerCar = component;
                    Debug.Log($"[CAR DETECT] ✓ Auto-detected player car in children: {playerCar.name} (Component: {component.GetType().Name})");
                    return;
                }
            }
        }
        
        // Method 2: Find any RCC car in scene
        Debug.Log("[CAR DETECT] No Player-tagged car found, searching for any RCC car...");
        MonoBehaviour[] allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var component in allComponents)
        {
            if (component.GetType().Name.Contains("RCC_CarController"))
            {
                playerCar = component;
                Debug.Log($"[CAR DETECT] ✓ Auto-assigned RCC car found: {playerCar.name} (Component: {component.GetType().Name})");
                return;
            }
        }
        
        Debug.LogWarning("[CAR DETECT] No RCC car found in scene! Please assign manually or ensure car is spawned.");
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
        bool wasOverwrite = CheckIfSaveExists();
        SaveOpenWorldState();
        
        if (wasOverwrite)
        {
            ShowSaveMessage("Progress Auto-Saved (Overwritten)!");
        }
        else
        {
            ShowSaveMessage("Progress Auto-Saved!");
        }
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
        
        // Check if save already exists for this character
        bool willOverwrite = CheckIfSaveExists();
        
        SaveOpenWorldState();
        
        if (willOverwrite)
        {
            ShowSaveMessage($"Game Overwritten! ({saveHotkey} to quick save)");
            Debug.Log("[SAVE] Existing save overwritten successfully!");
        }
        else
        {
            ShowSaveMessage($"Game Saved! ({saveHotkey} to quick save)");
            Debug.Log("[SAVE] New save created successfully!");
        }
    }
    
    private bool CheckIfSaveExists()
    {
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.HasValidPlayer())
        {
            PlayerProfile player = GameSaveManager.Instance.GetCurrentPlayer();
            OpenWorldSaveData existingSave = LoadExistingSaveForCharacter(player.playerName, player.selectedCharacterIndex);
            return existingSave != null;
        }
        return false;
    }
    
    // Direct method to load OpenWorldSaveData for a specific character
    private OpenWorldSaveData LoadExistingSaveForCharacter(string playerName, int characterIndex)
    {
        if (GameSaveManager.Instance == null) return null;
        
        string baseFileName = $"{SanitizeFileName(playerName)}_Char{characterIndex}";
        string fileName = $"{baseFileName}.save";
        string path = System.IO.Path.Combine(Application.persistentDataPath, "SaveGames", fileName);
        
        if (System.IO.File.Exists(path))
        {
            try
            {
                string json = System.IO.File.ReadAllText(path);
                Debug.Log($"[LOAD CHECK] Found save file: {fileName}");
                
                // Try to deserialize as OpenWorldSaveData
                OpenWorldSaveData saveData = JsonUtility.FromJson<OpenWorldSaveData>(json);
                saveData.saveFilePath = path;
                
                Debug.Log($"[LOAD CHECK] Successfully parsed OpenWorldSaveData for {saveData.playerName}");
                return saveData;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LOAD CHECK] Failed to load save file {path}: {e.Message}");
            }
        }
        else
        {
            Debug.Log($"[LOAD CHECK] No save file found at: {path}");
        }
        
        return null;
    }
    
    private string SanitizeFileName(string fileName)
    {
        // Remove invalid file name characters
        string invalid = new string(System.IO.Path.GetInvalidFileNameChars()) + new string(System.IO.Path.GetInvalidPathChars());
        foreach (char c in invalid)
        {
            fileName = fileName.Replace(c.ToString(), "");
        }
        return fileName.Trim();
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
        Debug.Log($"[SAVE] Current car position: {playerCar.transform.position}");
        Debug.Log($"[SAVE] Current car rotation: {playerCar.transform.rotation.eulerAngles}");
        
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
        
        // Don't load if we already loaded save data this session
        if (hasLoadedSaveData)
        {
            Debug.Log("[LOAD] Save data already loaded this session, skipping...");
            return;
        }
        
        OpenWorldSaveData saveData = null;
        
        // Try to load directly using the fixed method
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.HasValidPlayer())
        {
            PlayerProfile currentPlayer = GameSaveManager.Instance.GetCurrentPlayer();
            
            Debug.Log($"[LOAD] Looking for save for: {currentPlayer.playerName} (Character {currentPlayer.selectedCharacterIndex})");
            
            // Use our direct loading method instead of going through GetAllSaveGames
            saveData = LoadExistingSaveForCharacter(currentPlayer.playerName, currentPlayer.selectedCharacterIndex);
            
            if (saveData != null)
            {
                Debug.Log($"[LOAD] ✓ Found matching save file for {saveData.playerName}!");
            }
            else
            {
                Debug.Log("[LOAD] No save file found for current character - this is normal for new characters");
            }
        }
        else
        {
            Debug.LogWarning("[LOAD] GameSaveManager not found or no valid player");
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
            hasLoadedSaveData = true; // Mark as loaded
            Debug.Log("[LOAD] ======== LOAD COMPLETED SUCCESSFULLY ========");
        }
        else
        {
            Debug.Log("[LOAD] No save data found - setting up new game");
            SetDefaultStartingPosition();
            hasLoadedSaveData = true; // Mark as processed
            Debug.Log("[LOAD] ======== NEW GAME SETUP COMPLETED ========");
        }
    }
    
    void ApplyOpenWorldState(OpenWorldSaveData saveData)
    {
        if (playerCar == null) 
        {
            Debug.LogWarning("[LOAD] Cannot apply save data - no player car found!");
            return;
        }
        
        Debug.Log($"[LOAD] Applying save data...");
        Debug.Log($"[LOAD] Car position before load: {playerCar.transform.position}");
        Debug.Log($"[LOAD] Save data position: {saveData.playerPosition}");
        
        // Check if we need to load a different scene
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (saveData.currentSceneName != currentScene)
        {
            Debug.Log($"Player was in different scene '{saveData.currentSceneName}', current scene is '{currentScene}'");
        }
        
        // Stop car movement BEFORE moving it
        Rigidbody carRigid = playerCar.GetComponent<Rigidbody>();
        if (carRigid != null)
        {
            carRigid.linearVelocity = Vector3.zero;
            carRigid.angularVelocity = Vector3.zero;
            carRigid.Sleep(); // Force rigidbody to sleep
        }
        
        // Wait a frame then restore position
        StartCoroutine(RestoreCarPosition(saveData));
        
        // Restore zone
        currentZone = saveData.currentZone;
        
        // Restore car modifications
        LoadCarModifications(saveData);
        
        // Restore player progress
        playerLevel = saveData.playerLevel;
        totalMoney = saveData.totalMoney;
        totalRacesWon = saveData.totalRacesWon;
        totalRacesCompleted = saveData.totalRacesCompleted;
        
        Debug.Log($"[LOAD] Loaded open world state: Scene '{saveData.currentSceneName}', Zone '{saveData.currentZone}'");
        Debug.Log($"[LOAD] Player stats: Level {playerLevel}, Money ${totalMoney}, Races won {totalRacesWon}/{totalRacesCompleted}");
    }
    
    System.Collections.IEnumerator RestoreCarPosition(OpenWorldSaveData saveData)
    {
        // Wait a few frames to ensure everything is initialized
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        
        if (playerCar != null)
        {
            Debug.Log($"[LOAD] Restoring car position to: {saveData.playerPosition}");
            
            // Restore position exactly where saved
            playerCar.transform.position = saveData.playerPosition;
            playerCar.transform.rotation = saveData.playerRotation;
            
            // Stop car movement again after positioning
            Rigidbody carRigid = playerCar.GetComponent<Rigidbody>();
            if (carRigid != null)
            {
                carRigid.linearVelocity = Vector3.zero;
                carRigid.angularVelocity = Vector3.zero;
            }
            
            Debug.Log($"[LOAD] Car position after restore: {playerCar.transform.position}");
        }
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
        
        Debug.Log($"[LOAD] Loaded car modifications");
    }
    
    void SetDefaultStartingPosition()
    {
        // Set default starting position for new players
        if (playerCar != null)
        {
            Vector3 defaultPos = Vector3.zero + Vector3.up * 2f;
            playerCar.transform.position = defaultPos;
            playerCar.transform.rotation = Quaternion.identity;
            Debug.Log($"[LOAD] Set default starting position: {defaultPos}");
        }
        else
        {
            Debug.LogWarning("[LOAD] Cannot set default position - no player car found");
        }
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
            
            // Different display times based on message type
            if (message.Contains("Overwritten"))
            {
                Invoke(nameof(ClearSaveMessage), 4f); // Show overwrite message longer
            }
            else
            {
                Invoke(nameof(ClearSaveMessage), 3f);
            }
        }
        
        Debug.Log($"[SAVE MESSAGE] {message}");
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
    
    // Public method to force reload save data (useful for testing)
    [ContextMenu("Force Reload Save Data")]
    public void ForceReloadSaveData()
    {
        hasLoadedSaveData = false;
        LoadOpenWorldState();
    }
    
    // Public method to reset car position to default
    [ContextMenu("Reset Car to Default Position")]
    public void ResetCarToDefault()
    {
        SetDefaultStartingPosition();
    }
    #endregion
    
    #region Public Accessors
    public bool CanSave => !isInRace;
    public int GetMoney() => totalMoney;
    public int GetLevel() => playerLevel;
    public int GetRacesWon() => totalRacesWon;
    public string GetCurrentZone() => currentZone;
    public string GetCurrentCarModel() => playerCar?.name ?? "Unknown";
    public bool HasLoadedSaveData => hasLoadedSaveData;
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