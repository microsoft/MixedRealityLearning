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
    public static class HandJointUtils
    {
        /// <summary>
        /// Try to find the first matching hand controller and return the pose of the requested joint for that hand.
        /// </summary>
        public static bool TryGetJointPose(TrackedHandJoint joint, Handedness handedness, out MixedRealityPose pose)
        {
            IMixedRealityHand hand = FindHand(handedness);
            if (hand != null)
            {
                return hand.TryGetJoint(joint, out pose);
            }

            pose = MixedRealityPose.ZeroIdentity;
            return false;
        }

        /// <summary>
        /// Find the first detected hand controller with matching handedness.
        /// </summary>
        public static IMixedRealityHand FindHand(Handedness handedness)
        {
            foreach (var detectedController in MixedRealityToolkit.InputSystem.DetectedControllers)
            {
                var hand = detectedController as IMixedRealityHand;
                if (hand != null)
                {
                    if (detectedController.ControllerHandedness == handedness)
                    {
                        return hand;
                    }
                }
            }
            return null;
        }
    }
}
