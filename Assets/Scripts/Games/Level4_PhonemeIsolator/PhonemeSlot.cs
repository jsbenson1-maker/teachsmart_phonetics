using UnityEngine;
using TMPro;

namespace PhoneticsEdu.Games.PhonemeIsolator
{
    /// <summary>
    /// Component representing a spelling slot inside the magnifying glass baseline word display.
    /// Tracks index positions for drag-and-drop replacement.
    /// </summary>
    public class PhonemeSlot : MonoBehaviour
    {
        public int SlotIndex { get; private set; } = -1;
        public string CurrentLetter { get; private set; } = "";

        private TMP_Text? _tmpText;

        private void Awake()
        {
            _tmpText = GetComponentInChildren<TMP_Text>();
        }

        /// <summary>
        /// Configure the slot with its index position and default letter character.
        /// </summary>
        public void Initialize(int index, string letter)
        {
            SlotIndex = index;
            CurrentLetter = letter;

            if (_tmpText != null)
            {
                _tmpText.text = letter.ToUpper();
            }

            gameObject.name = $"PhonemeSlot_{index}_{letter}";
        }

        /// <summary>
        /// Updates the displayed letter character inside the slot.
        /// </summary>
        public void UpdateLetter(string newLetter)
        {
            CurrentLetter = newLetter;
            if (_tmpText != null)
            {
                _tmpText.text = newLetter.ToUpper();
            }
        }
    }
}
