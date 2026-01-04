// HangoutManager.cs (Gameplay)
using UnityEngine;
using Game.Core;  // << add

public class HangoutManager : MonoBehaviour
{
    public static HangoutManager Instance { get; private set; }
    public IHangoutPartner ActiveHangoutPartner { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }
    }

    public void StartHangout(IHangoutPartner partner)
    {
        if (ActiveHangoutPartner != null) return;

        ActiveHangoutPartner = partner;
        ActiveHangoutPartner.BeginHangoutState();

        // Instead of calling DialogueManager directly:
        DialogueSignals.RequestForceClose();
    }

    public void EndCurrentHangout()
    {
        if (ActiveHangoutPartner == null) return;
        ActiveHangoutPartner.EndHangoutState();
        ActiveHangoutPartner = null;
    }
}
