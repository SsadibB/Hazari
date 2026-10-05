using UnityEngine;

namespace Hazari.Utilities
{
    /// <summary>
    /// One persistent instance across scene loads. Used by managers that must survive Boot, menu, and match scenes.
    /// </summary>
    public abstract class PersistentSingleton<T> : MonoBehaviour where T : Component
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this as T;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (ReferenceEquals(Instance, this))
                Instance = null;
        }
    }
}
