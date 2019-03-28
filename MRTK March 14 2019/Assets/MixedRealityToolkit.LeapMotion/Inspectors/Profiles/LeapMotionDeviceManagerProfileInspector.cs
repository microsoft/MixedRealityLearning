// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.﻿

using Microsoft.MixedReality.Toolkit.Core.Inspectors.Profiles;
using Microsoft.MixedReality.Toolkit.Core.Inspectors.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.LeapMotion.Devices.Hands;
using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.LeapMotion.Inspectors
{
    [CustomEditor(typeof(LeapMotionDeviceManagerProfile))]
    public class MixedRealityLeapMotionDeviceManagerProfileInspector : BaseMixedRealityToolkitConfigurationProfileInspector
    {
        private SerializedProperty leapMotionPrefab;
        private SerializedProperty showLeapCapsuleHands;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (!MixedRealityInspectorUtility.CheckMixedRealityConfigured(false)) { return; }

            leapMotionPrefab = serializedObject.FindProperty("leapMotionPrefab");
            showLeapCapsuleHands = serializedObject.FindProperty("showLeapCapsuleHands");
        }

        public override void OnInspectorGUI()
        {
            RenderMixedRealityToolkitLogo();

            if (GUILayout.Button("Back to Registered Service Providers"))
            {
                Selection.activeObject = MixedRealityToolkit.Instance.ActiveProfile.RegisteredServiceProvidersProfile;
            }
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Leap Motion hand tracking settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use this for Leap-Motion-specific hand tracking settings.", MessageType.Info);
            CheckProfileLock(target);

            if (!MixedRealityInspectorUtility.CheckMixedRealityConfigured()) { return; }

            serializedObject.Update();

            GUILayout.Space(12f);
            EditorGUILayout.PropertyField(leapMotionPrefab);
            EditorGUILayout.PropertyField(showLeapCapsuleHands);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
