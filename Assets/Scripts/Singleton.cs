using UnityEngine;

/// <summary>
/// Generic MonoBehaviour singleton base class.
/// </summary>
/// <typeparam name="T">The singleton type.</typeparam>
public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    /// <summary>
    /// Gets the singleton instance. If none exists yet, it will try to find one or create a new GameObject.
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton<{typeof(T)}>] Instance already destroyed on application quit. Won’t create again – returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance != null)
                    return _instance;

                // Try to find existing instance in scene
                _instance = FindObjectOfType<T>();
                if (FindObjectsOfType<T>().Length > 1)
                {
                    Debug.LogError($"[Singleton<{typeof(T)}>] Something went wrong – more than one instance in the scene!");
                    // Optionally handle duplicates here
                }

                if (_instance == null)
                {
                    // Create new GameObject if none found
                    GameObject singletonGO = new GameObject($"{typeof(T).Name} (Singleton)");
                    _instance = singletonGO.AddComponent<T>();
                    DontDestroyOnLoad(singletonGO);
                    Debug.Log($"[Singleton<{typeof(T)}>] An instance was needed in the scene and was created: {singletonGO}");
                }
                else
                {
                    Debug.Log($"[Singleton<{typeof(T)}>] Using existing instance: {_instance.gameObject.name}");
                }

                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = (T)this;
            // Ensure this GameObject is a root object before calling DontDestroyOnLoad
            if (transform.parent != null)
            {
                transform.SetParent(null, true);
            }
            DontDestroyOnLoad(gameObject);
            OnSingletonAwake();
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"[Singleton<{typeof(T)}>] Duplicate instance found and destroyed: {gameObject.name}");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Optional override to do initialization when this is the instance.
    /// </summary>
    protected virtual void OnSingletonAwake() { }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }
}
