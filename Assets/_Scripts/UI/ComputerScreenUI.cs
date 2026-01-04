// ComputerScreenUI.cs   (Game.UI assembly)
using Game.Core;          // << interface + NoteView
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class ComputerScreenUI : MonoBehaviour, IComputerScreenUI
{
    /* ------------------------------------------------------------------ *
     *  Inspector-assigned references                                     *
     * ------------------------------------------------------------------ */
    [Header("Background / Blur")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Material backgroundMaterialInstance;

    [Header("PASSWORD SCREEN")]
    [SerializeField] private GameObject passwordScreenRoot;
    [SerializeField] private TMP_Text ownerNameText;
    [SerializeField] private Image ownerPictureImage;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button exitButtonPassword;

    [Header("DESKTOP")]
    [SerializeField] private GameObject desktopRoot;
    [SerializeField] private Button startMenuBlockerButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button shutdownButtonStartMenu;

    [Header("NOTES APP")]
    [SerializeField] private Button notesAppIcon;
    [SerializeField] private Button notesButtonStartMenu;
    [SerializeField] private GameObject notesAppRoot;
    [SerializeField] private Transform sidebarContentArea;
    [SerializeField] private TMP_Text noteContentText;
    [SerializeField] private GameObject noteButtonPrefab;
    [SerializeField] private Button closeNotesAppButton;

    [Header("IMAGE VIEWER")]
    [SerializeField] private Button imageViewerIcon;
    [SerializeField] private Button imageViewerButtonStartMenu;
    [SerializeField] private GameObject imageViewerRoot;
    [SerializeField] private Image imageViewerDisplayImage;
    [SerializeField] private Button previousImageButton;
    [SerializeField] private Button nextImageButton;
    [SerializeField] private Button closeImageViewerButton;

    [Header("OPTIONAL VISUALS")]
    [SerializeField] private GameObject startMenuRoot;          // for ToggleStartMenu

    /* ------------------------------------------------------------------ *
     *  Private helpers & cached state                                    *
     * ------------------------------------------------------------------ */
    private CanvasGroup _canvasGroup;

    public void CloseImageViewer() => CloseImageViewerInternal();
    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        ShowNothing();           // start hidden / fully transparent
    }

    /* =========  IComputerScreenUI implementation  ========= */

    // ----- shared look --------------------------------------------------
    public void SetBackground(Sprite bg)
    {
        if (backgroundImage) backgroundImage.sprite = bg;
    }
    public void SetBlurAmount(float amount)
    {
        if (backgroundMaterialInstance) backgroundMaterialInstance.SetFloat("_BlurAmount", amount);
    }

    // ----- password screen ----------------------------------------------
    public void ShowPasswordScreen()
    {
        ShowOnly(passwordScreenRoot);
        ShowPasswordScreenInternal();               // your old animation
    }
    public void SetupPasswordScreen(string owner, Sprite picture)
    {
        if (ownerNameText) ownerNameText.text = owner;
        if (ownerPictureImage) ownerPictureImage.sprite = picture;
        feedbackText?.SetText("");
        ClearPasswordInput();
        SetSubmitInteractable(true);
    }
    public void OnExitFromPassword(Action handler)
    {
        if (exitButtonPassword != null)
        {
            exitButtonPassword.onClick.RemoveAllListeners();
            exitButtonPassword.onClick.AddListener(() => handler?.Invoke());
        }
    }
    public void OnSubmitPassword(Action handler)
    {
        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(() => handler?.Invoke());
        }
    }
    public string ReadPasswordInput() => passwordInputField ? passwordInputField.text : "";
    public void ClearPasswordInput() { if (passwordInputField) passwordInputField.text = ""; }
    public void SetPasswordFeedback(string text, Color c)
    {
        if (feedbackText)
        {
            feedbackText.text = text;
            feedbackText.color = c;
        }
    }
    public void SetSubmitInteractable(bool interactable)
    {
        if (submitButton) submitButton.interactable = interactable;
    }

    // ----- desktop -------------------------------------------------------
    public void ShowDesktopScreen()
    {
        ShowOnly(desktopRoot);
        ShowDesktopScreenInternal();                // your old animation
    }
    public void ToggleStartMenu() => ToggleStartMenuInternal();
    public void OnStartMenuBlocker(Action h) => Wire(startMenuBlockerButton, h);
    public void OnStartButton(Action h) => Wire(startButton, h);
    public void OnShutdown(Action h) => Wire(shutdownButtonStartMenu, h);

    // ----- notes app -----------------------------------------------------
    public void OnNotesIcon(Action h) => Wire(notesAppIcon, h);
    public void OnNotesMenu(Action h) => Wire(notesButtonStartMenu, h);
    public void OnCloseNotes(Action h) => Wire(closeNotesAppButton, h);

    public void PopulateNoteList(List<NoteView> notes)
    {
        foreach (Transform c in sidebarContentArea) Destroy(c.gameObject);
        noteContentText?.SetText("");

        foreach (var n in notes)
        {
            var btnGO = Instantiate(noteButtonPrefab, sidebarContentArea);
            var txt = btnGO.GetComponentInChildren<TMP_Text>();
            if (txt) txt.text = n.title;

            var btn = btnGO.GetComponent<Button>();
            if (btn) btn.onClick.AddListener(() => noteContentText.SetText(n.content));
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)sidebarContentArea);
    }
    public void OpenNotesApp() { if (notesAppRoot) notesAppRoot.SetActive(true); }
    public void CloseNotesApp() { if (notesAppRoot) notesAppRoot.SetActive(false); }

    // ----- image viewer --------------------------------------------------
    public void OnImageViewerIcon(Action h) => Wire(imageViewerIcon, h);
    public void OnImageViewerMenu(Action h) => Wire(imageViewerButtonStartMenu, h);
    public void OnCloseImageViewer(Action h) => Wire(closeImageViewerButton, h);
    public void OnPrevImage(Action h) => Wire(previousImageButton, h);
    public void OnNextImage(Action h) => Wire(nextImageButton, h);

    public void DisplayImage(Sprite img)
    {
        if (imageViewerRoot && !imageViewerRoot.activeSelf) imageViewerRoot.SetActive(true);
        if (imageViewerDisplayImage) imageViewerDisplayImage.sprite = img;
    }
    public void SetImageCycleButtonsInteractable(bool enabled)
    {
        if (previousImageButton) previousImageButton.interactable = enabled;
        if (nextImageButton) nextImageButton.interactable = enabled;
    }

    // ----- show / hide whole UI -----------------------------------------
    public void HideDialogue(Action onHidden = null) => HideUI(onHidden);
    public void ShowDialogue(DialogueViewNode view) => Debug.LogWarning("DialogueViewNode not used here");
    public void UpdateDialogueContent(DialogueViewNode view) { /* no-op for computer UI */ }
    public void SetStatsButtonVisibility(bool visible) { }  // not used for computer UI

    /* ------------------------------------------------------------------ *
     *  Internal helpers (add your animations inside)                     *
     * ------------------------------------------------------------------ */

    private void ShowPasswordScreenInternal() { /* your old enter-password anim */ }
    private void ShowDesktopScreenInternal() { /* your old desktop anim */ }
    private void ToggleStartMenuInternal()
    {
        if (!startMenuRoot) return;
        startMenuRoot.SetActive(!startMenuRoot.activeSelf);
    }

    private void CloseImageViewerInternal()
    {
        if (imageViewerRoot) imageViewerRoot.SetActive(false);
        // (add animation / SFX if you had them)
    }

    // CanvasGroup fade out, then onHidden callback
    private void HideUI(Action onHidden)
    {
        StartCoroutine(FadeCanvas(1f, 0f, 0.25f, () =>
        {
            onHidden?.Invoke();
            Destroy(gameObject);
        }));
    }

    private void ShowOnly(GameObject root)
    {
        // disable others
        if (passwordScreenRoot) passwordScreenRoot.SetActive(root == passwordScreenRoot);
        if (desktopRoot) desktopRoot.SetActive(root == desktopRoot);
        if (notesAppRoot) notesAppRoot.SetActive(false);
        if (imageViewerRoot) imageViewerRoot.SetActive(false);

        if (!_canvasGroup) return;
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = true;
        StartCoroutine(FadeCanvas(0f, 1f, 0.25f));
    }

    private IEnumerator FadeCanvas(float from, float to, float duration, Action onDone = null)
    {
        if (!_canvasGroup) { onDone?.Invoke(); yield break; }
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        _canvasGroup.alpha = to;
        onDone?.Invoke();
    }

    private static void Wire(Button b, Action h)
    {
        if (b == null) return;
        b.onClick.RemoveAllListeners();
        if (h != null) b.onClick.AddListener(() => h());
    }

    private void ShowNothing()
    {
        if (passwordScreenRoot) passwordScreenRoot.SetActive(false);
        if (desktopRoot) desktopRoot.SetActive(false);
        if (notesAppRoot) notesAppRoot.SetActive(false);
        if (imageViewerRoot) imageViewerRoot.SetActive(false);
        if (_canvasGroup) _canvasGroup.alpha = 0f;
    }
}
