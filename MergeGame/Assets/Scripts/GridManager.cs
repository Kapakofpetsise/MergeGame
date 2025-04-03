using UnityEngine;

// A simple class to hold information about each slot in the grid
[System.Serializable] // Makes it visible in the Inspector (optional)
public class GridSlot
{
    public Vector2 position; // World position of the center of the slot
    public MergeItem currentItem = null; // Reference to the item currently in this slot (null if empty)
    public bool isOccupied => currentItem != null; // Convenience property

    public GridSlot(Vector2 pos)
    {
        position = pos;
        currentItem = null;
    }
}

public class GridManager : MonoBehaviour
{
    // --- Static Instance ---
    public static GridManager Instance { get; private set; }
    // ---------------------
    
    [Header("Grid Dimensions")]
    public int width = 5; // Number of columns
    public int height = 5; // rows
    public float cellSize = 1.5f; // Size of each grid cell
    public Vector2 gridOrigin; // The bottom-left corner of the grid

    [Header("Item Spawning")]
    // We need the prefab to create new items
    public GameObject mergeItemPrefab;
    // We need a definition for the item we want to spawn initially
    public ItemDefinition initialItemDefinition;

    // 2D array to hold the data for each slot
    private GridSlot[,] gridSlots;
    private Vector2 gridCellOffset; // Store calculated offset

    void Awake()
    {
        // --- Singleton Pattern ---
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple GridManagers found! Destroying this one.");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        // -----------------------
        
        gridCellOffset = new Vector2(cellSize / 2.0f, cellSize / 2.0f); // Calculate offset once
        InitializeGrid();
    }

    void Start()
    {
         // --- Modify Test Spawning ---
         // Spawn two items side-by-side for easy merge testing
         if (initialItemDefinition != null && mergeItemPrefab != null)
         {
             SpawnItemAt(initialItemDefinition, 0, 0);
             SpawnItemAt(initialItemDefinition, 1, 0); // Spawn a second one next to it
             // SpawnItemAt(initialItemDefinition, 0, 1); // Maybe another for testing non-merges
         }
         else
         {
             Debug.LogError("GridManager is missing MergeItem Prefab or Initial Item Definition!");
         }
         // -----------------------------
    }


    void InitializeGrid()
    {
        gridSlots = new GridSlot[width, height];
        Vector2 offset = new Vector2(cellSize / 2.0f, cellSize / 2.0f); // Offset to center items in cells

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Calculate the world position for the center of the cell
                Vector2 slotPosition = gridOrigin + new Vector2(x * cellSize, y * cellSize) + offset;
                gridSlots[x, y] = new GridSlot(slotPosition);
            }
        }
        Debug.Log($"Grid initialized with {width}x{height} slots.");
    }

    // Method to spawn a new item at a specific grid coordinate
    // --- Modified SpawnItemAt ---
    public MergeItem SpawnItemAt(ItemDefinition itemDef, int x, int y)
    {
        if (!IsValidCoord(x, y)) return null; // Use helper function

        GridSlot targetSlot = gridSlots[x, y];
        if (targetSlot.isOccupied)
        {
             // If slot is occupied maybe we should handle overwrite? For now, just warn and fail.
            Debug.LogWarning($"Grid slot ({x},{y}) is already occupied. Cannot spawn.");
            return null;
        }

        if (mergeItemPrefab == null) { /* ... error log ... */ return null; }

        // Inside GridManager.SpawnItemAt:
        Vector2 slotPosition = gridSlots[x, y].position; // Gets the correct position
        // Instantiates the prefab AT that position:
        GameObject newItemObject = Instantiate(mergeItemPrefab, slotPosition, Quaternion.identity, this.transform);
        MergeItem mergeItem = newItemObject.GetComponent<MergeItem>();

        if (mergeItem != null)
        {
            mergeItem.Initialize(itemDef, x, y); // Pass coordinates to the item
            targetSlot.currentItem = mergeItem;
            Debug.Log($"Spawned item '{itemDef.displayName}' at ({x},{y})");
            return mergeItem;
        }
        // ... error handling ...
        return null;
    }

    // --- New Helper Functions ---

    // Check if grid coordinates are valid
    public bool IsValidCoord(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
    // Convert world position to grid coordinates
    public Vector2Int? WorldToGridCoords(Vector3 worldPosition)
    {
        // Adjust position relative to grid origin
        Vector3 relativePos = worldPosition - (Vector3)gridOrigin;

        // Calculate grid indices
        int x = Mathf.FloorToInt(relativePos.x / cellSize);
        int y = Mathf.FloorToInt(relativePos.y / cellSize);

        // Check if the calculated coords are within bounds
        if (IsValidCoord(x, y))
        {
            return new Vector2Int(x, y);
        }
        else
        {
            return null; // Return null if outside grid bounds
        }
    }

    // Get the item currently at specified grid coordinates
    public MergeItem GetItemAt(int x, int y)
    {
        if (IsValidCoord(x, y))
        {
            return gridSlots[x, y].currentItem;
        }
        return null; // Return null if coords are invalid
    }

    // Clear the item reference from a specific grid slot
    public void ClearSlotAt(int x, int y)
    {
        if (IsValidCoord(x, y))
        {
            gridSlots[x, y].currentItem = null;
             Debug.Log($"Cleared slot ({x},{y})");
        }
    }
    // ------------------------

    // --- Gizmos for Editor Visualization ---
    void OnDrawGizmos()
    {
        if (!Application.isPlaying && gridSlots == null)
        {
            // Draw preview grid even before playing if dimensions are set
            Vector2 offset = new Vector2(cellSize / 2.0f, cellSize / 2.0f);
             for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                     Vector2 slotPosition = gridOrigin + new Vector2(x * cellSize, y * cellSize) + offset;
                     Gizmos.color = Color.grey;
                     Gizmos.DrawWireCube(slotPosition, new Vector3(cellSize, cellSize, 0.1f));
                }
            }
        }
        else if (gridSlots != null) // Draw during play mode using initialized data
        {
             Gizmos.color = Color.yellow;
             for (int x = 0; x < width; x++)
             {
                 for (int y = 0; y < height; y++)
                 {
                      // Draw slightly smaller cubes using calculated positions
                      Gizmos.DrawWireCube(gridSlots[x, y].position, new Vector3(cellSize * 0.95f, cellSize * 0.95f, 0.1f));
                 }
             }
        }

        // Draw origin
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(gridOrigin, cellSize * 0.1f);
    }
    // ------------------------------------
}
