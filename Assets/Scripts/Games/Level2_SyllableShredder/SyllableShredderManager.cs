using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using PhoneticsEdu.Core;
using PhoneticsEdu.UI;

namespace PhoneticsEdu.Games.SyllableShredder
{
    /// <summary>
    /// Database record representing a multisyllabic word.
    /// </summary>
    public class SyllableWord
    {
        public string FullWord { get; }
        public string[] Syllables { get; }

        public SyllableWord(string fullWord, string[] syllables)
        {
            FullWord = fullWord;
            Syllables = syllables;
        }
    }

    /// <summary>
    /// Level 2: Syllable Shredder Game Manager.
    /// Manages Fruit Ninja-style mechanics where players slice syllable blocks in chronological order.
    /// </summary>
    public class SyllableShredderManager : PhoneticGameModule
    {
        [Header("Prefabs & Visuals")]
        [SerializeField] private GameObject? syllableBlockPrefab;
        [SerializeField] private GameObject? sliceParticlePrefab;

        [Header("Gameplay Config")]
        [SerializeField] private float gravityValue = 9.81f;
        
        private readonly List<SyllableWord> _wordsDatabase = new()
        {
            new SyllableWord("cooperate", new[] { "co", "o", "pe", "rate" }),
            new SyllableWord("phonetics", new[] { "pho", "ne", "tics" }),
            new SyllableWord("computer", new[] { "com", "pu", "ter" }),
            new SyllableWord("education", new[] { "ed", "u", "ca", "tion" })
        };

        // Event fired when syllables of a word are successfully zapped in order
        public event Action<string>? OnWordSyllablesMerged;

        private readonly List<SyllableBlock> _activeBlocks = new();

        private int _currentWordIndex = 0;
        private int _nextSyllableIndexToSlice = 0;

        // Statistics
        private int _syllablesSlicedCorrectly = 0;
        private int _totalSlicesAttempted = 0;

        protected override void Awake()
        {
            moduleName = "Syllable Shredder (Level 2)";
            base.Awake();
        }

        public override void InitializeGame()
        {
            base.InitializeGame();
            _activeBlocks.Clear();
            _currentWordIndex = 0;
            _syllablesSlicedCorrectly = 0;
            _totalSlicesAttempted = 0;
            currentScore = 0;

            Debug.Log($"[{moduleName}] Game Initialized. Database contains {_wordsDatabase.Count} words.");
        }

        public override void StartGame()
        {
            base.StartGame();
            StartWordRound(_currentWordIndex);
        }

        public override void EndGame()
        {
            ClearAllActiveBlocks();
            base.EndGame();
        }

        /// <summary>
        /// Starts the round by playing the pronunciation and tossing blocks.
        /// </summary>
        public async void StartWordRound(int wordIdx)
        {
            if (wordIdx >= _wordsDatabase.Count)
            {
                Debug.Log($"[{moduleName}] All words successfully sliced and merged!");
                EndGame();
                return;
            }

            _currentWordIndex = wordIdx;
            _nextSyllableIndexToSlice = 0;
            SyllableWord target = _wordsDatabase[_currentWordIndex];

            Debug.Log($"[{moduleName}] Starting Round {wordIdx + 1}: Word is \"{target.FullWord}\" (Syllables: {string.Join("-", target.Syllables)})");

            // 1. Play the full word voice-over
            if (audioManager != null)
            {
                string fullWordSsml = $"<speak>{target.FullWord}</speak>";
                string fullWordKey = $"level2_fullword_{wordIdx}";
                await audioManager.PlayPhoneticClipAsync(fullWordSsml, fullWordKey);
            }

            // 2. Play the syllables chunk-by-chunk in the background with a delay
            VoiceSyllablesChunkedAsync(target.Syllables, wordIdx);

            // 3. Toss the blocks up onto the screen container
            TossSyllableBlocks(target.Syllables);
        }

        /// <summary>
        /// Voices the syllables in chronological order in a non-blocking sequence.
        /// </summary>
        private async void VoiceSyllablesChunkedAsync(string[] syllables, int wordIdx)
        {
            if (audioManager == null) return;

            // Wait a brief moment for the full word voice-over to complete
            await Task.Delay(1200);

            for (int i = 0; i < syllables.Length; i++)
            {
                if (!isGameActive || _currentWordIndex != wordIdx) break;

                string syllable = syllables[i];
                string ssml = $"<speak>{syllable}</speak>";
                string key = $"level2_chunk_{wordIdx}_{i}";
                
                await audioManager.PlayPhoneticClipAsync(ssml, key);
                await Task.Delay(800); // Stagger chunk voices
            }
        }

        /// <summary>
        /// Spawns blocks and launches them upwards in scattered trajectories.
        /// </summary>
        private void TossSyllableBlocks(string[] syllables)
        {
            float horizontalSpan = 4f; // Distribute spawn X between -2f and 2f
            float step = syllables.Length > 1 ? horizontalSpan / (syllables.Length - 1) : 0f;

            for (int i = 0; i < syllables.Length; i++)
            {
                GameObject blockObj;
#if MOCK_UNITY_COMPILATION
                blockObj = new GameObject();
#else
                blockObj = Instantiate(syllableBlockPrefab != null ? syllableBlockPrefab : new GameObject());
#endif
                SyllableBlock block = blockObj.GetComponent<SyllableBlock>();
                if (block == null)
                {
                    block = blockObj.AddComponent<SyllableBlock>();
                }

                // Initial position at bottom (Y = -5.0f)
                float spawnX = -2f + (i * step);
                blockObj.transform.position = new Vector3(spawnX, -5.0f, 0f);

                // Initial Fruit Ninja upward trajectories
                float vx = UnityEngine.Random.Range(-1.0f, 1.0f);
                float vy = UnityEngine.Random.Range(7.5f, 9.5f); // Upward velocity
                Vector3 launchVelocity = new Vector3(vx, vy, 0f);

                block.Initialize(syllables[i], i, launchVelocity, this);
                _activeBlocks.Add(block);

                Debug.Log($"[{moduleName}] Tossed syllable block: '{syllables[i]}' at Index {i} (Velocity: {launchVelocity.x:F1}, {launchVelocity.y:F1})");
            }
        }

