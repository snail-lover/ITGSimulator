using UnityEngine;

namespace Game.Core
{
    // Right-click > Create > Game > Definitions > Fluid Type
    [CreateAssetMenu(menuName = "Game/Definitions/Fluid Type")]
    public class FluidDefinition : ScriptableObject
    {
        public string fluidName; // "Water", "Hot Coffee"
        public int fluidID;      // 10 = Water, 20 = Coffee (Used for saving state)
        public Color visualColor; // Used later for the liquid mesh renderer
    }
}