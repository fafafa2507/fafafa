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
    [CustomEditor(typeof(SniperAndBallisticsSystem))]
    public class SniperAndBallisticsSystemEditor : Editor
    {
        private SniperAndBallisticsSystem m_data;
        private SerializedProperty m_dontDestroyOnLoad;
        private SerializedProperty m_zeroDistances;
        private SerializedProperty m_rayMask;
        private SerializedProperty m_environmentProperties;
        private SerializedProperty m_fireTransform;

        // Bullet time.
        private SerializedProperty m_bulletTimeEnabled;
        private SerializedProperty m_btChance;
        private SerializedProperty m_btMinDistance;

        // Tracers
        private SerializedProperty m_tracersEnabled;
        private SerializedProperty m_tracerPooler;
        private SerializedProperty m_tracerChance;
        private SerializedProperty m_enableTracerAfterPath;

        // Debug.
        private SerializedProperty m_drawBulletTrajectory;
        private SerializedProperty m_drawHits;
        private SerializedProperty m_debugRayDuration;

        // In game debug
        private SerializedProperty m_enableInGameDebug;
        private SerializedProperty m_trajectoryRendererPooler;
        private SerializedProperty m_trajectoryRendererDisableAfter;

        private void OnEnable()
        {
            m_data = (SniperAndBallisticsSystem)target;
            m_dontDestroyOnLoad = serializedObject.FindProperty("m_dontDestroyOnLoad");
            m_zeroDistances = serializedObject.FindProperty("m_zeroDistances");
            m_rayMask = serializedObject.FindProperty("m_rayMask");
            m_environmentProperties = serializedObject.FindProperty("m_environmentProperties");
            m_fireTransform = serializedObject.FindProperty("m_fireTransform");

            // Bullet time.
            m_bulletTimeEnabled = serializedObject.FindProperty("m_bulletTimeEnabled");
            m_btChance = serializedObject.FindProperty("m_btChance");
            m_btMinDistance = serializedObject.FindProperty("m_btMinDistance");

            // Tracers
            m_tracersEnabled = serializedObject.FindProperty("m_tracersEnabled");
            m_tracerPooler = serializedObject.FindProperty("m_tracerPooler");
            m_tracerChance = serializedObject.FindProperty("m_tracerChance");
            m_enableTracerAfterPath = serializedObject.FindProperty("m_enableTracerAfterTime");

            // Debug
            m_drawBulletTrajectory = serializedObject.FindProperty("m_drawBulletTrajectory");
            m_drawHits = serializedObject.FindProperty("m_drawHits");
            m_debugRayDuration = serializedObject.FindProperty("m_debugRaysDuration");

            // In game debug
            m_enableInGameDebug = serializedObject.FindProperty("m_enableInGameTrajectoryDebug");
            m_trajectoryRendererPooler = serializedObject.FindProperty("m_trajectoryRendererPooler");
            m_trajectoryRendererDisableAfter = serializedObject.FindProperty("m_trajectoryRendererDisableAfter");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();


            EditorGUILayout.BeginVertical("GroupBox");

            GUIStyle header = new GUIStyle("Label");
            header.fontStyle = FontStyle.Bold;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("General", header);
            EditorGUILayout.PropertyField(m_dontDestroyOnLoad, new GUIContent("Dont Destroy On Load", "Makes this stay alive during scene changes."));

            EditorGUILayout.PropertyField(m_environmentProperties, new GUIContent("Environment Properties", "The environment properties asset that will be used throughout the current level."));
            EditorGUILayout.PropertyField(m_rayMask, new GUIContent("Ray Mask", "Which layers will the bullet raycasting apply to?"));
            EditorGUILayout.PropertyField(m_fireTransform, new GUIContent("Fire Transform", "Where the bullet rays will be originated from. Usually it's the main camera, as you'd want to hit where you are aiming. Note: Effects like bullet time, muzzle flashes etc. are using separate transforms. This is only for determining where to start bullet raycasting."));
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_zeroDistances, new GUIContent("Zero Distances", "These are the zero distances user can cycle to adjust zeroing angle on their rifle. You can request to switch" +
                " the current zero distance by calling CycleZeroDistanceUp/Down method on SniperAndBallisticsSystem instance. First element is hard-coded to be zero. The maximum limit is 2000 units."), true);
            EditorGUI.indentLevel--;

           

            if (m_zeroDistances.arraySize == 0)
                m_zeroDistances.InsertArrayElementAtIndex(0);

            // Limit maximum zero distance to 2000.
            for(int i = 1; i < m_zeroDistances.arraySize; i++)
            {
                if (m_zeroDistances.GetArrayElementAtIndex(i).floatValue > 2000)
                    m_zeroDistances.GetArrayElementAtIndex(i).floatValue = 2000;
            }
            EditorGUILayout.EndVertical();


            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUILayout.LabelField("Bullet Time", header);

            EditorGUILayout.PropertyField(m_bulletTimeEnabled, new GUIContent("Enabled", "Whether the bullet time effect for successful hits are enabled or not."));

            if (m_bulletTimeEnabled.boolValue)
            {
                m_btChance.floatValue = EditorGUILayout.Slider(new GUIContent("Chance %", "What is the chance of triggering bullet time effect on each successful hit? 1.0f = 100%"), m_btChance.floatValue, 0.0f, 1.0f);
                EditorGUILayout.PropertyField(m_btMinDistance, new GUIContent("Min Distance", "Bullet time won't be triggered if a bullet time target is hit but the hit distance is smaller than this."));
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUILayout.LabelField("Tracers", header);
            EditorGUILayout.PropertyField(m_tracersEnabled, new GUIContent("Enabled", "If true, a tracer will be spawned each time a bullet is fired and bullet time is not entered. The tracer will follow the bullet's path displaying where the bullet is heading."));

            if (m_tracersEnabled.boolValue)
            {
                EditorGUILayout.PropertyField(m_tracerPooler, new GUIContent("Pooler", "Object pooler instance in the scene that will be responsible for spawning & pooling tracer prefabs."));
                m_tracerChance.floatValue = EditorGUILayout.Slider(new GUIContent("Chance", "What is the chance of spawning a tracer effect when a bullet is fired? 1.0 = spawn on all bullets, 0.0 = basically disabled."), m_tracerChance.floatValue, 0.0f, 1.0f);
                m_enableTracerAfterPath.floatValue = EditorGUILayout.Slider(new GUIContent("Enable After", "The amount of time system will wait before enabling a bullet's tracer. Sometimes its not desirable to spawn the tracers right away in the moment of shooting as they might look ugly close-range to the camera. So the system will spawn them after bullet has traveled for this amount of time."), m_enableTracerAfterPath.floatValue, 0.0f, 3.0f);

            }

            EditorGUILayout.EndVertical();

            // Debug.
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUILayout.PropertyField(m_drawBulletTrajectory, new GUIContent("Draw Debug Ray", "Whether to draw debug rays for visualizing the bullet trajectory or not."));
            EditorGUILayout.PropertyField(m_drawHits, new GUIContent("Draw Hit Rays", "Whether to draw debug rays for visualizing hits."));

            if (m_drawBulletTrajectory.boolValue || m_drawHits.boolValue)
                EditorGUILayout.PropertyField(m_debugRayDuration, new GUIContent("Duration", "Duration of the debug rays."));
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUILayout.PropertyField(m_enableInGameDebug, new GUIContent("In Game Debug", "In-game bullet trajectory debugging with a line renderer."));

            if (m_enableInGameDebug.boolValue)
            {
                EditorGUILayout.PropertyField(m_trajectoryRendererPooler, new GUIContent("Trajectory Renderer Pooler", "Pooler reference that will be used to get a new line renderer each time a shot is fired to render bullet's trajectory."));
                EditorGUILayout.PropertyField(m_trajectoryRendererDisableAfter, new GUIContent("Disable After", "After the bullet hits somewhere or expires its lifetime, the renderer used for that bullet will be disabled after this amount of time."));
            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(m_data);
        }
    }
}