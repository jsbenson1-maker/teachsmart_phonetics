using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhoneticsEdu.Core;
using PhoneticsEdu.UI;

namespace PhoneticsEdu.Games.PhonemeIsolator
{
    /// <summary>
    /// Database record representing a single spelling and IPA sound mutation.
    /// </summary>
    public class MutationData
    {
        public string SourceWord { get; }
        public string[] SourcePhonemes { get; }
        public int MutateIndex { get; }
        public string ReplacementPhoneme { get; }
        public string IpaPhonetic { get; }
        public string TargetWord { get; }
        public string RewardCardName { get; }

        public MutationData(string source, string[] sourcePhons, int index, string replacement, string ipa, string target, string card)
        {
            SourceWord = source;
            SourcePhonemes = sourcePhons;
            MutateIndex = index;
            ReplacementPhoneme = replacement;
            IpaPhonetic = ipa;
            TargetWord = target;
            RewardCardName = card;
        }
    }

    /// <summary>
    /// Level 4: Phoneme Isolator Game Manager.
    /// Manages spelling mutation in a magnifying glass viewer and card reward collectibles.
    /// </summary>
    public class PhonemeIsolatorManager : PhoneticGameModule
    {
        [Header("Prefabs & UI Components")]
        [SerializeField] private GameObject? slotPrefab;
        [SerializeField] private GameObject? bubblePrefab;

        private readonly List<MutationData> _mutationsDatabase = new()
        {
            new MutationData("bat", new[] { "b", "a", "t" }, 0, "c", "kæt", "cat", "Curious Cat Card"),
            new MutationData("cat", new[] { "c", "a", "t" }, 2, "p", "kæp", "cap", "Captain Cap Card"),
            new MutationData("cap", new[] { "c", "a", "p" }, 1, "o", "kɒp", "cop", "Copper Cop Card"),
            new MutationData("cop", new[] { "c", "o", "p" }, 0, "t", "tɒp", "top", "Spinning Top Card")
        };

        // Event fired when a spelling is mutated successfully, returning (newWord, rewardCard)
        public event Action<string, string>? OnWordMutated;

        private readonly List<PhonemeSlot> _activeSlots = new();
        private readonly List<PhonemeBubble> _activeBubbles = new();
        private readonly List<string> _earnedRewardCards = new();

        private string _currentBaselineWord = "bat";
        private string[] _currentPhonemes = new[] { "b", "a", "t" };

        // Statistics
        private int _correctMutationsCount = 0;
        private int _totalAttemptsCount = 0;

        protected override void Awake()
        {
            moduleName = "Phoneme Isolator (Level 4)";
            base.Awake();
        }

        public override void InitializeGame()
        {
            base.InitializeGame();
            _activeSlots.Clear();
            _activeBubbles.Clear();
            _earnedRewardCards.Clear();

            _currentBaselineWord = "bat";
            _currentPhonemes = new[] { "b", "a", "t" };
            
            _correctMutationsCount = 0;
            _totalAttemptsCount = 0;
            currentScore = 0;

            Debug.Log($"[{moduleName}] Game Initialized. Progression chain starts with baseline: \"{_currentBaselineWord}\"");
        }

        public override void StartGame()
        {
            base.StartGame();

            // 1. Play baseline voiceover instruction
            if (audioManager != null)
            {
                string ssml = "<speak>Start word is: <phoneme alphabet=\"ipa\" ph=\"bæt\">bat</phoneme></speak>";
                _ = audioManager.PlayPhoneticClipAsync(ssml, "level4_start_bat");
            }

            // 2. Spawn baseline slots inside the magnifier (stretching horizontally)
            SpawnWordSlots();

            // 3. Spawn floating IPA bubbles floating in the inventory panel
            SpawnPhonemeBubbles();
        }

        public override void EndGame()
        {
            ClearAllEntities();
            base.EndGame();
        }

