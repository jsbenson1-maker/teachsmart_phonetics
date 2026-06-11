using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PhoneticsEdu.Core;

namespace PhoneticsEdu.UI
{
    /// <summary>
    /// UI Controller for the Main Menu hub. Includes state transitions for the 4 games
    /// and the "Synth Voice Pro" diagnostic tester.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Level Buttons")]
        [SerializeField] private Button? wordBlasterButton;
        [SerializeField] private Button? syllableShredderButton;
        [SerializeField] private Button? onsetRimeButton;
        [SerializeField] private Button? phonemeIsolatorButton;

        [Header("Synth Voice Pro Diagnostic Panel")]
        [SerializeField] private GameObject? diagnosticPanel;
        [SerializeField] private TMP_InputField? ssmlTextInputField;
        [SerializeField] private Button? testTtsButton;
        [SerializeField] private Button? testMicButton;
        [SerializeField] private TMP_Text? statusDisplayText;
        [SerializeField] private TMP_Text? micLevelText;

        private bool _isRecordingMic = false;
        private string _connectedMicDevice = "";

        private void Start()
        {
            // Bind game loading actions to buttons
            // Bind game loading actions to buttons
            wordBlasterButton?.onClick.AddListener(() => LoadGame(GameState.WordBlaster, wordBlasterButton!));
            syllableShredderButton?.onClick.AddListener(() => LoadGame(GameState.SyllableShredder, syllableShredderButton!));
            onsetRimeButton?.onClick.AddListener(() => LoadGame(GameState.OnsetRime, onsetRimeButton!));
            phonemeIsolatorButton?.onClick.AddListener(() => LoadGame(GameState.PhonemeIsolator, phonemeIsolatorButton!));

            // Bind diagnostic panel buttons
            testTtsButton?.onClick.AddListener(() => {
                if (UiJuiceManager.Instance != null && testTtsButton != null) UiJuiceManager.Instance.ApplyButtonJuice(testTtsButton.transform);
                OnTestTtsClicked();
            });
            testMicButton?.onClick.AddListener(() => {
                if (UiJuiceManager.Instance != null && testMicButton != null) UiJuiceManager.Instance.ApplyButtonJuice(testMicButton.transform);
                OnTestMicClicked();
            });

            InitializeDiagnostics();
        }

        private void Update()
        {
            // If recording mic, display simulated mic volume level
            if (_isRecordingMic && PhoneticAudioManager.Instance != null && micLevelText != null)
            {
                float volume = PhoneticAudioManager.Instance.GetActiveMicrophoneVolume();
                micLevelText.text = $"Mic Input: {(volume * 100f):F1}% | " + new string('█', (int)(volume * 10));
            }
        }

        /// <summary>
        /// Transition the game state to launch the selected game level.
        /// </summary>
        private void LoadGame(GameState targetGame, Button button)
        {
            if (UiJuiceManager.Instance != null)
            {
                UiJuiceManager.Instance.ApplyButtonJuice(button.transform);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TransitionToState(targetGame);
            }
            else
            {
                Debug.LogError($"[MainMenuController] GameManager not found! Cannot transition to {targetGame}.");
            }
        }

        /// <summary>
        /// Scan for connected microphone devices and configure start state.
        /// </summary>
        private void InitializeDiagnostics()
        {
            if (PhoneticAudioManager.Instance != null)
            {
                bool hasMic = PhoneticAudioManager.Instance.HasMicrophoneConnected(out _connectedMicDevice);
                if (hasMic)
                {
                    UpdateStatus($"Ready. Connected Mic: {_connectedMicDevice}");
                }
                else
                {
                    UpdateStatus("Warning: No microphone detected.");
                    if (testMicButton != null) testMicButton.enabled = false;
                }
            }
            else
            {
                UpdateStatus("Error: PhoneticAudioManager not initialized.");
            }
        }

        /// <summary>
        /// Event triggered when user clicks "Play TTS" on the Synth Voice Pro diagnostic panel.
        /// </summary>
        private async void OnTestTtsClicked()
        {
            if (PhoneticAudioManager.Instance == null) return;

            string ssml = ssmlTextInputField != null ? ssmlTextInputField.text : "";
            if (string.IsNullOrWhiteSpace(ssml))
            {
                // Fallback default SSML if empty
                ssml = "<phoneme alphabet=\"ipa\" ph=\"kæt\">cat</phoneme>";
                if (ssmlTextInputField != null) ssmlTextInputField.text = ssml;
            }

            UpdateStatus("Synthesizing audio clip...");
            try
            {
                // Cache key unique to test text
                string testKey = "test_diagnostics_" + Math.Abs(ssml.GetHashCode());
                await PhoneticAudioManager.Instance.PlayPhoneticClipAsync(ssml, testKey);
                UpdateStatus("TTS synthesis complete, playing audio.");
            }
            catch (Exception ex)
            {
                UpdateStatus($"TTS synthesis failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Event triggered when user toggles microphone recording test.
        /// </summary>
        private void OnTestMicClicked()
        {
            if (PhoneticAudioManager.Instance == null || string.IsNullOrEmpty(_connectedMicDevice)) return;

            if (!_isRecordingMic)
            {
                PhoneticAudioManager.Instance.StartMicrophoneRecord(_connectedMicDevice);
                _isRecordingMic = true;
                UpdateStatus("Recording microphone... speak into mic.");
                if (testMicButton != null)
                {
                    var textComp = testMicButton.GetComponentInChildren<TMP_Text>();
                    if (textComp != null) textComp.text = "Stop Test";
                }
            }
            else
            {
                float peakLevel = PhoneticAudioManager.Instance.StopMicrophoneRecord();
                _isRecordingMic = false;
                UpdateStatus($"Mic test stopped. Peak volume captured: {(peakLevel * 100f):F1}%");
                
                if (micLevelText != null)
                {
                    micLevelText.text = "Mic Input: Idle";
                }

                if (testMicButton != null)
                {
                    var textComp = testMicButton.GetComponentInChildren<TMP_Text>();
                    if (textComp != null) textComp.text = "Record Mic";
                }
            }
        }

        private void UpdateStatus(string message)
        {
            Debug.Log($"[MainMenuUI] {message}");
            if (statusDisplayText != null)
            {
                statusDisplayText.text = $"Status: {message}";
            }
        }
    }
}
