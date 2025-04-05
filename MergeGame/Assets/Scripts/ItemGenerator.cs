using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class ItemGenerator : MonoBehaviour, IPointerClickHandler
{
    [Header("Generator Settings")]
    public ItemDefinition itemToSpawn;
    public int maxEnergy = 10;
    public int energyCost = 1;

    [Header("State")]
    [SerializeField]
    private int currentEnergy;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) {
             originalColor = spriteRenderer.color;
        }
    }

    void Start()
    {
        currentEnergy = maxEnergy;
         UpdateVisuals();
        if (itemToSpawn == null)
        {
            Debug.LogError($"ItemGenerator '{gameObject.name}' does not have an Item Definition assigned to spawn!", this);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentEnergy >= energyCost)
        {
             bool spawned = TrySpawnItem();

             if (spawned)
             {
                 currentEnergy -= energyCost;
                 Debug.Log($"Generator used. Energy left: {currentEnergy}/{maxEnergy}");
                 UpdateVisuals();
             }
        }
        else
        {
            Debug.Log("Generator has not enough energy!");
            // Optional: Add a visual/audio cue for no energy
        }
    }

    private bool TrySpawnItem()
    {
         if (itemToSpawn == null) {
             Debug.LogError("Cannot spawn: Item Definition is not set on the generator.", this);
             return false;
         }
         if (GridManager.Instance == null) {
             Debug.LogError("Cannot spawn: GridManager instance not found.", this);
             return false;
         }

        Vector2Int? emptySlotCoords = GridManager.Instance.FindEmptySlot();

        if (emptySlotCoords.HasValue)
        {
            Vector2Int coords = emptySlotCoords.Value;
            MergeItem spawnedItem = GridManager.Instance.SpawnItemAt(itemToSpawn, coords.x, coords.y);

            if (spawnedItem != null)
            {
                 Debug.Log($"Successfully spawned {itemToSpawn.displayName} at ({coords.x},{coords.y}).");
                return true;
            }
            else
            {
                Debug.LogWarning($"Found empty slot ({coords.x},{coords.y}), but SpawnItemAt failed. Grid might be full or another issue occurred.");
                return false;
            }
        }
        else
        {
            Debug.Log("Could not find an empty slot on the grid.");
             // Optional: Add visual/audio cue for grid full
            return false;
        }
    }

    void UpdateVisuals()
    {
         if (spriteRenderer == null) return;

        if (currentEnergy <= 0) {
             spriteRenderer.color = Color.gray;
        } else {
             spriteRenderer.color = originalColor;
        }
    }
}