// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Core.Utilities
{
    /// <summary>
    /// Utility component to animate and visualize a light that can be used with 
    /// the "MixedRealityToolkit/Standard" shader "_ProximityLight" feature.
    /// </summary>
    [ExecuteInEditMode]
    public class ProximityLight : MonoBehaviour
    {
        // Two proximity lights are supported at this time.
        private const int proximityLightCount = 2;
        private const int proximityLightDataSize = 5;
        private static List<ProximityLight> activeProximityLights = new List<ProximityLight>(proximityLightCount);
        private static Vector4[] proximityLightData = new Vector4[proximityLightCount * proximityLightDataSize];
        private static int _ProximityLightDataID;
        private static int lastProximityLightUpdate = -1;

        [Serializable]
        public class LightSettings
        {
            /// <summary>
            /// Specifies the radius of the ProximityLight effect when near to a surface.
            /// </summary>
            public float NearRadius
            {
                get { return nearRadius; }
                set { nearRadius = value; }
            }

            [Header("Proximity Settings")]
            [Tooltip("Specifies the radius of the ProximityLight effect when near to a surface.")]
            [SerializeField]
            [Range(0.0f, 1.0f)]
            private float nearRadius = 0.05f;

            /// <summary>
            /// Specifies the radius of the ProximityLight effect when far from a surface.
            /// </summary>
            public float FarRadius
            {
                get { return farRadius; }
                set { farRadius = value; }
            }

            [Tooltip("Specifies the radius of the ProximityLight effect when far from a surface.")]
            [SerializeField]
            [Range(0.0f, 1.0f)]
            private float farRadius = 0.2f;

            /// <summary>
            /// Specifies the distance a ProximityLight must be from a surface to be considered near.
            /// </summary>
            public float NearDistance
            {
                get { return nearDistance; }
                set { nearDistance = value; }
            }

            [Tooltip("Specifies the distance a ProximityLight must be from a surface to be considered near.")]
            [SerializeField]
            [Range(0.0f, 1.0f)]
            private float nearDistance = 0.02f;

            /// <summary>
            /// Specifies the distance a ProximityLight must be from a surface to be considered far.
            /// </summary>
            public float FarDistance
            {
                get { return farDistance; }
                set { farDistance = value; }
            }

            [Tooltip("Specifies the distance a ProximityLight must be from a surface to be considered far.")]
            [SerializeField]
            [Range(0.0f, 1.0f)]
            private float farDistance = 0.1f;

            /// <summary>
            /// The color of the ProximityLight gradient at the center (RGB) and (A) is gradient extent.
            /// </summary>
            public Color CenterColor
            {
                get { return centerColor; }
                set { centerColor = value; }
            }

            [Header("Color Settings")]
            [Tooltip("The color of the ProximityLight gradient at the center (RGB) and (A) is gradient extent.")]
            [ColorUsageAttribute(true, true)]
            [SerializeField]
            private Color centerColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

            /// <summary>
            /// The color of the ProximityLight gradient at the center (RGB) and (A) is gradient extent.
            /// </summary>
            public Color MiddleColor
            {
                get { return middleColor; }
                set { middleColor = value; }
            }

            [Tooltip("The color of the ProximityLight gradient at the middle (RGB) and (A) is gradient extent.")]
            [SerializeField]
            [ColorUsageAttribute(true, true)]
            private Color middleColor = new Color(0.01f, 0.63f, 1.0f, 0.75f);

            /// <summary>
            /// The color of the ProximityLight gradient at the center (RGB) and (A) is gradient extent.
            /// </summary>
            public Color OuterColor
            {
                get { return outerColor; }
                set { outerColor = value; }
            }

            [Tooltip("The color of the ProximityLight gradient at the outer (RGB) and (A) is gradient extent.")]
            [SerializeField]
            [ColorUsageAttribute(true, true)]
            private Color outerColor = new Color(0.64f, 0.0f, 0.75f, 0.0f) * 3.5f;
        }

        public LightSettings Settings
        {
            get { return settings; }
            set { settings = value; }
        }

        [SerializeField]
        private LightSettings settings = new LightSettings();

        private void OnEnable()
        {
            AddProximityLight(this);
        }

        private void OnDisable()
        {
            RemoveProximityLight(this);
            UpdateProximityLights(true);
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Application.isPlaying)
            {
                return;
            }

            Initialize();
            UpdateProximityLights();
        }
#endif // UNITY_EDITOR

        private void LateUpdate()
        {
            UpdateProximityLights();
        }

        private void OnDrawGizmosSelected()
        {
            if (!enabled)
            {
                return;
            }

            Vector3[] directions = new Vector3[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };

            Gizmos.color = new Color(Settings.CenterColor.r, Settings.CenterColor.g, Settings.CenterColor.b);
            Gizmos.DrawWireSphere(transform.position, Settings.NearRadius);

            foreach (Vector3 direction in directions)
            {
                Gizmos.DrawIcon(transform.position + direction * Settings.NearRadius, string.Empty, false);
            }

            Gizmos.color = new Color(Settings.OuterColor.r, Settings.OuterColor.g, Settings.OuterColor.b);
            Gizmos.DrawWireSphere(transform.position, Settings.FarRadius);

            foreach (Vector3 direction in directions)
            {
                Gizmos.DrawIcon(transform.position + direction * Settings.FarRadius, string.Empty, false);
            }
        }

        private void AddProximityLight(ProximityLight light)
        {
            if (activeProximityLights.Count >= proximityLightCount)
            {
                Debug.LogWarningFormat("Max proximity light count ({0}) exceeded.", proximityLightCount);
            }

            activeProximityLights.Add(light);
        }

        private void RemoveProximityLight(ProximityLight light)
        {
            activeProximityLights.Remove(light);
        }

        private void Initialize()
        {
            _ProximityLightDataID = Shader.PropertyToID("_ProximityLightData");
        }

        private void UpdateProximityLights(bool forceUpdate = false)
        {
            if (lastProximityLightUpdate == -1)
            {
                Initialize();
            }

            if (!forceUpdate && (Time.frameCount == lastProximityLightUpdate))
            {
                return;
            }

            for (int i = 0; i < proximityLightCount; ++i)
            {
                ProximityLight light = (i >= activeProximityLights.Count) ? null : activeProximityLights[i];
                int dataIndex = i * proximityLightDataSize;

                if (light)
                {
                    proximityLightData[dataIndex] = new Vector4(light.transform.position.x,
                                                                light.transform.position.y,
                                                                light.transform.position.z,
                                                                1.0f);
                    // Precompute to avoid work in the shader.
                    float distanceDelta = 1.0f / Mathf.Clamp(Settings.FarDistance - Settings.NearDistance, 0.01f, 1.0f);
                    proximityLightData[dataIndex + 1] = new Vector4(light.Settings.NearRadius,
                                                                    light.Settings.FarRadius,
                                                                    light.Settings.NearDistance,
                                                                    distanceDelta);
                    proximityLightData[dataIndex + 2] = light.Settings.CenterColor;
                    proximityLightData[dataIndex + 3] = light.Settings.MiddleColor;
                    proximityLightData[dataIndex + 4] = light.Settings.OuterColor;
                }
                else
                {
                    proximityLightData[dataIndex] = Vector4.zero;
                }
            }

            Shader.SetGlobalVectorArray(_ProximityLightDataID, proximityLightData);

            lastProximityLightUpdate = Time.frameCount;
        }
    }
}
