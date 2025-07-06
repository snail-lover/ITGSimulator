// ComputerInteractable.cs
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ComputerInteractable : MonoBehaviour, IClickable
{
    [Header("Interaction & Player Positioning")]
    [Tooltip("The exact spot where the player character will stand to interact with this object.")]
    public Transform interactionPoint;

    [Space(10)]
    [Header("Camera Control")]
    [Tooltip("The camera will pivot to look at this point. Typically the center of the computer screen.")]
    public Transform cameraFocusPoint;
    [Tooltip("OPTIONAL: For full manual control. If set, the camera will snap to this transform's exact position and rotation, ignoring other settings.")]
    public Transform cameraPositionOverride;
    [Tooltip("How close the camera gets to the 'Camera Focus Point'. Only used if 'Camera Position Override' is NOT set.")]
    public float cameraZoomedDistance = 1.0f;
    [Tooltip("An additional downward tilt for the camera view. Useful for angled screens. Only used if 'Camera Position Override' is NOT set.")]
    public float cameraViewAngle = 0f;
    [Tooltip("How long the camera transition takes, in seconds.")]
    public float cameraTransitionDuration = 1.0f;

    [Space(10)]
    [Header("Computer Data & UI")]
    [Tooltip("Assign the Computer Profile data asset that contains all the unique info for this specific terminal.")]
    public ComputerProfile computerProfile;
    [Tooltip("The World Space Canvas UI prefab that will be instantiated when the player interacts.")]
    public GameObject computerScreenUIPrefab;
    [Tooltip("The UI prefab will be created as a child of this empty transform, aligning it with the computer's screen model.")]
    public Transform screenAnchorPoint;

    // --- Private fields ---
    private bool isInteracting = false;
    private Coroutine _interactionCoroutine;
    private CameraFollow gameCamera;
    private NavMeshAgent playerAgent;
    private GameObject _currentScreenInstance;
    private ComputerScreenUI _uiController;
    private int _currentImageIndex = 0;

    void Start()
    {
        if (computerScreenUIPrefab == null) Debug.LogError($"[ComputerInteractable] Computer Screen UI Prefab not assigned on {gameObject.name}!", this);
        if (screenAnchorPoint == null) Debug.LogError($"[ComputerInteractable] Screen Anchor Point not assigned on {gameObject.name}!", this);
        if (interactionPoint == null) Debug.LogError($"[ComputerInteractable] Interaction Point not assigned on {gameObject.name}!", this);
        if (computerProfile == null) Debug.LogError($"[ComputerInteractable] A 'Computer Profile' data asset must be assigned on {gameObject.name}!", this);
        if (cameraFocusPoint == null && cameraPositionOverride == null) Debug.LogWarning($"[ComputerInteractable] For best results, assign either a Camera Focus Point or a Camera Position Override on {gameObject.name}.", this);

        gameCamera = Camera.main.GetComponent<CameraFollow>();
        if (gameCamera == null) Debug.LogError("[ComputerInteractable] CameraFollow script not found on main camera!");
    }

    public void OnClick()
    {
        if (isInteracting || _interactionCoroutine != null) return;
        _interactionCoroutine = StartCoroutine(ApproachAndInteractCoroutine());
    }

    private IEnumerator ApproachAndInteractCoroutine()
    {
        PointAndClickMovement.Instance.LockPlayerApproach(this);
        PointAndClickMovement.Instance.SetPlayerDestination(interactionPoint.position, true);
        playerAgent = PointAndClickMovement.Instance.GetPlayerAgent();
        if (playerAgent == null)
        {
            Debug.LogError($"[{gameObject.name}] Player agent not found!");
            PointAndClickMovement.Instance.UnlockPlayerApproach();
            _interactionCoroutine = null;
            yield break;
        }

        while (playerAgent.pathPending || playerAgent.remainingDistance > playerAgent.stoppingDistance + 0.2f)
        {
            if ((object)PointAndClickMovement.currentTarget != this)
            {
                _interactionCoroutine = null;
                yield break;
            }
            yield return null;
        }

        PointAndClickMovement.Instance.StopPlayerMovementAndNotify();
        PointAndClickMovement.Instance.HardLockPlayerMovement();
        PointAndClickMovement.Instance.UnlockPlayerApproach();
        isInteracting = true;

        yield return TransitionCameraToScreen();
        yield return ShowComputerScreen();
    }

    private IEnumerator TransitionCameraToScreen()
    {
        if (gameCamera == null) yield break;

        gameCamera.SetManualControl(true);
        Vector3 targetCamPos;
        Quaternion targetCamRot;

        if (cameraPositionOverride != null)
        {
            targetCamPos = cameraPositionOverride.position;
            targetCamRot = cameraPositionOverride.rotation;
        }
        else
        {
            targetCamPos = cameraFocusPoint.position - (cameraFocusPoint.forward * cameraZoomedDistance);
            Vector3 directionToFocus = (cameraFocusPoint.position - targetCamPos).normalized;
            targetCamRot = Quaternion.LookRotation(directionToFocus) * Quaternion.Euler(cameraViewAngle, 0, 0);
        }

        yield return gameCamera.StartCoroutine(gameCamera.TransitionToViewCoroutine(targetCamPos, targetCamRot, cameraTransitionDuration));
    }

    private IEnumerator ShowComputerScreen()
    {
        if (computerScreenUIPrefab == null) yield break;

        _currentScreenInstance = Instantiate(computerScreenUIPrefab, screenAnchorPoint);
        _currentScreenInstance.transform.localPosition = Vector3.zero;
        _currentScreenInstance.transform.localRotation = Quaternion.identity;

        _uiController = _currentScreenInstance.GetComponent<ComputerScreenUI>();

        if (_uiController == null)
        {
            Debug.LogError($"[{gameObject.name}] Failed to get UI Controller!", _currentScreenInstance);
            CleanupInteraction();
            yield break;
        }

        _uiController.SetBackground(computerProfile.backgroundImage);

        // --- MODIFIED LOGIC ---
        // Now we ONLY hook up the password screen's exit button here.
        _uiController.exitButtonPassword.onClick.AddListener(HandleExitInteraction);

        if (computerProfile.requiresPassword)
        {
            _uiController.SetBlurAmount(5f);
            _uiController.SetupPasswordScreen(computerProfile.ownerName, computerProfile.profilePicture);
            _uiController.ShowPasswordScreen();
            _uiController.submitButton.onClick.AddListener(HandlePasswordSubmit);
        }
        else
        {
            _uiController.SetBlurAmount(0f);
            _uiController.ShowDesktopScreen();
            HandleDesktopReady();
        }
    }

    private void HandlePasswordSubmit()
    {
        if (_uiController == null || computerProfile == null) return;

        string enteredPassword = _uiController.passwordInputField.text;
        if (enteredPassword == computerProfile.password)
        {
            _uiController.feedbackText.text = "Access Granted...";
            _uiController.feedbackText.color = Color.green;
            _uiController.submitButton.interactable = false;
            StartCoroutine(TransitionToDesktopScreen(0.5f));
        }
        else
        {
            _uiController.feedbackText.text = "ACCESS DENIED";
            _uiController.feedbackText.color = Color.red;
            _uiController.passwordInputField.text = "";
        }
    }

    private IEnumerator TransitionToDesktopScreen(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_uiController != null)
        {
            StartCoroutine(AnimateBlur(0f, 0.25f));
            _uiController.ShowDesktopScreen();
            HandleDesktopReady();
        }
    }

    private void HandleDesktopReady()
    {
        if (_uiController == null || computerProfile == null) return;

        // The blocker's only job is to close the start menu when clicked.
        if (_uiController.startMenuBlockerButton != null)
        {
            _uiController.startMenuBlockerButton.onClick.AddListener(_uiController.ToggleStartMenu);
        }

        // The main Start Button on the taskbar now toggles the menu
        if (_uiController.startButton != null)
        {
            _uiController.startButton.onClick.AddListener(_uiController.ToggleStartMenu);
        }

        // The Notes icon on the desktop still opens the app
        if (_uiController.notesAppIcon != null)
        {
            _uiController.notesAppIcon.onClick.AddListener(HandleOpenNotesApp);
        }

        // The Notes button IN THE START MENU now ALSO opens the app
        if (_uiController.notesButtonStartMenu != null)
        {
            _uiController.notesButtonStartMenu.onClick.AddListener(HandleOpenNotesApp);
        }

        // The Shutdown button in the start menu is now the exit button for the desktop
        if (_uiController.shutdownButtonStartMenu != null)
        {
            _uiController.shutdownButtonStartMenu.onClick.AddListener(HandleExitInteraction);
        }

        // The close button for the notes app itself
        if (_uiController.closeNotesAppButton != null)
        {
            _uiController.closeNotesAppButton.onClick.AddListener(_uiController.CloseNotesApp);
        }

        // --- ADDED SECTION FOR IMAGE VIEWER ---
        // Desktop Icon
        if (_uiController.imageViewerIcon != null)
        {
            _uiController.imageViewerIcon.onClick.AddListener(HandleOpenImageViewer);
        }
        // Start Menu Button
        if (_uiController.imageViewerButtonStartMenu != null)
        {
            _uiController.imageViewerButtonStartMenu.onClick.AddListener(HandleOpenImageViewer);
        }

        // Close Button
        if (_uiController.closeImageViewerButton != null)
        {
            _uiController.closeImageViewerButton.onClick.AddListener(_uiController.CloseImageViewer);
        }

        // Cycle Buttons
        if (_uiController.previousImageButton != null)
        {
            _uiController.previousImageButton.onClick.AddListener(HandlePreviousImage);
        }
        if (_uiController.nextImageButton != null)
        {
            _uiController.nextImageButton.onClick.AddListener(HandleNextImage);
        }
    }

    // --- NEW METHODS FOR IMAGE VIEWER LOGIC ---

    private void HandleOpenImageViewer()
    {
        if (_uiController == null || computerProfile == null) return;

        // Don't open if there are no images.
        if (computerProfile.imageGallery.Count == 0)
        {
            Debug.Log($"[{gameObject.name}] Tried to open Image Viewer, but no images are in the profile.");
            return;
        }

        // Reset to the first image and open the panel
        _currentImageIndex = 0;
        _uiController.OpenImageViewer();

        // Update the view
        UpdateDisplayedImage();
    }

    private void HandleNextImage()
    {
        if (computerProfile.imageGallery.Count <= 1) return; // No cycling if 0 or 1 images

        _currentImageIndex++;
        // Wrap around to the beginning if we go past the end
        if (_currentImageIndex >= computerProfile.imageGallery.Count)
        {
            _currentImageIndex = 0;
        }
        UpdateDisplayedImage();
    }

    private void HandlePreviousImage()
    {
        if (computerProfile.imageGallery.Count <= 1) return;

        _currentImageIndex--;
        // Wrap around to the end if we go past the beginning
        if (_currentImageIndex < 0)
        {
            _currentImageIndex = computerProfile.imageGallery.Count - 1;
        }
        UpdateDisplayedImage();
    }

    private void UpdateDisplayedImage()
    {
        if (_uiController == null) return;

        // Good practice: make sure the list isn't empty and the index is valid.
        if (computerProfile.imageGallery.Count > 0 && _currentImageIndex < computerProfile.imageGallery.Count)
        {
            // Get the sprite from the profile and tell the UI to display it
            Sprite spriteToShow = computerProfile.imageGallery[_currentImageIndex];
            _uiController.DisplayImage(spriteToShow);
        }

        // Also a good user experience: disable cycle buttons if there's only one image.
        bool enableButtons = computerProfile.imageGallery.Count > 1;
        _uiController.previousImageButton.interactable = enableButtons;
        _uiController.nextImageButton.interactable = enableButtons;
    }

    private void HandleOpenNotesApp()
    {
        if (_uiController == null || computerProfile == null) return;

        _uiController.PopulateNoteList(computerProfile.notes);
        _uiController.OpenNotesApp();
    }

    private IEnumerator AnimateBlur(float targetBlur, float duration)
    {
        if (_uiController == null || _uiController.backgroundImageComponent == null) yield break;

        var material = _uiController.backgroundImageComponent.material;
        if (material == null) yield break;

        float startBlur = material.GetFloat("_BlurAmount");
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newBlur = Mathf.Lerp(startBlur, targetBlur, elapsedTime / duration);
            _uiController.SetBlurAmount(newBlur);
            yield return null;
        }

        _uiController.SetBlurAmount(targetBlur);
    }

    public void ResetInteractionState()
    {
        if (_interactionCoroutine != null)
        {
            StopCoroutine(_interactionCoroutine);
            _interactionCoroutine = null;
        }

        StopAllCoroutines();

        if (isInteracting)
        {
            CleanupInteraction();
        }
        else
        {
            if (PointAndClickMovement.Instance != null)
                PointAndClickMovement.Instance.UnlockPlayerApproach();
        }
        isInteracting = false;
    }

    private void HandleExitInteraction()
    {
        if (!isInteracting) return;

        CleanupInteraction();

        if (PointAndClickMovement.Instance != null)
            PointAndClickMovement.currentTarget = null;
    }

    private void CleanupInteraction()
    {
        if (_currentScreenInstance != null)
        {
            Destroy(_currentScreenInstance);
        }

        if (gameCamera != null)
        {
            gameCamera.SetManualControl(false);
        }

        if (PointAndClickMovement.Instance != null)
        {
            PointAndClickMovement.Instance.HardUnlockPlayerMovement();
        }

        isInteracting = false;
        _interactionCoroutine = null;
        _uiController = null;
    }

    void OnDestroy()
    {
        if (_currentScreenInstance != null)
        {
            Destroy(_currentScreenInstance);
        }
    }
}