        /// <summary>
        /// Callback triggered by a SyllableBlock when sliced.
        /// </summary>
        public void SliceBlock(SyllableBlock block)
        {
            if (!isGameActive) return;

            _totalSlicesAttempted++;
            SyllableWord targetWord = _wordsDatabase[_currentWordIndex];

            if (block.SyllableIndex == _nextSyllableIndexToSlice)
            {
                // Correct chronological slice!
                _syllablesSlicedCorrectly++;
                _nextSyllableIndexToSlice++;
                currentScore += 150;

                Debug.Log($"[{moduleName}] CORRECT slice! Sliced syllable '{block.SyllableText}' in sequence. Score: {currentScore}");

                // Trigger visual feedback
                if (UiJuiceManager.Instance != null)
                {
                    UiJuiceManager.Instance.PopFloatingText("+150 PTS!", block.transform.position, Color.green);
                    UiJuiceManager.Instance.TriggerStarBurst(block.transform.position);
                }

                // Reinforce sound playback for this sliced chunk immediately
                if (audioManager != null)
                {
                    string ssml = $"<speak>{block.SyllableText}</speak>";
                    string key = $"level2_slice_reinforce_{_currentWordIndex}_{block.SyllableIndex}";
                    _ = audioManager.PlayPhoneticClipAsync(ssml, key); // Play non-blocking
                }

                TriggerSliceEffect(block.transform.position);
                _activeBlocks.Remove(block);

                // Check if all syllables of the word are completed
                if (_nextSyllableIndexToSlice >= targetWord.Syllables.Length)
                {
                    Debug.Log($"[{moduleName}] Word \"{targetWord.FullWord}\" successfully sliced in order! Merging syllables...");
                    
                    if (UiJuiceManager.Instance != null)
                    {
                        UiJuiceManager.Instance.TriggerCameraShake(0.3f, 0.15f);
                        UiJuiceManager.Instance.PopFloatingText("MERGED! +250 PTS!", new Vector3(0f, 0f, 0f), Color.yellow);
                        UiJuiceManager.Instance.TriggerConfetti(new Vector3(0f, 0f, 0f));
                    }

                    // Trigger merge explosion & merge event
                    TriggerMergeExplosion();
                    OnWordSyllablesMerged?.Invoke(targetWord.FullWord);

                    // Add completion bonus points
                    currentScore += 250;
                    ClearAllActiveBlocks();

                    // Advance to next word
                    StartWordRound(_currentWordIndex + 1);
                }
            }
            else
            {
                // Incorrect order or duplicate slice
                currentScore = Math.Max(0, currentScore - 50);
                Debug.LogWarning($"[{moduleName}] INCORRECT slice! Sliced '{block.SyllableText}' (Index: {block.SyllableIndex}) but expected index: {_nextSyllableIndexToSlice}. Score: {currentScore}");
                
                if (UiJuiceManager.Instance != null)
                {
                    UiJuiceManager.Instance.PopFloatingText("WRONG CHUNK!", block.transform.position, Color.red);
                }

                _activeBlocks.Remove(block);
            }
        }

        /// <summary>
        /// Callback when a block falls off the bottom screen boundary.
        /// </summary>
        public void OnBlockFell(SyllableBlock block)
        {
            _activeBlocks.Remove(block);

            // Apply minor score deduction if player missed a correct upcoming syllable
            if (block.SyllableIndex >= _nextSyllableIndexToSlice)
            {
                currentScore = Math.Max(0, currentScore - 15);
                Debug.Log($"[{moduleName}] Missed syllable block '{block.SyllableText}' fell past boundary. Score: {currentScore}");
            }
        }

        private void TriggerSliceEffect(Vector3 position)
        {
#if !MOCK_UNITY_COMPILATION
            if (sliceParticlePrefab != null)
            {
                Instantiate(sliceParticlePrefab, position, Quaternion.identity);
            }
#endif
            Debug.Log($"[{moduleName}] Slice visual particles spawned at ({position.x}, {position.y})");
        }

        private void TriggerMergeExplosion()
        {
            Debug.Log($"[{moduleName}] MERGE EXPLOSION! Unified syllables merged into word: \"{_wordsDatabase[_currentWordIndex].FullWord}\"");
        }

        private void ClearAllActiveBlocks()
        {
            var blocksToDestroy = new List<SyllableBlock>(_activeBlocks);
            _activeBlocks.Clear();

            foreach (var block in blocksToDestroy)
            {
                if (block != null && block.gameObject != null)
                {
                    Destroy(block.gameObject);
                }
            }
        }

        #region Statistics & Accessors

        public float GetAccuracy()
        {
            if (_totalSlicesAttempted == 0) return 1.0f;
            return (float)_syllablesSlicedCorrectly / _totalSlicesAttempted;
        }

        public int GetSyllablesSlicedCount() => _syllablesSlicedCorrectly;
        public int GetTotalSlicesCount() => _totalSlicesAttempted;
        public List<SyllableBlock> GetActiveBlocks() => _activeBlocks;
        public int GetNextSyllableIndexToSlice() => _nextSyllableIndexToSlice;
        public string[] GetCurrentWordSyllables() => _wordsDatabase[_currentWordIndex].Syllables;

        #endregion
    }
}
