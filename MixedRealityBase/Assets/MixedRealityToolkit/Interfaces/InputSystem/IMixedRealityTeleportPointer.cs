// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Interfaces.TeleportSystem;

namespace Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem
{
    public interface IMixedRealityTeleportPointer : IMixedRealityPointer
    {
        /// <summary>
        /// True when teleport pointer has raised a request with the teleport manager.
        /// </summary>
        bool TeleportRequestRaised { get; }

        /// <summary>
        /// The currently active teleport hotspot.
        /// </summary>
        IMixedRealityTeleportHotSpot TeleportHotSpot { get; set; }
    }
}