using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhoneticsEdu.Core;
using PhoneticsEdu.UI;

namespace PhoneticsEdu.Games.OnsetRime
{
    /// <summary>
    /// Database record representing an Onset-Rime syllable combination.
    /// </summary>
    public class OnsetRimeWord
    {
        public string FullWord { get; }
        public string Onset { get; }
        public string Rime { get; }

        public OnsetRimeWord(string fullWord, string onset, string rime)
        {
            FullWord = fullWord;
            Onset = onset;
            Rime = rime;
        }
    }

    /// <summary>
    /// Level 3: Onset-Rime Constructor Game Manager.
    /// Manages falling onset blocks and matching stationary rime blocks.
    /// </summary>
    public class OnsetRimeManager : PhoneticGameModule
    {
        [Header("Prefabs & Visuals")]
        [SerializeField] private GameObject? blockPrefab;
        [SerializeField] private float dropSpeed = 1.0f;

        private readonly List<OnsetRimeWord> _wordsDatabase = new()
        {
            new OnsetRimeWord("cat", "c", "at"),
            new OnsetRimeWord("string", "str", "ing"),
            new OnsetRimeWord("play", "pl", "ay"),
            new OnsetRimeWord("flat", "fl", "at")
        };

        // Event fired when the correct Onset block is snapped onto the matching Rime block
        public event Action<string>? OnWordConstructed;

        private readonly List<PhoneticBlock> _activeRimeBlocks = new();
        private PhoneticBlock? _fallingOnsetBlock;

        private int _currentWordIndex = 0;

        // Statistics
        private int _wordsConstructedCorrectly = 0;
        private int _totalAttempts = 0;

        protected override void Awake()
        {
            moduleName = "Onset-Rime Constructor (Level 3)";
            base.Awake();
        }

        public override void InitializeGame()
        {
            base.InitializeGame();
            ClearAllBlocks();
            _currentWordIndex = 0;
            _wordsConstructedCorrectly = 0;
            _totalAttempts = 0;
            currentScore = 0;

            Debug.Log($"[{moduleName}] Game Initialized. Database contains {_wordsDatabase.Count} words.");
        }

        public override void StartGame()
        {
            base.StartGame();
            StartTargetRound(_currentWordIndex);
        }

        public override void EndGame()
        {
            ClearAllBlocks();
            base.EndGame();
        }

        /// <summary>
        /// Starts a round targeting a specific word, voicing it, spawning rimes at the bottom,
        /// and launching the falling onset block.
        /// </summary>
        public async void StartTargetRound(int index)
        {
            if (index >= _wordsDatabase.Count)
            {
                Debug.Log($"[{moduleName}] All words successfully constructed!");
                EndGame();
                return;
            }

            _currentWordIndex = index;
            ClearAllBlocks();

            OnsetRimeWord target = _wordsDatabase[_currentWordIndex];
            Debug.Log($"[{moduleName}] Starting Round {index + 1}: Target word is \"{target.FullWord}\" (Spell: {target.Onset} + {target.Rime})");

            // 1. Voice target word
            if (audioManager != null)
            {
                string ssml = $"<speak>Construct the word: {target.FullWord}</speak>";
                string key = $"level3_target_{index}";
                _ = audioManager.PlayPhoneticClipAsync(ssml, key);
            }

            // 2. Spawn Rime blocks at the bottom row (Y = -4.0f)
            // Left block (X = -1.5f), Right block (X = 1.5f)
            // Determine a distractor Rime from the database
            string distractorRime = GetDistractorRime(target.Rime);

            // Statically position them
            SpawnRimeBlock(target.Rime, -1.5f);
            SpawnRimeBlock(distractorRime, 1.5f);

            // 3. Spawn falling Onset block at the top center (X = 0.0f, Y = 6.0f)
            SpawnOnsetBlock(target.Onset, 0.0f);
        }

        /// <summary>
        /// Helper to choose a distractor rime from the database that is different from the target rime.
        /// </summary>
        private string GetDistractorRime(string targetRime)
        {
            foreach (var w in _wordsDatabase)
            {
                if (w.Rime != targetRime)
                {
                    return w.Rime;
                }
            }
            return "ed"; // Default backup distractor
        }

        private void SpawnRimeBlock(string rime, float xPos)
        {
            GameObject blockObj;
#if MOCK_UNITY_COMPILATION
            blockObj = new GameObject();
#else
            blockObj = Instantiate(blockPrefab != null ? blockPrefab : new GameObject());
#endif
            PhoneticBlock block = blockObj.GetComponent<PhoneticBlock>();
            if (block == null)
            {
                block = blockObj.AddComponent<PhoneticBlock>();
            }

            blockObj.transform.position = new Vector3(xPos, -4.0f, 0f);
            block.Initialize(rime, false, true, 0f, this);
            _activeRimeBlocks.Add(block);

            Debug.Log($"[{moduleName}] Spawned static Rime block: '-{rime}' at X: {xPos}");
        }

