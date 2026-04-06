using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public class UI_Tag : MonoBehaviour
    {
        public Image background;
        public TMP_Text label;

        public void Setup(string text, Color bgColor)
        {
            if (label != null) label.text = text;
            if (background != null) background.color = bgColor;
        }
    }
}