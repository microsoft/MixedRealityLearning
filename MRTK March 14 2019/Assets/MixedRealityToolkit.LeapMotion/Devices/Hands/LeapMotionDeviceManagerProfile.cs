// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.LeapMotion.Devices.Hands
{
    [CreateAssetMenu(menuName = "Mixed Reality Toolkit/Mixed Reality Leap Motion Profile", fileName = "MixedRealityLeapMotionProfile", order = 4)]
    public class LeapMotionDeviceManagerProfile : BaseMixedRealityProfile
    {
        [SerializeField]
        [Tooltip("A reference to the standard LMHeadMountedRig from LeapMotion.")]
        private GameObject leapMotionPrefab = null;

        /// <summary>
        /// A reference to the standard LMHeadMountedRig from LeapMotion.
        /// </summary>
        public GameObject LeapMotionPrefab => leapMotionPrefab;

        [SerializeField]
        [Tooltip("Whether the Leap Motion capsule hands be shown.")]
        private bool showLeapCapsuleHands = true;

        /// <summary>
        /// Whether the Leap Motion capsule hands be shown.
        /// </summary>
        public bool ShowLeapCapsuleHands => showLeapCapsuleHands;
    }
}