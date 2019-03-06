// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Core.Definitions.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers;
using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.SDK.UX.Pointers;
using System.Linq;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos
{
    /// <summary>
    /// Simple example that allows an object to be grabbed using NEAR interaction
    /// Will not respond to far interaction
    /// </summary>
    public class HandInteractionGrab : MonoBehaviour, IMixedRealityPointerHandler, IMixedRealityFocusChangedHandler
    {
        [Header("Release Behavior")]
        [SerializeField]
        private bool useVelocity = true;

        public bool UseVelocity { get { return useVelocity; } set { useVelocity = value; } }

        [SerializeField]
        private bool useAngularVelocity = true;

        public bool UseAngularVelocity { get { return useAngularVelocity; } set { useAngularVelocity = value; } }

        [SerializeField]
        private MixedRealityInputAction grabAction = MixedRealityInputAction.None;

        [SerializeField]
        [HideInInspector]
        private Rigidbody rigidBody;

        private uint? grabbingSourceId = null;
        private bool isKinematicDefault = false;
        public TextMesh debugMessage;

        private Color? originalColor;

        private IMixedRealityController manipulatingController;


        // Object rotation relative to the hand when it was grabbed
        private Quaternion objectToHandRotation;

        // Object translation relative to the hand when it was grabbed
        private Vector3 objectToHandTranslation;

        private void OnValidate()
        {
            rigidBody = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                originalColor = mr.material?.color;
            }
            grabbingSourceId = null;
        }

        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            if (eventData.MixedRealityInputAction == grabAction && grabbingSourceId == eventData.SourceId)
            {
                if (debugMessage != null)
                {
                    debugMessage.text = "OnPointerUp: Parent = null" + Time.unscaledTime.ToString();
                }
                DropObject(eventData.Pointer);
            }
        }

        private MixedRealityInteractionMapping GetSpatialGripInfoForController(IMixedRealityController controller)
        {
            if (controller == null)
            {
                return null;
            }

            return controller.Interactions.First(x => x.InputType == DeviceInputType.SpatialGrip);
        }

        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            if (eventData.MixedRealityInputAction == grabAction && 
                grabbingSourceId == null && 
                eventData.Pointer is SpherePointer &&
                !eventData.used)
            {

                    if (debugMessage != null)
                    {
                        debugMessage.text = string.Format("OnPointerDown: Source ID = {0}", eventData.SourceId);
                    }

                    var interactionMapping = GetSpatialGripInfoForController(eventData.Pointer.Controller);
                    if(interactionMapping != null)
                    {
                        manipulatingController = eventData.Pointer.Controller;
                        // Calculate relative transfrom from object to hand
                        Quaternion worlToPalmRotation = Quaternion.Inverse(interactionMapping.PoseData.Rotation);
                        objectToHandRotation = worlToPalmRotation * transform.rotation;
                        objectToHandTranslation = worlToPalmRotation * (transform.position - interactionMapping.PoseData.Position);

                        if (rigidBody != null)
                        {
                            isKinematicDefault = rigidBody.isKinematic;
                            rigidBody.isKinematic = true;
                        }

                        grabbingSourceId = eventData.SourceId;
                    }

            }
        }

        public void Update()
        {
            if (manipulatingController != null)
            {
                var interactionMapping = GetSpatialGripInfoForController(manipulatingController);
                if (interactionMapping != null)
                {
                    // Set current object transform to the relative object to hand transform followed by the current hand transform 
                    transform.SetPositionAndRotation((interactionMapping.PoseData.Rotation * objectToHandTranslation) 
                        + interactionMapping.PoseData.Position, interactionMapping.PoseData.Rotation * objectToHandRotation);
                }
            }
        }

        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
            // Nothing
        }

        public void OnBeforeFocusChange(FocusEventData eventData)
        {
            // Nothing
        }

        /// <summary>
        /// Returns true if parent is an ancestor of child
        /// </summary>
        /// <param name="eventData"></param>
        private bool IsAncestorOf(Transform child, Transform parent)
        {
            Transform cur = child;
            while (cur != null)
            {
                if (cur == parent)
                {
                    return true;
                }
                cur = cur.parent;
            }
            return false;
        }

        public void OnFocusChanged(FocusEventData eventData)
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            // Only change visuals of object if it is not being grabbed / manipulated
            if (grabbingSourceId == null)
            {
                if (eventData.NewFocusedObject != null && IsAncestorOf(eventData.NewFocusedObject.transform, transform))
                {
                    if (eventData.Pointer is SpherePointer)
                    {
                        if (mr != null && originalColor.HasValue)
                        {
                            // Lerp to different color when hovering near to demonstrate differentiation between
                            // near and far hover
                            mr.material.color = Color.Lerp(originalColor.Value, Color.white, 0.75f);
                        }
                    }
                }
                else
                {
                    if (mr != null && originalColor.HasValue)
                    {
                        mr.material.color = originalColor.Value;
                    }
                }
            }
        }

        public void OnFocusExit(FocusEventData eventData)
        {
            if (grabbingSourceId == eventData.Pointer.PointerId)
            {
                DropObject(eventData.Pointer);
            }
        }

        private void DropObject(IMixedRealityPointer pointer)
        {
            manipulatingController = null;
            grabbingSourceId = null;

            if (rigidBody != null)
            {
                rigidBody.isKinematic = isKinematicDefault;

                if (useVelocity)
                {
                    rigidBody.velocity = pointer.Controller.Velocity;
                }

                if (useAngularVelocity)
                {
                    rigidBody.angularVelocity = pointer.Controller.AngularVelocity;
                }
            }
        }
    }
}