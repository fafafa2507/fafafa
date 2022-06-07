/*
 * Realistic Sniper and Ballistics System
 * Copyright (c) 2021, Inan Evin, Inc. All rights reserved.
 * Author: Inan Evin
 * https://www.inanevin.com/
 * 
 * Documentation: https://rsb.inanevin.com
 * Contact: inanevin@gmail.com
 * Support Discord: https://discord.gg/RCzpSAmBAb
 *
 * Feel free to ask about RSB, send feature recommendations or any other feedback!
 */

using UnityEngine;

namespace IE.RSB
{
    /// <summary>
    /// Base class for easily creating singleton classes. Handles thread-safe singleton mechanism.
    /// You can derive from this class as  MyClass : Singleton<MyClass> to quickly convert MyClass into a singleton.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        [SerializeField] protected bool m_dontDestroyOnLoad = false;
        private static T m_instance;
        private static readonly object m_instanceLock = new object();
        private static bool m_exiting = false;
        public static T instance
        {
            get
            {
                lock (m_instanceLock)
                {
                    if (m_instance == null && !m_exiting)
                    {
                        m_instance = GameObject.FindObjectOfType<T>();

                        // if (m_instance == null)
                            // Debug.LogWarning("Ballistics and Sniper System singleton does not exist in the scene. Please add the prefab to your scene.");

                    }

                    return m_instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (m_instance == null) m_instance = gameObject.GetComponent<T>();
            else if (m_instance.GetInstanceID() != GetInstanceID())
            {
                Destroy(gameObject);
                Debug.LogError("Ballistics and Sniper System singleton already exists in the scene, deleting object.");
            }

            if (m_dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnApplicationQuit()
        {
            m_exiting = true;
        }

    }

}
