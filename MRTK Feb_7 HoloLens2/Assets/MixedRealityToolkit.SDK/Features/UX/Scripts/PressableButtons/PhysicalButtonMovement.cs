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
    public class PhysicalButtonMovement : MonoBehaviour, IMixedRealityHandTrackHandler
    {
        [SerializeField]
        private GameObject handlerTarget = null;
        private IMixedRealityHandPressTriggerHandler cachedHandler;

        [SerializeField]
        private GameObject movingButtonVisuals = null;

        ///<summary>
        /// Note: It is important the visuals object has a collider to still work with Gaze & Far Select
        ///</summary>
        [SerializeField]
        private Collider visualsCollider = null;

        ///<summary>
        /// Force the Interactable to raycastable layer so it can be gazed with Cursor.
        /// Temporary workaround as Interactable is being set to PhysicalButtonMovement's layer (IgnoreRaycast) for some reason
        ///</summary>
        [SerializeField]
        private bool forceVisualToRaycastableLayer = false;

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

        [SerializeField]
        [Tooltip("How large the actual button's push size is relative to the touch collider's button collider size")]
        private float buttonSizeRelativeToCollider = 0.25f;

        [Tooltip("Used to force a reacquisition of the cachedHandler, necessary if the handler might've changed")]
        [SerializeField]
        private bool forceHandlerToUpdate = false;

        [Header("Display of Internal Vars")]
        [SerializeField]
        private float currentPushDistance = 0.0f;

        [SerializeField]
        private float deepestPressDistance = 0.0f;
        [SerializeField]
        private float lastClickDepth = 0.0f;

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

        private List<IMixedRealityHandVisualizer> handSources = new List<IMixedRealityHandVisualizer>();
        private List<uint> sourceIds = new List<uint>();
        private Transform initialPosition;
        private Transform finalPosition;

        private float previousJointDistance = 0.0f;

        private Vector3 previousPosition;
        private Vector3 targetLocalPosition;

        private void Start()
        {
            previousPosition = movingButtonVisuals.transform.localPosition;
            targetLocalPosition = movingButtonVisuals.transform.localPosition;
            moveMode = false;
            initialPosition = null;
            finalPosition = null;
            localSpacePressDirection.Normalize();

            //Set ourself to ignore raycast (to prevent gaze cursor getting caught on our larger collider)
            gameObject.layer = 2;

            if (visualsCollider != null && forceVisualToRaycastableLayer)
            {
                visualsCollider.gameObject.layer = 0;
            }
        }

        private void Update()
        {
            if (moveMode == false)
            {
                return;
            }
            previousPosition = movingButtonVisuals.transform.localPosition;

            Vector3 bestPushPointOnRay;
            float distance;
            jointWithinButton = false;
            bool IsTouchingCorrected = EvaluateProjectedTouchPosition(out bestPushPointOnRay, out distance);

            distance = Mathf.Min(maxPushDistance, distance);
            distance = Mathf.Clamp(distance, 0, float.MaxValue);
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
                if (maxPushDistance != 0.0f)
                {
                    //We remember the deepest the button has been pressed recently.
                    if (currentPushDistance > deepestPressDistance)
                    {
                        deepestPressDistance = currentPushDistance;
                    }

                    HandlePressProgress(currentPushDistance);
                }

                targetLocalPosition = initialPosition.localPosition + ((finalPosition.localPosition - initialPosition.localPosition).normalized * currentPushDistance);

                //Sanitize the vector so we only move in the correct press direction.
                targetLocalPosition = new Vector3(targetLocalPosition.x * localSpacePressDirection.x, targetLocalPosition.y * localSpacePressDirection.y, targetLocalPosition.z * localSpacePressDirection.z);

                movingButtonVisuals.transform.localPosition = Vector3.Lerp(previousPosition, targetLocalPosition, pressLerpRate);
                jointWithinButton = true;
            }

            if (initialPosition != null && jointWithinButton == false)
            {
                if (movingButtonVisuals.transform.localPosition != finalPosition.localPosition)
                {
                    float remainingDistance = (initialPosition.localPosition - movingButtonVisuals.transform.localPosition).magnitude;

                    HandlePressProgress(remainingDistance);

                    if (distance <= 0)
                    {
                        deepestPressDistance = 0;
                        lastClickDepth = 0;
                    }
                    else
                    {
                        //We only undo the depth press memory when we aren't currently partway through a click
                        if (!Pressing)
                        {
                            //This handles undoing the deepest press memory.
                            if (distance < deepestPressDistance)
                            {
                                deepestPressDistance = distance;
                            }
                            if (remainingDistance < lastClickDepth)
                            {
                                lastClickDepth = distance;
                            }
                        }
                    }

                    float recoverDistance = remainingDistance * returnRate * Time.deltaTime;
                    recoverDistance = Mathf.Min(remainingDistance, recoverDistance);
                    targetLocalPosition = previousPosition + (initialPosition.localPosition - movingButtonVisuals.transform.localPosition).normalized * recoverDistance;

                    //Sanitize this vector so we only move in the correct press direction.
                    targetLocalPosition = new Vector3(targetLocalPosition.x * localSpacePressDirection.x, targetLocalPosition.y * localSpacePressDirection.y, targetLocalPosition.z * localSpacePressDirection.z);

                    movingButtonVisuals.transform.localPosition = Vector3.Lerp(previousPosition, targetLocalPosition, returnLerpRate);
                }
            }

            previousJointDistance = distance;
        }

        ///<summary>
        /// Handles drawing some editor visual elements to give you an idea of the movement and size of the button.
        ///</summary>
        void OnDrawGizmos()
        {
            if (visualsCollider != null)
            {
                var worldPressDirection = (transform.rotation * localSpacePressDirection);
                var buttonSize = visualsCollider.bounds.size.z * buttonSizeRelativeToCollider;
                var startPoint = transform.position - worldPressDirection * buttonSize / 2;
                var endPoint = startPoint + worldPressDirection * maxPushDistance;
                var pushedPoint = startPoint + worldPressDirection * deepestPressDistance;

                Gizmos.DrawLine(startPoint, endPoint);
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(startPoint, pushedPoint);
                Gizmos.color = Color.white;

                //Black Plus indicates the push depth
                Gizmos.color = Color.black;
                Gizmos.DrawLine(endPoint, endPoint + transform.rotation * Vector3.up * visualsCollider.bounds.size.y / 2);
                Gizmos.DrawLine(endPoint, endPoint - transform.rotation * Vector3.up * visualsCollider.bounds.size.y / 2);
                Gizmos.DrawLine(endPoint, endPoint + transform.rotation * Vector3.right * visualsCollider.bounds.size.x / 2);
                Gizmos.DrawLine(endPoint, endPoint - transform.rotation * Vector3.right * visualsCollider.bounds.size.x / 2);
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
            handSources.Remove((IMixedRealityHandVisualizer)eventData.Controller.Visualizer);
            sourceIds.Remove(eventData.SourceId);

            moveMode = false;
            CompleteTouch();
        }

        public void OnTouchUpdated(HandTrackingInputEventData eventData)
        {
        }
        public void OnTouchStarted(HandTrackingInputEventData eventData)
        {
            if (moveMode == false)
            {
                ClearPathMarkers();
                SetPathMarkers();
                moveMode = true;
            }

            handSources.Add((IMixedRealityHandVisualizer)eventData.Controller.Visualizer);
            sourceIds.Add(eventData.SourceId);
        }
        #endregion OnTouch

        #region private Methods

        private void SetPathMarkers()
        {
            GameObject initialMarker = new GameObject("Initial");
            initialMarker.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            initialMarker.transform.position = transform.position;
            initialMarker.transform.parent = transform.transform.parent;
            initialPosition = initialMarker.transform;

            GameObject finalMarker = new GameObject("Final");
            finalMarker.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            finalMarker.transform.position = transform.position + (transform.TransformDirection(localSpacePressDirection.normalized).normalized * maxPushDistance);
            finalMarker.transform.parent = transform.parent;
            finalPosition = finalMarker.transform;
        }

        private void ClearPathMarkers()
        {
            if (initialPosition != null)
            {
                initialPosition.parent = null;
                DestroyImmediate(initialPosition.gameObject);
            }

            if (finalPosition != null)
            {
                finalPosition.parent = null;
                DestroyImmediate(finalPosition.gameObject);
            }
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
            float bestDistance = -1.0f;
            float testDistance;
            Vector3 pressPoint;
            Transform rawHandPoint = null;

            //The buttonSizeRelativeToCollider deals with the parenting problem caused by External Object Targeting
            //We are evaluating our own collider's volume, which is not the same size as the VisualsCollider.
            //This controls for it so we can get the correct dimensional data.
            float size = visualsCollider.bounds.size.z * buttonSizeRelativeToCollider;

            for (int i = 0; i < handSources.Count; ++i)
            {
                handSources[i].TryGetJoint(TrackedHandJoint.IndexTip, out rawHandPoint);

                //This case is to early-out if we have an invalid joint.
                if (rawHandPoint == null)
                {
                    bestHandPointOnRay = Vector3.zero;
                    distance = 0.0f;

                    return false;
                }

                pressPoint = ProjectPointToVector(initialPosition.position, finalPosition.position, rawHandPoint.position, transform.parent != null, out testDistance);
                if (testDistance > bestDistance)
                {
                    bestDistance = testDistance + size;
                    bestHandPointOnRay = pressPoint;
                }
            }

            //This case detects Backpresses (when you approach a button from behind)
            //If we aren't touching, and the current touch distance is further than our push and a bit of the size of the visual box collider.
            bool negativeDelta = bestDistance - previousJointDistance < 0;

            if (!Touching && (negativeDelta || bestDistance > (currentPushDistance + size / 4)))
            {
                //Debug.LogError($"BACKPRESS? tDist: {bestDistance}    curPshDist: {currentPushDistance}     size: {size}             ");

                //If we detect a backpress, this function returns zeroes and false. This results in the button doing nothing. The user must bring their finger in front of the button (and avoid this return case).
                bestHandPointOnRay = Vector3.zero;
                distance = 0.0f;

                return false;
            }

            //If the user is in front of the button, but not farther than the button front plate (-size), we result in no behavior.
            if (bestDistance < -size)
            {
                bestHandPointOnRay = Vector3.zero;
                distance = 0.0f;

                return false;
            }

            //Valid Pressing Case, return best point and distance
            bestHandPointOnRay = bestPoint;
            distance = bestDistance;// + size / 2;
            return true;
        }

        private void HandlePressProgress(float pushDistance)
        {
            //If we aren't in a click and can't start a simple one.
            if (Touching && !Pressing)
            {
                //Compare to our previous push depth.
                if (pushDistance >= lastClickDepth + minPressDepth)
                {
                    deepestPressDistance = pushDistance;
                    BeginClick();
                }
            }
            // If we're in a click and we can't begin a press (
            else if (Pressing)
            {
                var amountWithdrawn = deepestPressDistance - pushDistance;
                if (amountWithdrawn >= withdrawActivationAmount)
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

            movingButtonVisuals.transform.position = initialPosition.position;
            deepestPressDistance = 0;
            lastClickDepth = 0;

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

            lastClickDepth = Mathf.Clamp(pushDistance, 0.0f, maxPushDistance - minPressDepth);

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

        private Vector3 ProjectPointToVector(Vector3 vectorStart, Vector3 vectorEnd, Vector3 point, bool isChild, out float distance)
        {
            Vector3 localPoint = point - vectorStart;
            Vector3 localRay = (vectorEnd - vectorStart).normalized;
            float mag = Vector3.Dot(localPoint, localRay);
            distance = mag;// * (isChild ? 2.0f : 1.0f);
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