        private void SpawnWordSlots()
        {
            float startX = -1.0f;
            float step = 1.0f;

            for (int i = 0; i < _currentPhonemes.Length; i++)
            {
                GameObject slotObj;
#if MOCK_UNITY_COMPILATION
                slotObj = new GameObject();
#else
                slotObj = Instantiate(slotPrefab != null ? slotPrefab : new GameObject());
#endif
                PhonemeSlot slot = slotObj.GetComponent<PhonemeSlot>();
                if (slot == null)
                {
                    slot = slotObj.AddComponent<PhonemeSlot>();
                }

                // Place in magnifier viewer row (Y = 0f)
                slotObj.transform.position = new Vector3(startX + (i * step), 0f, 0f);
                slot.Initialize(i, _currentPhonemes[i]);
                _activeSlots.Add(slot);
            }

            Debug.Log($"[{moduleName}] Spawned {_currentPhonemes.Length} spelling slots in magnifying glass.");
        }

        private void SpawnPhonemeBubbles()
        {
            // Spawn unique IPA characters surrounding the glass
            var bubbleData = new[]
            {
                new { Sym = "/c/", Let = "c" },
                new { Sym = "/p/", Let = "p" },
                new { Sym = "/o/", Let = "o" },
                new { Sym = "/t/", Let = "t" }
            };

            for (int i = 0; i < bubbleData.Length; i++)
            {
                GameObject bubbleObj;
#if MOCK_UNITY_COMPILATION
                bubbleObj = new GameObject();
#else
                bubbleObj = Instantiate(bubblePrefab != null ? bubblePrefab : new GameObject());
#endif
                PhonemeBubble bubble = bubbleObj.GetComponent<PhonemeBubble>();
                if (bubble == null)
                {
                    bubble = bubbleObj.AddComponent<PhonemeBubble>();
                }

                // Position floating bubbles randomly above the magnifier glass (Y = 3.0f)
                float spawnX = -2.5f + (i * 1.5f);
                bubbleObj.transform.position = new Vector3(spawnX, 3.0f, 0f);

                bubble.Initialize(bubbleData[i].Sym, bubbleData[i].Let);
                _activeBubbles.Add(bubble);

                Debug.Log($"[{moduleName}] Spawned floating bubble: '{bubbleData[i].Sym}' at X: {spawnX}");
            }
        }

