using UnityEngine;
using TMPro;

namespace PhoneticsEdu.Games.OnsetRime
{
    /// <summary>
    /// Component representing a falling Onset block or a static Rime block.
    /// Manages sliding and snapping events inside the grid.
    /// </summary>
    public class PhoneticBlock : MonoBehaviour
    {
        public string PhonemeText { get; private set; } = "";
        public bool IsOnset { get; private set; } = false;
        public bool IsStatic { get; set; } = false;
        public float FallSpeed { get; private set; } = 1.0f;

        private OnsetRimeManager? _manager;
        private TMP_Text? _tmpText;
        private float _bottomBoundY = -4.0f; // Height of the bottom Rime blocks row

        private void Awake()
        {
            _tmpText = GetComponentInChildren<TMP_Text>();
        }

        /// <summary>
        /// Configure the phonetic block with text content, classification, and motion settings.
        /// </summary>
        public void Initialize(string text, bool isOnset, bool isStatic, float speed, OnsetRimeManager manager)
        {
            PhonemeText = text;
            IsOnset = isOnset;
            IsStatic = isStatic;
            FallSpeed = speed;
            _manager = manager;

            if (_tmpText != null)
            {
                _tmpText.text = text;
            }

            gameObject.name = $"PhoneticBlock_{(isOnset ? "Onset" : "Rime")}_{text}";
        }

        /// <summary>
        /// Shift the block horizontally (used by the player to align it).
        /// </summary>
        public void Slide(float deltaX)
        {
            if (IsStatic) return;

            // Translate horizontally
            transform.Translate(new Vector3(deltaX, 0f, 0f));
            
            // Constrain horizontal boundaries (e.g. within -3.0f and 3.0f)
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, -3.0f, 3.0f);
            transform.position = pos;

            Debug.Log($"[PhoneticBlock] Slid '{PhonemeText}' horizontally to X: {transform.position.x:F1}");
        }

        private void Update()
        {
            if (IsStatic) return;

            // Descend the block downwards
            transform.Translate(new Vector3(0f, -FallSpeed * Time.deltaTime, 0f));

            // Check if the falling block has landed on the bottom row
            if (transform.position.y <= _bottomBoundY)
            {
                // Snap Y coordinate to resting height
                Vector3 pos = transform.position;
                pos.y = _bottomBoundY;
                transform.position = pos;

                IsStatic = true; // Freeze vertical motion

                Debug.Log($"[PhoneticBlock] Falling Onset block '{PhonemeText}' snapped at bottom Y: {transform.position.y:F1}");
                
                if (_manager != null)
                {
                    _manager.SnapOnsetBlock(this);
                }
            }
        }
    }

    #if MOCK_UNITY_COMPILATION
    public static class Mathf
    {
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
    #endif
}
