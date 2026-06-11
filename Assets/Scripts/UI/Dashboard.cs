using UnityEngine;
using TMPro;
using PhoneticsEdu.Core;

namespace PhoneticsEdu.UI
{
    /// <summary>
    /// UI controller to track and display the user's progress and scores across
    /// the 4 Phonetics games.
    /// </summary>
    public class Dashboard : MonoBehaviour
    {
        [Header("Score Display Text Labels")]
        [SerializeField] private TMP_Text? wordBlasterScoreText;
        [SerializeField] private TMP_Text? syllableShredderScoreText;
        [SerializeField] private TMP_Text? onsetRimeScoreText;
        [SerializeField] private TMP_Text? phonemeIsolatorScoreText;
        [SerializeField] private TMP_Text? totalScoreText;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreUpdated += HandleScoreUpdated;
                RefreshAllScores();
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreUpdated -= HandleScoreUpdated;
            }
        }

        /// <summary>
        /// Recalculates and displays all scores by querying the GameManager.
        /// </summary>
        public void RefreshAllScores()
        {
            if (GameManager.Instance == null) return;

            UpdateScoreLabel(wordBlasterScoreText, "Word Blaster", GameManager.Instance.GetScoreForLevel(GameState.WordBlaster));
            UpdateScoreLabel(syllableShredderScoreText, "Syllable Shredder", GameManager.Instance.GetScoreForLevel(GameState.SyllableShredder));
            UpdateScoreLabel(onsetRimeScoreText, "Onset-Rime", GameManager.Instance.GetScoreForLevel(GameState.OnsetRime));
            UpdateScoreLabel(phonemeIsolatorScoreText, "Phoneme Isolator", GameManager.Instance.GetScoreForLevel(GameState.PhonemeIsolator));

            if (totalScoreText != null)
            {
                totalScoreText.text = $"Total Score: {GameManager.Instance.GetTotalScore()} pts";
            }
        }

        private void HandleScoreUpdated(GameState state, int newScore)
        {
            RefreshAllScores();
        }

        private void UpdateScoreLabel(TMP_Text? label, string gameName, int score)
        {
            if (label != null)
            {
                label.text = $"{gameName}: {score} pts";
            }
        }
    }
}
