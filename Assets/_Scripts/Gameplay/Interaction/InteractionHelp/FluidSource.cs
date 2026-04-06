using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    // Put this on your Sink, Lake, or Jerry Can
    public class FluidSource : MonoBehaviour
    {
        public FluidDefinition fluidType; // Drag "Water" asset here
        public bool isInfinite = true;    // Sinks are infinite, Jerry cans are not
    }
}