using UnityEngine;
using TMPro;

namespace PhoneticsEdu.Games.WordBlaster
{
    /// <summary>
    /// Component attached to spawned word ships. Moves down the screen
    /// and reports click/tap interactions.
    /// </summary>
    public class WordShip : MonoBehaviour
    {
        public string DisplayWord { get; private set; } = "";
        public bool IsCorrectWord { get; private set; } = false;
        public float MovementSpeed { get; private set; } = 1.0f;

        private WordBlasterManager? _manager;
        private TMP_Text? _tmpText;

        private void Awake()
        {
            // Try to find a text component on the object
            _tmpText = GetComponentInChildren<TMP_Text>();
        }

        /// <summary>
        /// Initializer called dynamically when spawning the ship.
        /// </summary>
        public void Initialize(string word, bool isCorrect, float speed, WordBlasterManager manager)
        {
            DisplayWord = word;
            IsCorrectWord = isCorrect;
            MovementSpeed = speed;
            _manager = manager;

            // Set the visual display text if available
            if (_tmpText != null)
            {
                _tmpText.text = word;
            }
            
            gameObject.name = $"Ship_{word}";
        }

        private void Update()
        {
            // Descent down the screen (negative Y movement)
            transform.Translate(new Vector3(0, -MovementSpeed * Time.deltaTime, 0));

            // If the ship escapes off the bottom of the screen (default threshold -5.0f)
            if (transform.position.y < -5.0f)
            {
                Escaped();
            }
        }

        /// <summary>
        /// Handle clicking/tapping this ship (standard Unity message).
        /// </summary>
        private void OnMouseDown()
        {
            Zap();
        }

        /// <summary>
        /// Destroys the ship and triggers score checking in the manager.
        /// </summary>
        public void Zap()
        {
            Debug.Log($"[WordShip] Ship carrying '{DisplayWord}' was zapped!");
            if (_manager != null)
            {
                _manager.ZapShip(this);
            }
            
            // Clean up
            Destroy(gameObject);
        }

        /// <summary>
        /// Clean up if the ship reaches the bottom boundaries.
        /// </summary>
        private void Escaped()
        {
            Debug.Log($"[WordShip] Ship carrying '{DisplayWord}' escaped past bottom boundary.");
            if (_manager != null)
            {
                _manager.OnShipEscaped(this);
            }
            
            Destroy(gameObject);
        }
    }
}
