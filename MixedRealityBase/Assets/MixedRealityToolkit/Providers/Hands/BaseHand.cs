// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Core.Definitions.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers;
using Microsoft.MixedReality.Toolkit.Core.Providers;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Core.Devices.Hands
{
    public abstract class BaseHand : BaseController
    {
        protected enum HandBehaviorType
        {
            Touch = 0,
            Grab
        }

        // Hand ray
        protected HandRay HandRay { get; } = new HandRay();
        public override bool IsInPointingPose
        {
            get
            {
                return HandRay.ShouldShowRay;
            }
        }

        protected IMixedRealityHandVisualizer HandVisualizer => handVisualizer ?? (handVisualizer = Visualizer as IMixedRealityHandVisualizer);
        private IMixedRealityHandVisualizer handVisualizer = null;

        // Touching/Pressing internal states
        private List<GameObject> objectsToTouchUpdate = new List<GameObject>();
        private Vector3 lastTouchingPoint;
        private TouchingState lastPressingState = TouchingState.NotPressing;
        private GameObject lastTouchedObject = null;

        // Velocity internal states
        private float deltaTimeStart;
        private Vector3 lastPosition;
        private Vector3 lastPalmNormal;
        private readonly int velocityUpdateInterval = 9;
        private int frameOn = 0;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="trackingState"></param>
        /// <param name="controllerHandedness"></param>
        /// <param name="inputSource"></param>
        /// <param name="interactions"></param>
        public BaseHand(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
                : base(trackingState, controllerHandedness, inputSource, interactions)
        {
        }

        /// <inheritdoc />
        public override MixedRealityInteractionMapping[] DefaultLeftHandedInteractions => DefaultInteractions;

        /// <inheritdoc />
        public override MixedRealityInteractionMapping[] DefaultRightHandedInteractions => DefaultInteractions;

        public override void SetupDefaultInteractions(Handedness controllerHandedness)
        {
            AssignControllerMappings(DefaultInteractions);
        }

        #region Protected InputSource Helpers

        #region Gesture Definitions

        protected void TestForTouching()
        {
            Vector3 indexTipPosition = GetJointPosition(TrackedHandJoint.IndexTip);
            if (indexTipPosition == Vector3.zero)
            {
                return;
            }

            GameObject[] touchableObjects = GetTouchableObjects();

            for (int i = 0; i < touchableObjects.Length; ++i)
            {
                //test touch here
                Collider[] colliders = touchableObjects[i].GetComponentsInChildren<Collider>();

                for (int c = 0; c < colliders.Length; ++c)
                {
                    if (DoesColliderContain(colliders[c], indexTipPosition) == true)
                    {
                        lastTouchedObject = colliders[c].gameObject;
                        lastTouchingPoint = indexTipPosition;
                        if (objectsToTouchUpdate.Contains(colliders[c].gameObject) == false)
                        {
                            objectsToTouchUpdate.Add(colliders[c].gameObject);

                            IMixedRealityHandTrackHandler[] handlers = touchableObjects[i].GetComponents<IMixedRealityHandTrackHandler>();
                            for (int h = 0; h < handlers.Length; ++h)
                            {
                                handlers[h].OnTouchStarted(GetHandSourceEventData(HandBehaviorType.Touch));
                            }
                        }
                        else
                        {
                            IMixedRealityHandTrackHandler[] handlers = touchableObjects[i].GetComponents<IMixedRealityHandTrackHandler>();
                            for (int h = 0; h < handlers.Length; ++h)
                            {
                                handlers[h].OnTouchUpdated(GetHandSourceEventData(HandBehaviorType.Touch));
                            }
                        }
                    }
                    else if (objectsToTouchUpdate.Contains(colliders[c].gameObject) == true)
                    {
                        objectsToTouchUpdate.Remove(colliders[c].gameObject);
                        lastTouchedObject = colliders[c].gameObject;

                        IMixedRealityHandTrackHandler[] handlers = touchableObjects[i].GetComponents<IMixedRealityHandTrackHandler>();
                        for (int h = 0; h < handlers.Length; ++h)
                        {
                            handlers[h].OnTouchCompleted(GetHandSourceEventData(HandBehaviorType.Touch));
                        }
                    }
                }
            }
        }

        protected void UpdateVelocity()
        {
            if (frameOn == 0)
            {
                deltaTimeStart = Time.unscaledTime;
                lastPosition = GetJointPosition(TrackedHandJoint.Palm);
                lastPalmNormal = GetPalmNormal();
                //lastPalmNormal = new Vector3(1, 0, 0);
            }
            else if (frameOn == velocityUpdateInterval)
            {
                //update linear velocity
                float deltaTime = Time.unscaledTime - deltaTimeStart;
                Vector3 newVelocity = (GetJointPosition(TrackedHandJoint.Palm) - lastPosition) / deltaTime;
                Velocity = (Velocity * 0.8f) + (newVelocity * 0.2f);

                //update angular velocity
                Vector3 currentPalmNormal = GetPalmNormal();
                //currentPalmNormal = new Vector3(0, 1, 0);
                Quaternion rotation = Quaternion.FromToRotation(lastPalmNormal, currentPalmNormal);
                Vector3 rotationRate = rotation.eulerAngles * Mathf.Deg2Rad;
                AngularVelocity = rotationRate / deltaTime;
            }

            frameOn++;
            frameOn = frameOn > velocityUpdateInterval ? 0 : frameOn;
        }

        #endregion Gesture Definitions

        private Vector3 GetJointPosition(TrackedHandJoint jointToGet)
        {
            if (HandVisualizer != null)
            {
                Transform transform;

                if (HandVisualizer.TryGetJoint(jointToGet, out transform))
                {
                    return transform.position;
                }
            }

            return Vector3.zero;
        }

        protected Vector3 GetPalmNormal()
        {
            if (HandVisualizer != null)
            {
                Transform transform;

                if (HandVisualizer.TryGetJoint(TrackedHandJoint.Palm, out transform))
                {
                    return -transform.up;
                }
            }

            return Vector3.zero;
        }

        private HandTrackingInputEventData GetHandSourceEventData(HandBehaviorType type)
        {
            HandTrackingInputEventData data = new HandTrackingInputEventData(UnityEngine.EventSystems.EventSystem.current);
            data.Initialize(InputSource, this, false,
                            lastPressingState == TouchingState.Pressing, lastTouchingPoint, lastTouchedObject, HandRay.Ray);
            return data;
        }

        /// <summary>
        /// Gets all IMixedRealityHandTrackHandler objects that are in the scene.
        /// </summary>
        /// <returns></returns>
        private IMixedRealityHandTrackHandler[] GetHandlers()
        {
            List<IMixedRealityHandTrackHandler> handHandlers = new List<IMixedRealityHandTrackHandler>();

            GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();
            for (int i = 0; i < gameObjects.Length; ++i)
            {
                IMixedRealityHandTrackHandler[] handlers = gameObjects[i].GetComponents<IMixedRealityHandTrackHandler>();
                if (handlers.Length > 0)
                {
                    handHandlers.AddRange(handlers);
                }
            }

            return handHandlers.ToArray();
        }

        private GameObject[] GetTouchableObjects()
        {
            List<GameObject> pressableObjects = new List<GameObject>();
            GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();
            for (int i = 0; i < gameObjects.Length; ++i)
            {
                IMixedRealityHandTrackHandler[] handlers = gameObjects[i].GetComponents<IMixedRealityHandTrackHandler>();
                if (handlers.Length > 0)
                {
                    pressableObjects.Add(gameObjects[i]);
                }
            }

            return pressableObjects.ToArray();
        }

        private bool DoesColliderContain(Collider collider, Vector3 point)
        {
            if (collider == null)
            {
                return false;
            }

            if (collider is SphereCollider)
            {
                SphereCollider sphereCollider = collider as SphereCollider;
                Vector3 xformedPt = sphereCollider.transform.InverseTransformPoint(point) - sphereCollider.center;
                return xformedPt.sqrMagnitude <= (sphereCollider.radius * sphereCollider.radius);
            }
            else if (collider is BoxCollider)
            {
                BoxCollider boxCollider = collider as BoxCollider;
                Vector3 xformedPt = collider.transform.InverseTransformPoint(point) - boxCollider.center;
                Vector3 extents = boxCollider.size * 0.5f;

                return (xformedPt.x <= extents.x && xformedPt.x >= -extents.x &&
                        xformedPt.y <= extents.y && xformedPt.y >= -extents.y &&
                        xformedPt.z <= extents.z && xformedPt.z >= -extents.z);
            }
            else if (collider is CapsuleCollider)
            {
                CapsuleCollider capsuleCollider = collider as CapsuleCollider;
                float radiusSqr = capsuleCollider.radius * capsuleCollider.radius;
                Vector3 xformedPt = capsuleCollider.transform.InverseTransformPoint(point) - capsuleCollider.center;

                //fast check
                if (xformedPt.sqrMagnitude <= radiusSqr)
                {
                    return true;
                }

                //other checks
                float halfCylinderHeight = (capsuleCollider.height - (capsuleCollider.radius * 2.0f)) * 0.5f;
                Vector3 offset = new Vector3(0.0f, halfCylinderHeight, 0.0f);

                //check end hemispheres
                if ((xformedPt - (capsuleCollider.center + offset)).sqrMagnitude <= radiusSqr)
                {
                    return true;
                }
                if ((xformedPt - (capsuleCollider.center - offset)).sqrMagnitude <= radiusSqr)
                {
                    return true;
                }

                //check cylinder
                float distSqr = DistanceSqrPointToLine(capsuleCollider.center - offset, capsuleCollider.center + offset, xformedPt);
                if (distSqr <= radiusSqr)
                {
                    return true;
                }

                //failed all tests- not in capsule
                return false;
            }
            else
            {
                Vector3 xformedPt = collider.transform.InverseTransformPoint(point) - collider.bounds.center;
                return collider.bounds.Contains(xformedPt);
            }
        }

        private float DistanceSqrPointToLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
        {
            if (lineStart == lineEnd)
            {
                return (point - lineStart).magnitude;
            }

            float lineSegmentMagnitude = (lineEnd - lineStart).magnitude;
            Vector3 ray = (lineEnd - lineStart);
            ray *= (1.0f / lineSegmentMagnitude);
            float dot = Vector3.Dot(point - lineStart, ray);
            if (dot <= 0)
            {
                return (point - lineStart).sqrMagnitude;
            }
            if (dot >= lineSegmentMagnitude)
            {
                return (point - lineEnd).sqrMagnitude;
            }
            return ((lineStart + (ray * dot)) - point).sqrMagnitude;
        }

        #endregion Private InputSource Helpers

    }
}