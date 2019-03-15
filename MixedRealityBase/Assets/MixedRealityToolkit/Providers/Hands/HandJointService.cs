// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Providers;
using Microsoft.MixedReality.Toolkit.Core.Services;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Core.Devices.Hands
{
    public class HandJointService : BaseDeviceManager, IMixedRealityHandJointService
    {
        private IMixedRealityHandVisualizer leftHandVisualizer;
        private IMixedRealityHandVisualizer rightHandVisualizer;

        private Dictionary<TrackedHandJoint, Transform> leftHandFauxJoints = new Dictionary<TrackedHandJoint, Transform>();
        private Dictionary<TrackedHandJoint, Transform> rightHandFauxJoints = new Dictionary<TrackedHandJoint, Transform>();

        #region BaseDeviceManager Implementation

        public HandJointService(string name, uint priority, BaseMixedRealityProfile profile) : base(name, priority, profile) { }

        /// <inheritdoc />
        public override void Update()
        {
            bool leftFound = false;
            bool rightFound = false;

            foreach (var detectedController in MixedRealityToolkit.InputSystem.DetectedControllers)
            {
                if (detectedController.Visualizer is IMixedRealityHandVisualizer)
                {
                    if (detectedController.ControllerHandedness == Handedness.Left)
                    {
                        leftFound = true;

                        if (leftHandVisualizer == null)
                        {
                            leftHandVisualizer = detectedController.Visualizer as IMixedRealityHandVisualizer;
                        }
                    }
                    else if (detectedController.ControllerHandedness == Handedness.Right)
                    {
                        rightFound = true;

                        if (rightHandVisualizer == null)
                        {
                            rightHandVisualizer = detectedController.Visualizer as IMixedRealityHandVisualizer;
                        }
                    }
                }
            }

            if (!leftFound)
            {
                leftHandVisualizer = null;
            }

            if (!rightFound)
            {
                rightHandVisualizer = null;
            }

            if (leftHandVisualizer != null)
            {
                foreach (var fauxJoint in leftHandFauxJoints)
                {
                    Transform realJoint;
                    if (leftHandVisualizer.TryGetJoint(fauxJoint.Key, out realJoint))
                    {
                        fauxJoint.Value.SetPositionAndRotation(realJoint.position, realJoint.rotation);
                    }
                }
            }

            if (rightHandVisualizer != null)
            {
                foreach (var fauxJoint in rightHandFauxJoints)
                {
                    Transform realJoint;
                    if (rightHandVisualizer.TryGetJoint(fauxJoint.Key, out realJoint))
                    {
                        fauxJoint.Value.SetPositionAndRotation(realJoint.position, realJoint.rotation);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void Disable()
        {
            // Check existence of fauxJoints before destroying. This avoids a (harmless) race
            // condition when the service is getting destroyed at the same time that the gameObjects
            // are being destroyed at shutdown.
            if (leftHandFauxJoints != null)
            {
                foreach (var fauxJoint in leftHandFauxJoints.Values)
                {
                    if (fauxJoint != null)
                    {
                        Object.Destroy(fauxJoint.gameObject);
                    }
                }
                leftHandFauxJoints.Clear();
            }

            if (rightHandFauxJoints != null)
            {
                foreach (var fauxJoint in rightHandFauxJoints.Values)
                {
                    if (fauxJoint != null)
                    {
                        Object.Destroy(fauxJoint.gameObject);
                    }
                }
                rightHandFauxJoints.Clear();
            }
        }

        #endregion BaseDeviceManager Implementation

        #region IMixedRealityHandJointService Implementation

        public Transform RequestJoint(TrackedHandJoint jointToEnable, Handedness handedness)
        {
            Transform jointTransform = null;
            Dictionary<TrackedHandJoint, Transform> fauxJoints = null;
            IMixedRealityHandVisualizer handVisualizer = null;

            if (handedness == Handedness.Left)
            {
                fauxJoints = leftHandFauxJoints;
                handVisualizer = leftHandVisualizer;
            }
            else if (handedness == Handedness.Right)
            {
                fauxJoints = rightHandFauxJoints;
                handVisualizer = rightHandVisualizer;
            }

            if (fauxJoints != null && !fauxJoints.TryGetValue(jointToEnable, out jointTransform))
            {
                jointTransform = new GameObject().transform;
                // Since this service survives scene loading and unloading, the fauxJoints it manages need to as well.
                Object.DontDestroyOnLoad(jointTransform.gameObject);
                jointTransform.name = string.Format("Joint Tracker: {1} {0}", jointToEnable, handedness);

                Transform realJointTransform;
                if (handVisualizer != null && handVisualizer.TryGetJoint(jointToEnable, out realJointTransform))
                {
                    jointTransform.SetPositionAndRotation(realJointTransform.position, realJointTransform.rotation);
                }

                fauxJoints.Add(jointToEnable, jointTransform);
            }

            return jointTransform;
        }

        public Transform CreateJointWithOffset(TrackedHandJoint jointToEnable, Handedness handedness, Vector3 positionOffset, Quaternion rotationOffset)
        {
            Transform parentJoint = RequestJoint(jointToEnable, handedness);

            Transform jointWithOffset = new GameObject().transform;
            jointWithOffset.parent = parentJoint;
            jointWithOffset.localPosition = positionOffset;
            jointWithOffset.localRotation = rotationOffset;
            jointWithOffset.name = string.Format("Offset Joint: {3} {2} by {0}, {1}", positionOffset, rotationOffset.eulerAngles, jointToEnable, handedness);

            return jointWithOffset;
        }

        public bool IsHandTracked(Handedness handedness)
        {
            return handedness == Handedness.Left ? leftHandVisualizer != null : handedness == Handedness.Right ? rightHandVisualizer != null : false;
        }

        #endregion IMixedRealityHandJointService Implementation
    }
}
