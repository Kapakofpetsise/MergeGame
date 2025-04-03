using UnityEngine;

[CreateAssetMenu(fileName = "ItemDef_New", menuName = "Merge Game/Item Definition", order = 1)]
public class ItemDefinition : ScriptableObject
{
    [Header("Core Properties")]
    public int level = 1;
    public string displayName = "New Item";
    public Sprite itemSprite; // The visual for this item level

    [Header("Merging")]
    // Link to the definition of the item created when this one merges
    public ItemDefinition nextLevelDefinition; // Can be null/empty for the highest level
}