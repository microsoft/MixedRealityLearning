// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Utilities;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Core.Devices.Hands
{
    /// <summary>
    /// Configuration profile settings for setting up boundary visualizations.
    /// </summary>
    [CreateAssetMenu(menuName = "Mixed Reality Toolkit/Mixed Reality Hand Light Service Profile", fileName = "HandLightServiceProfile", order = (int)CreateProfileMenuItemIndices.RegisteredServiceProviders)]
    public class HandLightServiceProfile : BaseMixedRealityProfile
    {
        #region Proximity Light Settings

        [SerializeField]
        [Tooltip("Proximity light settings for the left index finger.")]
        private ProximityLight.LightSettings leftIndexProximityLightSettings = null;

        /// <summary>
        /// Proximity light settings for the left index finger.
        /// </summary>
        public ProximityLight.LightSettings LeftIndexProximityLightSettings => leftIndexProximityLightSettings;

        [SerializeField]
        [Tooltip("Proximity light settings for the right index finger.")]
        private ProximityLight.LightSettings rightIndexProximityLightSettings = null;

        /// <summary>
        /// Proximity light settings for the right index finger.
        /// </summary>
        public ProximityLight.LightSettings RightIndexProximityLightSettings => rightIndexProximityLightSettings;

        #endregion Proximity Light Settings

    }
}
