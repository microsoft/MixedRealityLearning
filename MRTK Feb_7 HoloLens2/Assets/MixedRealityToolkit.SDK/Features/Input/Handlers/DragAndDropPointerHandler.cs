// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.MixedReality.Toolkit.Core.Definitions.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Physics;
using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers;
using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.Core.Utilities;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.SDK.Input.Handlers
{
    /// <summary>
    /// Component that allows dragging a <see cref="GameObject"/> using a Pointer (instead of GGV Hands)
    /// </summary>
    public class DragAndDropPointerHandler : BaseFocusHandler, IMixedRealitySourceStateHandler, IMixedRealityPointerHandler
    {
        private enum RotationModeEnum
        {
            Default,
            LockObjectRotation,
            OrientTowardUser,
            OrientTowardUserAndKeepUpright
        }

        [SerializeField]
        [Tooltip("The action that will start/stop the dragging.")]
        private MixedRealityInputAction dragAction = MixedRealityInputAction.None;

        [SerializeField]
        [Tooltip("Transform that will be dragged. Defaults to the object of the component.")]
        private Transform hostTransform;

        [SerializeField]
        [Tooltip("How should the GameObject be rotated while being dragged?")]
        private RotationModeEnum rotationMode = RotationModeEnum.Default;

        [SerializeField]
        [Range(0.01f, 1.0f)]
        [Tooltip("Controls the speed at which the object will interpolate toward the desired position")]
        private float positionLerpSpeed = 0.2f;

        [SerializeField]
        [Range(0.01f, 1.0f)]
        [Tooltip("Controls the speed at which the object will interpolate toward the desired rotation")]
        private float rotationLerpSpeed = 0.2f;

        private bool isDragging;
        private bool isDraggingEnabled = true;

        private float stickLength;
        private Vector3 previousPointerPosition;
        private Vector3 previousPointerPositionHeadSpace;

        private Vector3 draggingPosition;
        private Vector3 objectReferenceUp;
        private Vector3 objectReferenceForward;
        private Vector3 objectReferenceGrabPoint;

        private Quaternion draggingRotation;

        private Rigidbody hostRigidbody;
        private bool hostRigidbodyWasKinematic;

        private IMixedRealityPointer currentPointer;
        private IMixedRealityInputSource currentInputSource;

        // If dot product between hand movement and head forward is less than this amount,
        // don't exponentially increas the length of the stick
        private readonly float zPushTolerance = 0.1f;

        #region MonoBehaviour Implementation

        private void Start()
        {
            if (hostTransform == null)
            {
                hostTransform = transform;
            }

            hostRigidbody = hostTransform.GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (isDraggingEnabled && isDragging)
            {
                UpdateDragging();
            }
        }

        private void OnDestroy()
        {
            if (isDragging)
            {
                StopDragging();
            }
        }

        #endregion MonoBehaviour Implementation

        #region IMixedRealityPointerHandler Implementation

        void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData)
        {
            if (eventData.used)
            {
                // A global handler or other object has already used the event
                return;
            }

            if (currentInputSource != null && eventData.SourceId == currentInputSource.SourceId)
            {
                eventData.Use(); // Mark the event as used, so it doesn't fall through to other handlers.

                StopDragging();
            }
        }

        void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData)
        {
            if (isDragging)
            {
                // We're already handling drag input, so we can't start a new drag operation.
                return;
            }

            if (eventData.used)
            {
                // A global handler or other object has already used the event
                return;
            }

            if (eventData.MixedRealityInputAction != dragAction)
            {
                // If we're not grabbing.
                return;
            }

            eventData.Use(); // Mark the event as used, so it doesn't fall through to other handlers.

            currentInputSource = eventData.InputSource;
            currentPointer = eventData.Pointer;

            FocusDetails focusDetails;
            Vector3 initialDraggingPosition = MixedRealityToolkit.InputSystem.FocusProvider.TryGetFocusDetails(currentPointer, out focusDetails)
                    ? focusDetails.Point
                    : hostTransform.position;

            StartDragging(initialDraggingPosition);
        }

        void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData) { }

        #endregion IMixedRealityPointerHandler Implementation

        #region IMixedRealitySourceStateHandler Implementation

        void IMixedRealitySourceStateHandler.OnSourceDetected(SourceStateEventData eventData) { }

        void IMixedRealitySourceStateHandler.OnSourceLost(SourceStateEventData eventData)
        {
            if (currentInputSource != null && eventData.SourceId == currentInputSource.SourceId)
            {
                StopDragging();
            }
        }

        #endregion IMixedRealitySourceStateHandler Implementation

        /// <summary>
        /// Enables or disables dragging.
        /// </summary>
        /// <param name="isEnabled">Indicates whether dragging should be enabled or disabled.</param>
        public void SetDragging(bool isEnabled)
        {
            if (isDraggingEnabled == isEnabled)
            {
                return;
            }

            isDraggingEnabled = isEnabled;

            if (isDragging)
            {
                StopDragging();
            }
        }

        /// <summary>
        /// Starts dragging the object.
        /// </summary>
        private void StartDragging(Vector3 initialDraggingPosition)
        {
            if (!isDraggingEnabled || isDragging)
            {
                return;
            }


            isDragging = true;

            if (hostRigidbody != null)
            {
                hostRigidbodyWasKinematic = hostRigidbody.isKinematic;
                hostRigidbody.isKinematic = true;
            }
            Transform cameraTransform = CameraCache.Main.transform;

            Vector3 inputPosition;
            if (!currentPointer.TryGetPointerPosition(out inputPosition))
            {
                StopDragging();
                return;
            }

            previousPointerPosition = inputPosition;
            previousPointerPositionHeadSpace = cameraTransform.InverseTransformPoint(inputPosition);
            stickLength = Vector3.Distance(initialDraggingPosition, inputPosition);

            Vector3 objForward = hostTransform.forward;
            Vector3 objUp = hostTransform.up;

            // Store where the object was grabbed from
            objectReferenceGrabPoint = cameraTransform.transform.InverseTransformDirection(hostTransform.position - initialDraggingPosition);

            // in camera space
            objForward = cameraTransform.InverseTransformDirection(objForward);
            objUp = cameraTransform.InverseTransformDirection(objUp);

            objectReferenceForward = objForward;
            objectReferenceUp = objUp;

            // Store the initial offset between the hand and the object, so that we can consider it when dragging
            draggingPosition = initialDraggingPosition;
        }

        /// <summary>
        /// Update the position of the object being dragged.
        /// </summary>
        private void UpdateDragging()
        {
            Transform cameraTransform = CameraCache.Main.transform;

            Vector3 pointerPosition;
            if (!currentPointer.TryGetPointerPosition(out pointerPosition))
            {
                StopDragging();
                return;
            }

            Ray pointingRay;
            currentPointer.TryGetPointingRay(out pointingRay);

            Vector3 currentPosition = pointerPosition;
            Vector3 positionDelta = currentPosition - previousPointerPosition;
            Vector3 currentPositionHeadSpace = cameraTransform.InverseTransformPoint(currentPosition);
            Vector3 positionDeltaHeadSpace = currentPositionHeadSpace - previousPointerPositionHeadSpace;

            float pushDistance = Vector3.Dot(positionDeltaHeadSpace,
                cameraTransform.InverseTransformDirection(pointingRay.direction.normalized));
            if(Mathf.Abs(Vector3.Dot(positionDeltaHeadSpace.normalized, Vector3.forward)) > zPushTolerance)
            {

                stickLength = distanceRamp(stickLength, pushDistance);

            }

            draggingPosition = pointingRay.GetPoint(stickLength);

            switch (rotationMode)
            {
                case RotationModeEnum.OrientTowardUser:
                case RotationModeEnum.OrientTowardUserAndKeepUpright:
                    draggingRotation = Quaternion.LookRotation(hostTransform.position - cameraTransform.position);
                    break;
                case RotationModeEnum.LockObjectRotation:
                    draggingRotation = hostTransform.rotation;
                    break;
                default:
                    // in world space
                    Vector3 objForward = cameraTransform.TransformDirection(objectReferenceForward);
                    // in world space
                    Vector3 objUp = cameraTransform.TransformDirection(objectReferenceUp);
                    draggingRotation = Quaternion.LookRotation(objForward, objUp);
                    break;
            }

            Vector3 newPosition = Vector3.Lerp(hostTransform.position, draggingPosition + cameraTransform.TransformDirection(objectReferenceGrabPoint), positionLerpSpeed);
            // Apply Final Position
            if (hostRigidbody == null)
            {
                hostTransform.position = newPosition;
            }
            else
            {
                hostRigidbody.MovePosition(newPosition);
            }

            // Apply Final Rotation
            Quaternion newRotation = Quaternion.Lerp(hostTransform.rotation, draggingRotation, rotationLerpSpeed);
            if (hostRigidbody == null)
            {
                hostTransform.rotation = newRotation;
            }
            else
            {
                hostRigidbody.MoveRotation(newRotation);
            }

            if (rotationMode == RotationModeEnum.OrientTowardUserAndKeepUpright)
            {
                Quaternion upRotation = Quaternion.FromToRotation(hostTransform.up, Vector3.up);
                hostTransform.rotation = upRotation * hostTransform.rotation;
            }

            previousPointerPosition = pointerPosition;
            previousPointerPositionHeadSpace = currentPositionHeadSpace;
        }

        /*
        An exponential distance ramping where distance is determined by:
        f(t) = (e^At - 1)/B
        where:
        A is a scaling factor: how fast the function ramps to infinity
        B is a second scaling factor: a denominator that shallows out the ramp near the origin
        t is a linear input
        f(t) is the distance exponentially ramped along variable t

        Here's a quick derivation for the expression below.
        A = constant
        B = constant
        d = ramp(t) = (e^At - 1)/B
        t = ramp_inverse(d) =  ln(B*d+1)/A

        In general, if y=f(x), then f(currentY, deltaX) = f( f_inverse(currentY) + deltaX )
        So,
            ramp(currentD, deltaT) = (e^(A*(ln(B*currentD + 1)/A + deltaT)) - 1)/B
        simplified:
            ramp(currentD, deltaT) = (e^(A*deltaT) * (B*currentD + 1) - 1) / B
        */
        private float distanceRamp(float currentDistance, float deltaT, float A = 4.0f, float B = 75.0f)
        {
            return (Mathf.Exp(A * deltaT) * (B * currentDistance + 1) - 1) / B;
        }

        /// <summary>
        /// Stops dragging the object.
        /// </summary>
        private void StopDragging()
        {
            if (!isDragging)
            {
                return;
            }



            isDragging = false;

            if (hostRigidbody != null)
            {
                hostRigidbody.isKinematic = hostRigidbodyWasKinematic;
            }
        }

        public override void OnFocusExit(FocusEventData eventData)
        {
            base.OnFocusExit(eventData);
            if (isDragging && currentPointer == eventData.Pointer)
            {
                StopDragging();
            }
        }
    }
}
