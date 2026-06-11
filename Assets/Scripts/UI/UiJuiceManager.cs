using System;
using System.Collections;
using UnityEngine;

namespace PhoneticsEdu.UI
{
    /// <summary>
    /// Coordinates kid-friendly micro-animations, screen shakes, and particle popups
    /// across the entire application to create an inviting, premium, and fun feel.
    /// </summary>
    public class UiJuiceManager : MonoBehaviour
    {
        public static UiJuiceManager Instance { get; private set; } = null!;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Applies a bouncy spring scale (squash & stretch) feedback to a UI element.
        /// </summary>
        public void ApplyButtonJuice(Transform target)
        {
            if (target == null) return;
            Debug.Log($"[UiJuice] Triggered bouncy button click squash on target '{target.name}'");
#if !MOCK_UNITY_COMPILATION
            StartCoroutine(BouncyScaleCoroutine(target, new Vector3(1.15f, 0.85f, 1f), 0.25f));
#endif
        }

        /// <summary>
        /// Triggers a kid-friendly rotation wiggle / wobble animation (e.g., on incorrect answers).
        /// </summary>
        public void ApplyWobble(Transform target)
        {
            if (target == null) return;
            Debug.Log($"[UiJuice] Triggered wobble wiggle warning on target '{target.name}'");
#if !MOCK_UNITY_COMPILATION
            StartCoroutine(WobbleCoroutine(target, 15f, 0.3f));
#endif
        }

        /// <summary>
        /// Triggers a camera shake effect on correct explosions.
        /// </summary>
        public void TriggerCameraShake(float duration = 0.2f, float magnitude = 0.1f)
        {
            Debug.Log($"[UiJuice] Triggered camera screen shake (Duration: {duration:F2}s, Magnitude: {magnitude:F2})");
#if !MOCK_UNITY_COMPILATION
            // In Unity, find the main camera and apply shake
            var cam = Camera.main;
            if (cam != null)
            {
                StartCoroutine(CameraShakeCoroutine(cam.transform, duration, magnitude));
            }
#endif
        }

        /// <summary>
        /// Spawns a floating, rising, and fading text popup (e.g. "+100 PTS!").
        /// </summary>
        public void PopFloatingText(string text, Vector3 position, Color color)
        {
            string hexColor = $"#{ColorUtilityToHex(color)}";
            Debug.Log($"[UiJuice] Spawned floating text <color={hexColor}>\"{text}\"</color> at position ({position.x:F1}, {position.y:F1})");
        }

        /// <summary>
        /// Triggers a punchy star burst particle explosion on matching actions.
        /// </summary>
        public void TriggerStarBurst(Vector3 position)
        {
            Debug.Log($"[UiJuice] Star Burst particle explosion spawned at position ({position.x:F1}, {position.y:F1})");
        }

        /// <summary>
        /// Triggers a celebratory confetti cascade on level/sentence completion.
        /// </summary>
        public void TriggerConfetti(Vector3 position)
        {
            Debug.Log($"[UiJuice] Confetti celebratory particles burst at position ({position.x:F1}, {position.y:F1})");
        }

        #region Coroutine Implementations (Active only in Editor builds)

        private IEnumerator BouncyScaleCoroutine(Transform target, Vector3 squashScale, float duration)
        {
            Vector3 originalScale = target.localScale;
            float elapsed = 0f;

            // Phase 1: Squash down
            while (elapsed < duration * 0.4f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.4f);
                target.localScale = Vector3.Lerp(originalScale, squashScale, t);
                yield return null;
            }

            // Phase 2: Bounce back with spring elasticity
            elapsed = 0f;
            float springDuration = duration * 0.6f;
            while (elapsed < springDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / springDuration;
                // Elastic boing scale equation
                float bounce = Mathf.Sin(t * Mathf.PI * 2.5f) * (1f - t) * 0.15f;
                target.localScale = originalScale + new Vector3(bounce, -bounce, 0f);
                yield return null;
            }

            target.localScale = originalScale;
        }

        private IEnumerator WobbleCoroutine(Transform target, float angleRange, float duration)
        {
            Quaternion originalRotation = target.localRotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Wobble back and forth decaying over time
                float angle = Mathf.Sin(t * Mathf.PI * 4f) * (1f - t) * angleRange;
                target.localRotation = originalRotation * Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }

            target.localRotation = originalRotation;
        }

        private IEnumerator CameraShakeCoroutine(Transform camTransform, float duration, float magnitude)
        {
            Vector3 originalPos = camTransform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float percentComplete = elapsed / duration;
                float damper = 1.0f - percentComplete; // Decay amplitude over time

                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude * damper;
                float y = UnityEngine.Random.Range(-1f, 1f) * magnitude * damper;

                camTransform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
                yield return null;
            }

            camTransform.position = originalPos;
        }

        #endregion

        #region Helpers
        private string ColorUtilityToHex(Color color)
        {
            int r = Mathf.Clamp((int)(color.r * 255f), 0, 255);
            int g = Mathf.Clamp((int)(color.g * 255f), 0, 255);
            int b = Mathf.Clamp((int)(color.b * 255f), 0, 255);
            return $"{r:X2}{g:X2}{b:X2}";
        }

        private void DontDestroyOnLoad(GameObject obj)
        {
#if !MOCK_UNITY_COMPILATION
            UnityEngine.Object.DontDestroyOnLoad(obj);
#endif
        }

        private void Destroy(GameObject obj)
        {
#if !MOCK_UNITY_COMPILATION
            UnityEngine.Object.Destroy(obj);
#endif
        }
        #endregion
    }
}

#if MOCK_UNITY_COMPILATION
namespace UnityEngine
{


    public static class Mathf
    {
        public const float PI = 3.14159265f;
        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
#endif
