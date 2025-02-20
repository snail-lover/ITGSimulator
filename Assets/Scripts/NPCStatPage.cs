using UnityEngine;
using UnityEngine.UI;

public class NPCStatPage : MonoBehaviour
{
    public Button statsButton;
    public Text statsText;
    public GameObject statsPanel; // Reference to the stats panel
    public BaseNPC currentNPC;

    void Start()
    {
        statsButton.onClick.AddListener(OnStatsButtonClick);
        statsPanel.SetActive(false); // Hide the stats panel at the start
    }

    public void LoadStats()
    {
        if (currentNPC != null)
        {
            statsText.text = currentNPC.GetStats();
        }
    }

    void OnStatsButtonClick()
    {
        ToggleStatsPanel();
    }

    public void ToggleStatsPanel()
    {
        if (statsPanel.activeSelf)
        {
            statsPanel.SetActive(false); // Hide the stats panel
        }
        else
        {
            if (currentNPC != null)
            {
                statsText.text = currentNPC.GetStats();
                statsPanel.SetActive(true); // Show the stats panel
            }
        }
    }

    public void HideStatsPanel()
    {
        statsPanel.SetActive(false); // Method to hide the stats panel
    }
}
