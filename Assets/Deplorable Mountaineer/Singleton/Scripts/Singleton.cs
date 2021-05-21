#region

using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Singleton {
    // Based on https://github.com/UnityCommunity/UnitySingleton/tree/master/Assets/Scripts/Singleton.cs
    public abstract class PersistentSingleton<T> : MonoBehaviour where T : Component {
        #region Fields

        /// <summary>
        ///     The instance.
        /// </summary>
        private static T _instance;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        [PublicAPI]
        public static T Instance {
            get {
                if(_instance != null) return _instance;
                _instance = FindObjectOfType<T>();
                if(_instance != null) return _instance;
                GameObject obj = new GameObject {name = typeof(T).Name};
                _instance = obj.AddComponent<T>();
                return _instance;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Use this for initialization.
        /// </summary>
        protected virtual void Awake(){
            if(_instance == null){
                _instance = this as T;
                Transform t = gameObject.transform;
                t.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else if(_instance != this){
                Destroy(gameObject);
            }
            else{
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
        }

        #endregion
    }

    public abstract class NonPersistentSingleton<T> : MonoBehaviour where T : Component {
        #region Fields

        /// <summary>
        ///     The instance.
        /// </summary>
        private static T _instance;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        [PublicAPI]
        public static T Instance {
            get {
                if(_instance != null) return _instance;
                _instance = FindObjectOfType<T>();
                if(_instance != null) return _instance;
                GameObject obj = new GameObject {name = typeof(T).Name};
                _instance = obj.AddComponent<T>();
                return _instance;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Use this for initialization.
        /// </summary>
        protected virtual void Awake(){
            if(_instance == null)
                _instance = this as T;
            else if(_instance != this)
                Destroy(gameObject);
        }

        #endregion
    }
}