        private void SpawnOnsetBlock(string onset, float xPos)
        {
            GameObject blockObj;
#if MOCK_UNITY_COMPILATION
            blockObj = new GameObject();
#else
            blockObj = Instantiate(blockPrefab != null ? blockPrefab : new GameObject());
#endif
            _fallingOnsetBlock = blockObj.GetComponent<PhoneticBlock>();
            if (_fallingOnsetBlock == null)
            {
                _fallingOnsetBlock = blockObj.AddComponent<PhoneticBlock>();
            }

            blockObj.transform.position = new Vector3(xPos, 6.0f, 0f);
            _fallingOnsetBlock.Initialize(onset, true, false, dropSpeed, this);

            Debug.Log($"[{moduleName}] Spawned falling Onset block: '{onset}-' at top");
        }

        /// <summary>
        /// Slides the active falling Onset block horizontally.
        /// </summary>
        public void SlideActiveBlock(float deltaX)
        {
            if (_fallingOnsetBlock != null && !_fallingOnsetBlock.IsStatic)
            {
                _fallingOnsetBlock.Slide(deltaX);
            }
        }

        /// <summary>
        /// Triggered when the falling Onset block snaps to the bottom row.
        /// </summary>
        public void SnapOnsetBlock(PhoneticBlock onsetBlock)
        {
            if (!isGameActive) return;

            _totalAttempts++;
            OnsetRimeWord target = _wordsDatabase[_currentWordIndex];

            // Locate the closest Rime block horizontally
            PhoneticBlock? matchedRime = null;
            float minDistance = float.MaxValue;

            foreach (var rime in _activeRimeBlocks)
            {
                float distance = Math.Abs(rime.transform.position.x - onsetBlock.transform.position.x);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    matchedRime = rime;
                }
            }

            if (matchedRime != null)
            {
                string combined = (onsetBlock.PhonemeText + matchedRime.PhonemeText).ToLower().Trim();
                string targetWord = target.FullWord.ToLower().Trim();

                if (combined.Equals(targetWord))
                {
                    // Correct word construction!
                    _wordsConstructedCorrectly++;
                    currentScore += 200;

                    Debug.Log($"[{moduleName}] CORRECT match! Spelled \"{combined}\" successfully. Score: {currentScore}");

                    // Trigger visual feedback
                    if (UiJuiceManager.Instance != null)
                    {
                        UiJuiceManager.Instance.PopFloatingText("SPELLED! +200 PTS!", onsetBlock.transform.position, Color.green);
                        UiJuiceManager.Instance.TriggerStarBurst(onsetBlock.transform.position);
                        UiJuiceManager.Instance.TriggerCameraShake(0.2f, 0.1f);
                    }

                    // Play the pronunciation of the merged word
                    if (audioManager != null)
                    {
                        string ssml = $"<speak>{combined}</speak>";
                        string key = $"level3_constructed_{combined}";
                        _ = audioManager.PlayPhoneticClipAsync(ssml, key);
                    }

                    OnWordConstructed?.Invoke(combined);

                    // Clear and load next target word
                    StartTargetRound(_currentWordIndex + 1);
                }
                else
                {
                    // Incorrect combination (spelled wrong word or gibberish)
                    currentScore = Math.Max(0, currentScore - 50);
                    Debug.LogWarning($"[{moduleName}] INCORRECT match! Spelled \"{combined}\" but expected target: \"{targetWord}\". Score: {currentScore}");

                    // Trigger warning visual feedback
                    if (UiJuiceManager.Instance != null)
                    {
                        UiJuiceManager.Instance.PopFloatingText("TRY AGAIN!", onsetBlock.transform.position, Color.red);
                        UiJuiceManager.Instance.ApplyWobble(matchedRime.transform);
                    }

                    // Play the incorrect blend sound for phonetic feedback
                    if (audioManager != null)
                    {
                        string ssml = $"<speak>{combined}</speak>";
                        string key = $"level3_incorrect_blend_{combined}";
                        _ = audioManager.PlayPhoneticClipAsync(ssml, key);
                    }

                    // Destroy the failed onset block and respawn a fresh one
                    Destroy(onsetBlock.gameObject);
                    _fallingOnsetBlock = null;
                    SpawnOnsetBlock(target.Onset, 0.0f);
                }
            }
        }

        private void ClearAllBlocks()
        {
            if (_fallingOnsetBlock != null)
            {
                if (_fallingOnsetBlock.gameObject != null)
                {
                    Destroy(_fallingOnsetBlock.gameObject);
                }
                _fallingOnsetBlock = null;
            }

            foreach (var rime in _activeRimeBlocks)
            {
                if (rime != null && rime.gameObject != null)
                {
                    Destroy(rime.gameObject);
                }
            }
            _activeRimeBlocks.Clear();
        }

        #region Statistics & Accessors

        public float GetAccuracy()
        {
            if (_totalAttempts == 0) return 1.0f;
            return (float)_wordsConstructedCorrectly / _totalAttempts;
        }

        public int GetWordsConstructedCount() => _wordsConstructedCorrectly;
        public int GetTotalAttemptsCount() => _totalAttempts;
        public PhoneticBlock? GetFallingOnsetBlock() => _fallingOnsetBlock;
        public List<PhoneticBlock> GetActiveRimeBlocks() => _activeRimeBlocks;
        public string GetTargetWordString() => _wordsDatabase[_currentWordIndex].FullWord;

        #endregion
    }
}
