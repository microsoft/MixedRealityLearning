// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Attributes;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Core.Definitions.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Services;

#if UNITY_WSA
using Microsoft.MixedReality.Toolkit.Core.Devices.Hands;
using UnityEngine;
using UnityEngine.XR.WSA.Input;
#endif

#if WINDOWS_UWP
using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers;
using Microsoft.MixedReality.Toolkit.Core.Utilities;
using System;
using System.Collections.Generic;
using Windows.Perception;
using Windows.Perception.People;
using Windows.UI.Input.Spatial;
#endif

namespace Microsoft.MixedReality.Toolkit.Providers.WindowsMixedReality
{
    /// <summary>
    /// A Windows Mixed Reality Controller Instance.
    /// </summary>
    [MixedRealityController(
        SupportedControllerType.GGVHand,
        new[] { Handedness.Left, Handedness.Right })]
    public class WindowsMixedRealityGGVHand : WindowsMixedRealityController
    {
        public WindowsMixedRealityGGVHand(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
        : base(trackingState, controllerHandedness, inputSource, interactions)
        {
        }

        /// <summary>
        /// The GGV hand default interactions
        /// </summary>
        /// <remarks>A single interaction mapping works for both left and right controllers.</remarks>
        public override MixedRealityInteractionMapping[] DefaultInteractions { get; } = new[]
        {
            new MixedRealityInteractionMapping(0, "Select", AxisType.Digital, DeviceInputType.Select, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(1, "Grip Pose", AxisType.SixDof, DeviceInputType.SpatialGrip, MixedRealityInputAction.None),
        };
    }
}