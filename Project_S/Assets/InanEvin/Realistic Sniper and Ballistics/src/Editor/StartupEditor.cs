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
using System;
using System.Text.RegularExpressions;

namespace IE.RSB
{
    public class RSBAP : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            string[] entries = Array.FindAll(importedAssets, name => name.Contains("RichFXStartupEditor") && !name.EndsWith(".meta"));

            for (int i = 0; i < entries.Length; i++)
                if (StartupEditor.Init(false))
                    break;
        }
    }

    public sealed class StartupEditor : EditorWindow
    {
        public static string versionID = "ogs_v";
        static string imagePath = "";

        Texture2D coverImage;
        Vector2 changelogScroll = Vector2.zero;
        GUIStyle labelStyle;
        GUIStyle buttonStyle;
        GUIStyle iconButtonStyle;
        private bool warningRPAsset;

        public enum CategoryFindResult { NotFound, FoundFull, FoundEmpty };

        [MenuItem("Help/Realistic Sniper and Ballistics/About", false, 0)]
        public static void MenuInit()
        {
            Init(true);
        }

        [MenuItem("Help/Realistic Sniper and Ballistics/Online Docs", false, 0)]
        public static void MenuManual()
        {
            Application.OpenURL("https://rsb.inanevin.com/");
        }

        public static bool Init(bool force)
        {
            string[] assets = AssetDatabase.FindAssets("rsb_cover_img", null);

            if (assets.Length > 0)
                imagePath = AssetDatabase.GUIDToAssetPath(assets[0]);
            else
                imagePath = "";

            if (force || EditorPrefs.GetString(versionID) != changelogText.Split('\n')[0])
            {
                StartupEditor window;
                window = GetWindow<StartupEditor>(true, "About RSB", true);
                Vector2 size = new Vector2(620, 600);
                window.minSize = size;
                window.maxSize = size;
                window.ShowUtility();
                return true;
            }

            return false;
        }

        void OnEnable()
        {
            EditorPrefs.SetString(versionID, changelogText.Split('\n')[0]);

            string[] assets = AssetDatabase.FindAssets("rsb_cover_img", null);

            if (assets.Length > 0)
                imagePath = AssetDatabase.GUIDToAssetPath(assets[0]);
            else
                imagePath = "";

            string versionColor = EditorGUIUtility.isProSkin ? "#ffffffee" : "#000000ee";
            int maxLength = 10000;
            bool tooLong = changelogText.Length > maxLength;
            if (tooLong)
            {
                changelogText = changelogText.Substring(0, maxLength);
                changelogText += "...\n\n<color=" + versionColor + ">[Check online documentation for more.]</color>";
            }
            changelogText = Regex.Replace(changelogText, @"^[0-9].*", "<color=" + versionColor + "><size=13><b>Version $0</b></size></color>", RegexOptions.Multiline);
            changelogText = Regex.Replace(changelogText, @"^- (\w+:)", "  <color=" + versionColor + ">$0</color>", RegexOptions.Multiline);

            if (imagePath != "")
                coverImage = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
            else
                coverImage = null;
        }

        private void SetupLabelStyles()
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.richText = true;
            labelStyle.wordWrap = true;
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.richText = true;
        }

        void OnGUI()
        {
            SetupLabelStyles();

            Rect headerRect = new Rect(0, 0, 620, 245);

            if (coverImage != null)
                GUI.DrawTexture(headerRect, coverImage, ScaleMode.StretchToFill, false);

            GUILayout.Space(250);

            using (new GUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                // Doc
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("<b><size=13>Documentation</size></b>\n<size=11>Online manual.</size>", buttonStyle, GUILayout.MaxWidth(310), GUILayout.Height(56)))
                        Application.OpenURL("https://rsb.inanevin.com");

                    if (GUILayout.Button("<b><size=13>Rate & Review</size></b>\n<size=11>Rate RSB on Asset Store!</size>", buttonStyle, GUILayout.Height(56)))
                        Application.OpenURL("https://assetstore.unity.com/packages/slug/196337");

                    //if (GUILayout.Button("<b>Unity Forum Post</b>\n<size=9>Unity Community</size>", buttonStyle, GUILayout.Height(56)))
                    //Application.OpenURL("https://assetstore.unity.com/packages/slug/167489");
                }

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("<b>E-mail</b>\n<size=9>inanevin@gmail.com</size>", buttonStyle, GUILayout.MaxWidth(310), GUILayout.Height(56)))
                        Application.OpenURL("mailto:inanevin@gmail.com");

                    if (GUILayout.Button("<b>Twitter</b>\n<size=9>@lineupthesky</size>", buttonStyle, GUILayout.Height(56)))
                        Application.OpenURL("http://twitter.com/lineupthesky");

                }

                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);


                using (var scope = new GUILayout.ScrollViewScope(changelogScroll))
                {
                    GUILayout.Label(changelogText, labelStyle);
                    changelogScroll = scope.scrollPosition;
                }
            }
        }
        static string changelogText = "1.0 \n" +
       "- Initial version.\n";
    }

}
