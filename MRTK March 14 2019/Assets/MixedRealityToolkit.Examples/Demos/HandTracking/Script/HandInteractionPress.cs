// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers;
using Microsoft.MixedReality.Toolkit.Services.InputSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos
{
    public class HandInteractionPress : MonoBehaviour, IMixedRealityTouchHandler, IMixedRealityHandPressTriggerHandler
    {
#pragma warning disable 0067
        public event Action<HandInteractionPress> PressTriggered;
        public event Action<HandInteractionPress> PressCompleted;
#pragma warning restore 0067
        [Header("Press Settings")]

        [HideInInspector]
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

        [SerializeField]
        protected TextMesh debugMessage;

        private bool hasPressClicked = false;
        private bool isMoving = false;
        private float initialOffset = 0.0f;
        private List<Vector3> touchPoints = new List<Vector3>();

        [HideInInspector]
        [SerializeField]
        private Vector3 initialPosition = Vector3.zero;

        [HideInInspector]
        [SerializeField]
        private Vector3 finalPosition = Vector3.zero;

        private void OnValidate()
        {
            var nearInteractionTouchable = GetComponent<NearInteractionTouchable>();
            if (nearInteractionTouchable != null)
            {
                pressDirection = -1 * nearInteractionTouchable.Forward;
            }
            pressDirection.Normalize();

            initialPosition = transform.localPosition;
            finalPosition = initialPosition + pressDirection * maxPushDistance;
        }

        public virtual void Update()
        {
            if (!isMoving)
            {
                return;
            }

            bool pushed = false;

            if (GetCorrectedTouchPosition(out float distance))
            {
                if (distance >= initialOffset)
                {
                    distance = Mathf.Max(0.0f, distance);
                    if (maxPushDistance > 0.0f)
                    {
                        distance = Mathf.Min(maxPushDistance, distance);
                    }

                    HandlePressProgress(distance);
                    transform.localPosition = initialPosition + pressDirection * distance;

                    pushed = true;
                    Debug.DrawLine(transform.TransformPoint(initialPosition), transform.TransformPoint(finalPosition), Color.yellow);
                    Debug.DrawLine(transform.TransformPoint(initialPosition), transform.position, Color.cyan);
                }
            }
            
            if (!pushed)
            {
                float remainingDistance = (initialPosition - transform.localPosition).magnitude;
                float recoverDistance = remainingDistance * returnRate * Time.deltaTime;
                remainingDistance -= recoverDistance;

                if (remainingDistance <= 0.001f)
                {
                    isMoving = false;
                    remainingDistance = 0.0f;
                }
                
                HandlePressProgress(remainingDistance);
                transform.localPosition = initialPosition + pressDirection * remainingDistance;
            }

            touchPoints.Clear();
        }

        void IMixedRealityTouchHandler.OnTouchCompleted(HandTrackingInputEventData eventData) { }

        void IMixedRealityTouchHandler.OnTouchStarted(HandTrackingInputEventData eventData)
        {
            Vector3 touchPointLocal = eventData.InputData;
            if (transform.parent != null)
            {
                touchPointLocal = transform.parent.transform.InverseTransformPoint(touchPointLocal);
            }

            if (!isMoving)
            {
                isMoving = true;

                initialOffset = GetProjectedDistance(initialPosition, pressDirection, touchPointLocal);
            }

            touchPoints.Add(touchPointLocal);
        }

        void IMixedRealityTouchHandler.OnTouchUpdated(HandTrackingInputEventData eventData)
        {
            Vector3 touchPointLocal = eventData.InputData;
            if (transform.parent != null)
            {
                touchPointLocal = transform.parent.transform.InverseTransformPoint(touchPointLocal);
            }
            touchPoints.Add(touchPointLocal);
        }

        public void OnHandPressUntouched() { }

        public void OnHandPressTouched() { }

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

        private bool GetCorrectedTouchPosition(out float distance)
        {
            if (touchPoints.Count == 0)
            {
                distance = 0.0f;
                return false;
            }
            
            float bestDistance = float.MinValue;

            for (int i = 0; i < touchPoints.Count; ++i)
            {
                // action point corresponds to the position of the fingertip, or whatever other point was used to compute
                // touch dispatch (touch down, update, etc.)
                ProjectPointToVector(initialPosition, pressDirection, touchPoints[i], out float testDistance);
                bestDistance = Mathf.Max(bestDistance, testDistance /*- initialOffset*/);
            }
            
            distance = bestDistance;
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

        private Vector3 ProjectPointToVector(Vector3 vectorStart, Vector3 pressDirection, Vector3 point, out float distance)
        {
            Vector3 vectorToPoint = point - vectorStart;
            distance = Vector3.Dot(vectorToPoint, pressDirection);
            return vectorStart + (pressDirection * distance);
        }

        private float GetProjectedDistance(Vector3 vectorStart, Vector3 pressDirection, Vector3 point)
        {
            Vector3 vectorToPoint = point - vectorStart;
            return Vector3.Dot(vectorToPoint, pressDirection);
        }

        #endregion
    }
}