// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers;
using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos
{
    public class HandInteractionTouch : MonoBehaviour, IMixedRealityHandTrackHandler
    {
        public TextMesh debugMessage;

        public void OnTouchCompleted(HandTrackingInputEventData eventData)
        {
            if(debugMessage != null)
            {
                debugMessage.text = "OnTouchCompleted: " + Time.unscaledTime.ToString();
            }
        }

        public void OnTouchStarted(HandTrackingInputEventData eventData)
        {
            if (debugMessage != null)
            {
                debugMessage.text = "OnTouchStarted: " + Time.unscaledTime.ToString();
            }

        }

        public void OnTouchUpdated(HandTrackingInputEventData eventData)
        {
        }

    }
}