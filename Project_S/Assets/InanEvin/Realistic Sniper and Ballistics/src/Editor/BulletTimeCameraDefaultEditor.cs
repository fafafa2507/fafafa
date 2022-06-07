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
using UnityEditor;

namespace IE.RSB
{
    [CustomEditor(typeof(BulletTimeCameraDefault))]
    public class BulletTimeCameraDefaultEditor : Editor
    {
        private SerializedProperty m_cameraEffects;
        private SerializedProperty m_randomizeEffects;
        private SerializedProperty m_selectedEffect;
        private SerializedProperty m_collisionDetectionMask;
        private SerializedProperty m_dontCheckCollisionsInSameHierarchy;
        private SerializedProperty m_negligibleCollisionDistance;
        private SerializedProperty m_audioSource;

        private void OnEnable()
        {
            m_cameraEffects = serializedObject.FindProperty("m_cameraEffects");
            m_randomizeEffects = serializedObject.FindProperty("m_randomizeEffects");
            m_selectedEffect = serializedObject.FindProperty("m_selectedEffect");
            m_collisionDetectionMask = serializedObject.FindProperty("m_collisionDetectionMask");
            m_dontCheckCollisionsInSameHierarchy = serializedObject.FindProperty("m_dontCheckCollisionsInSameHierarchy");
            m_negligibleCollisionDistance = serializedObject.FindProperty("m_negligibleCollisionDistance");
            m_audioSource = serializedObject.FindProperty("m_audioSource");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            GUIStyle headerStyle = GUI.skin.label;
            headerStyle.fontStyle = FontStyle.Bold;

            // General settings & camera setup.
            EditorGUILayout.LabelField("General Settings", headerStyle);

            EditorGUILayout.PropertyField(m_collisionDetectionMask, new GUIContent("Collision Detection Mask", "Layers selected in this mask will be checked for collision avoidance. The camera will try to position itself in front of those objects to constantly have a clear shot of the target."));
            EditorGUILayout.PropertyField(m_dontCheckCollisionsInSameHierarchy, new GUIContent("Dont Check Same Hierarchy", "If true, the camera will not check for any collisions in the hierarchy of the hit object. E.g when the hit object is a humanoid's Torso, we wouldn't want the camera to check collision avoidance in humanoid's arms or legs."));
            EditorGUILayout.PropertyField(m_negligibleCollisionDistance, new GUIContent("Negligible Collision Distance", "If a collision is detected between the camera's focus and the camera, this distance is the upper limit to neglect that collision."));
            EditorGUILayout.PropertyField(m_audioSource, new GUIContent("Audio Source", "Some camera effects might want to play audio during transitions, this is the source those clips will be played on."));


            // Effect selection
            EditorGUILayout.PropertyField(m_randomizeEffects, new GUIContent("Randomize Effects", "If true, each time bullet time is triggered, a random active camera effect will be selected and played."));

            if (!m_randomizeEffects.boolValue)
            {
                string[] effectList = new string[m_cameraEffects.arraySize];

                for (int i = 0; i < m_cameraEffects.arraySize; i++)
                    effectList[i] = m_cameraEffects.GetArrayElementAtIndex(i).FindPropertyRelative("m_name").stringValue;

                m_selectedEffect.intValue = EditorGUILayout.Popup("Used Effect", m_selectedEffect.intValue,  effectList);

                if (effectList.Length == 0)
                    m_selectedEffect.intValue = -1;
            }

            EditorGUILayout.Space();

            // List of camera effects 

            Rect lastRect = GUILayoutUtility.GetLastRect();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Camera Effects", headerStyle, GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
            Rect rect = GUILayoutUtility.GetRect(1, 1);
            rect.x = lastRect.width - 135;
            rect.width = 70;
            rect.height = 20;

            // Buttons to add / remove.
            if (GUI.Button(rect, "Add New"))
            {
                List<BulletTimeCameraDefault.CameraEffect> list = (List<BulletTimeCameraDefault.CameraEffect>)EditorExtensions.GetTargetObjectOfProperty(serializedObject.FindProperty("m_cameraEffects"));
                BulletTimeCameraDefault.CameraEffect ce = new BulletTimeCameraDefault.CameraEffect();
                list.Add(ce);
                ce.m_name = "Effect " + (list.Count - 1).ToString();
                ce.m_enabled = true;
                CheckCameraEffectName(ce, list.Count - 1);
            }

            rect.x += 75;
            if (GUI.Button(rect, "Clear All"))
                m_cameraEffects.ClearArray();

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Show the actual list.
            DisplayCameraEffectsList();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);



