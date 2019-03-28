// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using UnityEngine.EventSystems;

namespace Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers
{
    /// <summary>
    // Implementation of this interface causes a script to receive notifications when an object being pressed with HandInteractionPress has finished pressing.
    /// </summary>
    public interface IMixedRealityHandPressTriggerHandler : IEventSystemHandler
    {
        void OnHandPressUntouched();

        void OnHandPressTouched();

        void OnHandPressTriggered();

        void OnHandPressCompleted();
    }
}