using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    // Keeps track of WHICH item this is, so the Save System and Pickup Actions know.
    public class ItemInstance : MonoBehaviour
    {
        public ItemDefinition itemData;
    }
}