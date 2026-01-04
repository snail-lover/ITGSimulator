using System.Collections.Generic;

namespace Game.Core
{
    public interface IInteractable
    {
        // Pure data. No MonoBehaviour references.
        string GetName();

        // Returns the list of things we can do.
        List<InteractionOption> GetInteractions(InteractionContext context);
    }
}