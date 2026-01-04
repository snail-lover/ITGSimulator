namespace Game.Core
{
    using System;

    // Simple event hub so lower layers can request dialogue actions
    public static class DialogueSignals
    {
        public static event Action ForceCloseRequested;

        public static void RequestForceClose() => ForceCloseRequested?.Invoke();
    }

    // If this isn’t already in Core, put it here so Gameplay can type against it.
    public interface IHangoutPartner
    {
        void BeginHangoutState();
        void EndHangoutState();
    }
}
