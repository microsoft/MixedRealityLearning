// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Core.Devices.Hands
{
    public class SimulatedHandDataUtils
    {
        private static readonly int jointCount = Enum.GetNames(typeof(TrackedHandJoint)).Length;

        public static Vector3 GetJoint(Handedness side, TrackedHandJoint jointIndex)
        {
            if (side == Handedness.Left)
            {
                return SimulatedHandDataProvider.Instance.CurrentFrameLeft.Joints[(int)jointIndex];
            }
            else
            {
                return SimulatedHandDataProvider.Instance.CurrentFrameRight.Joints[(int)jointIndex];
            }
        }

        /// <summary>
        /// Gets vector corresponding to +z using the same coordinate space
        /// as Leap Motion does.
        /// In Leap motion, the forward vecotr moves from the ThumbMetaCarpal to the index finger.
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        public static Vector3 GetPalmForwardVector(Handedness side)
        {
            Vector3 indexBase = GetJoint(side, TrackedHandJoint.IndexKnuckle);
            Vector3 thumbMetaCarpal = GetJoint(side, TrackedHandJoint.ThumbMetacarpalJoint);

            Vector3 thumbMetaCarpalToIndex = indexBase - thumbMetaCarpal;
            return thumbMetaCarpalToIndex.normalized;
        }

        /// <summary>
        /// Gets the vector corresponding to +y using same coordinate space as leap motion
        /// In Leap Motion the up vector moves out of the palm.
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        public static Vector3 GetPalmUpVector(Handedness side)
        {
            Vector3 indexBase = GetJoint(side, TrackedHandJoint.IndexKnuckle);
            Vector3 pinkyBase = GetJoint(side, TrackedHandJoint.PinkyKnuckle);
            Vector3 ThumbMetaCarpal = GetJoint(side, TrackedHandJoint.ThumbMetacarpalJoint);

            Vector3 ThumbMetaCarpalToPinky = pinkyBase - ThumbMetaCarpal;
            Vector3 ThumbMetaCarpalToIndex = indexBase - ThumbMetaCarpal;
            if (side == Handedness.Left)
            {
                return Vector3.Cross(ThumbMetaCarpalToPinky, ThumbMetaCarpalToIndex).normalized;
            }
            else
            {
                return Vector3.Cross(ThumbMetaCarpalToIndex, ThumbMetaCarpalToPinky).normalized;
            }
        }


        public static Vector3 GetPalmRightVector(Handedness side)
        {
            Vector3 indexBase = GetJoint(side, TrackedHandJoint.IndexKnuckle);
            Vector3 pinkyBase = GetJoint(side, TrackedHandJoint.PinkyKnuckle);
            Vector3 thumbMetaCarpal = GetJoint(side, TrackedHandJoint.ThumbMetacarpalJoint);

            Vector3 thumbMetaCarpalToPinky = pinkyBase - thumbMetaCarpal;
            Vector3 thumbMetaCarpalToIndex = indexBase - thumbMetaCarpal;
            Vector3 thumbMetaCarpalUp = Vector3.zero;
            if (side == Handedness.Left)
            {
                thumbMetaCarpalUp = Vector3.Cross(thumbMetaCarpalToPinky, thumbMetaCarpalToIndex).normalized;
            }
            else
            {
                thumbMetaCarpalUp = Vector3.Cross(thumbMetaCarpalToIndex, thumbMetaCarpalToPinky).normalized;
            }

            return Vector3.Cross(thumbMetaCarpalUp, thumbMetaCarpalToIndex).normalized;
        }
    }

}