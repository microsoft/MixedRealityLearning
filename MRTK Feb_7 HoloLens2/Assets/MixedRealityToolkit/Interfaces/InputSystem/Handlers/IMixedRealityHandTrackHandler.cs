// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using UnityEngine.EventSystems;

namespace Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers
{
    /// <summary>
    // Implementation of this interface causes a script to receive notifications of Grab and Touch events from HandTrackingInputSources
    /// </summary>
    public interface IMixedRealityHandTrackHandler : IEventSystemHandler
    {
        /// <summary>
        /// When a Touch motion has occurred, this handler receives the event.
        /// <remarks>
        /// A Touch motion is defined as occurring within the bounds of an object (transitive).
        /// </remarks>
        /// <param name="eventData">Contains information about the HandTrackingInputSource.</param>
        void OnTouchStarted(HandTrackingInputEventData eventData);

        /// <summary>
        /// When a Touch motion ends, this handler receives the event.
        /// </summary>
        /// <remarks>
        /// A Touch motion is defined as occurring within the bounds of an object (transitive).
        /// </remarks>
        /// <param name="eventData">Contains information about the HandTrackingInputSource.</param>
        void OnTouchCompleted(HandTrackingInputEventData eventData);

        /// <summary>
        /// When a Touch motion is updated, this handler receives the event.
        /// </summary>
        /// <remarks>
        /// A Touch motion is defined as occurring within the bounds of an object (transitive).
        /// </remarks>
        /// <param name="eventData">Contains information about the HandTrackingInputSource.</param>
        void OnTouchUpdated(HandTrackingInputEventData eventData);
    }
}
