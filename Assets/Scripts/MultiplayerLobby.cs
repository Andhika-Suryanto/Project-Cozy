using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class MultiplayerLobby : MonoBehaviour
{
    [Header("Player Boxes")]
    public PlayerBox[] playerBoxes = new PlayerBox[4];
    
    [Header("Player Colors")]
    public Color[] playerColors = { Color.yellow, Color.blue, Color.green, Color.magenta };
    
    private List<PlayerData> connectedPlayers = new List<PlayerData>();
    private PlayerInputManager playerInputManager;
    
    void Start()
    {
        // Set up Player Input Manager
        playerInputManager = FindFirstObjectByType<PlayerInputManager>();
        if (playerInputManager == null)
        {
            GameObject manager = new GameObject("PlayerInputManager");
            playerInputManager = manager.AddComponent<PlayerInputManager>();
        }
        
        // Configure input manager - these are set in the Inspector
        // playerInputManager.maxPlayerCount and joiningEnabled are read-only in Unity 6000
        // Set these values in the PlayerInputManager component Inspector instead
        
        // Enable device joining
        playerInputManager.EnableJoining();
        
        // Listen for player join events
        playerInputManager.onPlayerJoined += OnPlayerJoined;
        
        InitializePlayerBoxes();
    }
    
    void InitializePlayerBoxes()
    {
        for (int i = 0; i < playerBoxes.Length; i++)
        {
            playerBoxes[i].SetupBox(i + 1, playerColors[i]);
        }
    }
    
    void OnPlayerJoined(PlayerInput playerInput)
    {
        // Find next available slot
        int playerIndex = GetNextAvailableSlot();
        if (playerIndex == -1) return; // No slots available
        
        // Create player data
        PlayerData newPlayer = new PlayerData
        {
            playerInput = playerInput,
            playerIndex = playerIndex,
            playerColor = playerColors[playerIndex],
            controllerId = playerInput.devices[0].deviceId
        };
        
        // Add to connected players
        connectedPlayers.Add(newPlayer);
        
        // Update UI
        playerBoxes[playerIndex].AssignPlayer(newPlayer);
        
        // Set up input callbacks for this specific player
        SetupPlayerInputCallbacks(playerInput, newPlayer);
        
        // Make this player persistent across scenes
        DontDestroyOnLoad(playerInput.gameObject);
        
        Debug.Log($"Player {playerIndex + 1} joined with {playerInput.devices[0].displayName}");
    }
    
    void SetupPlayerInputCallbacks(PlayerInput playerInput, PlayerData playerData)
    {
        // Get the input action for confirm button (A/X)
        var confirmAction = playerInput.actions["Confirm"];
        if (confirmAction != null)
        {
            confirmAction.performed += (ctx) => OnPlayerConfirm(playerData);
        }
        
        // Get the input action for cancel button (B/Circle)
        var cancelAction = playerInput.actions["Cancel"];
        if (cancelAction != null)
        {
            cancelAction.performed += (ctx) => OnPlayerCancel(playerData);
        }
    }
    
    void OnPlayerConfirm(PlayerData playerData)
    {
        // Handle player confirmation (ready up, etc.)
        playerBoxes[playerData.playerIndex].SetPlayerReady(true);
        Debug.Log($"Player {playerData.playerIndex + 1} is ready!");
        
        // Check if all players are ready
        CheckAllPlayersReady();
    }
    
    void OnPlayerCancel(PlayerData playerData)
    {
        // Handle player leaving
        playerBoxes[playerData.playerIndex].RemovePlayer();
        connectedPlayers.Remove(playerData);
        
        // Destroy the player input
        if (playerData.playerInput != null)
        {
            Destroy(playerData.playerInput.gameObject);
        }
        
        Debug.Log($"Player {playerData.playerIndex + 1} left");
    }
    
    int GetNextAvailableSlot()
    {
        for (int i = 0; i < playerBoxes.Length; i++)
        {
            if (!playerBoxes[i].IsOccupied)
            {
                return i;
            }
        }
        return -1; // No available slots
    }
    
    void CheckAllPlayersReady()
    {
        if (connectedPlayers.Count < 2) return; // Need at least 2 players
        
        bool allReady = true;
        foreach (var player in connectedPlayers)
        {
            if (!playerBoxes[player.playerIndex].IsReady)
            {
                allReady = false;
                break;
            }
        }
        
        if (allReady)
        {
            StartGame();
        }
    }
    
    void StartGame()
    {
        // Save player data for next scene
        GameManager.Instance.SetPlayerData(connectedPlayers);
        
        // Load game scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }
}

[System.Serializable]
public class PlayerData
{
    public PlayerInput playerInput;
    public int playerIndex;
    public Color playerColor;
    public int controllerId;
    public bool isReady;
}

[System.Serializable]
public class PlayerBox
{
    [Header("UI References")]
    public GameObject numberUI;        // The "1", "2", "3", "4" UI
    public GameObject colorWheelUI;    // The Color Wheel UI (Yellow, Blue, Green, Pink)
    public GameObject pressToJoinUI;   // The "Press to Join" UI
    
    private bool isOccupied = false;
    private bool isReady = false;
    private PlayerData assignedPlayer;
    
    public bool IsOccupied => isOccupied;
    public bool IsReady => isReady;
    
    public void SetupBox(int playerNumber, Color color)
    {
        // Show number and press to join UI, hide color wheel initially
        numberUI.SetActive(true);
        colorWheelUI.SetActive(false);
        
        // Show "Press to Join" only if this UI exists
        if (pressToJoinUI != null)
        {
            pressToJoinUI.SetActive(true);
        }
    }
    
    public void AssignPlayer(PlayerData playerData)
    {
        assignedPlayer = playerData;
        isOccupied = true;
        
        // Switch from number to color wheel and hide "Press to Join"
        numberUI.SetActive(false);
        colorWheelUI.SetActive(true);
        
        // Hide "Press to Join" when player joins
        if (pressToJoinUI != null)
        {
            pressToJoinUI.SetActive(false);
        }
    }
    
    public void SetPlayerReady(bool ready)
    {
        isReady = ready;
        
        // Visual feedback for ready state (optional)
        // You can add effects to the color wheel here if needed
    }
    
    public void RemovePlayer()
    {
        assignedPlayer = null;
        isOccupied = false;
        isReady = false;
        
        // Switch back to number UI and show "Press to Join" again
        numberUI.SetActive(true);
        colorWheelUI.SetActive(false);
        
        // Show "Press to Join" again when player leaves
        if (pressToJoinUI != null)
        {
            pressToJoinUI.SetActive(true);
        }
    }
}

// Singleton to persist player data across scenes
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    private List<PlayerData> playerData = new List<PlayerData>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void SetPlayerData(List<PlayerData> players)
    {
        playerData = players;
    }
    
    public List<PlayerData> GetPlayerData()
    {
        return playerData;
    }
    
    public Color GetPlayerColor(int playerIndex)
    {
        if (playerIndex < playerData.Count)
        {
            return playerData[playerIndex].playerColor;
        }
        return Color.white;
    }
}