// ComputerScreenUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;

public class ComputerScreenUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject passwordScreenPanel;
    public GameObject desktopPanel;
    public GameObject startMenuPanel;

    [Header("Background")]
    public Image backgroundImageComponent;

    [Header("Password Screen Elements")]
    public Image profilePictureImage;
    public TMP_Text ownerNameText;
    public TMP_InputField passwordInputField;
    public Button submitButton;
    public TMP_Text feedbackText;
    public Button exitButtonPassword;

    [Header("Desktop Elements")]
    public Button startButton;
    public TMP_Text clockText;
    public Button notesAppIcon;
    public Button imageViewerIcon; // <-- NEW

    [Header("Start Menu Elements")]
    public Button notesButtonStartMenu;
    public Button shutdownButtonStartMenu;
    public Button startMenuBlockerButton;
    public Button imageViewerButtonStartMenu;

    [Header("Notes App Elements")]
    public GameObject notesAppPanel;
    public GameObject noteButtonPrefab;
    public Transform sidebarContentArea;
    public TMP_Text noteContentText;
    public Button closeNotesAppButton;

    [Header("Image Viewer Elements")] // <-- NEW SECTION
    public GameObject imageViewerPanel;
    public Image mainDisplayImage;
    public AspectRatioFitter mainDisplayFitter; // <-- ADD THIS LINE
    public Button previousImageButton;
    public Button nextImageButton;
    public Button closeImageViewerButton;



    private Material backgroundMaterialInstance;
    private Coroutine _clockCoroutine;

    void Awake()
    {
        if (backgroundImageComponent != null && backgroundImageComponent.material != null)
        {
            backgroundMaterialInstance = Instantiate(backgroundImageComponent.material);
            backgroundImageComponent.material = backgroundMaterialInstance;
        }
    }

    void OnDestroy()
    {
        if (_clockCoroutine != null)
        {
            StopCoroutine(_clockCoroutine);
        }
    }

    public void SetupPasswordScreen(string owner, Sprite picture)
    {
        ownerNameText.text = owner;
        if (picture != null)
        {
            profilePictureImage.sprite = picture;
            profilePictureImage.enabled = true;
        }
        else
        {
            profilePictureImage.enabled = false;
        }
        passwordInputField.text = "";
        feedbackText.text = "";
    }

    public void ShowPasswordScreen()
    {
        passwordScreenPanel.SetActive(true);
        desktopPanel.SetActive(false);
    }

    public void ShowDesktopScreen()
    {
        passwordScreenPanel.SetActive(false);
        desktopPanel.SetActive(true);
        startMenuPanel.SetActive(false);
        if (startMenuBlockerButton != null) // Also ensure blocker is closed
        {
            startMenuBlockerButton.gameObject.SetActive(false);
        }

        if (_clockCoroutine == null)
        {
            _clockCoroutine = StartCoroutine(UpdateClockCoroutine());
        }
    }

    private IEnumerator UpdateClockCoroutine()
    {
        while (true)
        {
            clockText.text = DateTime.Now.ToString("h:mm tt");
            yield return new WaitForSeconds(1f);
        }
    }

    public void SetBackground(Sprite bgSprite)
    {
        if (backgroundImageComponent != null && backgroundMaterialInstance != null)
        {
            backgroundImageComponent.sprite = bgSprite;
            backgroundImageComponent.enabled = (bgSprite != null);
            if (bgSprite != null)
            {
                backgroundMaterialInstance.SetTexture("_MainTex", bgSprite.texture);
            }
        }
    }

    public void SetBlurAmount(float amount)
    {
        if (backgroundMaterialInstance != null)
        {
            backgroundMaterialInstance.SetFloat("_BlurAmount", amount);
        }
    }

    public void OpenNotesApp()
    {
        notesAppPanel.SetActive(true);
    }

    public void CloseNotesApp()
    {
        notesAppPanel.SetActive(false);
    }

    public void PopulateNoteList(List<NoteEntry> notes)
    {
        // 1. Clear any old buttons from a previous session
        foreach (Transform child in sidebarContentArea)
        {
            Destroy(child.gameObject);
        }

        // 2. Clear the content view
        noteContentText.text = "";

        // 3. Create a new button for each note in the profile
        foreach (NoteEntry note in notes)
        {
            GameObject buttonGO = Instantiate(noteButtonPrefab, sidebarContentArea);

            // Get the button's text component and set the title
            TMP_Text buttonText = buttonGO.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = note.title;
            }

            // Add a listener to the button's click event
            Button button = buttonGO.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => {
                    DisplayNoteContent(note);
                });
            }
        }

        // === THE FIX ===
        // After instantiating all buttons, force the layout group on the sidebar
        // to recalculate its size and the position of its children immediately.
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)sidebarContentArea);
    }

    public void DisplayNoteContent(NoteEntry note)
    {
        if (note != null)
        {
            noteContentText.text = note.content;
        }
    }

    public void ToggleStartMenu()
    {
        if (startMenuPanel != null)
        {
            bool isActive = !startMenuPanel.activeSelf;
            startMenuPanel.SetActive(isActive);

            // The blocker's active state should always match the menu's state.
            if (startMenuBlockerButton != null)
            {
                startMenuBlockerButton.gameObject.SetActive(isActive);
            }
        }
    }

    // --- NEW METHODS FOR IMAGE VIEWER ---

    public void OpenImageViewer()
    {
        imageViewerPanel.SetActive(true);
    }

    public void CloseImageViewer()
    {
        imageViewerPanel.SetActive(false);
    }

    public void DisplayImage(Sprite image)
    {
        if (mainDisplayImage != null)
        {
            if (image != null)
            {
                mainDisplayImage.sprite = image;
                mainDisplayImage.enabled = true;

                // *** THIS IS THE CRUCIAL NEW CODE ***
                // Update the AspectRatioFitter with the new image's dimensions.
                if (mainDisplayFitter != null)
                {
                    mainDisplayFitter.aspectRatio = (float)image.rect.width / (float)image.rect.height;
                }
            }
            else
            {
                // If for some reason a null image is passed, hide the display.
                mainDisplayImage.enabled = false;
            }
        }
    }
}