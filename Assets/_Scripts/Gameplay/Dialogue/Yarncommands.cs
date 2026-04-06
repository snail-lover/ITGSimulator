// Place at: Assets/_Scripts/Gameplay/Dialogue/YarnCommands.cs
// Attach to the Dialogue System GameObject (same one as your DialogueRunner)

using UnityEngine;
using Yarn.Unity;
using Game.Gameplay;
using Game.Core;

public class YarnCommands : MonoBehaviour
{
    private DialogueRunner _runner;

    private void Awake()
    {
        _runner = GetComponent<DialogueRunner>();

        if (_runner == null)
            _runner = FindFirstObjectByType<DialogueRunner>();

        if (_runner == null)
        {
            Debug.LogError("[YarnCommands] No DialogueRunner found in scene.");
            return;
        }

        RegisterAll();
    }

    private void RegisterAll()
    {
        _runner.AddCommandHandler<string, float>("modify_relationship", ModifyRelationship);
        _runner.AddCommandHandler<string>       ("set_flag",            SetFlag);
        _runner.AddCommandHandler<string>       ("clear_flag",          ClearFlag);
        _runner.AddCommandHandler<string, float>("modify_trait",        ModifyTrait);
        _runner.AddCommandHandler<string>       ("give_item",           GiveItem);
        _runner.AddCommandHandler<string>       ("remove_item",         RemoveItem);

        Debug.Log("[YarnCommands] All commands registered.");
    }

    // -------------------------------------------------------------------------
    // Relationships
    // -------------------------------------------------------------------------

    // <<modify_relationship viktor 10>>
    private void ModifyRelationship(string npcID, float amount)
    {
        if (RelationshipManager.Instance == null) { Debug.LogWarning("[YarnCommands] RelationshipManager not found."); return; }
        RelationshipManager.Instance.ModifyRelationship(npcID, amount);
    }

    // -------------------------------------------------------------------------
    // Flags
    // -------------------------------------------------------------------------

    // <<set_flag met_viktor>>
    private void SetFlag(string flagName)
    {
        if (WorldDataManager.Instance == null) { Debug.LogWarning("[YarnCommands] WorldDataManager not found."); return; }
        WorldDataManager.Instance.SetGlobalFlag(flagName, true);
    }

    // <<clear_flag met_viktor>>
    private void ClearFlag(string flagName)
    {
        if (WorldDataManager.Instance == null) { Debug.LogWarning("[YarnCommands] WorldDataManager not found."); return; }
        WorldDataManager.Instance.SetGlobalFlag(flagName, false);
    }

    // -------------------------------------------------------------------------
    // Personality
    // -------------------------------------------------------------------------

    // <<modify_trait Romantic 5>>
    private void ModifyTrait(string traitName, float amount)
    {
        if (PersonalityManager.Instance == null) { Debug.LogWarning("[YarnCommands] PersonalityManager not found."); return; }

        if (!System.Enum.TryParse<TraitType>(traitName, ignoreCase: true, out var trait))
        {
            Debug.LogWarning($"[YarnCommands] Unknown trait '{traitName}'. Valid: Lawful, Chaotic, Academic, Slacker, Romantic, Sigma.");
            return;
        }

        PersonalityManager.Instance.ModifyTrait(trait, amount);
    }

    // -------------------------------------------------------------------------
    // Inventory
    // -------------------------------------------------------------------------

    // <<give_item coffee_cup>>
    private void GiveItem(string itemID)
    {
        if (Inventory.Instance == null) { Debug.LogWarning("[YarnCommands] Inventory not found."); return; }
        Inventory.Instance.AddItem(itemID);
    }

    // <<remove_item coffee_cup>>
    private void RemoveItem(string itemID)
    {
        if (Inventory.Instance == null) { Debug.LogWarning("[YarnCommands] Inventory not found."); return; }
        Inventory.Instance.RemoveItemByID(itemID);
    }
}