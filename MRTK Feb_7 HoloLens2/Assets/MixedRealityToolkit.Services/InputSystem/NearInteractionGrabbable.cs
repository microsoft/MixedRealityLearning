// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Services.InputSystem
{
    /// <summary>
    /// Add a NearInteractionGrabbable component to any GameObject that has a collidable
    /// on it in order to make that collidable near grabbable.
    /// 
    /// Any IMixedRealityNearPointer will then send pointer events to all IMixedRealityPointerHandler
    /// objects when the pointer is close enough to grab the object.
    ///
    /// Additionally, the near pointer will send focus enter and exit events when the near pointer
    /// is focusing this grabbable object (when this is the closes grabbable to the pointer).
    /// </summary>
    public class NearInteractionGrabbable : MonoBehaviour
    {

    }
}