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

using System.Collections.Generic;
using UnityEngine;

namespace IE.RSB
{
    /// <summary>
    /// Simply instantiates m_pooledAmount of the given prefab in awake. Disables all of the instantiated objects & keeps track of them in a list.
    /// Whenever somebody requests the instantiated prefab, they call GetPooledObject method, and this class returns an available one right away.
    /// Available means being deactive, ready to serve. If no available ones exists, it will instantiate new ones in run-time depending on whether
    /// m_willGrow is checked or not.
    /// </summary>
    public class ObjectPooler : MonoBehaviour
    {
        // Exposed properties.
        [SerializeField, Tooltip("Prefab to pool.")]
        private GameObject m_pooledObject = null;

        [SerializeField, Tooltip("How many objects will be spawned out of the prefab when the scene starts. Set it to the maximum value that you might need this prefab at once.")] 
        private int m_pooledAmount = 20;

        [SerializeField, Tooltip("When requested an object, if the system can not find an available spawned prefab, e.g. all of them are in use & not disabled, will the system just return null, or will it grow & instantiate a new one?")]
        private bool m_willGrow = true;

        [SerializeField, Tooltip("Hide flags for hierarchy window. HideInHierarchy will make the pooled objects invisible in hierarchy window. Useful if you want to avoid having hundreds of pooled objects showing up in your window.")] 
        private HideFlags m_hideFlags = HideFlags.HideInHierarchy;

        // Private class members.
        private List<GameObject> m_pooledObjects = new List<GameObject>();

        void Start()
        {
            m_pooledObjects = new List<GameObject>();

            // Instantiate the objects & add them to the list.
            for (int i = 0; i < m_pooledAmount; i++)
            {
                GameObject obj = (GameObject)Instantiate(m_pooledObject);
                obj.hideFlags = m_hideFlags;
                obj.SetActive(false);
                m_pooledObjects.Add(obj);
            }
        }

        public GameObject GetPooledObject()
        {
            // Check if we have available objects.
            for (int i = 0; i < m_pooledObjects.Count; i++)
            {
                // Make sure nothing is nulled out, if so, instantiate again.
                if (m_pooledObjects[i] == null)
                {
                    GameObject obj = (GameObject)Instantiate(m_pooledObject);
                    obj.SetActive(false);
                    obj.hideFlags = m_hideFlags;
                    m_pooledObjects[i] = obj;
                    return m_pooledObjects[i];
                }
                if (!m_pooledObjects[i].activeInHierarchy)
                {
                    // Return the avialable one.
                    return m_pooledObjects[i];
                }
            }

            // If we haven't returned so far, means no available object is found.
            // Create a new one & grow if desired.
            if (m_willGrow)
            {
                GameObject obj = (GameObject)Instantiate(m_pooledObject);
                obj.hideFlags = m_hideFlags;
                m_pooledObjects.Add(obj);
                return obj;
            }

            return null;
        }

    }
}
