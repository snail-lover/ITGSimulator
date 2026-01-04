using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    // Minimal view model for Notes so Core doesn't depend on your Features NoteEntry type
    public sealed class NoteView
    {
        public string title;
        public string content;
    }

    // The UI contract ComputerInteractable talks to
    public interface IComputerScreenUI
    {
        // General visuals
        void SetBackground(Sprite bg);
        void SetBlurAmount(float amount);
        void ShowPasswordScreen();
        void ShowDesktopScreen();
        void SetupPasswordScreen(string owner, Sprite picture);
        void ToggleStartMenu();

        // Password screen interactions
        void OnExitFromPassword(Action handler);
        void OnSubmitPassword(Action handler);
        string ReadPasswordInput();
        void ClearPasswordInput();
        void SetPasswordFeedback(string text, Color color);
        void SetSubmitInteractable(bool interactable);

        // Desktop interactions
        void OnStartMenuBlocker(Action handler);
        void OnStartButton(Action handler);
        void OnNotesIcon(Action handler);
        void OnNotesMenu(Action handler);
        void OnShutdown(Action handler);
        void OnCloseNotes(Action handler);

        // Notes app
        void PopulateNoteList(List<NoteView> notes);
        void OpenNotesApp();
        void CloseNotesApp();

        // Image viewer
        void OnImageViewerIcon(Action handler);
        void OnImageViewerMenu(Action handler);
        void OnCloseImageViewer(Action handler);
        void OnPrevImage(Action handler);
        void OnNextImage(Action handler);
        void DisplayImage(Sprite image);
        void SetImageCycleButtonsInteractable(bool enabled);
        void CloseImageViewer();
    }
}
