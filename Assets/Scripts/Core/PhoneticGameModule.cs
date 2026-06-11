using System;
using UnityEngine;

namespace PhoneticsEdu.Core
{
    /// <summary>
    /// Base class for all game modules in the Phonetics Education App.
    /// Provides standard lifecycle methods and score tracking.
    /// </summary>
    public abstract class PhoneticGameModule : MonoBehaviour
    {
        [Header("Module Configuration")]
        [SerializeField] protected string moduleName = "Phonetic Game Module";
        
        protected GameManager? gameManager;
        protected PhoneticAudioManager? audioManager;

        protected int currentScore = 0;
        protected bool isGameActive = false;

        public int CurrentScore => currentScore;
        public bool IsGameActive => isGameActive;

        protected virtual void Awake()
        {
            // Find references if not already assigned
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }
            if (audioManager == null)
            {
                audioManager = PhoneticAudioManager.Instance;
            }
        }

        /// <summary>
        /// Initial set up for the game level. Called once when loading.
        /// </summary>
        public virtual void InitializeGame()
        {
            currentScore = 0;
            isGameActive = false;
            Debug.Log($"[{moduleName}] Game Initialized.");
        }

        /// <summary>
        /// Starts the game level logic.
        /// </summary>
        public virtual void StartGame()
        {
            isGameActive = true;
            Debug.Log($"[{moduleName}] Game Started.");
        }

        /// <summary>
        /// Ends the game level and reports the score.
        /// </summary>
        public virtual void EndGame()
        {
            isGameActive = false;
            Debug.Log($"[{moduleName}] Game Ended. Final Score: {currentScore}");
            SubmitScore(currentScore);
        }

        /// <summary>
        /// Submits the final level score to the GameManager.
        /// </summary>
        protected virtual void SubmitScore(int score)
        {
            if (gameManager != null)
            {
                gameManager.UpdateScoreForCurrentLevel(score);
            }
            else
            {
                Debug.LogWarning($"[{moduleName}] GameManager not found, score of {score} could not be persisted.");
            }
        }
    }
}
