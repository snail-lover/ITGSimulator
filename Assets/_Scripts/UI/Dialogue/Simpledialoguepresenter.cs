#nullable enable
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class SimpleDialoguePresenter : DialoguePresenterBase
{

    [Header("Speaker Panel")]
    [SerializeField] private GameObject panel = null!;
    [SerializeField] private TextMeshProUGUI speakerNameText = null!;
    [SerializeField] private TextMeshProUGUI lineText = null!;

    [Header("Choices Panel")]
    [SerializeField] private GameObject choicesPanel = null!;
    [SerializeField] private Transform choicesContainer = null!;
    [SerializeField] private GameObject choiceButtonPrefab = null!;

    private TaskCompletionSource<int> _waitForInput = null!;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        panel.SetActive(false);
        choicesPanel.SetActive(false);

        // Make the whole speaker panel clickable to advance lines
        var clickArea = panel.GetComponent<Button>();
        if (clickArea == null) clickArea = panel.AddComponent<Button>();

        // Remove the default button visual transition so it doesn't flash
        clickArea.transition = Selectable.Transition.None;
        clickArea.onClick.AddListener(OnPanelClicked);
    }

    // -------------------------------------------------------------------------
    // DialoguePresenterBase overrides
    // -------------------------------------------------------------------------

    public override async YarnTask OnDialogueStartedAsync()
    {
        await YarnTask.CompletedTask;
    }

    public override async YarnTask OnDialogueCompleteAsync()
    {
        DialogueEvents.RaiseDialogueEnded();
        panel.SetActive(false);
        choicesPanel.SetActive(false);
        await YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        choicesPanel.SetActive(false);
        panel.SetActive(true);

        bool hasSpeaker = !string.IsNullOrEmpty(line.CharacterName);
        if (speakerNameText != null)
        {
            speakerNameText.gameObject.SetActive(hasSpeaker);
            speakerNameText.text = hasSpeaker ? line.CharacterName : string.Empty;
        }

        lineText.text = line.TextWithoutCharacterName.Text;

        // If choices follow immediately, return without waiting —
        // RunOptionsAsync will fire right away
        bool isLastLine = line.Metadata != null &&
                          System.Array.IndexOf(line.Metadata, "lastline") >= 0;

        if (isLastLine) return;

        // Otherwise wait for a panel click to advance
        _waitForInput = new TaskCompletionSource<int>();
        token.NextContentToken.Register(() => _waitForInput.TrySetResult(-1));
        await _waitForInput.Task;
    }

    public override async YarnTask<DialogueOption?> RunOptionsAsync(
        DialogueOption[] dialogueOptions,
        LineCancellationToken token)
    {
        foreach (Transform child in choicesContainer)
            Destroy(child.gameObject);

        _waitForInput = new TaskCompletionSource<int>();

        for (int i = 0; i < dialogueOptions.Length; i++)
        {
            int capturedIndex = i;
            DialogueOption option = dialogueOptions[i];

            GameObject buttonObj = Instantiate(choiceButtonPrefab, choicesContainer);

            TextMeshProUGUI label = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = option.Line.TextWithoutCharacterName.Text;

            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = option.IsAvailable;
                button.onClick.AddListener(() =>
                {
                    _waitForInput.TrySetResult(capturedIndex);
                });
            }
        }

        choicesPanel.SetActive(true);

        token.NextContentToken.Register(() => _waitForInput.TrySetResult(-1));
        int chosenIndex = await _waitForInput.Task;

        choicesPanel.SetActive(false);
        foreach (Transform child in choicesContainer)
            Destroy(child.gameObject);

        if (chosenIndex < 0 || chosenIndex >= dialogueOptions.Length)
            return null;

        return dialogueOptions[chosenIndex];
    }

    // -------------------------------------------------------------------------
    // Click handler
    // -------------------------------------------------------------------------

    private void OnPanelClicked()
    {
        _waitForInput?.TrySetResult(-1);
    }
}