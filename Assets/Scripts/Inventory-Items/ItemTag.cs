// ItemTag.cs
using System;

[Flags] // This attribute is CRITICAL. It allows you to select multiple tags in the Inspector.
public enum ItemTag
{
    None = 0, // Represents no tags

    // Functional Tags (What does it DO?)
    Camera = 1 << 0,      // Binary: 00001
    Key = 1 << 1,         // Binary: 00010
    Consumable = 1 << 2,  // Binary: 00100
    Weapon = 1 << 3,      // Binary: 01000

    // Descriptive Tags (What IS it?)
    Technology = 1 << 4,  // Binary: 10000
    Organic = 1 << 5,
    Valuable = 1 << 6,
    QuestItem = 1 << 7,

    // You can add many more...
}