using UnityEngine;

// A simple class to hold information about each slot in the grid
[System.Serializable]
public class GridSlot
{
    public Vector2 position;
    public MergeItem currentItem = null;
    public bool isOccupied => currentItem != null;

    public GridSlot(Vector2 pos)
    {
        position = pos;
        currentItem = null;
    }
}

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }


    [Header("Grid Dimensions")]
    public int width = 5;
    public int height = 5;
    public float cellSize = 1.5f;
    public Vector2 gridOrigin;

    [Header("Item Spawning")]
    public GameObject mergeItemPrefab;
    public ItemDefinition initialItemDefinition;

    private GridSlot[,] gridSlots;
    private Vector2 gridCellOffset;

    void Awake()
    {
        //Singleton
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple GridManagers found! Destroying this one.");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;


        gridCellOffset = new Vector2(cellSize / 2.0f, cellSize / 2.0f); // Calculate offset once
        InitializeGrid();
    }

    void Start()
    {
        if (initialItemDefinition != null && mergeItemPrefab != null)
        {
            SpawnItemAt(initialItemDefinition, 0, 0);
            SpawnItemAt(initialItemDefinition, 1, 0);
            SpawnItemAt(initialItemDefinition, 2, 0);
            SpawnItemAt(initialItemDefinition, 3, 0);
            SpawnItemAt(initialItemDefinition, 4, 0);
        }
        else
        {
            Debug.LogError("GridManager is missing MergeItem Prefab or Initial Item Definition!");
        }
    }

    void InitializeGrid()
    {
        gridSlots = new GridSlot[width, height];
        Vector2 offset = new Vector2(cellSize / 2.0f, cellSize / 2.0f); // Offset to center items in cells

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 slotPosition = gridOrigin + new Vector2(x * cellSize, y * cellSize) + offset;
                gridSlots[x, y] = new GridSlot(slotPosition);
            }
        }
        Debug.Log($"Grid initialized with {width}x{height} slots.");
    }


    public MergeItem SpawnItemAt(ItemDefinition itemDef, int x, int y)
    {
        if (!IsValidCoord(x, y)) return null;

        GridSlot targetSlot = gridSlots[x, y];
        if (targetSlot.isOccupied)
        {
            // If slot is occupied maybe we should handle overwrite? For now, just warn and fail.
            Debug.LogWarning($"Grid slot ({x},{y}) is already occupied. Cannot spawn.");
            return null;
        }

        if (mergeItemPrefab == null) { /* ... error log ... */ return null; }

        Vector2 slotPosition = gridSlots[x, y].position;
        GameObject newItemObject = Instantiate(mergeItemPrefab, slotPosition, Quaternion.identity, this.transform);
        MergeItem mergeItem = newItemObject.GetComponent<MergeItem>();

        if (mergeItem != null)
        {
            mergeItem.Initialize(itemDef, x, y);
            targetSlot.currentItem = mergeItem;
            Debug.Log($"Spawned item '{itemDef.displayName}' at ({x},{y})");
            return mergeItem;
        }
        return null;
    }

    // --- New Helper Functions ---

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
        return null;
    }

    public void ClearSlotAt(int x, int y)
    {
        if (IsValidCoord(x, y))
        {
            gridSlots[x, y].currentItem = null;
            Debug.Log($"Cleared slot ({x},{y})");
        }
    }

    public Vector2 GetSlotPosition(int x, int y)
    {
        if (IsValidCoord(x, y))
        {
            return gridSlots[x, y].position;
        }
        Debug.LogError($"GetSlotPosition: Invalid coordinates ({x},{y})");

        return Vector2.positiveInfinity;
    }

    public void SetItemAt(MergeItem item, int x, int y)
    {
        if (!IsValidCoord(x, y))
        {
            Debug.LogError($"SetItemAt: Invalid coordinates ({x},{y}) for item {item?.name}");
            return;
        }

        GridSlot slot = gridSlots[x, y];

        if (slot.isOccupied && slot.currentItem != item)
        {
            Debug.LogWarning($"SetItemAt: Slot ({x},{y}) is already occupied by {slot.currentItem?.name}. Overwriting reference for {item?.name}. This might indicate a logic error elsewhere.");
        }

        slot.currentItem = item;
    }

    public Vector2Int? FindEmptySlot()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (gridSlots != null && IsValidCoord(x, y) && !gridSlots[x, y].isOccupied)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return null;
    }



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
