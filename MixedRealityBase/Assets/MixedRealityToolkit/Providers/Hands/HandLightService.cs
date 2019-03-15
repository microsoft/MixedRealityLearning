// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.Core.Utilities;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Core.Devices.Hands
{
    public class HandLightService : BaseExtensionService, IMixedRealityExtensionService
    {
        private IMixedRealityHandJointService HandJointService => handJointService ?? (handJointService = MixedRealityToolkit.Instance.GetService<IMixedRealityHandJointService>());
        private IMixedRealityHandJointService handJointService = null;

        private GameObject handLightServiceRoot;

        private ProximityLight leftIndexProximityLight;
        private ProximityLight rightIndexProximityLight;

        private Transform leftIndex;
        private Transform rightIndex;

        #region BaseExtensionService Implementation

        public HandLightService(string name, uint priority, BaseMixedRealityProfile profile) : base(name, priority, profile) { }

        /// <inheritdoc />
        public override void Update()
        {
            // Update the location of the proximity lights.
            if (HandJointService.IsHandTracked(Handedness.Left))
            {
                leftIndex = leftIndex ?? HandJointService.RequestJoint(TrackedHandJoint.IndexTip, Handedness.Left);
                ToggleLight(leftIndexProximityLight, leftIndex);
            }
            else
            {
                ToggleLight(leftIndexProximityLight, null);
            }

            if (HandJointService.IsHandTracked(Handedness.Right))
            {
                rightIndex = rightIndex ?? HandJointService.RequestJoint(TrackedHandJoint.IndexTip, Handedness.Right);
                ToggleLight(rightIndexProximityLight, rightIndex);
            }
            else
            {
                ToggleLight(rightIndexProximityLight, null);
            }
        }

        /// <inheritdoc />
        public override void Enable()
        {
            HandLightServiceProfile handLightServiceProfile = ConfigurationProfile as HandLightServiceProfile;

            handLightServiceRoot = new GameObject("Hand Light Service");

            // Create the proximity lights.
            if (leftIndexProximityLight == null)
            {
                leftIndexProximityLight = new GameObject("LeftIndexProximityLight").AddComponent<ProximityLight>();
                leftIndexProximityLight.transform.parent = handLightServiceRoot.transform;
                leftIndexProximityLight.Settings = (handLightServiceProfile != null) ? handLightServiceProfile.LeftIndexProximityLightSettings : leftIndexProximityLight.Settings;
                leftIndexProximityLight.enabled = false;
            }

            if (rightIndexProximityLight == null)
            {
                rightIndexProximityLight = new GameObject("RightIndexProximityLight").AddComponent<ProximityLight>();
                rightIndexProximityLight.transform.parent = handLightServiceRoot.transform;
                rightIndexProximityLight.Settings = (handLightServiceProfile != null) ? handLightServiceProfile.RightIndexProximityLightSettings : rightIndexProximityLight.Settings;
                rightIndexProximityLight.enabled = false;
            }
        }

        /// <inheritdoc />
        public override void Disable()
        {
            Object.Destroy(handLightServiceRoot);
            handLightServiceRoot = null;
        }

        #endregion BaseExtensionService Implementation

        #region HandLightService Implementation

        /// <summary>
        /// Accessors for hand index  lights.
        /// </summary>
        /// <param name="handedness">Which hand to request the light from. Should be Left or Right.</param>
        /// <returns></returns>
        public ProximityLight RequestIndexLight(Handedness handedness)
        {
            switch (handedness)
            {
                case Handedness.Left:
                    {
                        return leftIndexProximityLight;
                    }

                case Handedness.Right:
                    {
                        return rightIndexProximityLight;
                    }

                default:
                    {
                        return null;
                    }
            }
        }

        private static void ToggleLight(ProximityLight light, Transform joint)
        {
            if (light != null)
            {
                if (joint != null)
                {
                    light.enabled = true;
                    light.transform.position = joint.position;
                }
                else
                {
                    light.enabled = false;
                }
            }
        }

        #endregion HandLightService Implementation
    }
}
