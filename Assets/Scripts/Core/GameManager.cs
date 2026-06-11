using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PhoneticsEdu.Core
{
    /// <summary>
    /// Game states mapping to the Phonological Awareness Hierarchy levels.
    /// </summary>
    public enum GameState
    {
        MainMenu,
        WordBlaster,       // Level 1: Word-level awareness
        SyllableShredder,  // Level 2: Syllable level
        OnsetRime,         // Level 3: Onset-Rime level
        PhonemeIsolator    // Level 4: Phoneme isolation/manipulation level
    }

    /// <summary>
    /// Central manager for the application. Implements a State Machine for scene transitions
    /// and persists global scoring state across all levels.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; } = null!;

        [Header("Scene Configurations")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string level1SceneName = "WordBlasterScene";
        [SerializeField] private string level2SceneName = "SyllableShredderScene";
        [SerializeField] private string level3SceneName = "OnsetRimeScene";
        [SerializeField] private string level4SceneName = "PhonemeIsolatorScene";

        // State Machine Events
        public event Action<GameState>? OnStateChanged;
        public event Action<GameState, int>? OnScoreUpdated;

        // Scoring database
        private readonly Dictionary<GameState, int> _levelScores = new()
        {
            { GameState.WordBlaster, 0 },
            { GameState.SyllableShredder, 0 },
            { GameState.OnsetRime, 0 },
            { GameState.PhonemeIsolator, 0 }
        };

        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Transition to MainMenu on startup
            TransitionToState(GameState.MainMenu);
        }

        /// <summary>
        /// Transition the application state machine to a new state and load the corresponding scene.
        /// </summary>
        public void TransitionToState(GameState newState)
        {
            Debug.Log($"[GameManager] Transitioning state: {CurrentState} -> {newState}");
            CurrentState = newState;

            string targetScene = newState switch
            {
                GameState.MainMenu => mainMenuSceneName,
                GameState.WordBlaster => level1SceneName,
                GameState.SyllableShredder => level2SceneName,
                GameState.OnsetRime => level3SceneName,
                GameState.PhonemeIsolator => level4SceneName,
                _ => mainMenuSceneName
            };

            LoadSceneAsync(targetScene);
            OnStateChanged?.Invoke(CurrentState);
        }

        /// <summary>
        /// Loads a scene asynchronously.
        /// </summary>
        private void LoadSceneAsync(string sceneName)
        {
            Debug.Log($"[GameManager] Asynchronously loading scene: {sceneName}");
            // In Unity, this will load the scene. Under stubs, it runs a mock log.
            SceneManager.LoadSceneAsync(sceneName);
        }

        /// <summary>
        /// Update the score for the level currently running.
        /// </summary>
        public void UpdateScoreForCurrentLevel(int score)
        {
            if (CurrentState == GameState.MainMenu)
            {
                Debug.LogWarning("[GameManager] Cannot update level score while in MainMenu state.");
                return;
            }

            if (_levelScores.ContainsKey(CurrentState))
            {
                // We keep the highest score as the persistent score, or update it
                _levelScores[CurrentState] = Math.Max(_levelScores[CurrentState], score);
                Debug.Log($"[GameManager] Level {CurrentState} score updated to {_levelScores[CurrentState]}.");
                OnScoreUpdated?.Invoke(CurrentState, _levelScores[CurrentState]);
            }
        }

        /// <summary>
        /// Retrieve the score for a specific game level.
        /// </summary>
        public int GetScoreForLevel(GameState state)
        {
            return _levelScores.TryGetValue(state, out int score) ? score : 0;
        }

        /// <summary>
        /// Calculate the total score accumulated across all games.
        /// </summary>
        public int GetTotalScore()
        {
            int total = 0;
            foreach (var score in _levelScores.Values)
            {
                total += score;
            }
            return total;
        }

        // Mock method to simulate loading scene for testing purposes
        private void DontDestroyOnLoad(GameObject target)
        {
#if !MOCK_UNITY_COMPILATION
            UnityEngine.Object.DontDestroyOnLoad(target);
#endif
        }

        private void Destroy(GameObject target)
        {
#if !MOCK_UNITY_COMPILATION
            UnityEngine.Object.Destroy(target);
#endif
        }
    }
}
