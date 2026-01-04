// ItemTag.cs
using System;

[Flags] // This attribute is CRITICAL. It allows you to select multiple tags in the Inspector.
public enum ItemTag
{
    None = 0, // Represents no tags

    // --- Functional Tags (What an item IS or DOES) ---
    Key = 1 << 0,  // Binary: 00000001
    Consumable = 1 << 1,  // Binary: 00000010
    Weapon = 1 << 2,  // Binary: 00000100
    Tool = 1 << 3,  // Binary: 00001000  (Good general-purpose tag to add)
    QuestItem = 1 << 4,  // Binary: 00010000

    // --- Descriptive Tags (What are its qualities?) ---
    Technology = 1 << 5,  // Binary: 00100000
    Organic = 1 << 6,  // Binary: 01000000
    Valuable = 1 << 7,  // Binary: 10000000

    // =========================================================================
    // --- NEW: Personality & Approach Tags (For the Attraction System) ---
    // (What does USING this item say about the player's personality?)
    // =========================================================================

    // Spontaneity vs. Planning Axis
    Instinct = 1 << 8,  // Binary: 0001 00000000 (Acting on a gut feeling)
    Planning = 1 << 9,  // Binary: 0010 00000000 (A deliberate, thought-out action)

    // Directness vs. Subtlety Axis
    Forceful = 1 << 10, // Binary: 0100 00000000 (Smashing, breaking, intimidating)
    Subtle = 1 << 11, // Binary: 1000 00000000 (Lockpicking, sneaking, deceiving)

    // Other Useful Personality Tags
    Intellect = 1 << 12, // (Solving a complex puzzle, using knowledge)
    Mischief = 1 << 13, // (Playing a prank, a playful non-destructive action)
    Destructive = 1 << 14, // (Explicitly causing damage)
    Creative = 1 << 15,  // (Combining items in a novel way)

    //God cube
    GodCube = 1 << 16 // (A powerful, unique item)
}