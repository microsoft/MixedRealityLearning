// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.SDK.UX.PressableButtons
{
    ///<summary>
    /// This is the way to do physical hand interaction for Interactables.
    /// It uses External Object Targeting. This object has a BoxCollider which is larger than the Interactable.
    /// When we get physical touch, we figure out how close it is to the Button. When it is close, we Begin Touch.
    /// When we push beyond a certain threshold, we Begin Press.
    /// When we withdraw back beyond a threshold, we finish the Click (AKA Unpress)
    /// When we stop touching the button, or leave the collider through the side or back we finish the Touch (AKA Untouch). This also does not complete an in progress Press/Click.
    /// This class will execute IMRHandPressTriggerHandler. You can use <see cref="PhysicalPressEventRouter"/> to route these events to Interactable.
    /// If you don't like <see cref="PhysicalPressEventRouter"/>, you can clone it and make your own!
    ///</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class PhysicalButtonMovement : MonoBehaviour, IMixedRealityTouchHandler
    {
        [SerializeField]
        private GameObject handlerTarget = null;
        private IMixedRealityHandPressTriggerHandler cachedHandler;

        [SerializeField]
        private GameObject movingButtonVisuals = null;

        [SerializeField]
        [Header("Press Settings")]
        [Tooltip("Maximum push distance")]
        private float maxPushDistance = 0.2f;

        [SerializeField]
        [Tooltip("Speed of the object movement on release. High values are recommended otherwise the button feels sluggish.")]
        private float returnRate = 25.0f;

        [SerializeField]
        [Tooltip("Distance the button must be pushed before it can be withdrawn to fire the PressCompleted event.")]
        private float minPressDepth = 0.02f;

        [SerializeField]
        [Tooltip("Withdraw amount needed to progress from Press to ClickCompleted. .01 is 1 cm.")]
        private float withdrawActivationAmount = 0.01f;

        [Tooltip("Used to force a reacquisition of the cachedHandler, necessary if the handler might've changed")]
        [SerializeField]
        private bool forceHandlerToUpdate = false;

        [Header("Display of Internal Vars")]
        [SerializeField]
        private float currentPushDistance = 0.0f;

        ///<summary>
        /// Represents the rate at which the button lerps to follow the press depth.
        /// Doesn't look good, defaulted of 1.0f is the equivalent of OFF.
        ///</summary>
        private float pressLerpRate = 1.0f;

        ///<summary>
        /// Represents the rate at which the button lerps to return from a press depth.
        /// Doesn't look good, defaulted of 1.0f is the equivalent of OFF.
        ///</summary>
        private float returnLerpRate = 1.0f;

        [Tooltip("Whether movement is currently being observed. Lacks a good case to exit this mode (without immediately resetting button position).")]
        [SerializeField]
        private bool moveMode = false;

        ///<summary>
        /// Represents the state of whether or not a finger is currently touching this button.
        ///</summary>
        [SerializeField]
        private bool Touching = false;

        ///<summary>
        /// Have we finished the first part of the click (surpassing the initial or previous depth by <see cref="minPressDepth"/>)
        ///</summary>
        [SerializeField]
        private bool Pressing = false;

        ///<summary>
        /// A field that is set every update if a joint is in a valid place for pressing the button.
        ///</summary>
        [SerializeField]
        private bool jointWithinButton = false;

        ///<summary>
        /// Changing the press vector away from Z isn't recommended as support for non-Z+ vectors isn't entirely finished
        ///</summary>
        private Vector3 localSpacePressDirection = new Vector3(0, 0, 1);

        private List<IMixedRealityHand> handSources = new List<IMixedRealityHand>();
        private List<uint> sourceIds = new List<uint>();
        private Transform initialPosition;
        private Transform finalPosition;

        private float previousJointDistance = 0.0f;

        private void Start()
        {
            moveMode = false;
            initialPosition = null;
            localSpacePressDirection.Normalize();

            if (gameObject.layer == 2)
            {
                Debug.LogWarning("PhysicalButtonMovement will not work if game object layer is set to 'Ignore Raycast'.");
            }
        }

        private void Update()
        {
            if (moveMode == false)
            {
                return;
            }
            float previousPushDistance = currentPushDistance;
            Vector3 prevLocalPosition = ComputeVisualsLocalPosition(previousPushDistance);

            Vector3 bestPushPointOnRay;
            float distance;
            jointWithinButton = false;
            bool IsTouchingCorrected = EvaluateProjectedTouchPosition(out bestPushPointOnRay, out distance);
            currentPushDistance = distance;

            if (IsTouchingCorrected && !Touching)
            {
                BeginTouch();
            }
            else if (!IsTouchingCorrected && Touching)
            {
                CompleteTouch();
            }

            if (IsTouchingCorrected)
            {
                HandlePressProgress(currentPushDistance, previousPushDistance);

                if (movingButtonVisuals != null)
                {
                    Vector3 targetLocalPosition = ComputeVisualsLocalPosition(currentPushDistance);
                    targetLocalPosition = Vector3.Lerp(prevLocalPosition, targetLocalPosition, pressLerpRate);
                    movingButtonVisuals.transform.localPosition = targetLocalPosition;
                }
                jointWithinButton = true;
            }

            if (initialPosition != null && jointWithinButton == false)
            {
                currentPushDistance = (initialPosition.localPosition - prevLocalPosition).magnitude;
                currentPushDistance = Mathf.Max(0.0f, currentPushDistance - currentPushDistance * returnRate * Time.deltaTime);

                HandlePressProgress(currentPushDistance, previousPushDistance);

                if (currentPushDistance <= 0.0001f)
                {
                    currentPushDistance = 0;
                    if (movingButtonVisuals != null)
                    {
                        movingButtonVisuals.transform.localPosition = ComputeVisualsLocalPosition(currentPushDistance);
                    }
                    ClearPathMarkers();
                    moveMode = false;
                    return;
                }

                if (movingButtonVisuals != null)
                {
                    Vector3 targetLocalPosition = ComputeVisualsLocalPosition(currentPushDistance);
                    movingButtonVisuals.transform.localPosition = Vector3.Lerp(prevLocalPosition, targetLocalPosition, returnLerpRate);
                }
            }

            previousJointDistance = distance;
        }

        ///<summary>
        /// Handles drawing some editor visual elements to give you an idea of the movement and size of the button.
        ///</summary>
        void OnDrawGizmos()
        {
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                Vector3 worldPressDirection = (transform.rotation * localSpacePressDirection);

                Vector3 boundsCenter = collider.bounds.center;
                Vector3 startPoint;
                if (movingButtonVisuals != null)
                {
                    startPoint = movingButtonVisuals.transform.position;
                }
                else
                {
                    startPoint = transform.position;
                }
                startPoint = ProjectPointToVector(boundsCenter, boundsCenter + worldPressDirection, startPoint, out float distance);

                Vector3 endPoint = startPoint + worldPressDirection * maxPushDistance;
                Vector3 pushedPoint = startPoint + worldPressDirection * currentPushDistance;

                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(startPoint, pushedPoint);
                Vector3 lastPoint = pushedPoint;

                float releaseDistance = minPressDepth - withdrawActivationAmount;
                if (releaseDistance > currentPushDistance)
                {
                    Gizmos.color = Color.yellow;
                    Vector3 releasePoint = startPoint + worldPressDirection * releaseDistance;
                    Gizmos.DrawLine(lastPoint, releasePoint);
                    lastPoint = releasePoint;
                }

                if (minPressDepth > currentPushDistance)
                {
                    Gizmos.color = Color.cyan;
                    Vector3 pressPoint = startPoint + worldPressDirection * minPressDepth;
                    Gizmos.DrawLine(lastPoint, pressPoint);
                    lastPoint = pressPoint;
                }

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(lastPoint, endPoint);

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(endPoint, endPoint + transform.rotation * Vector3.up * collider.bounds.extents.y);
                Gizmos.DrawLine(endPoint, endPoint - transform.rotation * Vector3.up * collider.bounds.extents.y);
                Gizmos.DrawLine(endPoint, endPoint + transform.rotation * Vector3.right * collider.bounds.extents.x);
                Gizmos.DrawLine(endPoint, endPoint - transform.rotation * Vector3.right * collider.bounds.extents.x);
            }
        }

        #region OnTouch
        /// <summary>
        /// This Handler is called by a HandTrackingInputSource when a Touch action for that hand starts.
        /// </summary>
        /// <remarks>    
        /// A Touch action requires a target. a Touch action must occur inside the bounds of a gameObject.
        /// The eventData argument contains.
        /// </remarks>
        /// <param name="eventData">
        /// The argument passed contains information about the InputSource, the point in space where
        /// the Touch action occurred and the status of the Touch action.
        /// </param>
        public void OnTouchCompleted(HandTrackingInputEventData eventData)
        {
            handSources.Remove((IMixedRealityHand)eventData.Controller);
            sourceIds.Remove(eventData.SourceId);
        }

        public void OnTouchUpdated(HandTrackingInputEventData eventData)
        {
        }

        public void OnTouchStarted(HandTrackingInputEventData eventData)
        {
            handSources.Add((IMixedRealityHand)eventData.Controller);
            sourceIds.Add(eventData.SourceId);

            if (moveMode == false)
            {
                SetPathMarkers();
                Vector3 bestPushPointOnRay;
                EvaluateProjectedTouchPosition(out bestPushPointOnRay, out currentPushDistance);
                moveMode = true;
            }
        }
        #endregion OnTouch

        #region private Methods

        private void SetPathMarkers()
        {
            GameObject initialMarker = new GameObject("Initial");
            initialMarker.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

            GameObject finalMarker = new GameObject("Final");
            finalMarker.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

            Vector3 worldSpacePressDirection = transform.TransformDirection(localSpacePressDirection).normalized;

            if (movingButtonVisuals != null)
            {
                initialMarker.transform.position = movingButtonVisuals.transform.position;
                initialMarker.transform.parent = movingButtonVisuals.transform.parent;

                finalMarker.transform.position = movingButtonVisuals.transform.position + worldSpacePressDirection * maxPushDistance;
                finalMarker.transform.parent = movingButtonVisuals.transform.parent;
            }
            else
            {
                initialMarker.transform.position = transform.position;
                initialMarker.transform.parent = transform.parent;

                finalMarker.transform.position = transform.position + worldSpacePressDirection * maxPushDistance;
                finalMarker.transform.parent = transform.parent;
            }

            initialPosition = initialMarker.transform;
            finalPosition = finalMarker.transform;
        }

        private void ClearPathMarkers()
        {
            if (initialPosition != null)
            {
                initialPosition.parent = null;
                DestroyImmediate(initialPosition.gameObject);
                initialPosition = null;
            }
        }

        private Vector3 ComputeVisualsLocalPosition(float distance)
        {
            Debug.Assert(initialPosition != null);
            Vector3 dir = (finalPosition.localPosition - initialPosition.localPosition).normalized;
            return initialPosition.localPosition + dir * distance;
        }

        // This function projects the joint position onto the 1D push direction of the button. Then depending on the depth and current state it will return the distance of the joint. It will return false with zeroed outs for invalid interaction cases (Backpressing, Too Far, No Hand Sources) 
        private bool EvaluateProjectedTouchPosition(out Vector3 bestHandPointOnRay, out float distance)
        {
            //NO HAND SOURCES
            if (handSources.Count == 0)
            {
                bestHandPointOnRay = Vector3.zero;
                distance = 0.0f;
                return false;
            }

            Vector3 bestPoint = Vector3.zero;
            float bestDistance = float.MinValue;
            float testDistance;
            Vector3 pressPoint;

            for (int i = 0; i < handSources.Count; ++i)
            {
                // This fails on Hololens v2, at least on a build from 2019/01/10
                if (handSources[i].TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose rawHandPoint))
                {
                    pressPoint = ProjectPointToVector(initialPosition.position, finalPosition.position, rawHandPoint.Position, out testDistance);
                    if (testDistance > bestDistance)
                    {
                        bestDistance = testDistance;
                        bestHandPointOnRay = pressPoint;
                    }
                }
            }

            if (bestDistance == float.MinValue)
            {
                bestHandPointOnRay = Vector3.zero;
                distance = 0.0f;
                return false;
            }
            
            bestHandPointOnRay = bestPoint;
            distance = Mathf.Clamp(bestDistance, 0.0f, maxPushDistance); ;
            return true;
        }

        private void HandlePressProgress(float pushDistance, float previousPushDistance)
        {
            //If we aren't in a click and can't start a simple one.
            if (Touching && !Pressing)
            {
                //Compare to our previous push depth. Use previous push distance to handle back-presses.
                if (pushDistance >= minPressDepth && previousPushDistance < minPressDepth)
                {
                    BeginClick();
                }
            }
            // If we're in a click and we can't begin a press
            else if (Pressing)
            {
                float releaseThreshold = minPressDepth - withdrawActivationAmount;
                if (pushDistance <= releaseThreshold && previousPushDistance > releaseThreshold)
                {
                    CompleteClick(pushDistance);
                }
            }
        }

        private void BeginTouch()
        {
            Touching = true;

            if (cachedHandler == null || forceHandlerToUpdate)
            {
                ValidateHandlerTarget();
            }

            if (cachedHandler != null)
            {
                cachedHandler.OnHandPressTouched();
            }
        }

        private void CompleteTouch()
        {
            Touching = false;
            Pressing = false;

            if (cachedHandler == null || forceHandlerToUpdate)
            {
                ValidateHandlerTarget();
            }

            if (cachedHandler != null)
            {
                cachedHandler.OnHandPressUntouched();
            }
        }

        private void BeginClick()
        {
            // Deliberately leaving the BEGAN and COMPLETE logs so future developers can uncomment them if debug is needed
            // Debug.LogError($"CLICK BEGAN       {DateTime.Now} ");

            Pressing = true;

            if (cachedHandler == null || forceHandlerToUpdate)
            {
                ValidateHandlerTarget();
            }

            if (cachedHandler != null)
            {
                cachedHandler.OnHandPressTriggered();
            }
        }

        private void CompleteClick(float pushDistance)
        {
            // Deliberately leaving the BEGAN and COMPLETE logs so future developers can uncomment them if debug is needed
            // Debug.LogError($"CLICK COMPLETE   {DateTime.Now}");

            Pressing = false;

            if (cachedHandler == null || forceHandlerToUpdate)
            {
                ValidateHandlerTarget();
            }

            if (cachedHandler != null)
            {
                cachedHandler.OnHandPressCompleted();
            }
        }

        private void ValidateHandlerTarget()
        {
            if (handlerTarget == null)
            {
                Debug.LogError("Physical Button Movement's handler target has not been assigned. Either disable this component or assign a valid target.");
            }
            else //if (handlerTarget != null)
            {
                cachedHandler = handlerTarget.GetComponent<IMixedRealityHandPressTriggerHandler>();
            }
        }

        private Vector3 ProjectPointToVector(Vector3 vectorStart, Vector3 vectorEnd, Vector3 point, out float distance)
        {
            Vector3 localPoint = point - vectorStart;
            Vector3 localRay = (vectorEnd - vectorStart).normalized;
            float mag = Vector3.Dot(localPoint, localRay);
            distance = mag;
            return vectorStart + (localRay * mag);
        }

        private float GetProjectedDistance(Vector3 vectorStart, Vector3 vectorEnd, Vector3 point)
        {
            Vector3 localPoint = point - vectorStart;
            Vector3 localRay = (vectorEnd - vectorStart).normalized;
            float mag = Vector3.Dot(localPoint, localRay);
            return mag;
        }

        #endregion
    }
}