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
using UnityEditor;

namespace IE.RSB
{
    [CustomEditor(typeof(MotionController))]
    public class MotionControllerEditor : Editor
    {

        private MotionController m_data;

        private void OnEnable()
        {
            m_data = (MotionController)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();


            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_bobbingEnabled"), new GUIContent("Enabled", "Enables bobbing motion simulating a head-bob effect."));

            if (serializedObject.FindProperty("m_bobbingEnabled").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_walkBobAmount"), new GUIContent("Walk Amount", "Amount in degrees determinining how much the object should bob, in euler angles."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_walkBobSpeed"), new GUIContent("Walk Speed", "How fast each axis should apply it's corresponding amount."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_runBobAmount"), new GUIContent("Run Amount", "Amount in degrees determinining how much the object should bob, in euler angles."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_runBobSpeed"), new GUIContent("Run Speed", "How fast each axis should apply it's corresponding amount."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_bobDamping"), new GUIContent("Damping", "Smaller values result is faster response while bobbing."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_resetSpeed"), new GUIContent("Reset Speed", "How fast should the object get back to zero euler once stopped bobbing."));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_swayEnabled"), new GUIContent("Enabled", "If true, the object will sway it's rotation based on mouse & keyboard input."));

            if (serializedObject.FindProperty("m_swayEnabled").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_keyboardSway"), new GUIContent("Keyboard Sway", "How much does the keyboard axes (horizontal, vertical) influence the swaying."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_mouseSway"), new GUIContent("Mouse Sway", "How much does the mouse axes (Mouse X, Mouse Y) influence the swaying."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_swaySmooth"), new GUIContent("Smooth", "Lower the value, faster the response will be to sway motion."));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_recoilEnabled"), new GUIContent("Enabled", "If true, the object will start making a recoil motion whenever Recoil() method on this component is called."));

            if (serializedObject.FindProperty("m_recoilEnabled").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_positionRecoil"), new GUIContent("Position Amount", "How much will the object offset itself from the zero position during recoil?"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_positionRandomizationFactor"), new GUIContent("Randomization Factor", "Each axis on the position amount will be offseted by this factor to create randomization."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_positionDuration"), new GUIContent("Duration", "How long does it take to interpolate towards the position amount?"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rotationRecoil"), new GUIContent("Rotation Amount", "How much will the object offset it's rotation from the zero rotation during recoil?"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rotationRandomizationFactor"), new GUIContent("Randomization Factor", "Each axis on the rotation amount will be offseted by this factor to create randomization."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_rotationDuration"), new GUIContent("Duration", "How long does it take to interpolate towards the rotationposition amount?"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_breathSwayEnabled"), new GUIContent("Enabled", "If true, the object will start swaying randomly depending on the parameters whenever BreathSwayActivation(true) is called. Used for swaying the camera when scope is active."));

            if (serializedObject.FindProperty("m_breathSwayEnabled").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_breathSwayAmount"), new GUIContent("Amount", "How much in degrees the object will sway?"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_breathSwaySpeed"), new GUIContent("Speed", "How fast the object will sway?"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_breathMaxHoldTime"), new GUIContent("Max Hold Time", "You can hold breath by pressing Left Shift button, which will stabilize the sway. How long can you do that?"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_breathStabilizeSpeed"), new GUIContent("Stabilize Speed", "Upon holding breath, the sway will be stabilized. This value determines how fast the stabilization will occur."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_catchBreathSpeed"), new GUIContent("Catch Breath Speed", "Once the max hold time is reached, the breath will be exhaled, causing sway amount to be exaggerating. This value determines how fast we'll go back to the original sway."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_breathSwaySmoothSpeed"), new GUIContent("Smooth", "Lower the value,faster the response will be to the sway motion."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_breathSource"), new GUIContent("Audio Source", "Source that the breath sound effects will be played on."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_breathInSFX"), new GUIContent("Breath In SFX", "Breath inhale sound effect."));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_positionerEnabled"), new GUIContent("Enabled", "If true, the object can move to 3 different positions; aim, hip or running position, when methods like ToAimPosition(), ToRunPosition() etc. are called."));

            if(serializedObject.FindProperty("m_positionerEnabled").boolValue)
            {
                EditorGUILayout.BeginVertical("GroupBox");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_aimedPosition"), new GUIContent("Aim Position", "Local position the object will interpolate towards when ToAimPosition() is called."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_aimedPosition"), new GUIContent("Aim Rotation", "Local rotation the object will interpolate towards when ToAimPosition() is called."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_toAimDuration"), new GUIContent("Aim Duration", "Time it will take to get to the aim orientation."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_hideRenderersOnAim"), new GUIContent("Hide Renderers", "Whether to hide renderers under this object when ToAimPosition() is called."));
                if (serializedObject.FindProperty("m_hideRenderersOnAim").boolValue)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_enableRenderersBackOnBulletTime"), new GUIContent("Enable Back On Bullet Time", "If the renderers were hidden during aim, whether to enable them back on when Bullet Time Started event is received."));
                EditorGUILayout.BeginHorizontal();
                if(GUILayout.Button("Save Current as Aim Orientation", GUILayout.MaxWidth(200)))
                {
                    serializedObject.FindProperty("m_aimedPosition").vector3Value = m_data.transform.localPosition;
                    serializedObject.FindProperty("m_aimedRotation").quaternionValue = m_data.transform.localRotation;
                }
                
                if(GUILayout.Button("Set to Aim Orientation"))
                {
                    m_data.transform.localPosition = serializedObject.FindProperty("m_aimedPosition").vector3Value;
                    m_data.transform.localRotation = serializedObject.FindProperty("m_aimedRotation").quaternionValue;
                }


                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();


                EditorGUILayout.BeginVertical("GroupBox");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_runningPosition"), new GUIContent("Running Position", "Local position the object will interpolate towards when ToRunPosition() is called."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_runningPosition"), new GUIContent("Running Rotation", "Local rotation the object will interpolate towards when ToRunPosition() is called."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_toRunDuration"), new GUIContent("Running Duration", "Time it will take to get to the running orientation."));

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Save Current as Run Orientation"))
                {
                    serializedObject.FindProperty("m_runningPosition").vector3Value = m_data.transform.localPosition;
                    serializedObject.FindProperty("m_runningRotation").quaternionValue = m_data.transform.localRotation;
                }

                if (GUILayout.Button("Set to Run Orientation", GUILayout.MaxWidth(200)))
                {
                    m_data.transform.localPosition = serializedObject.FindProperty("m_runningPosition").vector3Value;
                    m_data.transform.localRotation = serializedObject.FindProperty("m_runningRotation").quaternionValue;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_toHipDuration"), new GUIContent("To Hip Duration", "How long will it take for the object to get back to start position (position taken in awake)?"));

            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
