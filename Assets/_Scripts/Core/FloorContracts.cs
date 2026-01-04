// Game.Core/FloorContracts.cs
namespace Game.Core
{
    // Shared enum used by all assemblies
    public enum FloorLevel { Lower = 0, First = 1, Second = 2 }

    // Anything that should react when the visible floor changes implements this
    public interface IFloorVisibilityAware
    {
        void UpdateVisibilityBasedOnPlayerFloor(FloorLevel playerFloor);
    }
}
