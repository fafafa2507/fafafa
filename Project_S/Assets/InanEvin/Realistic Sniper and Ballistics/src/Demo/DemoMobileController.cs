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
    /// Demo class for mobile controls. It's responsible for moving the joystics & listening to their touch events,
    /// then calling appropriate functions on camera controllers or weapon controllers to simulate input.
    /// </summary>
    public class DemoMobileController : MonoBehaviour
    {
        // Exposed
        [SerializeField] private float m_joystickResetSpeed = 500.0f;
        [SerializeField] private float m_lookInputMultiplier = 0.25f;
        [SerializeField] private RectTransform m_lookJoystickBase = null;
        [SerializeField] private RectTransform m_lookJoystickHandle = null;
        [SerializeField] private RectTransform m_moveJoystickHandle = null;
        [SerializeField] private PlayerWeaponController m_weaponController = null;
        [SerializeField] private DynamicScopeSystem m_dynamicScopeSystem = null;
        [SerializeField] private GameObject[] m_controlsToEnableOnlyInAim = new GameObject[] { };

        // Public Access
        public static Vector2 LookInput { get { return s_lookInput; } }
        public static Vector2 MoveInput { get { return s_moveInput; } }

        // Private members
        private float m_joystickMaxRadius = 0.0f;
        private bool m_isControlsInAimEnabled = false;
        private DemoMobileJoystick m_lookJoystick = null;
        private DemoMobileJoystick m_moveJoystick = null;
        private static Vector2 s_moveInput = Vector2.zero;
        private static Vector2 s_lookInput = Vector2.zero;

        private void Awake()
        {
            m_joystickMaxRadius = m_lookJoystickBase.sizeDelta.x / 2.0f - m_lookJoystickHandle.sizeDelta.x / 2.0f - 2;
            m_lookJoystick = m_lookJoystickHandle.GetComponent<DemoMobileJoystick>();
            m_moveJoystick = m_moveJoystickHandle.GetComponent<DemoMobileJoystick>();

            AimControlsActivation(false);

            // Disable if we are not on mobile.
#if !(!UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS))
            this.enabled = false;
#else
            Input.simulateMouseWithTouches = false;
#endif
        }

        public void PointerDown_AimButton()
        {
            m_weaponController.AimInput(!m_isControlsInAimEnabled);

            // Enable aim controls (zoom in&out, zero distance up&down) if aimed in.
            m_isControlsInAimEnabled = !m_isControlsInAimEnabled;
            AimControlsActivation(m_isControlsInAimEnabled);
        }

        public void PointerDown_FireButton()
        {
            m_weaponController.FireInput();
        }

        public void PointerDown_ReloadButton()
        {
            m_weaponController.ReloadInput();
        }

        public void PointerDown_ZoomIn()
        {
            if (m_dynamicScopeSystem)
                m_dynamicScopeSystem.ZoomIn();
        }

        public void PointerDown_ZoomOut()
        {
            if (m_dynamicScopeSystem)
                m_dynamicScopeSystem.ZoomOut();
        }

        public void PointerDown_ZeroDistanceUpButton()
        {
            SniperAndBallisticsSystem.instance.CycleZeroDistanceUp();
        }

        public void PointerDown_ZeroDistanceDownButton()
        {
            SniperAndBallisticsSystem.instance.CycleZeroDistanceDown();
        }

        private void Update()
        {

            Vector2 lookPosition = m_lookJoystick.GetEventPosition();
            Vector2 movePosition = m_moveJoystick.GetEventPosition();

            if (lookPosition != Vector2.zero)
            {
                Vector2 delta = new Vector2(lookPosition.x, lookPosition.y) - m_lookJoystick.GetDragBeginPosition();
                delta = Vector2.ClampMagnitude(delta, m_joystickMaxRadius);
                m_lookJoystickHandle.localPosition = delta;
                s_lookInput = new Vector2(delta.x / m_joystickMaxRadius, delta.y / m_joystickMaxRadius) * m_lookInputMultiplier;
            }
            else
            {
                m_lookJoystickHandle.localPosition = Vector3.MoveTowards(m_lookJoystickHandle.localPosition, Vector3.zero, Time.unscaledDeltaTime * m_joystickResetSpeed);
                s_lookInput = Vector2.zero;
            }

            // Movement joystick
            if (movePosition != Vector2.zero)
            {
                Vector2 delta = new Vector2(movePosition.x, movePosition.y) - m_moveJoystick.GetDragBeginPosition();
                delta = Vector2.ClampMagnitude(delta, m_joystickMaxRadius);
                m_moveJoystickHandle.localPosition = delta;
                s_moveInput = new Vector2(delta.x / m_joystickMaxRadius, delta.y / m_joystickMaxRadius);
            }
            else
            {
                m_moveJoystickHandle.localPosition = Vector3.MoveTowards(m_moveJoystickHandle.localPosition, Vector3.zero, Time.unscaledDeltaTime * m_joystickResetSpeed);
                s_moveInput = Vector2.zero;
            }
        }

        private void AimControlsActivation(bool activate)
        {
            for (int i = 0; i < m_controlsToEnableOnlyInAim.Length; i++)
                m_controlsToEnableOnlyInAim[i].SetActive(activate);
        }

        private bool IsOverUI(RectTransform rt, Vector2 position)
        {
            var normalizedMousePosition = new Vector2(position.x / Screen.width, position.y / Screen.height);
            if (normalizedMousePosition.x > rt.anchorMin.x && normalizedMousePosition.x < rt.anchorMax.x &&
                normalizedMousePosition.y > rt.anchorMin.y && normalizedMousePosition.y < rt.anchorMax.y)
            {
                return true;
            }

            return false;
        }
    }

}