            serializedObject.ApplyModifiedProperties();
        }

        private void DisplayCameraEffectsList()
        {
            Rect lastRect = GUILayoutUtility.GetLastRect();
            if (m_cameraEffects.arraySize > 0)
            {
                for (int i = 0; i < m_cameraEffects.arraySize; i++)
                {
                    GUILayout.Space(5);

                    // Show the effect as a foldout along with buttons.
                    EditorGUILayout.BeginVertical();
                    SerializedProperty cameraEffect = m_cameraEffects.GetArrayElementAtIndex(i);
                    SerializedProperty cameraEffectFoldout = cameraEffect.FindPropertyRelative("m_foldout");
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginHorizontal();
                    cameraEffectFoldout.boolValue = EditorGUILayout.Foldout(cameraEffectFoldout.boolValue, cameraEffect.FindPropertyRelative("m_name").stringValue);

                    // Prepare rect for Enabled toggle, add new and remove current buttons.
                    Rect rect = GUILayoutUtility.GetRect(1, 20);
                    rect.width = 70;
                    rect.x = lastRect.width - 125;
                    rect.y += 3;

                    // Enabled toggle label.
                    GUIStyle toggleStyle = GUI.skin.label;
                    toggleStyle.fontStyle = FontStyle.Normal;
                    GUI.Label(rect, new GUIContent("Enabled:", "Disabled effects are not selectable and won't be selected if effects are to randomize."), toggleStyle);
                    rect.x += 55;

                    // Enabled toggle itself.
                    rect.width = 10;
                    cameraEffect.FindPropertyRelative("m_enabled").boolValue = GUI.Toggle(rect, cameraEffect.FindPropertyRelative("m_enabled").boolValue, "");
                    rect.x += 25;
                    rect.y -= 3;
                    rect.width = 25;

                    // Copy button.
                    if (GUI.Button(rect, new GUIContent("C", "Copy")))
                    {
                        // Duplicate this element.            
                        List<BulletTimeCameraDefault.CameraEffect> list = (List<BulletTimeCameraDefault.CameraEffect>)EditorExtensions.GetTargetObjectOfProperty(serializedObject.FindProperty("m_cameraEffects"));
                        BulletTimeCameraDefault.CameraEffect ce = new BulletTimeCameraDefault.CameraEffect(list[i]);
                        list.Insert(i + 1, ce);
                        CheckCameraEffectName(ce, (i + 1));
                    }

                    rect.x += 30;

                    // Remove button
                    if (GUI.Button(rect, new GUIContent("-", "Remove")))
                    {
                        m_cameraEffects.DeleteArrayElementAtIndex(i);
                        continue;
                    }

                    EditorGUILayout.EndHorizontal();


                    // If the foldout is open, display the contents of the camera effect.
                    if (cameraEffectFoldout.boolValue)
                    {
                        // EditorGUILayout.Space();
                        DisplayCameraEffectProperties(cameraEffect, i);
                    }

                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();

                }

            }
        }

        private void CheckCameraEffectName(SerializedProperty property, int index)
        {
            SerializedProperty nameProperty = property.FindPropertyRelative("m_name");
            for (int i = 0; i < m_cameraEffects.arraySize; i++)
            {
                if (i == index) continue;
                if (m_cameraEffects.GetArrayElementAtIndex(i).FindPropertyRelative("m_name").stringValue == nameProperty.stringValue)
                {
                    nameProperty.stringValue += " (1)";
                    CheckCameraEffectName(property, index);
                }
            }
        }

        private void CheckCameraEffectName(BulletTimeCameraDefault.CameraEffect ce, int index)
        {
            for (int i = 0; i < m_cameraEffects.arraySize; i++)
            {
                if (i == index) continue;
                if (m_cameraEffects.GetArrayElementAtIndex(i).FindPropertyRelative("m_name").stringValue == ce.m_name)
                {
                    ce.m_name += " (1)";
                    CheckCameraEffectName(ce, index);
                }
            }
        }

