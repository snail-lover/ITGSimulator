// PlayerData.cs
using UnityEngine;
using Game.Core; // Assuming PlayerPersonalityProfile is in Core namespace

namespace Game.Gameplay
{
    /// <summary>
    /// A central component that provides convenient access to the player's
    /// runtime data, which is sourced directly from the WorldDataManager.
    /// </summary>
    public class PlayerData : MonoBehaviour
    {
        public static PlayerData Instance { get; private set; }

        // This property now acts as a shortcut to the data held by the save system.
        // It has no 'set' accessor because only the WorldDataManager should change the root object.
        public PlayerPersonalityProfile Profile { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Instead of creating a new profile, we get the one from the master save data object.
            // This ensures that if a game was loaded, we are using the loaded data.
            // If it's a new game, we will be using the fresh instance from the new GameSaveData.
            if (WorldDataManager.Instance != null)
            {
                Profile = WorldDataManager.Instance.saveData.playerPersonality;
            }
            else
            {
                // This will fire the error you wisely put in the WorldDataManager's getter.
                Debug.LogError("PlayerData could not find the WorldDataManager instance!");
                this.enabled = false;
            }
        }
    }
}