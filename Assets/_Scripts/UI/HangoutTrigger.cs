// In HangoutTrigger.cs (in _Scripts/UI/)
using UnityEngine;

public class HangoutTrigger : MonoBehaviour
{
    private NpcController _npcController;

    public void Initialize(NpcController npcController)
    {
        _npcController = npcController;
    }

    public void OnHangoutButtonClicked()
    {
        if (_npcController != null)
        {
            // Call the new request method
            _npcController.RequestHangout();
        }
    }
}