        private void DisplayCameraEffectProperties(SerializedProperty cameraEffect, int index)
        {
            Rect lastRect = GUILayoutUtility.GetLastRect();

            // Name of the effect.
            SerializedProperty nameProperty = cameraEffect.FindPropertyRelative("m_name");
            SerializedProperty previousNameProperty = cameraEffect.FindPropertyRelative("m_previousName");
            EditorGUILayout.PropertyField(nameProperty, new GUIContent("Name", "Name of the effect, for display purposes."));
            if (nameProperty.stringValue != previousNameProperty.stringValue)
                CheckCameraEffectName(m_cameraEffects.GetArrayElementAtIndex(index), index);
            previousNameProperty.stringValue = nameProperty.stringValue;

            // Start percent && end durations
            cameraEffect.FindPropertyRelative("m_startPathPercentage").floatValue = EditorGUILayout.Slider(new GUIContent("Start Percent", "Determines what percent of the bullet's path will this effect start. 0.0f = from the beginning, 0.5 = effect will start when the bullets on the middle etc."), cameraEffect.FindPropertyRelative("m_startPathPercentage").floatValue, 0.0f, 1.0f);
            EditorGUILayout.PropertyField(cameraEffect.FindPropertyRelative("m_timeToResetTimescale"), new GUIContent("Time to Reset Timescale", "Once the bullet has reached it's target, this amount of time will be waited before resetting the timescale back to 1.0."));
            EditorGUILayout.PropertyField(cameraEffect.FindPropertyRelative("m_timeToEndAfterReset"), new GUIContent("Time to End", "Once the timescale is resetted, this amount of time will be waited before disabling bullet time camera."));
            // Label for the stages.
            SerializedProperty stages = cameraEffect.FindPropertyRelative("m_stages");
            EditorGUILayout.BeginHorizontal();
            GUIStyle stagesLabelStyle = GUI.skin.label;
            stagesLabelStyle.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField("Stages");

            Rect rect = GUILayoutUtility.GetRect(1, 1);
            rect.x = lastRect.width - 135;
            rect.width = 70;
            rect.height = 20;

            // Buttons to add / remove stages.
            if (GUI.Button(rect, "Add New"))
            {
                List<BulletTimeCameraDefault.CameraEffectStage> list = (List<BulletTimeCameraDefault.CameraEffectStage>)EditorExtensions.GetTargetObjectOfProperty(stages);
                list.Add(new BulletTimeCameraDefault.CameraEffectStage());
            }


            rect.x += 75;
            if (GUI.Button(rect, "Clear All"))
                stages.ClearArray();

            EditorGUILayout.EndHorizontal();

            if (stages.arraySize > 0)
            {
                lastRect = GUILayoutUtility.GetLastRect();
                EditorGUILayout.BeginVertical("GroupBox");

                // Now actually display a toggle for each stage.
                for (int i = 0; i < stages.arraySize; i++)
                {
                    SerializedProperty stageFoldout = stages.GetArrayElementAtIndex(i).FindPropertyRelative("m_foldout");


                    EditorGUILayout.BeginHorizontal();

                    // Actual toggle.
                    stageFoldout.boolValue = EditorGUILayout.Foldout(stageFoldout.boolValue, stages.GetArrayElementAtIndex(i).FindPropertyRelative("m_name").stringValue);
                    Rect toggleAndButtonRect = GUILayoutUtility.GetRect(1, 20);
          

                    toggleAndButtonRect.x = lastRect.width - 85;
                    toggleAndButtonRect.width = 25;
                    stages.GetArrayElementAtIndex(i).FindPropertyRelative("m_enabled").boolValue = GUI.Toggle(toggleAndButtonRect, stages.GetArrayElementAtIndex(i).FindPropertyRelative("m_enabled").boolValue, "");

                    //   EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                    toggleAndButtonRect.x += 25;
                    toggleAndButtonRect.y -= 3;
                    toggleAndButtonRect.width = 25;

                    // Buttons to copy / remove stages.
                    if (GUI.Button(toggleAndButtonRect, new GUIContent("C", "Copy")))
                    {
                        List<BulletTimeCameraDefault.CameraEffectStage> list = (List<BulletTimeCameraDefault.CameraEffectStage>)EditorExtensions.GetTargetObjectOfProperty(stages);
                        list.Insert(i + 1, new BulletTimeCameraDefault.CameraEffectStage(list[i]));
                    }

                    toggleAndButtonRect.x += 30;

                    if (GUI.Button(toggleAndButtonRect, new GUIContent("-", "Remove")))
                    {
                        stages.DeleteArrayElementAtIndex(i);
                        continue;
                    }

                    EditorGUILayout.EndHorizontal();

                    // Display the stage properties when foldout is open.
                    if (stageFoldout.boolValue)
                        DisplayStageProperties(stages.GetArrayElementAtIndex(i), i == 0 ? null : stages.GetArrayElementAtIndex(i - 1));
                    // GUILayout.Label("", GUI.skin.horizontalSlider);
                    EditorGUILayout.Space();

                }

                EditorGUILayout.EndVertical();
            }
            else
                GUILayout.Space(5);

        }

