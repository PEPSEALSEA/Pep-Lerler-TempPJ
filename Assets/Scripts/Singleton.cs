using UnityEngine;

// T must be a class that inherits from MonoSingleton<T>
public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                // Try to find an existing instance in the scene
                instance = FindObjectOfType<T>();

                if (instance == null)
                {
                    // If none found, create a new GameObject and attach the script
                    GameObject singletonObject = new GameObject(typeof(T).Name);
                    instance = singletonObject.AddComponent<T>();
                }
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = (T)this;
            // Optional: Keep the object alive across scene loads
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If an instance already exists, destroy this duplicate
            Destroy(gameObject);
        }
    }
}