// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers;
using Microsoft.MixedReality.Toolkit.Core.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Core.Devices.Hands
{
    public class BaseHandVisualizer : MonoBehaviour, IMixedRealityHandVisualizer, IMixedRealitySourceStateHandler, IMixedRealityHandJointHandler, IMixedRealityHandMeshHandler
    {
        public virtual Handedness Handedness { get; set; }

        public virtual bool IsHandTracked => false;

        public GameObject GameObjectProxy => gameObject;

        public IMixedRealityController Controller { get; set; }

        protected readonly Dictionary<TrackedHandJoint, Transform> joints = new Dictionary<TrackedHandJoint, Transform>();
        protected GameObject handMesh;

        private void OnEnable()
        {
            MixedRealityToolkit.InputSystem?.Register(gameObject);
        }

        private void OnDisable()
        {
            MixedRealityToolkit.InputSystem?.Unregister(gameObject);
        }

        private void OnDestroy()
        {
            foreach (var joint in joints)
            {
                Destroy(joint.Value.gameObject);
            }

            if(handMesh != null)
            {
                Destroy(handMesh);
            }
        }

        public virtual bool TryGetJoint(TrackedHandJoint jointToEnable, out Transform jointTransform)
        {
            if (joints == null)
            {
                jointTransform = null;
                return false;
            }

            if (joints.TryGetValue(jointToEnable, out jointTransform))
            {
                return true;
            }

            jointTransform = null;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetJointWithOffset(TrackedHandJoint jointToEnable, Vector3 positionOffset, Quaternion rotationOffset, out Transform jointTransform)
        {
            Transform parentJoint;

            if (TryGetJoint(jointToEnable, out parentJoint))
            {
                GameObject jointWithOffset = new GameObject();
                jointWithOffset.transform.parent = parentJoint;
                jointWithOffset.transform.localPosition = positionOffset;
                jointWithOffset.transform.localRotation = rotationOffset;
                jointWithOffset.name = $"Offset Joint: {Handedness} {jointToEnable} by {positionOffset}, {rotationOffset.eulerAngles}";

                jointTransform = jointWithOffset.transform;
                return true;
            }

            jointTransform = null;
            return false;
        }

        void IMixedRealitySourceStateHandler.OnSourceDetected(SourceStateEventData eventData) { }

        void IMixedRealitySourceStateHandler.OnSourceLost(SourceStateEventData eventData)
        {
            if (Controller?.InputSource.SourceId == eventData.SourceId)
            {
                Destroy(gameObject);
            }
        }

        void IMixedRealityHandJointHandler.OnHandJointsUpdated(InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> eventData)
        {
            if (eventData.Handedness != Controller?.ControllerHandedness)
            {
                return;
            }

            foreach (TrackedHandJoint handJoint in eventData.InputData.Keys)
            {
                Transform jointTransform;
                if (joints.TryGetValue(handJoint, out jointTransform))
                {
                    jointTransform.position = eventData.InputData[handJoint].Position;
                    jointTransform.rotation = eventData.InputData[handJoint].Rotation;
                }
                else
                {
                    GameObject prefab = MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.HandTrackingProfile.JointPrefab;
                    if (handJoint == TrackedHandJoint.Palm)
                    {
                        prefab = MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.HandTrackingProfile.PalmJointPrefab;
                    }
                    else if (handJoint == TrackedHandJoint.IndexTip)
                    {
                        prefab = MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.HandTrackingProfile.FingerTipPrefab;
                    }
                    GameObject jointObject;
                    if (prefab != null)
                    {
                        jointObject = Instantiate(prefab);
                    }
                    else
                    {
                        jointObject = new GameObject();
                    }
                    
                    
                    jointObject.name = handJoint.ToString() + " Proxy Transform";
                    jointObject.transform.position = eventData.InputData[handJoint].Position;
                    jointObject.transform.rotation = eventData.InputData[handJoint].Rotation;
                    jointObject.transform.parent = transform;

                    joints.Add(handJoint, jointObject.transform);
                }
            }
        }

        public void OnHandMeshUpdated(InputEventData<HandMeshInfo> eventData)
        {
            if (eventData.Handedness != Controller?.ControllerHandedness)
            {
                return;
            }
            
            GameObject meshPrefab = MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.HandTrackingProfile.HandMeshPrefab;
            if (meshPrefab == null)
            {
                return;
            }

            if (handMesh == null)
            {
                handMesh = Instantiate(meshPrefab);
            }

            if(handMesh != null)
            {
                Mesh m = handMesh.GetComponent<MeshFilter>().mesh;
                Vector3[] newVertices = new Vector3[eventData.InputData.vertices.Length];
                Array.Copy(eventData.InputData.vertices, newVertices, m.vertices.Length);
                m.vertices = newVertices;

                Vector3[] newNormals = new Vector3[eventData.InputData.vertices.Length];
                Array.Copy(eventData.InputData.normals, newNormals, m.vertices.Length);
                m.normals = newNormals;

                int[] newTriangles = new int[eventData.InputData.triangles.Length];
                Array.Copy(eventData.InputData.triangles, newTriangles, m.triangles.Length);
                m.triangles = newTriangles;

                if (eventData.InputData.uvs != null && eventData.InputData.uvs.Length > 0)
                {
                    m.uv = eventData.InputData.uvs;
                }

                handMesh.transform.position = eventData.InputData.position;
                handMesh.transform.rotation = eventData.InputData.rotation;
            }
        }
    }
}