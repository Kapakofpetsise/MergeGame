using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class MergeItem : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public ItemDefinition definition;

    // --- Add Grid Position Storage ---
    public int gridX { get; private set; }
    public int gridY { get; private set; }
    // --------------------------------

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

    // Start remains the same (or can be empty now)
    void Start() { ApplyDefinition(); }

#if UNITY_EDITOR
    // OnValidate remains the same
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

        // --- Removed direct access to gridSlots ---
        // The GridManager.SpawnItemAt method already places the item at the correct position during Instantiate.
        // We just need to record that starting position here so snap-back works correctly.
        originalPosition = transform.position;
        // -------------------------------------------
    }

    // ApplyDefinition and ApplyDefinitionVisualsOnly remain the same
    private void ApplyDefinition()
{
    if (definition != null)
    {
        ApplyDefinitionVisualsOnly();
         gameObject.name = definition.displayName; // Using displayName now
    }
    else
    {
         Debug.LogError("MergeItem is missing its definition!", this);
         if (spriteRenderer != null) spriteRenderer.enabled = false; // Check if renderer gets disabled here
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

    // OnPointerDown and OnDrag remain the same
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

    // (OnPointerUp remains the same as before)

    // --- Implement Merge Logic in OnPointerUp ---
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!enabled || !isDragging) return; // Added check for enabled

        isDragging = false;
        if (itemCollider != null) itemCollider.enabled = true; // Re-enable collider BEFORE checks
        // if (spriteRenderer != null) spriteRenderer.sortingOrder = 0; // Reset sorting order

        // Get current world position (where it was dropped)
        Vector3 dropPosition = transform.position;

        // Try to convert drop position to grid coordinates
        Vector2Int? targetCoordsNullable = GridManager.Instance.WorldToGridCoords(dropPosition);

        bool merged = false; // Flag to track if a merge occurred

        if (targetCoordsNullable.HasValue)
        {
            Vector2Int targetCoords = targetCoordsNullable.Value;
            MergeItem targetItem = GridManager.Instance.GetItemAt(targetCoords.x, targetCoords.y);

            // --- Merge Check ---
            if (targetItem != null && // Is there an item in the target slot?
                targetItem != this && // Is it not the item we just dropped?
                this.definition != null && // Does the dragged item have a definition?
                this.definition.nextLevelDefinition != null && // Can the dragged item actually merge up?
                targetItem.definition == this.definition) // Do the items have the same definition (level/type)?
            {
                Debug.Log($"Valid Merge detected at ({targetCoords.x},{targetCoords.y})!");
                merged = true;

                // 1. Get the definition for the new item
                ItemDefinition nextLevelDef = this.definition.nextLevelDefinition;

                // 2. Clear the grid slots in the GridManager's data
                GridManager.Instance.ClearSlotAt(this.gridX, this.gridY); // Clear original slot
                GridManager.Instance.ClearSlotAt(targetCoords.x, targetCoords.y); // Clear target slot

                // 3. Destroy the existing game objects
                Destroy(targetItem.gameObject); // Destroy the item we dropped onto
                Destroy(this.gameObject); // Destroy the item we dragged

                // 4. Spawn the new merged item in the target slot
                GridManager.Instance.SpawnItemAt(nextLevelDef, targetCoords.x, targetCoords.y);
            }
            // --- End Merge Check ---

            // --- Add Move Logic Here Later (Optional) ---
            // If target slot is empty, could move item here instead of snapping back
            // else if (targetItem == null) { ... move logic ... }
            // --------------------------------------------

        } // End if targetCoordsNullable.HasValue

        // If no merge occurred (dropped off-grid, on self, on mismatch, or on empty)
        if (!merged)
        {
            // Snap back to the original position
            transform.position = originalPosition;
        }
        // Note: If a merge happened, 'this' object is destroyed, so snapping back won't occur.
    }
    // --------------------------------------
}