        /// <summary>
        /// Simulates dragging a phoneme bubble onto a letter slot.
        /// </summary>
        public void PerformMutation(string replacementLetter, int targetSlotIndex)
        {
            if (!isGameActive) return;

            _totalAttemptsCount++;

            if (targetSlotIndex < 0 || targetSlotIndex >= _currentPhonemes.Length)
            {
                Debug.LogError($"[{moduleName}] Invalid slot index targeted: {targetSlotIndex}");
                return;
            }

            // Find matching mutation configuration
            MutationData? mutation = _mutationsDatabase.Find(m =>
                m.SourceWord.Equals(_currentBaselineWord, StringComparison.OrdinalIgnoreCase) &&
                m.MutateIndex == targetSlotIndex &&
                m.ReplacementPhoneme.Equals(replacementLetter, StringComparison.OrdinalIgnoreCase));

            if (mutation != null)
            {
                // Successful sound mutation!
                _correctMutationsCount++;
                currentScore += 300;

                string oldWord = _currentBaselineWord;
                _currentBaselineWord = mutation.TargetWord;
                _currentPhonemes[targetSlotIndex] = replacementLetter;

                // Update slot letter displays
                _activeSlots[targetSlotIndex].UpdateLetter(replacementLetter);

                // Earn the collectible card
                _earnedRewardCards.Add(mutation.RewardCardName);

                Debug.Log($"[{moduleName}] CRAFT SUCCESS! Mutated \"{oldWord.ToUpper()}\" -> \"{_currentBaselineWord.ToUpper()}\" (Swapped slot {targetSlotIndex} with '{replacementLetter}'). Score: {currentScore}");
                Debug.Log($"[{moduleName}] Awarded Collectible Asset: [{mutation.RewardCardName}]!");

                // Trigger bouncy feedback on slot, screen shake, and star burst
                Vector3 slotPos = _activeSlots[targetSlotIndex].transform.position;
                if (UiJuiceManager.Instance != null)
                {
                    UiJuiceManager.Instance.TriggerCameraShake(0.25f, 0.12f);
                    UiJuiceManager.Instance.TriggerStarBurst(slotPos);
                    UiJuiceManager.Instance.PopFloatingText("+300 PTS!", slotPos, Color.green);
                    UiJuiceManager.Instance.ApplyButtonJuice(_activeSlots[targetSlotIndex].transform);
                }

                // Voice the newly mutated word using IPA tags
                if (audioManager != null)
                {
                    string ssml = $"<speak>You crafted: <phoneme alphabet=\"ipa\" ph=\"{mutation.IpaPhonetic}\">{mutation.TargetWord}</phoneme></speak>";
                    string key = $"level4_craft_{mutation.TargetWord}";
                    _ = audioManager.PlayPhoneticClipAsync(ssml, key);
                }

                OnWordMutated?.Invoke(mutation.TargetWord, mutation.RewardCardName);

                // Check final game chain completion
                if (_earnedRewardCards.Count >= _mutationsDatabase.Count)
                {
                    Debug.Log($"[{moduleName}] Progression chain complete! Crafted all mutations and collected all cards!");
                    if (UiJuiceManager.Instance != null)
                    {
                        UiJuiceManager.Instance.TriggerConfetti(new Vector3(0f, 0f, 0f));
                    }
                    EndGame();
                }
            }
            else
            {
                // Mismatch or invalid spelling mutation
                currentScore = Math.Max(0, currentScore - 75);
                
                string[] tempPhonemes = (string[])_currentPhonemes.Clone();
                tempPhonemes[targetSlotIndex] = replacementLetter;
                string gibberish = string.Join("", tempPhonemes);

                Debug.LogWarning($"[{moduleName}] CRAFT FAILURE! Swapped slot {targetSlotIndex} with '{replacementLetter}' producing \"{gibberish.ToUpper()}\" (Gibberish/Invalid). Score: {currentScore}");

                // Trigger wobble and negative feedback text
                Vector3 slotPos = _activeSlots[targetSlotIndex].transform.position;
                if (UiJuiceManager.Instance != null)
                {
                    UiJuiceManager.Instance.PopFloatingText("-75 PTS! WRONG BLEND", slotPos, Color.red);
                    UiJuiceManager.Instance.ApplyWobble(_activeSlots[targetSlotIndex].transform);
                }

                // Voice the incorrect blend phonetic output
                if (audioManager != null)
                {
                    string ssml = $"<speak>{gibberish}</speak>";
                    string key = $"level4_failed_blend_{gibberish}";
                    _ = audioManager.PlayPhoneticClipAsync(ssml, key);
                }
            }
        }

        private void ClearAllEntities()
        {
            foreach (var slot in _activeSlots)
            {
                if (slot != null && slot.gameObject != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            _activeSlots.Clear();

            foreach (var bubble in _activeBubbles)
            {
                if (bubble != null && bubble.gameObject != null)
                {
                    Destroy(bubble.gameObject);
                }
            }
            _activeBubbles.Clear();
        }

        #region Statistics & Accessors

        public float GetAccuracy()
        {
            if (_totalAttemptsCount == 0) return 1.0f;
            return (float)_correctMutationsCount / _totalAttemptsCount;
        }

        public int GetCorrectMutationsCount() => _correctMutationsCount;
        public int GetTotalAttemptsCount() => _totalAttemptsCount;
        public string GetCurrentBaselineWord() => _currentBaselineWord;
        public string[] GetCurrentPhonemes() => _currentPhonemes;
        public List<string> GetEarnedRewardCards() => _earnedRewardCards;
        public List<PhonemeSlot> GetActiveSlots() => _activeSlots;
        public List<PhonemeBubble> GetActiveBubbles() => _activeBubbles;

        #endregion
    }
}
