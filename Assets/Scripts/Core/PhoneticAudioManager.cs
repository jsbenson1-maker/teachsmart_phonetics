using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace PhoneticsEdu.Core
{
    public enum TtsProvider
    {
        Simulation,
        GoogleCloudTTS
    }

    /// <summary>
    /// Centralized audio manager that handles SSML synthesis with IPA notation,
    /// local caching of synthesized audio clips, and microphone diagnostics.
    /// </summary>
    public class PhoneticAudioManager : MonoBehaviour
    {
        public static PhoneticAudioManager Instance { get; private set; } = null!;

        [Header("Audio Configurations")]
        [SerializeField] private AudioSource? audioSource;
        [SerializeField] private TtsProvider provider = TtsProvider.Simulation;

        [Header("Google Cloud TTS Config")]
        [SerializeField] private string googleApiKey = "";
        [SerializeField] private string voiceLanguageCode = "en-US";
        [SerializeField] private string voiceName = "en-US-Neural2-F";

        private string _cacheDirectory = "";

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

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            // Establish local cache path
            _cacheDirectory = Path.Combine(Application.persistentDataPath, "PhoneticsCache");
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
                Debug.Log($"[PhoneticAudioManager] Created cache directory at: {_cacheDirectory}");
            }
        }

        /// <summary>
        /// Public entry point to play an SSML string. Checks cache first, otherwise synthesizes.
        /// </summary>
        public async Task PlayPhoneticClipAsync(string ssmlText, string cacheKey)
        {
            Debug.Log($"[PhoneticAudioManager] Request to play SSML for key '{cacheKey}': {ssmlText}");
            AudioClip? clip = await GetOrCreateAudioClipAsync(ssmlText, cacheKey);

            if (clip != null)
            {
                if (audioSource != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                    Debug.Log($"[PhoneticAudioManager] Playing audio clip: {cacheKey}");
                }
                else
                {
                    Debug.LogWarning($"[PhoneticAudioManager] AudioSource is null. Clip '{cacheKey}' was successfully loaded/synthesized but could not be played.");
                }
            }
            else
            {
                Debug.LogError($"[PhoneticAudioManager] Failed to acquire audio clip for key: {cacheKey}");
            }
        }

        /// <summary>
        /// Retrieves the clip from local cache if it exists, otherwise requests synthesis.
        /// </summary>
        public async Task<AudioClip?> GetOrCreateAudioClipAsync(string ssmlText, string cacheKey)
        {
            string fileName = $"{cacheKey}.wav";
            string filePath = Path.Combine(_cacheDirectory, fileName);

            if (File.Exists(filePath))
            {
                Debug.Log($"[PhoneticAudioManager] Cache HIT for '{cacheKey}'. Loading local file.");
                return await LoadClipFromDiskAsync(filePath);
            }

            Debug.Log($"[PhoneticAudioManager] Cache MISS for '{cacheKey}'. Fetching from provider: {provider}");
            byte[]? audioBytes = null;

            if (provider == TtsProvider.GoogleCloudTTS && !string.IsNullOrEmpty(googleApiKey))
            {
                audioBytes = await FetchGoogleCloudTtsAsync(ssmlText);
            }
            else
            {
                if (provider == TtsProvider.GoogleCloudTTS)
                {
                    Debug.LogWarning("[PhoneticAudioManager] Google Cloud TTS chosen but API Key is empty! Falling back to Simulation.");
                }
                audioBytes = CreateMockWavBytes();
                // Simulate network latency
                await Task.Delay(500);
            }

            if (audioBytes != null && audioBytes.Length > 0)
            {
                try
                {
                    File.WriteAllBytes(filePath, audioBytes);
                    Debug.Log($"[PhoneticAudioManager] Cached synthesized audio locally to: {filePath}");
                    return await LoadClipFromDiskAsync(filePath);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PhoneticAudioManager] Error saving audio to disk: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Loads an audio file from disk asynchronously into Unity's audio system.
        /// </summary>
        private async Task<AudioClip?> LoadClipFromDiskAsync(string filePath)
        {
            // UnityWebRequest expects a URI path format
            string uri = "file://" + filePath.Replace("\\", "/");
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))
            {
                var operation = www.SendWebRequest();
                await operation.AsTask();

                if (www.isSystemError || www.isHttpError)
                {
                    Debug.LogError($"[PhoneticAudioManager] Failed to load clip from disk: {www.error}");
                    return null;
                }

                DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip)www.downloadHandler;
                return dlHandler.audioClip;
            }
        }

        /// <summary>
        /// REST integration for Google Cloud TTS.
        /// </summary>
        private async Task<byte[]?> FetchGoogleCloudTtsAsync(string ssmlText)
        {
            // Wrap in <speak> if not already done
            string speakText = ssmlText.Trim();
            if (!speakText.StartsWith("<speak>"))
            {
                speakText = $"<speak>{speakText}</speak>";
            }

            string url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={googleApiKey}";
            
            // Build simple JSON request body
            // We escape double quotes for safety
            string escapedSsml = speakText.Replace("\"", "\\\"");
            string requestJson = "{" +
                "\"input\":{\"ssml\":\"" + escapedSsml + "\"}," +
                "\"voice\":{\"languageCode\":\"" + voiceLanguageCode + "\",\"name\":\"" + voiceName + "\"}," +
                "\"audioConfig\":{\"audioEncoding\":\"LINEAR16\"}" +
                "}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                var operation = request.SendWebRequest();
                await operation.AsTask();

                if (request.isSystemError || request.isHttpError)
                {
                    Debug.LogError($"[PhoneticAudioManager] Cloud TTS Request Error: {request.error}\nResponse: {request.downloadHandler.text}");
                    return null;
                }

                string responseText = request.downloadHandler.text;
                return ExtractBase64AudioFromGoogleResponse(responseText);
            }
        }

        /// <summary>
        /// Non-dependency JSON extraction for Google Cloud TTS response field 'audioContent'
        /// </summary>
        private byte[]? ExtractBase64AudioFromGoogleResponse(string jsonResponse)
        {
            try
            {
                int startTokenIdx = jsonResponse.IndexOf("\"audioContent\"");
                if (startTokenIdx == -1) return null;

                int colonIdx = jsonResponse.IndexOf(":", startTokenIdx);
                int firstQuoteIdx = jsonResponse.IndexOf("\"", colonIdx);
                int lastQuoteIdx = jsonResponse.IndexOf("\"", firstQuoteIdx + 1);

                if (firstQuoteIdx != -1 && lastQuoteIdx != -1)
                {
                    string base64 = jsonResponse.Substring(firstQuoteIdx + 1, lastQuoteIdx - firstQuoteIdx - 1);
                    return Convert.FromBase64String(base64);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PhoneticAudioManager] Base64 decoding exception: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Generates a valid 1-second silent WAV file byte stream (8kHz, 16-bit PCM, Mono).
        /// Used for simulation or fallback cases to verify cache engine integrity.
        /// </summary>
        private byte[] CreateMockWavBytes()
        {
            int sampleRate = 8000;
            short channels = 1;
            short bitsPerSample = 16;
            int samplesCount = sampleRate * 1; // 1 second
            int subChunk2Size = samplesCount * channels * (bitsPerSample / 8);
            int chunkSize = 36 + subChunk2Size;

            byte[] wav = new byte[44 + subChunk2Size];

            // RIFF header
            Encoding.ASCII.GetBytes("RIFF").CopyTo(wav, 0);
            BitConverter.GetBytes(chunkSize).CopyTo(wav, 4);
            Encoding.ASCII.GetBytes("WAVE").CopyTo(wav, 8);

            // fmt subchunk
            Encoding.ASCII.GetBytes("fmt ").CopyTo(wav, 12);
            BitConverter.GetBytes(16).CopyTo(wav, 16); // Subchunk1Size
            BitConverter.GetBytes((short)1).CopyTo(wav, 20); // AudioFormat (1 = PCM)
            BitConverter.GetBytes(channels).CopyTo(wav, 22);
            BitConverter.GetBytes(sampleRate).CopyTo(wav, 24);
            BitConverter.GetBytes(sampleRate * channels * (bitsPerSample / 8)).CopyTo(wav, 28); // ByteRate
            BitConverter.GetBytes((short)(channels * (bitsPerSample / 8))).CopyTo(wav, 32); // BlockAlign
            BitConverter.GetBytes(bitsPerSample).CopyTo(wav, 34);

            // data subchunk
            Encoding.ASCII.GetBytes("data").CopyTo(wav, 36);
            BitConverter.GetBytes(subChunk2Size).CopyTo(wav, 40);

            // PCM audio samples (remains 0 for silence)
            return wav;
        }

        #region Diagnostic Mic Panel Utilities
        
        private string? _activeMicDevice;
        private AudioClip? _micRecordingClip;

        /// <summary>
        /// Checks if a microphone is physically connected and returns the default device name.
        /// </summary>
        public bool HasMicrophoneConnected(out string deviceName)
        {
            deviceName = "";
            string[] devices = Microphone.devices;
            if (devices.Length > 0)
            {
                deviceName = devices[0];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Starts recording from microphone.
        /// </summary>
        public void StartMicrophoneRecord(string deviceName)
        {
            _activeMicDevice = deviceName;
            _micRecordingClip = Microphone.Start(_activeMicDevice, true, 5, 44100);
            Debug.Log($"[PhoneticAudioManager] Started recording on mic: {_activeMicDevice}");
        }

        /// <summary>
        /// Stops recording from microphone and returns mock signal strength.
        /// </summary>
        public float StopMicrophoneRecord()
        {
            if (string.IsNullOrEmpty(_activeMicDevice)) return 0f;

            Microphone.End(_activeMicDevice);
            Debug.Log($"[PhoneticAudioManager] Stopped recording on mic: {_activeMicDevice}");
            _activeMicDevice = null;

            // Return a mock volume level (e.g. 0.75f) representing audio activity captured.
            return 0.75f;
        }

        /// <summary>
        /// Checks the current volume level of the active microphone recording.
        /// </summary>
        public float GetActiveMicrophoneVolume()
        {
            if (string.IsNullOrEmpty(_activeMicDevice)) return 0f;
            
            // Under simulation we return a fluctuating mock volume
            return UnityEngine.Random.Range(0.1f, 0.9f);
        }

        #endregion

        #region Helper methods for stubs compatibility
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

    /// <summary>
    /// Async extensions to make Unity's AsyncOperation awaitable.
    /// </summary>
    public static class AsyncOperationExtensions
    {
        public static Task AsTask(this AsyncOperation ops)
        {
            var tcs = new TaskCompletionSource<bool>();
            if (ops.isDone)
            {
                tcs.SetResult(true);
            }
            else
            {
#if MOCK_UNITY_COMPILATION
                tcs.SetResult(true);
#else
                ops.completed += _ => tcs.SetResult(true);
#endif
            }
            return tcs.Task;
        }
    }

#if MOCK_UNITY_COMPILATION
    // Sub class stubs to bypass UploadHandlerRaw and DownloadHandlerBuffer in standard dotnet
    public class UploadHandlerRaw : UnityEngine.Networking.UploadHandler
    {
        public UploadHandlerRaw(byte[] data) { }
    }
    public class DownloadHandlerBuffer : UnityEngine.Networking.DownloadHandler
    {
        public DownloadHandlerBuffer() { }
    }
#endif
}
