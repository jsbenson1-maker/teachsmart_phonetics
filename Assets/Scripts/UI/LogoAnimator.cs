using System;
using UnityEngine;

namespace PhoneticsEdu.UI
{
    /// <summary>
    /// Animates the main menu logo for "TeachSmart: Phonetic Challenge" with a modern,
    /// bouncy, and smooth kid-friendly feel. To prevent annoying repetition, it combines
    /// three distinct animation channels (Float, Scale Pulse, Rotation Sway) with prime-number
    /// wave periods (5.0s, 7.0s, and 11.0s), creating a long 385-second looping animation sequence.
    /// </summary>
    public class LogoAnimator : MonoBehaviour
    {
        [Header("Float Settings")]
        [SerializeField] private float floatAmplitude = 15f;
        [SerializeField] private float floatPeriod = 5.0f; // Prime 1

        [Header("Scale Pulse Settings")]
        [SerializeField] private float scalePulseAmplitude = 0.08f;
        [SerializeField] private float scalePeriod = 7.0f; // Prime 2

        [Header("Rotation Sway Settings")]
        [SerializeField] private float rotationSwayAmplitude = 3.5f;
        [SerializeField] private float rotationPeriod = 11.0f; // Prime 3

        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private Quaternion _originalRotation;

        private float _timeElapsed = 0f;

        private void Start()
        {
            _originalPosition = transform.position;
            _originalScale = transform.localScale;
            _originalRotation = transform.localRotation;

            Debug.Log($"[LogoAnimator] \"TeachSmart: Phonetic Challenge\" Logo Animation Initialized.");
            Debug.Log($"[LogoAnimator] Float Period: {floatPeriod}s | Scale Period: {scalePeriod}s | Rotation Period: {rotationPeriod}s. Combined Loop Cycle: 385 seconds!");
        }

        private void Update()
        {
            _timeElapsed += Time.deltaTime;

            // 1. Position float animation (Y-axis oscillation)
            float floatPhase = (_timeElapsed / floatPeriod) * (Mathf.PI * 2f);
            float yOffset = Mathf.Sin(floatPhase) * floatAmplitude;
            transform.position = _originalPosition + new Vector3(0f, yOffset, 0f);

            // 2. Breathing scale pulse animation (X & Y scale oscillation)
            float scalePhase = (_timeElapsed / scalePeriod) * (Mathf.PI * 2f);
            float scaleMultiplier = 1f + (Mathf.Sin(scalePhase) * scalePulseAmplitude);
            transform.localScale = new Vector3(_originalScale.x * scaleMultiplier, _originalScale.y * scaleMultiplier, _originalScale.z);

            // 3. Rotation sway animation (Z-axis roll oscillation)
            float rotationPhase = (_timeElapsed / rotationPeriod) * (Mathf.PI * 2f);
            float zRotation = Mathf.Sin(rotationPhase) * rotationSwayAmplitude;
            transform.localRotation = _originalRotation * Quaternion.Euler(0f, 0f, zRotation);

#if MOCK_UNITY_COMPILATION
            // Under simulated runtime, periodically print our animated values to show the longer loop calculations
            if (Math.Abs(_timeElapsed % 2.0f) < 0.05f)
            {
                Debug.Log($"[LogoAnimator - Loop Time: {_timeElapsed:F1}s] Y-Float: {yOffset:+0.00;-0.00} | Scale Multiplier: {scaleMultiplier:P1} | Z-Sway: {zRotation:+0.00;-0.00}°");
            }
#endif
        }
    }
}
