using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using PhoneticsEdu.Core;
using PhoneticsEdu.UI;

namespace PhoneticsEdu.Games.WordBlaster
{
    /// <summary>
    /// Level 1: Word Blaster Game Manager.
    /// Manages the Space Invaders-style gameplay loop where players blast words of a sentence in chronological order.
    /// </summary>
    public class WordBlasterManager : PhoneticGameModule
    {
        [Header("Prefabs & Visuals")]
        [SerializeField] private GameObject? wordShipPrefab;
        [SerializeField] private GameObject? explosionPrefab;

        [Header("Gameplay Settings")]
        [SerializeField] private float shipSpeed = 1.5f;
        [SerializeField] private float spawnRateSeconds = 2.0f;

        [Header("Sentence Database")]
        [SerializeField] private string[] targetSentences = new[]
        {
            "The quick brown fox",
            "Phonetics is very fun",
            "Zap the correct word ship"
        };

        [SerializeField] private string[] distractorWords = new[]
        {
            "cat", "dog", "jumped", "run", "slowly", "big", "blue", "sky", "happy", "tree", "bird", "cloud"
        };

        // Event dispatched when a correct ship is blown up
        public event Action<string>? OnWordShipExploded;

        private readonly List<WordShip> _activeShips = new();
        
        private int _currentSentenceIndex = 0;
        private string[] _currentSentenceWords = Array.Empty<string>();
        private int _nextWordIndexToZap = 0;

        // Statistics
        private int _wordsZappedCorrectly = 0;
        private int _totalZapsAttempted = 0;

        protected override void Awake()
        {
            moduleName = "Word Blaster (Level 1)";
            base.Awake();
        }

        public override void InitializeGame()
        {
            base.InitializeGame();
            _activeShips.Clear();
            _currentSentenceIndex = 0;
            _wordsZappedCorrectly = 0;
            _totalZapsAttempted = 0;
            currentScore = 0;

            Debug.Log($"[{moduleName}] Game Initialized. Ready to play {targetSentences.Length} sentences.");
        }

        public override void StartGame()
        {
            base.StartGame();
            StartSentenceRound(_currentSentenceIndex);
#if !MOCK_UNITY_COMPILATION
            StartCoroutine(SpawnRoutine());
#endif
        }

        public override void EndGame()
        {
            // Clean up any remaining ships
            ClearAllActiveShips();
#if !MOCK_UNITY_COMPILATION
            StopAllCoroutines();
#endif
            base.EndGame();
        }

        /// <summary>
        /// Splits target sentence and plays it aloud via the PhoneticAudioManager.
        /// </summary>
        public async void StartSentenceRound(int sentenceIdx)
        {
            if (sentenceIdx >= targetSentences.Length)
            {
                Debug.Log($"[{moduleName}] All sentences completed!");
                EndGame();
                return;
            }

            _currentSentenceIndex = sentenceIdx;
            string rawSentence = targetSentences[_currentSentenceIndex];
            
            // Split by space, strip punctuation
            _currentSentenceWords = SplitSentenceIntoWords(rawSentence);
            _nextWordIndexToZap = 0;

            Debug.Log($"[{moduleName}] Starting Round {sentenceIdx + 1}: \"{rawSentence}\"");

            if (audioManager != null)
            {
                // Convert text to a phonetic-friendly SSML wrapper
                string ssml = $"<speak>{rawSentence}</speak>";
                string cacheKey = $"level1_sentence_{sentenceIdx}";
                await audioManager.PlayPhoneticClipAsync(ssml, cacheKey);
            }
        }

        /// <summary>
        /// Main spawning routine running inside Unity.
        /// </summary>
        private IEnumerator SpawnRoutine()
        {
            while (isGameActive)
            {
                yield return new WaitForSeconds(spawnRateSeconds);
                SpawnRandomShip();
            }
        }

        /// <summary>
        /// Spawns a single ship. Chooses whether to spawn a target word or a distractor word.
        /// </summary>
        public void SpawnRandomShip()
        {
            if (!isGameActive) return;

            // 50% chance to spawn the next correct target word if we still have words left
            bool spawnCorrect = UnityEngine.Random.Range(0f, 1f) > 0.5f;

            if (spawnCorrect && _nextWordIndexToZap < _currentSentenceWords.Length)
            {
                // Find a target word that has not been zapped yet
                int targetWordIdx = UnityEngine.Random.Range(_nextWordIndexToZap, _currentSentenceWords.Length);
                string word = _currentSentenceWords[targetWordIdx];
                SpawnShip(word, true);
            }
            else
            {
                // Spawn a distractor word
                int distractorIdx = UnityEngine.Random.Range(0, distractorWords.Length);
                string word = distractorWords[distractorIdx];
                SpawnShip(word, false);
            }
        }

        /// <summary>
        /// Instantiates a new word ship object and configures it.
        /// </summary>
        private void SpawnShip(string word, bool isCorrect)
        {
            GameObject shipObj;
#if MOCK_UNITY_COMPILATION
            shipObj = new GameObject();
#else
            shipObj = Instantiate(wordShipPrefab != null ? wordShipPrefab : new GameObject());
#endif
            WordShip ship = shipObj.GetComponent<WordShip>();
            if (ship == null)
            {
                ship = shipObj.AddComponent<WordShip>();
            }

            // Spawn at random X, top of screen Y = 6.0f
            float spawnX = UnityEngine.Random.Range(-4.0f, 4.0f);
            shipObj.transform.position = new Vector3(spawnX, 6.0f, 0f);

            ship.Initialize(word, isCorrect, shipSpeed, this);
            _activeShips.Add(ship);

            Debug.Log($"[{moduleName}] Spawned word ship: '{word}' (IsCorrect: {isCorrect})");
        }

