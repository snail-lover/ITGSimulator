using UnityEngine;

public interface IClickable
{
    void OnClick();
    void ResetInteractionState();
    void WhenHovered();
    void HideHover();
}