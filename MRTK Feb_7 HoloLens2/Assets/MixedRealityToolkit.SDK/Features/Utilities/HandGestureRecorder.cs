// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Devices.Hands;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.Core.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.SDK.Utilities
{

    /// <summary>
    /// Record joint positions of a hand and log them for use in simulated hands
    /// </summary>
    public class HandGestureRecorder : MonoBehaviour
    {
        private static readonly int jointCount = Enum.GetNames(typeof(TrackedHandJoint)).Length;

        public TrackedHandJoint ReferenceJoint = TrackedHandJoint.IndexTip;

        private IMixedRealityHandJointService HandJointService => handJointService ?? (handJointService = MixedRealityToolkit.Instance.GetService<IMixedRealityHandJointService>());
        private IMixedRealityHandJointService handJointService = null;

        private Vector3 offset = Vector3.zero;

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F9))
            {
                Transform joint = HandJointService.RequestJoint(ReferenceJoint, Handedness.Left);
                offset = (joint ? joint.position : Vector3.zero);
            }
            if (UnityEngine.Input.GetKeyUp(KeyCode.F9))
            {
                RecordPose(Handedness.Left);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.F10))
            {
                Transform joint = HandJointService.RequestJoint(ReferenceJoint, Handedness.Right);
                offset = (joint ? joint.position : Vector3.zero);
            }
            if (UnityEngine.Input.GetKeyUp(KeyCode.F10))
            {
                RecordPose(Handedness.Right);
            }
        }

        private void RecordPose(Handedness handedness)
        {
            Vector3[] jointPositions = new Vector3[jointCount];

            for (int i = 0; i < jointCount; ++i)
            {
                GetJointPosition(jointPositions, (TrackedHandJoint)i, handedness);
            }

            SimulatedHandPose pose = new SimulatedHandPose();
            pose.ParseFromJointPositions(jointPositions, handedness, Quaternion.identity, offset);

            Debug.Log(pose.GenerateInitializerCode());
        }

        private void GetJointPosition(Vector3[] positions, TrackedHandJoint jointId, Handedness handedness)
        {
            Transform joint = HandJointService.RequestJoint(jointId, handedness);
            positions[(int)jointId] = (joint ? joint.position : Vector3.zero);
        }
    }

}