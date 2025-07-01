using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MapSelectionManager : MonoBehaviour
{
    [Header("Map Data")]
    public MapSelectionData[] maps; // Array of all available maps
    
    [Header("Navigation")]
    public Button nextButton; // "NEXT" button
    
    [Header("Selection Colors")]
    public Color selectedBorderColor = Color.cyan;     // Color for selected border
    public Color unselectedBorderColor = Color.white;  // Color for unselected borders
    
    // Private variables
    private int selectedMapIndex = 0;
    
    void Start()
    {
        SetupMapSelection();
        
        // Ensure we start with first map selected (index 0)
        if (maps.Length > 0)
        {
            SelectMap(0); // Start with first map selected
        }
        
        // Setup next button
        nextButton.onClick.AddListener(ProceedWithSelectedMap);
    }
    
    void SetupMapSelection()
    {
        for (int i = 0; i < maps.Length; i++)
        {
            int mapIndex = i; // Capture for closure
            
            // Set default names if they're still "New Map"
            if (maps[i].mapName == "New Map" || string.IsNullOrEmpty(maps[i].mapName))
            {
                maps[i].mapName = $"Map {i + 1}"; // Map 1, Map 2, Map 3, etc.
            }
            
            // Get the button component from the icon button
            if (maps[i].iconButton == null)
            {
                Debug.LogWarning($"Map {i} ({maps[i].mapName}) has no icon button assigned!");
                continue;
            }
            
            Button iconButton = maps[i].iconButton.GetComponent<Button>();
            
            if (iconButton == null)
            {
                Debug.LogError($"Map {i} ({maps[i].mapName}) icon doesn't have a Button component!");
                continue;
            }
            
            // Setup click listener on the icon button
            iconButton.onClick.AddListener(() => SelectMap(mapIndex));
            
            Debug.Log($"Map {i} ({maps[i].mapName}) setup complete");
        }
    }
    
    public void SelectMap(int mapIndex)
    {
        // Validate map index
        if (mapIndex < 0 || mapIndex >= maps.Length)
        {
            Debug.LogError($"Invalid map index: {mapIndex}");
            return;
        }
        
        // Update selected index
        selectedMapIndex = mapIndex;
        
        // Update visual displays
        UpdateBorderColors();
        UpdateMapInfoVisibility();
        
        Debug.Log($"Selected Map: {maps[mapIndex].mapName}");
    }
    
    void UpdateBorderColors()
    {
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i].border == null) continue;
            
            // Get the image component of the border
            Image borderImage = maps[i].border.GetComponent<Image>();
            if (borderImage != null)
            {
                // Set border color based on selection
                borderImage.color = (i == selectedMapIndex) ? selectedBorderColor : unselectedBorderColor;
            }
        }
    }
    
    void UpdateMapInfoVisibility()
    {
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i].mapInfoFolder == null) continue;
            
            // Show map info only for selected map, hide others
            maps[i].mapInfoFolder.SetActive(i == selectedMapIndex);
        }
    }
    
    void ProceedWithSelectedMap()
    {
        // Save selected map data
        SaveSelectedMap();
        
        // Load next scene (replace with your actual scene name)
        SceneManager.LoadScene("GameScene"); // Change to your game scene name
        
        Debug.Log($"Proceeding with map: {maps[selectedMapIndex].mapName}");
    }
    
    void SaveSelectedMap()
    {
        MapSelectionData selectedMap = maps[selectedMapIndex];
        
        // Save to PlayerPrefs
        PlayerPrefs.SetString("SelectedMapName", selectedMap.mapName);
        PlayerPrefs.SetInt("SelectedMapIndex", selectedMapIndex);
        PlayerPrefs.SetString("SelectedMapSceneName", selectedMap.sceneToLoad);
        PlayerPrefs.Save();
        
        Debug.Log($"Saved map selection: {selectedMap.mapName}");
    }
    
    // Optional: Load previously selected map
    public void LoadPreviousSelection()
    {
        if (PlayerPrefs.HasKey("SelectedMapIndex"))
        {
            int savedIndex = PlayerPrefs.GetInt("SelectedMapIndex");
            if (savedIndex >= 0 && savedIndex < maps.Length)
            {
                SelectMap(savedIndex);
            }
        }
    }
    
    // Public methods for external use
    public MapSelectionData GetSelectedMap()
    {
        return maps[selectedMapIndex];
    }
    
    public int GetSelectedMapIndex()
    {
        return selectedMapIndex;
    }
    
    public string GetSelectedMapName()
    {
        return maps[selectedMapIndex].mapName;
    }
    
    // Utility method to select map by name
    public void SelectMapByName(string mapName)
    {
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i].mapName.Equals(mapName, System.StringComparison.OrdinalIgnoreCase))
            {
                SelectMap(i);
                return;
            }
        }
        Debug.LogWarning($"Map with name '{mapName}' not found!");
    }
    
    // Editor utility - refresh visuals in play mode
    [ContextMenu("Refresh Map Selection")]
    public void RefreshMapSelection()
    {
        UpdateBorderColors();
        UpdateMapInfoVisibility();
    }
    
    // Editor utility - test map selection
    [ContextMenu("Test Map 1")]
    public void TestMap1() { SelectMap(0); }
    
    [ContextMenu("Test Map 2")]
    public void TestMap2() { SelectMap(1); }
    
    [ContextMenu("Test Map 3")]
    public void TestMap3() { SelectMap(2); }
}

[System.Serializable]
public class MapSelectionData
{
    [Header("Map Info")]
    public string mapName = "New Map";        // Default name for new maps
    
    [Header("UI Elements")]
    public GameObject iconButton;             // The clickable icon/button
    public GameObject border;                 // Border that changes color when selected
    public GameObject mapInfoFolder;          // Entire map info folder (shows/hides)
    
    [Header("Game Data")]
    public string sceneToLoad = "GameScene";  // Default scene name
    public int maxPlayers = 4;                // Optional: Max players for this map
}