// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos
{
    public class HandInteractionPress : MonoBehaviour, IMixedRealityHandTrackHandler, IMixedRealityHandPressTriggerHandler
    {
#pragma warning disable 0067
        public event Action<HandInteractionPress> PressTriggered;
        public event Action<HandInteractionPress> PressCompleted;
#pragma warning restore 0067
        [Header("Press Settings")]
        [SerializeField]
        private Vector3 pressDirection = new Vector3(0, 0, 1);

        [SerializeField]
        [Tooltip("Maximum push distance")]
        private float maxPushDistance = 0.2f;

        [SerializeField]
        [Tooltip("Speed of the object movement on release")]
        private float returnRate = 10.0f;

        [SerializeField]
        [Tooltip("Fraction of the max push distance that triggers OnHandPressTriggered() event")]
        private float pressEventFireFraction = 0.8f;

        [SerializeField]
        [Tooltip("Fraction of the max push distance that triggers OnHandPressCompleted() event")]
        private float pressEventReleaseFraction = 0.3f;
        private float currentPushDistance = 0.0f;
        private bool hasPressClicked = false;
        private List<IMixedRealityHandVisualizer> handSources;
        private Dictionary<uint, Vector3> handSourceIdToActionPoint = new Dictionary<uint, Vector3>();
        private List<uint> sourceIds;
        private Transform initialPosition;
        private Transform finalPosition;
        private float initialOffset;
        private bool moveMode = false;
        public TextMesh debugMessage;

        private void Start()
        {
            moveMode = false;
            handSources = new List<IMixedRealityHandVisualizer>();
            sourceIds = new List<uint>();
            initialPosition = null;
            finalPosition = null;
            pressDirection.Normalize();
        }

        public virtual void Update()
        {
            if (moveMode == false)
            {
                return;
            }

            Vector3 bestPushPointOnRay;
            float distance;
            bool pushed = false;

            if (true == GetCorrectedTouchPosition(out bestPushPointOnRay, out distance))
            {
                if (distance >= initialOffset)
                {
                    if (maxPushDistance != 0.0f)
                    {
                        distance = Mathf.Min(maxPushDistance, distance);
                        currentPushDistance = distance;
                        HandlePressProgress(currentPushDistance);
                    }
                    transform.localPosition = initialPosition.localPosition + ((finalPosition.localPosition - initialPosition.localPosition).normalized * (currentPushDistance));

                    pushed = true;
                }
            }

            if (initialPosition != null && pushed == false)
            {
                if (transform.localPosition != initialPosition.localPosition)
                {
                    float remainingDistance = (initialPosition.localPosition - transform.localPosition).magnitude;

                    HandlePressProgress(remainingDistance);

                    if (remainingDistance <= 0.001f)
                    {
                        transform.localPosition = initialPosition.localPosition;
                        moveMode = false;
                        ClearPathMarkers();
                    }
                    else
                    {
                        float recoverDistance = remainingDistance * returnRate * Time.deltaTime;
                        recoverDistance = Mathf.Min(remainingDistance, recoverDistance);
                        transform.localPosition += (initialPosition.localPosition - transform.localPosition).normalized * recoverDistance;
                    }
                }
            }
        }

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
            if (handSourceIdToActionPoint.ContainsKey(eventData.SourceId))
            {
                handSourceIdToActionPoint.Remove(eventData.SourceId);
            }
        }

        public void OnTouchStarted(HandTrackingInputEventData eventData)
        {
            if (moveMode == false)
            {
                ClearPathMarkers();
                SetPathMarkers();
                moveMode = true;
            }

            if (handSources.Count == 0)
            {
                initialOffset = GetProjectedDistance(initialPosition.position, finalPosition.position, eventData.ActionPoint);
            }
            handSources.Add((IMixedRealityHandVisualizer)eventData.Controller.Visualizer);
            sourceIds.Add(eventData.SourceId);
            handSourceIdToActionPoint.Add(eventData.SourceId, eventData.ActionPoint);
        }

        public void OnTouchUpdated(HandTrackingInputEventData eventData)
        {
            uint sourceId = eventData.SourceId;
            if (handSourceIdToActionPoint.ContainsKey(eventData.SourceId))
            {
                handSourceIdToActionPoint[eventData.SourceId] = eventData.ActionPoint;
            }
        }

        public void OnHandPressUntouched()
        {
        }

        public void OnHandPressTouched()
        {
        }

        public virtual void OnHandPressTriggered()
        {
            // Do something on specified distance for fire event
            if (debugMessage != null)
            {
                debugMessage.text = "OnHandPressTriggered: " + Time.unscaledTime.ToString();
            }
        }

        public virtual void OnHandPressCompleted()
        {
            // Do something when the button completely moves back
            if (debugMessage != null)
            {
                debugMessage.text = "OnHandPressCompleted: " + Time.unscaledTime.ToString();
            }
        }

        #region private Methods

        private void SetPathMarkers()
        {
            GameObject initialMarker = new GameObject("Initial");
            initialMarker.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            initialMarker.transform.position = transform.position;
            initialMarker.transform.parent = transform.parent;
            initialPosition = initialMarker.transform;

            GameObject finalMarker = new GameObject("Final");
            finalMarker.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            finalMarker.transform.position = transform.position + (transform.TransformDirection(pressDirection.normalized).normalized * maxPushDistance);
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

        private bool GetCorrectedTouchPosition(out Vector3 bestHandPointOnRay, out float distance)
        {
            if (handSources.Count == 0)
            {
                bestHandPointOnRay = Vector3.zero;
                distance = 0.0f;
                return false;
            }

            Vector3 bestPoint = Vector3.zero;
            float bestDistance = -1.0f;
            float testDistance;
            Vector3 pressPoint = Vector3.zero;
            Vector3 actionPoint;

            for (int i = 0; i < handSources.Count; ++i)
            {
                // action point corresponds to the position of the fingertip, or whatever other point was used to compute
                // touch dispatch (touch down, update, etc.)
                actionPoint = handSourceIdToActionPoint[handSources[i].Controller.InputSource.SourceId];
                pressPoint = ProjectPointToVector(initialPosition.position, finalPosition.position, actionPoint, transform.parent != null, out testDistance);
                if (testDistance > bestDistance)
                {
                    bestDistance = testDistance;
                    bestHandPointOnRay = pressPoint;
                }
            }

            bestHandPointOnRay = bestPoint;
            distance = (pressPoint - initialPosition.position).magnitude;
            return true;
        }

        private void HandlePressProgress(float pushDistance)
        {
            if (hasPressClicked == true)
            {
                if (pushDistance <= maxPushDistance * pressEventReleaseFraction)
                {
                    hasPressClicked = false;
                    IMixedRealityHandPressTriggerHandler[] handlers = GetComponentsInChildren<IMixedRealityHandPressTriggerHandler>();
                    foreach (IMixedRealityHandPressTriggerHandler handler in handlers)
                    {
                        if (handler != null)
                        {
                            handler.OnHandPressCompleted();
                        }
                    }
                }
            }
            else
            {
                if (pushDistance >= maxPushDistance * pressEventFireFraction)
                {
                    hasPressClicked = true;
                    IMixedRealityHandPressTriggerHandler[] handlers = GetComponentsInChildren<IMixedRealityHandPressTriggerHandler>();
                    foreach (IMixedRealityHandPressTriggerHandler handler in handlers)
                    {
                        if (handler != null)
                        {
                            handler.OnHandPressTriggered();
                        }
                    }
                }
            }
        }

        private Vector3 ProjectPointToVector(Vector3 vectorStart, Vector3 vectorEnd, Vector3 point, bool isChild, out float distance)
        {
            Vector3 localPoint = point - vectorStart;
            Vector3 localRay = (vectorEnd - vectorStart).normalized;
            float mag = Vector3.Dot(localPoint, localRay);
            distance = mag * (isChild ? 2.0f : 1.0f);
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