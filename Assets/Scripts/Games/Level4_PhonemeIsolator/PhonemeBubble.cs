using UnityEngine;
using TMPro;

namespace PhoneticsEdu.Games.PhonemeIsolator
{
    /// <summary>
    /// Component representing a floating phoneme bubble carrying an IPA label.
    /// Used by players as a crafting token to drop onto spelling slots.
    /// </summary>
    public class PhonemeBubble : MonoBehaviour
    {
        public string PhonemeSymbol { get; private set; } = "";
        public string RawCharacter { get; private set; } = "";

        private TMP_Text? _tmpText;

        private void Awake()
        {
            _tmpText = GetComponentInChildren<TMP_Text>();
        }

        /// <summary>
        /// Configure the phoneme bubble with its IPA symbol and spelling character.
        /// </summary>
        public void Initialize(string symbol, string character)
        {
            PhonemeSymbol = symbol;
            RawCharacter = character;

            if (_tmpText != null)
            {
                _tmpText.text = symbol;
            }

            gameObject.name = $"PhonemeBubble_{character}";
        }
    }
}
