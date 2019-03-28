﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices
{
    /// <summary>
    /// Mixed Reality Toolkit device definition, used to instantiate and manage a specific device / SDK
    /// </summary>
    public interface IMixedRealityHandJointService : IMixedRealityExtensionService
    {
        /// <summary>
        /// Get a game object following the hand joint.
        /// </summary>
        Transform RequestJointTransform(TrackedHandJoint joint, Handedness handedness);

        bool IsHandTracked(Handedness handedness);
    }
}