        /// <summary>
        /// Callback triggered when a player zaps/taps a word ship.
        /// </summary>
        public void ZapShip(WordShip ship)
        {
            if (!isGameActive) return;

            _totalZapsAttempted++;
            string zappedWord = CleanWord(ship.DisplayWord);
            string targetWord = CleanWord(_currentSentenceWords[_nextWordIndexToZap]);

            if (zappedWord.Equals(targetWord, StringComparison.OrdinalIgnoreCase))
            {
                // Correct chronologically!
                _wordsZappedCorrectly++;
                _nextWordIndexToZap++;
                currentScore += 100;

                Debug.Log($"[{moduleName}] CORRECT hit! Zapped '{ship.DisplayWord}' in sequence. Score: {currentScore}");
                
                // Trigger visual juice and feedback
                if (UiJuiceManager.Instance != null)
                {
                    UiJuiceManager.Instance.TriggerCameraShake(0.25f, 0.12f);
                    UiJuiceManager.Instance.TriggerStarBurst(ship.transform.position);
                    UiJuiceManager.Instance.PopFloatingText("+100 PTS!", ship.transform.position, Color.green);
                }

                // Trigger explosion effect
                TriggerExplosion(ship.transform.position);
                OnWordShipExploded?.Invoke(ship.DisplayWord);

                _activeShips.Remove(ship);

                // Check round completion
                if (_nextWordIndexToZap >= _currentSentenceWords.Length)
                {
                    Debug.Log($"[{moduleName}] Completed sentence: \"{targetSentences[_currentSentenceIndex]}\"!");
                    if (UiJuiceManager.Instance != null)
                    {
                        UiJuiceManager.Instance.TriggerConfetti(new Vector3(0f, 0f, 0f));
                    }
                    ClearAllActiveShips();
                    
                    // Proceed to next sentence
                    StartSentenceRound(_currentSentenceIndex + 1);
                }
            }
            else
            {
                // Incorrect: either it was a dummy word, or a correct word out of sequence
                currentScore = Math.Max(0, currentScore - 25);
                Debug.LogWarning($"[{moduleName}] INCORRECT hit! Zapped '{ship.DisplayWord}' but expected target word: '{_currentSentenceWords[_nextWordIndexToZap]}'. Score: {currentScore}");
                
                if (UiJuiceManager.Instance != null)
                {
                    UiJuiceManager.Instance.PopFloatingText("WRONG ORDER!", ship.transform.position, Color.red);
                }

                _activeShips.Remove(ship);
            }
        }

        /// <summary>
        /// Callback triggered when a word ship escapes past the bottom boundary.
        /// </summary>
        public void OnShipEscaped(WordShip ship)
        {
            _activeShips.Remove(ship);

            // Penalty if the correct target word escaped
            if (ship.IsCorrectWord && CleanWord(ship.DisplayWord).Equals(CleanWord(_currentSentenceWords[_nextWordIndexToZap]), StringComparison.OrdinalIgnoreCase))
            {
                currentScore = Math.Max(0, currentScore - 10);
                Debug.Log($"[{moduleName}] Target word '{ship.DisplayWord}' escaped! Penalty applied. Score: {currentScore}");
            }
        }

        /// <summary>
        /// Instantiates an explosion effect.
        /// </summary>
        private void TriggerExplosion(Vector3 position)
        {
#if !MOCK_UNITY_COMPILATION
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, position, Quaternion.identity);
            }
#endif
            Debug.Log($"[{moduleName}] Explosion triggered at position ({position.x}, {position.y}, {position.z})");
        }

        /// <summary>
        /// Clears all currently active ships from the screen.
        /// </summary>
        private void ClearAllActiveShips()
        {
            // Duplicate active list to prevent collection modification errors during destroy
            var shipsToDestroy = new List<WordShip>(_activeShips);
            _activeShips.Clear();

            foreach (var ship in shipsToDestroy)
            {
                if (ship != null && ship.gameObject != null)
                {
                    Destroy(ship.gameObject);
                }
            }
        }

        #region Helper Utilities

        private string[] SplitSentenceIntoWords(string sentence)
        {
            // Strip punctuation and split by space
            string cleaned = Regex.Replace(sentence, @"[^\w\s]", "");
            return cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private string CleanWord(string word)
        {
            return Regex.Replace(word, @"[^\w]", "").Trim();
        }

        public float GetAccuracy()
        {
            if (_totalZapsAttempted == 0) return 1.0f; // Start at 100%
            return (float)_wordsZappedCorrectly / _totalZapsAttempted;
        }

        public int GetWordsZappedCount() => _wordsZappedCorrectly;
        public int GetTotalZapsCount() => _totalZapsAttempted;
        public List<WordShip> GetActiveShips() => _activeShips;
        public int GetNextWordIndexToZap() => _nextWordIndexToZap;
        public string[] GetCurrentSentenceWords() => _currentSentenceWords;

        #endregion
    }
}