        private void DisplayStageProperties(SerializedProperty stage, SerializedProperty previousStage)
        {
            GUIStyle stageCategory = new GUIStyle();
            stageCategory.fontStyle = FontStyle.Bold;
            stageCategory.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            EditorGUILayout.LabelField("General", stageCategory);
            EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_name"), new GUIContent("Name", "Stage name, for display purposes."));
            EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_playAudio"), new GUIContent("Play Audio", "Check true if you want to play audio when this stage is triggered or when this stage is being exited."));

            if (stage.FindPropertyRelative("m_playAudio").boolValue)
            {
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_inClip"), new GUIContent("In Clip", "Clip that will be played when this stage is triggered."));
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_outClip"), new GUIContent("Out Clip", "Clip that will be played when this stage is being exited."));
            }
            EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_triggerNextMethod"), new GUIContent("Trigger Next Method",
                "How will the next stage get triggered? " +
                "Duration = wait for x seconds and then trigger." +
                " BulletTravelDistance = trigger when bullet has travelled x meters from the time this stage was triggered." +
                " DistanceToBulletBigger/Smaller = trigger when the distance between the camera and the bullet is bigger/smaller than x meters." +
                " DistanceToTargetSmaller = trigger when the distance between the camera and the hit target is smaller than x meters." +
                " BulletPathPercentage = trigger when the bullet has travelled a certain percentage of its path."));

            BulletTimeCameraDefault.CameraEffectStage.TriggerNextMethod triggerNextMethod = (BulletTimeCameraDefault.CameraEffectStage.TriggerNextMethod)stage.FindPropertyRelative("m_triggerNextMethod").enumValueIndex;

            if (triggerNextMethod == BulletTimeCameraDefault.CameraEffectStage.TriggerNextMethod.BulletPathPercentage)
            {
                stage.FindPropertyRelative("m_triggerNextBulletPathPercent").floatValue = EditorGUILayout.Slider(new GUIContent("Percentage", "When the bullet has travelled this percentage much of its path, the next stage will be triggered."), stage.FindPropertyRelative("m_triggerNextBulletPathPercent").floatValue, 0.0f, 1.0f);
            }
            else if (triggerNextMethod == BulletTimeCameraDefault.CameraEffectStage.TriggerNextMethod.BulletTravelDistance || triggerNextMethod == BulletTimeCameraDefault.CameraEffectStage.TriggerNextMethod.DistanceToBulletBiggerThan
                || triggerNextMethod == BulletTimeCameraDefault.CameraEffectStage.TriggerNextMethod.DistanceToBulletSmallerThan || triggerNextMethod == BulletTimeCameraDefault.CameraEffectStage.TriggerNextMethod.DistanceToTargetSmallerThan)
            {
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_triggerNextDistance"), new GUIContent("Distance", "Distance to compare depending on the chosen trigger next method"));
            }
            else if (triggerNextMethod == BulletTimeCameraDefault.CameraEffectStage.TriggerNextMethod.Duration)
            {
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_triggerNextDuration"), new GUIContent("Duration", "Time to wait once this stage is triggered before triggering the next stage."));
            }
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Timescale", stageCategory);
            EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_timescaleConnectedToHitDistance"), new GUIContent("Connected to Hit Distance", "If true, the timescale value here will be multiplied with (hitDistance/100). Useful in the cases where the timescale is sufficient for a target in 100 meters, but for a target in 500 meters it is too low, making bullet time take more time. Use this so that the timescale will be multiplied accordingly."));
            EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_interpolateTimescale"), new GUIContent("Interpolate Timescale", "If true, the timescale will be interpolated from Start to End. If false, it'll be set to End once."));
            EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_startTimescale"), new GUIContent("Start Timescale", "Start timescale value that's going to be set when this stage is triggered."));
            if (stage.FindPropertyRelative("m_interpolateTimescale").boolValue)
            {
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_endTimescale"), new GUIContent("End Timescale", "End timescale value that the timescale will be interpolated to."));
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_timescaleDuration"), new GUIContent("Duration", "Time it will take to interpolate timescale towards the end timescale."));
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_timescaleCurve"), new GUIContent("Interpolation Curve", "Animation curve that will be used to ease the timescale interpolation."));
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Motion", stageCategory);

            if (previousStage == null)
            {
                GUI.enabled = false;
                stage.FindPropertyRelative("m_continueFromPrevious").boolValue = false;
            }
            EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_continueFromPrevious"), new GUIContent("Continue from previous", "If true, the Start position for this stage will be set to the End position of the previous stage."));
            bool willContinueFromPrevious = stage.FindPropertyRelative("m_continueFromPrevious").boolValue;
            GUI.enabled = true;



            EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_positionType"), new GUIContent("Position Type", "Determines where the camera offsets be based on. It can either are based on bullet, hit target, or a predetermined position on the bullet's path."));

            if (((BulletTimeCameraDefault.CameraEffectStage.PositionType)stage.FindPropertyRelative("m_positionType").enumValueIndex) == BulletTimeCameraDefault.CameraEffectStage.PositionType.OnPath)
            {
                stage.FindPropertyRelative("m_pathPercent").floatValue = EditorGUILayout.Slider(new GUIContent("Path Percent", "Where exactly the camera will be placed on the bullet's path. 0 = start, 1 = end, 0.5 middle etc."), stage.FindPropertyRelative("m_pathPercent").floatValue, 0.0f, 1.0f);
            }

            if (willContinueFromPrevious)
            {
                stage.FindPropertyRelative("m_startPosition").vector3Value = previousStage.FindPropertyRelative("m_endPosition").vector3Value;
            }

            EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_interpolatePosition"), new GUIContent("Interpolate Position", "If true, the position will be interpolated from Start to End. If false, it'll be set to Start once."));

            if (!willContinueFromPrevious)
            {
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_randomizeStartPosition"), new GUIContent("Randomize Start Position", "If true, the Start position offset will be randomized between min and max variables given."));

                if (stage.FindPropertyRelative("m_randomizeStartPosition").boolValue)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Randomize Axis (XYZ)", "If true, the randomized position between min and max will also be randomly multiplied with 1.0 or -1.0 to change its direction on separate axes. (Left side to right, front to back etc."), GUILayout.Width(150));
                    GUILayout.FlexibleSpace();
                    stage.FindPropertyRelative("m_randomizeStartDirectionX").boolValue = EditorGUILayout.ToggleLeft("", stage.FindPropertyRelative("m_randomizeStartDirectionX").boolValue, GUILayout.Width(25));
                    stage.FindPropertyRelative("m_randomizeStartDirectionY").boolValue = EditorGUILayout.ToggleLeft("", stage.FindPropertyRelative("m_randomizeStartDirectionY").boolValue, GUILayout.Width(25));
                    stage.FindPropertyRelative("m_randomizeStartDirectionZ").boolValue = EditorGUILayout.ToggleLeft("", stage.FindPropertyRelative("m_randomizeStartDirectionZ").boolValue, GUILayout.Width(30));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_minStartPosition"), new GUIContent("Min", "Minimum Start position for randomization."));
                    EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_maxStartPosition"), new GUIContent("Max", "Maximum Start position for randomization."));
                }
                else
                    EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_startPosition"), new GUIContent("Start Position", "Start position the camera will be set to once this stage is triggered."));
            }

            if (stage.FindPropertyRelative("m_interpolatePosition").boolValue)
            {
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_randomizeEndPosition"), new GUIContent("Randomize End Position", "If true, the end position offset will be randomized between min and max variables given."));

                if (stage.FindPropertyRelative("m_randomizeEndPosition").boolValue)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Randomize Axis (XYZ)", "If true, the randomized position between min and max will also be randomly multiplied with 1.0 or -1.0 to change its direction, on separate axes. (Left side to right, front to back etc."), GUILayout.Width(150));
                    GUILayout.FlexibleSpace();
                    stage.FindPropertyRelative("m_randomizeEndDirectionX").boolValue = EditorGUILayout.ToggleLeft("", stage.FindPropertyRelative("m_randomizeEndDirectionX").boolValue, GUILayout.Width(25));
                    stage.FindPropertyRelative("m_randomizeEndDirectionY").boolValue = EditorGUILayout.ToggleLeft("", stage.FindPropertyRelative("m_randomizeEndDirectionY").boolValue, GUILayout.Width(25));
                    stage.FindPropertyRelative("m_randomizeEndDirectionZ").boolValue = EditorGUILayout.ToggleLeft("", stage.FindPropertyRelative("m_randomizeEndDirectionZ").boolValue, GUILayout.Width(30));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_minEndPosition"), new GUIContent("Min", "Minimum End position for randomization."));
                    EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_maxEndPosition"), new GUIContent("Max", "Maximum End position for randomization."));
                }
                else
                    EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_endPosition"), new GUIContent("End Position", "End position the camera will be interpolated towards while this stage is running."));
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_positionDuration"), new GUIContent("Duration", "The time it will take to interpolate the position from Start to End."));
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_positionCurve"), new GUIContent("Curve", "Animation curve that will be used to ease the position interpolation."));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Look At", stageCategory);
            EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_lookAtBullet"), new GUIContent("Look At Bullet", "If true the camera will look at the bullet in this stage, else it will look at the hit target."));
            EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_interpolateLookSpeed"), new GUIContent("Interpolate Speed", "If the look at speed will be interpolated from Start towards End, if not it'll be set to Start once."));
            EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_startLookSpeed"), new GUIContent("Start Speed", "Initial look at speed."));

            if (stage.FindPropertyRelative("m_interpolateLookSpeed").boolValue)
            {
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_endLookSpeed"), new GUIContent("End Speed", "Look at speed that will be interpolated towards."));
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_lookSpeedDuration"), new GUIContent("Duration", "Time it will take to interpolate from Start to End look speed."));
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_lookSpeedCurve"), new GUIContent("Curve", "Animation curve that will be used to ease the look at speed interpolation."));


            }
            EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_lookAtOffset"), new GUIContent("Offset", "The camera will look at the point where the target position is offseted by this amount relative to the target's rotation."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Z Tilt", stageCategory);
            EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_interpolateZTilt"), new GUIContent("Interpolate Tilt", "If true, z tilt angle will be interpolate from Start towards End, if not, it will be set to Start once."));
            EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_startZTilt"), new GUIContent("Start Z Tilt", "Initial z tilt angle of the camera."));

            if (stage.FindPropertyRelative("m_interpolateZTilt").boolValue)
            {
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_endZTilt"), new GUIContent("End Z Tilt", "Z tilt angle of the camera that will be interpolated towards."));
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_zTiltDuration"), new GUIContent("Duration", "Time it will take to interpolate the z tilt from Start to End."));
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_zTiltCurve"), new GUIContent("Curve", "Animation curve that will be used to ease the z tilt interpolation."));
            }
            EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_zTiltSpeed"), new GUIContent("Speed", "Speed that will be used to interpolate the euler z angle of the camera."));
        
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shake", stageCategory);
            EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_shakeEnabled"), new GUIContent("Enabled", "If true, the camera will shake depending on the shake properties once this stage is triggered"));
            if (stage.FindPropertyRelative("m_shakeEnabled").boolValue)
            {
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_shakeAmount"), new GUIContent("Amount", "Shake amount that'll be applied as euler angles on each axis."));
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_shakeSpeed"), new GUIContent("Speed", "How fast will the camera shake?"));
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("m_shakeDuration"), new GUIContent("Duration", "How long will the camera shake in real seconds?"));
            }

        }
    }

}
