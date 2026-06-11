#if MOCK_UNITY_COMPILATION

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeField : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header) { }
    }

    public class Object
    {
        public string name { get; set; } = "";
        public static void Destroy(Object obj) { }
    }

    public class Transform : Component
    {
        public Vector3 position { get; set; } = new Vector3(0, 0, 0);
        public Vector3 localScale { get; set; } = new Vector3(1f, 1f, 1f);
        public Quaternion localRotation { get; set; } = Quaternion.identity;
        public void Translate(Vector3 translation)
        {
            position = new Vector3(position.x + translation.x, position.y + translation.y, position.z + translation.z);
        }
    }

    public class Component : Object
    {
        private Transform? _transform;
        public Transform transform => gameObject != null ? gameObject.transform : (_transform ??= new Transform());
        public GameObject gameObject { get; internal set; } = null!;
        public T GetComponent<T>() where T : Component => gameObject != null ? gameObject.GetComponent<T>() : null!;
        public T GetComponentInChildren<T>() where T : Component => gameObject != null ? gameObject.GetComponentInChildren<T>() : null!;
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; } = true;
    }

    public class MonoBehaviour : Behaviour
    {
        public void StartCoroutine(IEnumerator routine) { }
        public void StopAllCoroutines() { }
    }

    public class GameObject : Object
    {
        private Transform? _transform;
        public Transform transform => _transform ??= new Transform();
        public bool activeSelf => true;
        public void SetActive(bool value) { }

        private readonly List<Component> _components = new();

        public T AddComponent<T>() where T : Component
        {
            T comp = (T)Activator.CreateInstance(typeof(T), true)!;
            comp.gameObject = this;
            _components.Add(comp);
            return comp;
        }

        public T GetComponent<T>() where T : Component
        {
            foreach (var comp in _components)
            {
                if (comp is T matched) return matched;
            }
            return null!;
        }

        public T GetComponentInChildren<T>() where T : Component
        {
            return GetComponent<T>();
        }
    }

    public class AudioSource : MonoBehaviour
    {
        public AudioClip? clip { get; set; }
        public float volume { get; set; } = 1f;
        public bool isPlaying { get; set; } = false;
        public void Play() { }
        public void Stop() { }
    }

    public class AudioClip : Object
    {
        public float length => 2.0f;
        public int channels => 1;
        public int frequency => 44100;
        
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream)
        {
            return new AudioClip { name = name };
        }
    }

    public static class Debug
    {
        public static void Log(object message) => Console.WriteLine($"[Unity-Log] {message}");
        public static void LogWarning(object message) => Console.WriteLine($"[Unity-Warning] {message}");
        public static void LogError(object message) => Console.WriteLine($"[Unity-Error] {message}");
    }

    public static class Random
    {
        private static readonly System.Random _r = new();
        public static float Range(float min, float max) => (float)(_r.NextDouble() * (max - min) + min);
        public static int Range(int min, int max) => _r.Next(min, max);
    }

    public static class Application
    {
        public static string persistentDataPath => AppDomain.CurrentDomain.BaseDirectory;
    }

    public static class Microphone
    {
        public static string[] devices => new string[] { "Default Microphone" };
        public static AudioClip Start(string? deviceName, bool loop, int lengthSec, int frequency)
        {
            return AudioClip.Create("MicClip", lengthSec * frequency, 1, frequency, false);
        }
        public static void End(string? deviceName) { }
        public static bool IsRecording(string? deviceName) => false;
        public static int GetPosition(string? deviceName) => 0;
    }

    public class AsyncOperation
    {
        public bool isDone { get; protected set; }
        public float progress { get; protected set; }
    }

    public class Coroutine { }

    public static class Time
    {
        public static float deltaTime { get; set; } = 0.016f;
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 operator +(Vector3 v1, Vector3 v2) => new Vector3(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        public static Vector3 operator -(Vector3 v1, Vector3 v2) => new Vector3(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        public static Vector3 operator *(Vector3 v, float f) => new Vector3(v.x * f, v.y * f, v.z * f);
        public static Vector3 operator *(float f, Vector3 v) => new Vector3(v.x * f, v.y * f, v.z * f);
        public static Vector3 Lerp(Vector3 start, Vector3 end, float t)
        {
            return new Vector3(
                start.x + (end.x - start.x) * t,
                start.y + (end.y - start.y) * t,
                start.z + (end.z - start.z) * t
            );
        }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color red => new Color(1f, 0f, 0f);
        public static Color green => new Color(0f, 1f, 0f);
        public static Color yellow => new Color(1f, 0.92f, 0.016f);
        public static Color white => new Color(1f, 1f, 1f);
    }

    public struct Quaternion
    {
        public static Quaternion identity => new Quaternion();
        public static Quaternion Euler(float x, float y, float z) => new Quaternion();
        public static Quaternion operator *(Quaternion q1, Quaternion q2) => new Quaternion();
    }

    public class WaitForSeconds
    {
        public WaitForSeconds(float seconds) { }
    }
}

namespace UnityEngine.Events
{
    public class UnityEvent
    {
        private readonly List<Action> _listeners = new();
        public void AddListener(Action call) => _listeners.Add(call);
        public void RemoveListener(Action call) => _listeners.Remove(call);
        public void Invoke()
        {
            foreach (var listener in _listeners)
                listener();
        }
    }

    public class UnityEvent<T>
    {
        private readonly List<Action<T>> _listeners = new();
        public void AddListener(Action<T> call) => _listeners.Add(call);
        public void RemoveListener(Action<T> call) => _listeners.Remove(call);
        public void Invoke(T arg)
        {
            foreach (var listener in _listeners)
                listener(arg);
        }
    }
}

namespace UnityEngine.Networking
{
    using UnityEngine;

    public class UnityWebRequest : IDisposable
    {
        public string url { get; set; } = "";
        public string method { get; set; } = "GET";
        public bool isSystemError => false;
        public bool isHttpError => false;
        public string error { get; set; } = "";
        public DownloadHandler? downloadHandler { get; set; }
        public UploadHandler? uploadHandler { get; set; }

        public UnityWebRequest() { }
        public UnityWebRequest(string url, string method) { this.url = url; this.method = method; }

        public static UnityWebRequest Get(string uri) => new UnityWebRequest(uri, "GET");
        public AsyncOperation SendWebRequest() => new AsyncOperation();
        public void SetRequestHeader(string name, string value) { }
        public void Dispose() { }
    }

    public class DownloadHandler
    {
        public byte[] data { get; set; } = Array.Empty<byte>();
        public string text { get; set; } = "";
    }

    public class UploadHandler
    {
    }

    public class DownloadHandlerAudioClip : DownloadHandler
    {
        public AudioClip audioClip { get; set; } = new AudioClip();
        public DownloadHandlerAudioClip(string url, AudioType type) { }
    }

    public static class UnityWebRequestMultimedia
    {
        public static UnityWebRequest GetAudioClip(string uri, AudioType audioType)
        {
            var req = new UnityWebRequest { url = uri };
            req.downloadHandler = new DownloadHandlerAudioClip(uri, audioType);
            return req;
        }
    }

    public enum AudioType
    {
        UNKNOWN = 0,
        ACC = 1,
        AIFF = 2,
        IT = 10,
        MOD = 12,
        MPEG = 13,
        OGGVORBIS = 14,
        S3M = 17,
        WAV = 20,
        XM = 21,
        XMA = 22,
        VAG = 23,
        AUDIOQUEUE = 24
    }
}

namespace UnityEngine.SceneManagement
{
    using UnityEngine;

    public struct Scene
    {
        public string name { get; set; }
    }

    public class SceneManager
    {
        public static Scene GetActiveScene() => new Scene { name = "MainMenu" };
        public static AsyncOperation LoadSceneAsync(string sceneName)
        {
            Debug.Log($"Loading scene asynchronously: {sceneName}");
            return new AsyncOperation();
        }
        public static void LoadScene(string sceneName)
        {
            Debug.Log($"Loading scene: {sceneName}");
        }
    }
}

namespace UnityEngine.UI
{
    using UnityEngine;
    using UnityEngine.Events;

    public class Button : MonoBehaviour
    {
        public UnityEvent onClick { get; set; } = new UnityEvent();
    }

    public class InputField : MonoBehaviour
    {
        public string text { get; set; } = "";
        public UnityEvent<string> onValueChanged { get; set; } = new UnityEvent<string>();
        public UnityEvent<string> onEndEdit { get; set; } = new UnityEvent<string>();
    }

    public class Text : MonoBehaviour
    {
        public string text { get; set; } = "";
    }
}

namespace TMPro
{
    using UnityEngine;

    public class TextMeshProUGUI : MonoBehaviour
    {
        public string text { get; set; } = "";
    }

    public class TMP_Text : MonoBehaviour
    {
        public string text { get; set; } = "";
    }

    public class TMP_InputField : MonoBehaviour
    {
        public string text { get; set; } = "";
    }
}

#endif
