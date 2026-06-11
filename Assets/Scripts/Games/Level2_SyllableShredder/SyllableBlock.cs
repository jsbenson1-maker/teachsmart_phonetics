using UnityEngine;
using TMPro;

namespace PhoneticsEdu.Games.SyllableShredder
{
    /// <summary>
    /// Component attached to falling/tossed syllable blocks.
    /// Simulates physics trajectories and slice interactions.
    /// </summary>
    public class SyllableBlock : MonoBehaviour
    {
        public string SyllableText { get; private set; } = "";
        public int SyllableIndex { get; private set; } = -1;

        private SyllableShredderManager? _manager;
        private TMP_Text? _tmpText;
        private Vector3 _velocity;
        private float _gravity = 9.81f;
        private bool _isInitialized = false;

        private void Awake()
        {
            _tmpText = GetComponentInChildren<TMP_Text>();
        }

        /// <summary>
        /// Configure the syllable block with its text, index, and launch velocity.
        /// </summary>
        public void Initialize(string syllable, int index, Vector3 initialVelocity, SyllableShredderManager manager)
        {
            SyllableText = syllable;
            SyllableIndex = index;
            _velocity = initialVelocity;
            _manager = manager;
            _isInitialized = true;

            if (_tmpText != null)
            {
                _tmpText.text = syllable;
            }

            gameObject.name = $"SyllableBlock_{index}_{syllable}";
        }

        private void Update()
        {
            if (!_isInitialized) return;

            // Apply gravity to velocity
            _velocity.y -= _gravity * Time.deltaTime;

            // Apply velocity translation
            transform.Translate(_velocity * Time.deltaTime);

            // Check if the block has fallen past the bottom boundary (default threshold -6.0f)
            if (transform.position.y < -6.0f && _velocity.y < 0)
            {
                FellOffScreen();
            }
        }

        /// <summary>
        /// Standard Unity message called when mouse sweeps over the collider (Fruit Ninja style).
        /// </summary>
        private void OnMouseEnter()
        {
            Slice();
        }

        /// <summary>
        /// Destroys this block and reports the slice to the manager.
        /// </summary>
        public void Slice()
        {
            Debug.Log($"[SyllableBlock] Block '{SyllableText}' (Index: {SyllableIndex}) was sliced!");
            if (_manager != null)
            {
                _manager.SliceBlock(this);
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Clean up if the block falls off the bottom of the screen.
        /// </summary>
        private void FellOffScreen()
        {
            Debug.Log($"[SyllableBlock] Block '{SyllableText}' (Index: {SyllableIndex}) fell off the screen.");
            if (_manager != null)
            {
                _manager.OnBlockFell(this);
            }

            Destroy(gameObject);
        }
    }
}
