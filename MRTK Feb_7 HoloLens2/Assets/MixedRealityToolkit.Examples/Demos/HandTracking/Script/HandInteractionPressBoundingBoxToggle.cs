// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers;
using Microsoft.MixedReality.Toolkit.SDK.UX;
using Microsoft.MixedReality.Toolkit.SDK.UX.Receivers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos
{
    public class HandInteractionPressBoundingBoxToggle : HandInteractionPress, IMixedRealityHandTrackHandler, IMixedRealityHandPressTriggerHandler
    {
        public BoundingBox boundingBox;

        public override void OnHandPressTriggered()
        {
            // Do something on specified distance for fire event
            if(boundingBox != null)
            {
                boundingBox.Active = !boundingBox.Active;
            }

        }
    }
}