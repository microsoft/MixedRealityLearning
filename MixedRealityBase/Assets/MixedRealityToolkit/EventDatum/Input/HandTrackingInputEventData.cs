// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Microsoft.MixedReality.Toolkit.Core.EventDatum.Input
{
    public class HandTrackingInputEventData : InputEventData
    {
        /// <summary>
        /// This property describes whether the Hand described by the HandTrackingInputSource
        /// is currently grabbing.
        /// </summary>
        public bool IsHandGrabbing { get; private set; }

        /// <summary>
        /// This property describes whether the Hand described by the HandTrackingInputSource
        /// is currently touching an object or any of its children.
        /// </summary>
        public bool IsHandTouching { get; private set; }

        /// <summary>
        /// This property describes the global position the Hand described by the HandTrackingInputSource
        /// is currently grabbing.
        /// </summary>
        public Vector3 ActionPoint { get; private set; }

        /// <summary>
        /// Returns the ray corresponding to distant interaction for the hand.
        /// </summary>
        public Ray HandRay { get; private set; }

        public GameObject TouchedObject { get; set; }

        /// <summary>
        /// Constructor creates a default EventData object.
        /// Requires initialization.
        /// </summary>
        /// <param name="eventSystem"></param>
        public HandTrackingInputEventData(EventSystem eventSystem) : base(eventSystem) { }

        public IMixedRealityController Controller { get; set; }

        /// <summary>
        /// This function is called to fill the HandTrackingIntputEventData object with information
        /// </summary>
        /// <param name="inputSource">This is a reference to the HandTrackingInputSource that created the EventData</param>
        /// <param name="controller">This is a reference to the IMixedRealityController that created the EventData</param>
        /// <param name="grabbing">This is a the state (grabbing or not grabbing) of the HandTrackingInputSource that created the EventData</param>
        /// <param name="pressing">This is a the state (pressing or not pressing) of the HandTrackingInputSource that created the EventData</param>
        /// <param name="actionPoint">This is a the global position grabbed by the HandTrackingInputSource that created the EventData</param>
        /// <param name="touchedObject">This is a the global position of the HandTrackingInputSource that created the EventData</param>
        public void Initialize(IMixedRealityInputSource inputSource, IMixedRealityController controller, bool grabbing, bool pressing, Vector3 actionPoint, GameObject touchedObject, Ray handRay)
        {
            Initialize(inputSource, Definitions.InputSystem.MixedRealityInputAction.None);
            Controller = controller;
            IsHandGrabbing = grabbing;
            IsHandTouching = pressing;
            ActionPoint = actionPoint;
            TouchedObject = touchedObject;
            HandRay = handRay;
        }
    }
}
