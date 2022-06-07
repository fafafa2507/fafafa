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
    /// Simple first-person camera controller.
    /// </summary>
    public class PlayerCameraController : MonoBehaviour
    {
        // Exposed properties.
        [Header("General")]
        [SerializeField] private Transform m_playerTransform = null;

        [Header("Input Settings")]
        [SerializeField] private Vector2 m_sensitivity = new Vector2(0.3f, 0.3f);
        [SerializeField] private Vector2 m_smooth = new Vector3(0.03f, 0.03f);

        [Header("Control Settings")]
        [SerializeField] [MinMaxSlider(-90, 90)] private Vector2 yLimits = new Vector2(-80.0f, 80.0f);

        // Private class members.
        private float m_refX = 0.0f;
        private float m_refY = 0.0f;
        private float m_smoothedX = 0.0f;
        private float m_smoothedY = 0.0f;
        private Vector2 m_inputs = Vector2.zero;
        private Quaternion m_cameraInitialRot = Quaternion.identity;
        private Quaternion m_playerInitialRot = Quaternion.identity;
        private Transform m_thisTransform = null;
        private Vector2 m_input = Vector2.zero;

        void Start()
        {
            m_thisTransform = base.transform;
            m_cameraInitialRot = m_thisTransform.localRotation;
            m_playerInitialRot = m_playerTransform.rotation;
        }

        void LateUpdate()
        {

            // Static value, regardless of whether there is a dynamic scope system instance in the scene or not.
            // If DSS is not used, it will stay in default value of 1.0
            // You can replace with your own sensitivity multiplier logic to decrease mouse control sensitivity during scope zooms.
            float sensitivityMultiplier = DynamicScopeSystem.ScopeSensitivityMultiplier;

            // If we are not in any mobile platforms set input,
            // Else we're gonna get it from DemoMobileControls
            // Which is supposed to be a joystick canvas in the scene.
#if !(!UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS))
            m_input.x = Input.GetAxis("Mouse X");
            m_input.y = Input.GetAxis("Mouse Y");
#else
            m_input = DemoMobileController.LookInput;
#endif

            // If RSB instance is in the scene, only move the camera when the bullet time is not running.
            // If not, move normally.
            if (SniperAndBallisticsSystem.instance != null)
            {
                if (!SniperAndBallisticsSystem.instance.BulletTimeRunning)
                {
                    m_inputs.x += m_input.x * m_sensitivity.x * sensitivityMultiplier;
                    m_inputs.y += -m_input.y * m_sensitivity.y * sensitivityMultiplier;
                }
            }
            else
            {
                m_inputs.x += m_input.x * m_sensitivity.x;
                m_inputs.y += -m_input.y * m_sensitivity.y;
            }

            // Smooth & clamp
            m_smoothedX = Mathf.SmoothDamp(m_smoothedX, m_inputs.x, ref m_refX, m_smooth.x);
            m_smoothedY = Mathf.SmoothDamp(m_smoothedY, m_inputs.y, ref m_refY, m_smooth.y);
            m_inputs.y = Mathf.Clamp(m_inputs.y, yLimits.x, yLimits.y);

            // Apply rotation.
            Quaternion q_X = Quaternion.AngleAxis(m_smoothedX, Vector3.up);
            Quaternion q_Y = Quaternion.AngleAxis(m_smoothedY, Vector3.right);
            m_playerTransform.rotation = m_playerInitialRot * q_X;
            m_thisTransform.localRotation = m_cameraInitialRot * q_Y;
        }


    }
}
