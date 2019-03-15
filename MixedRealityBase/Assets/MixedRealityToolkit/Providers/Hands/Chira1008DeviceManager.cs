// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
using Chira;
#endif // UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
using Microsoft.MixedReality.Toolkit.Core.Definitions;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces;
using Microsoft.MixedReality.Toolkit.Core.Providers;
using Microsoft.MixedReality.Toolkit.Core.Services;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Core.Devices.Hands
{
    public class Chira1008DeviceManager : BaseDeviceManager, IMixedRealityExtensionService
    {
        private GameObject[] handMeshes;
        private readonly GameObject handMeshPrefab = null;
        private GameObject prefabInstance;

        private bool wasRightHandTracked, wasLeftHandTracked = false;

        /// <summary>
        /// Dictionary to capture all active hands detected
        /// </summary>
        private readonly Dictionary<Handedness, ChiraHand> trackedHands = new Dictionary<Handedness, ChiraHand>();

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
        private ChiraShowHandsModeEnum handMode = ChiraShowHandsModeEnum.Joints;
#endif // UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA

        private bool showHands;

        public bool ShowHands
        {
            get { return showHands; }
            set
            {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
                if (!showHands)
                {
                    handMode = ChiraShowHandsModeEnum.Hidden;
                }
                else
                {
                    if (handMode == ChiraShowHandsModeEnum.Hidden)
                    {
                        handMode = ChiraShowHandsModeEnum.Joints;
                    }
                }
#endif // UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA

                showHands = value;
            }
        }

        #region BaseDeviceManager Implementation

        public Chira1008DeviceManager(string name, uint priority, BaseMixedRealityProfile profile) : base(name, priority, profile)
        {
            // handMeshPrefab = MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.HandTrackingProfile.HandMeshPrefab;
        }

        /// <inheritdoc />
        public override void Initialize()
        {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
            handMode = ChiraShowHandsModeEnum.Joints;
#endif // UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
        }

        /// <inheritdoc />
        public override void Enable()
        {
            if (MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.HandTrackingProfile.SimulatedHandPrefab == null)
            {
                Debug.LogError("Tried to use the Chira1008HandManager, but no prefab was supplied.");
                return;
            }

            prefabInstance = Object.Instantiate(MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.HandTrackingProfile.SimulatedHandPrefab);

            var playspaceRoot = MixedRealityToolkit.Instance.MixedRealityPlayspace;
            prefabInstance.transform.SetPositionAndRotation(playspaceRoot.transform.position, playspaceRoot.transform.rotation);
            prefabInstance.transform.parent = playspaceRoot.transform;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
            InitializeHandMesh();

            if (ChiraDataProvider.Instance != null)
            {
                ChiraDataProvider.Instance.OnChiraDataChanged += OnChiraDataChanged;
            }
#endif // UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
        }

        /// <inheritdoc />
        public override void Disable()
        {
            if (prefabInstance != null)
            {
                Object.Destroy(prefabInstance);
            }

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
            if (ChiraDataProvider.Instance != null)
            {
                ChiraDataProvider.Instance.OnChiraDataChanged -= OnChiraDataChanged;
            }
#endif // UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA

            for (int i = 0; i < handMeshes?.Length; i++)
            {
                Object.Destroy(handMeshes[i]);
            }
        }

        #endregion BaseDeviceManager Implementation

        #region ChiraHandManager Implementation

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
        public void SetHandMode(ChiraShowHandsModeEnum mode)
        {
            handMode = mode;

            if (handMode == ChiraShowHandsModeEnum.Hidden)
            {
                ShowHands = false;
            }
            else
            {
                ShowHands = true;
            }
        }

        private void NextHandMode()
        {
            SetHandMode((ChiraShowHandsModeEnum)(((int)handMode + 1) % (int)ChiraShowHandsModeEnum.Joints + 1));
            Debug.Log("HandMode is now " + handMode);
        }

        private void OnChiraDataChanged()
        {
            ChiraDataUnity chiraData = ChiraDataProvider.Instance?.CurrentFrame;

            if (chiraData == null)
            {
                return;
            }

            if (chiraData.IsTracked[0])
            {
                ChiraHand hand = GetOrAddHand(Handedness.Left);

                if (hand != null)
                {
                    if (!wasLeftHandTracked)
                    {
                        MixedRealityToolkit.InputSystem?.RaiseSourceDetected(hand.InputSource, hand);
                        wasLeftHandTracked = true;
                    }

                    hand.UpdateState(chiraData);
                }
            }
            else if (wasLeftHandTracked)
            {
                ChiraHand hand = GetOrAddHand(Handedness.Left);

                if (hand != null)
                {
                    MixedRealityToolkit.InputSystem?.RaiseSourceLost(hand.InputSource, hand);
                    trackedHands.Remove(Handedness.Left);
                }

                wasLeftHandTracked = false;
            }


            if (chiraData.IsTracked[1])
            {
                ChiraHand hand = GetOrAddHand(Handedness.Right);

                if (hand != null)
                {
                    if (!wasRightHandTracked)
                    {
                        MixedRealityToolkit.InputSystem?.RaiseSourceDetected(hand.InputSource, hand);
                        wasRightHandTracked = true;
                    }

                    hand.UpdateState(chiraData);
                }
            }
            else if (wasRightHandTracked)
            {
                ChiraHand hand = GetOrAddHand(Handedness.Right);

                if (hand != null)
                {
                    MixedRealityToolkit.InputSystem?.RaiseSourceLost(hand.InputSource, hand);
                    trackedHands.Remove(Handedness.Right);
                }

                wasRightHandTracked = false;
            }

            // Update position, rendering of mesh
            if (handMode == ChiraShowHandsModeEnum.HandMesh)
            {
                const int vertCount = ChiraDataUnity.MaxVertices / ChiraDataUnity.MaxHands;
                for (var i = 0; i < handMeshes.Length; i++)
                {
                    GameObject handMeshObj = handMeshes[i];
                    Chira.HandSide side = i == 1 ? Chira.HandSide.Right : Chira.HandSide.Left;
                    bool isTracked = ChiraDataUtils.IsHandTracked(side);
                    if (isTracked && handMode == ChiraShowHandsModeEnum.HandMesh)
                    {
                        Mesh mesh = handMeshObj.GetComponent<MeshFilter>().mesh;
                        mesh.Clear();

                        Vector3[] vertices = new Vector3[vertCount];

                        for (var j = 0; j < vertCount; j++)
                        {
                            vertices[j] = chiraData.Vertices[j + i * vertCount];
                        }
#if !UNITY_EDITOR && UNITY_WSA
                        mesh.vertices = vertices;
                        int[] triangles = new int[HandTracking.ChiraAPI.Vertices.Length];

                        if (side == Chira.HandSide.Right)
                        {
                            for (int k = triangles.Length - 1; k >= 0; k--)
                            {
                                triangles[triangles.Length - 1 - k] = HandTracking.ChiraAPI.Vertices[k];
                            }
                        }
                        else
                        {
                            triangles = HandTracking.ChiraAPI.Vertices;
                        }
                        mesh.triangles = triangles;
                        mesh.RecalculateNormals();
#endif
                        handMeshObj.SetActive(true);
                    }
                    else
                    {
                        handMeshObj.SetActive(false);
                    }
                }
            }
        }

        private void InitializeHandMesh()
        {
            handMeshes = new GameObject[ChiraDataUnity.MaxHands];
            for (int i = 0; i < handMeshes.Length; i++)
            {
                handMeshes[i] = Object.Instantiate(handMeshPrefab, prefabInstance.transform);
                handMeshes[i].GetComponent<MeshFilter>().mesh.Clear();
            }
        }

        protected ChiraHand GetOrAddHand(Handedness handedness)
        {
            if (trackedHands.ContainsKey(handedness))
            {
                var hand = trackedHands[handedness];
                Debug.Assert(hand != null);
                return hand;
            }

            var pointers = RequestPointers(typeof(ChiraHand), handedness);
            var inputSource = MixedRealityToolkit.InputSystem?.RequestNewGenericInputSource($"{handedness} Hand", pointers);
            var detectedController = new ChiraHand(TrackingState.NotTracked, handedness, inputSource);

            if (detectedController == null)
            {
                Debug.LogError($"Failed to create {typeof(ChiraHand).Name} controller");
                return null;
            }

            if (!detectedController.SetupConfiguration(typeof(ChiraHand), InputSourceType.Hand))
            {
                // Controller failed to be set up correctly.
                Debug.LogError($"Failed to set up {typeof(ChiraHand).Name} controller");
                // Return null so we don't raise the source detected.
                return null;
            }

            for (int i = 0; i < detectedController.InputSource?.Pointers?.Length; i++)
            {
                detectedController.InputSource.Pointers[i].Controller = detectedController;
            }

            trackedHands.Add(handedness, detectedController);
            return detectedController;
        }
#endif // UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA

        #endregion ChiraHandManager Implementation
    }
}