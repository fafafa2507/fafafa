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
using UnityEngine.UI;

namespace IE.RSB
{
    /// <summary>
    /// Handles user controls in the demo scene. Used to cycle through different target distances, enable/disable targets etc.
    /// </summary>
    public class DemoFullController : MonoBehaviour
    {
        public enum DistanceTargetType { Stationary,  BulletTime, Dynamic };
        public enum CloseTargetType { None, Ricochet, Penetration };

        // Exposed properties
        [Header("General")]
        [SerializeField] private DistanceTargetType m_selectedFarTarget = DistanceTargetType.Stationary;
        [SerializeField] private CloseTargetType m_selectedCloseTarget = CloseTargetType.None;
        [SerializeField] private BulletTimeCameraDefault m_bulletTimeCamera = null;

        [Header("Target Transforms")]
        [SerializeField] private GameObject m_ricochetTargets = null;
        [SerializeField] private GameObject m_penetrationTargets = null;
        [SerializeField] private GameObject[] m_farTargets = new GameObject[] { }; // 0 stationary, 1 dynamic, 2 bullet time
        private int m_selectedDistance = 0; // 100, 200, 500, 800, 1000
        private string[] m_selectedDistanceStrings = { "100", "200", "500", "800", "1000" };

        [Header("UI")]
        [SerializeField] private Text m_farTargetText = null;
        [SerializeField] private Text m_farTargetDistanceText = null;
        [SerializeField] private Text m_closeTargetText = null;
        [SerializeField] private Text m_inGameTrajectoryShownText = null;
        [SerializeField] private Text m_currentBulletTimeEffectText = null;
        private bool m_inGameTrajectoryShown;

        private void Awake()
        {
            // Disable all
            m_ricochetTargets.SetActive(false);
            m_penetrationTargets.SetActive(false);
            for (int i = 0; i < m_farTargets.Length; i++)
            {
                for (int j = 0; j < m_farTargets[i].transform.childCount; j++)
                    m_farTargets[i].transform.GetChild(j).gameObject.SetActive(false);
            }

            // Switch to initial
            SwitchFarTarget(m_selectedFarTarget);
            SwitchCloseTarget(m_selectedCloseTarget);

            // Save whether in-game trajectory debug is enabled or not
            m_inGameTrajectoryShown = SniperAndBallisticsSystem.instance.UseInGameTrajectory;
            m_inGameTrajectoryShownText.text = m_inGameTrajectoryShown.ToString();

            // Cuırrent bullet time effect.
            if (!m_bulletTimeCamera.RandomizeEffects)
                m_currentBulletTimeEffectText.text = m_bulletTimeCamera.GetEffectName(m_bulletTimeCamera.SelectedEffectIndex);

            // Lock cursor
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            // Switches far targets, which are either: Stationary, Dynamic or Bullet Time Targets.
            if (Input.GetKeyDown(KeyCode.Alpha1))
                FarTargetInput();

            // Switches the distance of far targets, 100, 200, 500, 800, 1000 meters
            if (Input.GetKeyDown(KeyCode.Alpha2))
                FarTargetDistanceInput();

            // Switches close targets, which are either: None, Ricochet or Penetration targets.
            if (Input.GetKeyDown(KeyCode.Alpha3))
                CloseTargetInput();

            // Toggles in-game bullet trajectory.
            if (Input.GetKeyDown(KeyCode.Alpha4))
                InGameTrajectoryInput();

            // Switches the current selected bullet time effect.
            if (Input.GetKeyDown(KeyCode.Alpha5))
                BulletTimeEffectInput();
        }

        public void FarTargetInput()
        {
            int nextTargetIndex = (int)m_selectedFarTarget + 1;

            if (nextTargetIndex >= 3)
                nextTargetIndex = 0;

            SwitchFarTarget((DistanceTargetType)nextTargetIndex);
        }

        public void FarTargetDistanceInput()
        {
            m_farTargets[(int)m_selectedFarTarget].transform.GetChild(m_selectedDistance).gameObject.SetActive(false);

            m_selectedDistance++;

            if (m_selectedDistance >= 5)
                m_selectedDistance = 0;
            m_farTargets[(int)m_selectedFarTarget].transform.GetChild(m_selectedDistance).gameObject.SetActive(true);

            m_farTargetDistanceText.text = m_selectedDistanceStrings[m_selectedDistance];
        }

        public void CloseTargetInput()
        {
            int nextTargetIndex = (int)m_selectedCloseTarget + 1;

            if (nextTargetIndex >= 3)
                nextTargetIndex = 0;

            SwitchCloseTarget((CloseTargetType)nextTargetIndex);
        }

        public void InGameTrajectoryInput()
        {
            m_inGameTrajectoryShown = !m_inGameTrajectoryShown;
            SniperAndBallisticsSystem.instance.UseInGameTrajectory = m_inGameTrajectoryShown;
            m_inGameTrajectoryShownText.text = m_inGameTrajectoryShown.ToString();
        }

        public void BulletTimeEffectInput()
        {
            if (SniperAndBallisticsSystem.instance.BulletTimeRunning) return;

            int currentEffectIndex = m_bulletTimeCamera.SelectedEffectIndex;
            currentEffectIndex++;

            if (currentEffectIndex >= m_bulletTimeCamera.CameraEffectsCount)
                currentEffectIndex = 0;

            m_bulletTimeCamera.ChangeSelectedEffect(currentEffectIndex);

            if (!m_bulletTimeCamera.RandomizeEffects)
                m_currentBulletTimeEffectText.text = m_bulletTimeCamera.GetEffectName(m_bulletTimeCamera.SelectedEffectIndex);

        }

        private void SwitchFarTarget(DistanceTargetType type)
        {
            for (int i = 0; i < m_farTargets.Length; i++)
            {
                if ((int)type == i)
                {
                    m_farTargets[i].transform.GetChild(m_selectedDistance).gameObject.SetActive(true);
                }
                else
                    m_farTargets[i].transform.GetChild(m_selectedDistance).gameObject.SetActive(false);

            }

            m_selectedFarTarget = type;
            m_farTargetText.text = m_selectedFarTarget.ToString();
        }

        private void SwitchCloseTarget(CloseTargetType type)
        {
            if (type == CloseTargetType.None)
            {
                m_ricochetTargets.SetActive(false);
                m_penetrationTargets.SetActive(false);
            }
            else if (type == CloseTargetType.Penetration)
            {
                m_ricochetTargets.SetActive(false);
                m_penetrationTargets.SetActive(true);
            }
            else if (type == CloseTargetType.Ricochet)
            {
                m_ricochetTargets.SetActive(true);
                m_penetrationTargets.SetActive(false);
            }

            m_selectedCloseTarget = type;
            m_closeTargetText.text = m_selectedCloseTarget.ToString();
        }

    }
}