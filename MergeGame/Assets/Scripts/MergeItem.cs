using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class MergeItem : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public ItemDefinition definition;

    public int gridX { get; private set; }
    public int gridY { get; private set; }

    private SpriteRenderer spriteRenderer;
    private Collider2D itemCollider;

    private Vector3 originalPosition;
    private Vector3 offset;
    private bool isDragging = false;
    private Camera mainCamera;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        itemCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;
    }

    void Start() { ApplyDefinition(); }

#if UNITY_EDITOR
    void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && !Application.isPlaying)
            {
                if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
                ApplyDefinitionVisualsOnly();
                gameObject.name = (definition != null && !string.IsNullOrEmpty(definition.displayName)) ? definition.displayName : "MergeItem (No Definition)";
                if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this)) UnityEditor.EditorUtility.SetDirty(this);
            }
        };
    }
#endif

    public void Initialize(ItemDefinition newDefinition, int x, int y)
    {
        definition = newDefinition;
        gridX = x;
        gridY = y;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyDefinition();

        originalPosition = transform.position;

    }

    private void ApplyDefinition()
    {
        if (definition != null)
        {
            ApplyDefinitionVisualsOnly();
            gameObject.name = definition.displayName;
        }
        else
        {
            Debug.LogError("MergeItem is missing its definition!", this);
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            gameObject.name = "MergeItem (Missing Definition)";
        }
    }

    private void ApplyDefinitionVisualsOnly()
    {
        // You can add temporary debug logs here later if needed:
        // Debug.Log($"ApplyVisuals: Def='{definition?.name}', Renderer='{(spriteRenderer ? "OK" : "NULL")}', Sprite='{definition?.itemSprite?.name}'");

        if (definition != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = definition.itemSprite; // The key line
            spriteRenderer.enabled = true; // Ensure it's enabled
        }
        else if (spriteRenderer != null) // If definition is null BUT renderer exists
        {
            // This path might be taken if definition is null, hiding the sprite
            spriteRenderer.sprite = null;
            spriteRenderer.enabled = false;
            Debug.LogWarning($"ApplyVisualsOnly called with null definition for {gameObject.name}. Disabling renderer.");
        }
        // Implicit else: if spriteRenderer is null, nothing happens here.
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if (!enabled) return;

        isDragging = true;
        originalPosition = transform.position;

        // --- Corrected World Point Calculation ---
        // 1. Get the mouse/touch position (screen coordinates)
        Vector3 screenPoint = eventData.position;
        // 2. Set the Z coordinate for ScreenToWorldPoint. Use the distance from the
        //    camera to the object's current Z plane. WorldToScreenPoint gives us this.
        screenPoint.z = mainCamera.WorldToScreenPoint(transform.position).z;
        // 3. Convert screen point to world point at the correct depth
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(screenPoint);
        // --- End Correction ---

        // Calculate the offset based on the correctly calculated world point
        offset = transform.position - worldPoint;

        // Optional: Bring item visually to the front while dragging (uncomment if needed)
        // if (spriteRenderer != null) spriteRenderer.sortingOrder = 10;
        if (itemCollider != null) itemCollider.enabled = false; // Disable collider temporarily

        Debug.Log($"Pointer Down on {gameObject.name}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!enabled || !isDragging) return;

        // --- Corrected World Point Calculation ---
        // 1. Get the mouse/touch position
        Vector3 screenPoint = eventData.position;
        // 2. Set the Z coordinate using the camera-object distance
        screenPoint.z = mainCamera.WorldToScreenPoint(transform.position).z;
        // 3. Convert to world point
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(screenPoint);
        // --- End Correction ---

        // Calculate the new position by applying the offset
        Vector3 newPos = worldPoint + offset;

        // --- Crucial: Maintain original Z ---
        // Ensure the object stays on its original Z plane during drag
        newPos.z = originalPosition.z;
        // ----------------------------------

        transform.position = newPos;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!enabled || !isDragging) return;

        isDragging = false;
        if (itemCollider != null) itemCollider.enabled = true;

        Vector3 dropPosition = transform.position;
        Vector2Int? targetCoordsNullable = GridManager.Instance.WorldToGridCoords(dropPosition);

        bool actionTaken = false;

        if (targetCoordsNullable.HasValue)
        {
            Vector2Int targetCoords = targetCoordsNullable.Value;
            MergeItem targetItem = GridManager.Instance.GetItemAt(targetCoords.x, targetCoords.y);

            // --- Attempt Merge ---
            if (CanMergeWith(targetItem))
            {
                Debug.Log($"Valid Merge detected at ({targetCoords.x},{targetCoords.y})!");
                actionTaken = true;

                ItemDefinition nextLevelDef = this.definition.nextLevelDefinition;

                if (nextLevelDef == null) {
                     Debug.LogError($"Merge failed: Next level definition is null for {this.definition.name}");
                     actionTaken = false;
                } else {
                    GridManager.Instance.ClearSlotAt(this.gridX, this.gridY); 
                    GridManager.Instance.ClearSlotAt(targetCoords.x, targetCoords.y);
                    
                    Destroy(targetItem.gameObject);
                    GridManager.Instance.SpawnItemAt(nextLevelDef, targetCoords.x, targetCoords.y);
                    Destroy(this.gameObject);
                }
            }

            // --- Attempt Move to Empty Slot ---
            else if (targetItem == null &&
                    (targetCoords.x != this.gridX || targetCoords.y != this.gridY))
            {
                Debug.Log($"Moving item to empty slot ({targetCoords.x},{targetCoords.y})");
                actionTaken = true;

                Vector2 newPosition = GridManager.Instance.GetSlotPosition(targetCoords.x, targetCoords.y);

                if (newPosition.x != float.PositiveInfinity)
                {
                    GridManager.Instance.ClearSlotAt(this.gridX, this.gridY);

                    GridManager.Instance.SetItemAt(this, targetCoords.x, targetCoords.y);

                    this.gridX = targetCoords.x;
                    this.gridY = targetCoords.y;

                    transform.position = newPosition;

                    originalPosition = newPosition;
                }
                else
                {
                    actionTaken = false;
                    Debug.LogWarning("Failed to get target slot position for move.");
                }
            }
        }
        // --- If NO Action Occurred (Merge or Move) ---
        // This handles: Dropping off-grid, dropping back on original slot, dropping on incompatible item.
        if (!actionTaken)
        {
            transform.position = originalPosition;
        }
    }

    private bool CanMergeWith(MergeItem targetItem)
    {
        if (targetItem == null || targetItem == this) return false;
        if (this.definition == null || this.definition.nextLevelDefinition == null) return false;
        return targetItem.definition == this.definition;
    }
}