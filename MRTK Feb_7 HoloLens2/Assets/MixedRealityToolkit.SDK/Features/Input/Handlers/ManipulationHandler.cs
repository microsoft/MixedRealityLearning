// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using System.Linq;
using UnityEngine.Assertions;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Utilities.Physics;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.SDK.UX.Pointers;
using Microsoft.MixedReality.Toolkit.Core.Definitions.InputSystem;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos
{
    /// <summary>
    /// This script allows for an object to be movable, scalable, and rotatable with one or two hands. 
    /// You may also configure the script on only enable certain manipulations. The script works with 
    /// both HoloLens' gesture input and immersive headset's motion controller input.
    /// See Assets/HoloToolkit-Examples/Input/Readme/README_TwoHandManipulationTest.md
    /// for instructions on how to use the script.
    /// </summary>
    /// 
    public class ManipulationHandler : MonoBehaviour, IMixedRealityPointerHandler, IMixedRealityFocusChangedHandler
    {
        #region Private Enums
        private enum HandMovementType
        {
            oneHandedOnly = 0,
            twoHandedOnly,
            oneAndTwoHanded
        }
        private enum TwoHandedManipulation
        {
            Scale,
            Rotate,
            MoveScale,
            RotateScale,
            MoveRotateScale
        };
        #endregion Private Enums

        #region Serialized Fields
        
        [SerializeField]
        [Tooltip("Transform that will be dragged. Defaults to the object of the component.")]
        private Transform hostTransform = null;

        [SerializeField]
        [Tooltip("What manipulation will two hands perform?")]
        private TwoHandedManipulation ManipulationMode = TwoHandedManipulation.Scale;

        [SerializeField]
        [Tooltip("Constrain rotation along an axis")]
        private RotationConstraintType constraintOnRotation = RotationConstraintType.None;

        [SerializeField]
        private HandMovementType handMoveType = HandMovementType.oneAndTwoHanded;

        [SerializeField]
        [Tooltip("Turns on and off lerping. If off- ignores Lerp Time.")]
        private bool lerpActive = true;

        [SerializeField]
        [Tooltip("Enter a value representing the time in seconds the Lerp should take")]
        private float lerpTime = 0.125f;

        [SerializeField]
        private bool rotateInOneHand = true;
        [SerializeField]
        private bool oneHandRotateWithGrab = true;

        [System.Flags]
        private enum State
        {
            Start = 0x000,
            Moving = 0x001,
            Scaling = 0x010,
            Rotating = 0x100,
            MovingScaling = 0x011,
            RotatingScaling = 0x110,
            MovingRotatingScaling = 0x111
        };
        #endregion Serialized Fields

        #region Private Properties
        private State currentState;
        private TwoHandMoveLogic m_moveLogic;
        private TwoHandScaleLogic m_scaleLogic;
        private TwoHandRotateLogic m_rotateLogic;
        private Dictionary<uint, IMixedRealityPointer> pointerIdToPointerMap = new Dictionary<uint, IMixedRealityPointer>();
        private Dictionary<uint, Vector3> handPositionMap = new Dictionary<uint, Vector3>();
        private Dictionary<uint, bool> handModeMap = new Dictionary<uint, bool>();
        private Dictionary<uint, Quaternion> handOrientationMap = new Dictionary<uint, Quaternion>();
        private Dictionary<uint, Vector3> handTargetOffsetMap = new Dictionary<uint, Vector3>();
        
        #endregion

        #region Monobehaviour Functions
        private void Awake()
        {
            m_moveLogic = new TwoHandMoveLogic();
            m_rotateLogic = new TwoHandRotateLogic(constraintOnRotation);
            m_scaleLogic = new TwoHandScaleLogic();
        }
        private void Start()
        {
            if (hostTransform == null)
            {
                hostTransform = transform;
            }
        }
        private void Update()
        {
            foreach(KeyValuePair<uint, IMixedRealityPointer> entry in pointerIdToPointerMap)
            {
                Vector3 newPosition;
                if (entry.Value.TryGetPointerPosition(out newPosition))
                {
                    handPositionMap[entry.Key] = newPosition;
                }
            }

            if (currentState != State.Start)
            {
                UpdateStateMachine();
            }
        }
        #endregion Monobehaviour Functions

        #region Private Methods
        private bool TryGetGripPositionAndOrientation(IMixedRealityPointer pointer, out Quaternion orientation, out Vector3 position)
        {
            IMixedRealityHandVisualizer handVisualizer = pointer.Controller.Visualizer as IMixedRealityHandVisualizer;
            if (handVisualizer != null)
            {
                Transform palm;
                if (handVisualizer.TryGetJoint(TrackedHandJoint.Palm, out palm) == true)
                {
                    orientation = palm.rotation;
                    Transform tip;
                    handVisualizer.TryGetJoint(TrackedHandJoint.IndexTip, out tip);
                    position = tip.position;
                    return true;
                }
            }
            orientation = Quaternion.identity;
            position = Vector3.zero;
            return false;
        }
        private void SetManipulationMode(TwoHandedManipulation mode)
        {
            ManipulationMode = mode;
        }
        private Vector3 GetHandsCentroid()
        {
            Vector3 result = handPositionMap.Values.Aggregate(Vector3.zero, (current, state) => current + state);
            return result / handPositionMap.Count;
        }
        private bool IsPointerNear(IMixedRealityPointer pointer)
        {
            return pointer is IMixedRealityNearPointer;
        }
        private void SetHandOrientation()
        {
			if( rotateInOneHand == true)
			{
                var arrayOfAllKeys = pointerIdToPointerMap.Keys.ToArray();
                if (arrayOfAllKeys.Length != 1 || handModeMap[arrayOfAllKeys[0]] == false)
                {
                    return;
                }

                uint key = arrayOfAllKeys[0];
                Vector3 grabPoint;
                Quaternion currentOrientation;
                if (true == TryGetGripPositionAndOrientation(pointerIdToPointerMap[key], out currentOrientation, out grabPoint))
                {
                    //if palmorientation isnt in handorientationMap then add it- this insures that the rotation delta on first
                    //update is zero
                    if (handOrientationMap.Keys.Contains(key) == false)
                    {
                        handOrientationMap.Add(key, currentOrientation);
                    }

                    Quaternion delta = handOrientationMap[key] * Quaternion.Inverse(currentOrientation);
                    Quaternion rotationToAppend = Quaternion.Inverse(delta);
                    Quaternion finalRotation = rotationToAppend * hostTransform.rotation;

                    if (oneHandRotateWithGrab == false)
                    {
                        hostTransform.rotation = finalRotation;
                    }
                    else
                    {
                        Vector3 finalPosition = grabPoint + handTargetOffsetMap[key];
                        hostTransform.SetPositionAndRotation(finalPosition, finalRotation);
                    }

                    handOrientationMap[key] = currentOrientation;
                }
			}
        }
        private bool GetHandsNearFar()
        {
            //if at least one hand is in Near then return true;
            var arrayOfAllKeys = pointerIdToPointerMap.Keys.ToArray();
            bool value;
            foreach (uint key in arrayOfAllKeys)
            {
                if (handModeMap.TryGetValue(key, out value) == true && value == true)
                {
                    return true;
                }
            }

            return false;
        }
        private void UpdateStateMachine()
        {
            var handsPressedCount = handPositionMap.Count;
            State newState = currentState;
            switch (currentState)
            {
                case State.Start:
                case State.Moving:
                    if (handsPressedCount == 0)
                    {
                        newState = State.Start;
                    }
                    else
                        if (handsPressedCount == 1 && handMoveType != HandMovementType.twoHandedOnly)
                    {
                        newState = State.Moving;
                    }
                    else if (handsPressedCount > 1 && handMoveType != HandMovementType.oneHandedOnly)
                    {
                        switch (ManipulationMode)
                        {
                            case TwoHandedManipulation.Scale:
                                newState = State.Scaling;
                                break;
                            case TwoHandedManipulation.Rotate:
                                newState = State.Rotating;
                                break;
                            case TwoHandedManipulation.MoveScale:
                                newState = State.MovingScaling;
                                break;
                            case TwoHandedManipulation.RotateScale:
                                newState = State.RotatingScaling;
                                break;
                            case TwoHandedManipulation.MoveRotateScale:
                                newState = State.MovingRotatingScaling;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    break;
                case State.Scaling:
                case State.Rotating:
                case State.MovingScaling:
                case State.RotatingScaling:
                case State.MovingRotatingScaling:
                    // TODO: if < 2, make this go to start state ('drop it')
                    if (handsPressedCount == 0)
                    {
                        newState = State.Start;
                    }
                    else if (handsPressedCount == 1)
                    {
                        newState = State.Moving;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            InvokeStateUpdateFunctions(currentState, newState);
            currentState = newState;
        }
        private void InvokeStateUpdateFunctions(State oldState, State newState)
        {
            if (newState != oldState)
            {
                switch (newState)
                {
                    case State.Moving:
                        OnOneHandMoveStarted();
                        break;
                    case State.Start:
                        OnManipulationEnded();
                        break;
                    case State.RotatingScaling:
                    case State.MovingRotatingScaling:
                    case State.Scaling:
                    case State.Rotating:
                    case State.MovingScaling:
                        OnTwoHandManipulationStarted(newState);
                        break;
                }
                switch (oldState)
                {
                    case State.Start:
                        OnManipulationStarted();
                        break;
                    case State.Scaling:
                    case State.Rotating:
                    case State.RotatingScaling:
                    case State.MovingRotatingScaling:
                    case State.MovingScaling:
                        OnTwoHandManipulationEnded();
                        break;
                }
            }
            else
            {
                switch (newState)
                {
                    case State.Moving:
                        OnOneHandMoveUpdated();
                        break;
                    case State.Scaling:
                    case State.Rotating:
                    case State.RotatingScaling:
                    case State.MovingRotatingScaling:
                    case State.MovingScaling:
                        OnTwoHandManipulationUpdated();
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion Private Methods

        #region Hand Event Handlers
        private bool IsEventAGrabInteraction(MixedRealityPointerEventData eventData)
        {
            return eventData.MixedRealityInputAction.Description == "Grip Press" || eventData.MixedRealityInputAction.Description == "Select";
        }

        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            uint id = eventData.Pointer.PointerId;
            // Ignore poke pointer events
            if (!eventData.used 
                && IsEventAGrabInteraction(eventData)
                && !pointerIdToPointerMap.ContainsKey(eventData.Pointer.PointerId))
            {
                Quaternion gripOrientation;
                Vector3 gripPoint;
                Vector3 position;
                if (eventData.Pointer.TryGetPointerPosition(out position))
                {
                    pointerIdToPointerMap.Add(id, eventData.Pointer);
                    handPositionMap.Add(id, position);
                    handModeMap.Add(id, IsPointerNear(eventData.Pointer));

                    TryGetGripPositionAndOrientation(eventData.Pointer, out gripOrientation, out gripPoint);
                    //do not set handOrientationMap here. Instead set it in first update loop.
                    handTargetOffsetMap.Add(id, hostTransform.position - gripPoint);

                }

                UpdateStateMachine();
                eventData.Use();
            }
        }
        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            uint id = eventData.Pointer.PointerId;
            if (pointerIdToPointerMap.ContainsKey(id))
            {
                pointerIdToPointerMap.Remove(id);
                handPositionMap.Remove(id);
                handModeMap.Remove(id);
                handOrientationMap.Remove(id);
                handTargetOffsetMap.Remove(id);
            }


            UpdateStateMachine();
            eventData.Use();
        }
        #endregion Hand Event Handlers

        #region Private Event Handlers
        private void OnTwoHandManipulationUpdated()
        {
            var targetRotation = hostTransform.rotation;
            var targetPosition = hostTransform.position;
            var targetScale = hostTransform.localScale;

            if ((currentState & State.Moving) > 0)
            {
                targetPosition = m_moveLogic.Update(GetHandsCentroid(), targetPosition, GetHandsNearFar());
            }
            if ((currentState & State.Rotating) > 0)
            {
                targetRotation = m_rotateLogic.Update(handPositionMap, targetRotation);
            }
            if ((currentState & State.Scaling) > 0)
            {
                targetScale = m_scaleLogic.UpdateMap(handPositionMap);
            }

            hostTransform.position += ComputeWeightedVector3(hostTransform.position, targetPosition);
            hostTransform.rotation = targetRotation;
            hostTransform.localScale += ComputeWeightedVector3(hostTransform.localScale, targetScale);
        }
        private void OnOneHandMoveUpdated()
        {
            var targetPosition = m_moveLogic.Update(GetHandsCentroid(), hostTransform.position, GetHandsNearFar());
            hostTransform.position += ComputeWeightedVector3(hostTransform.position, targetPosition);
            SetHandOrientation();
        }
        private Vector3 ComputeWeightedVector3(Vector3 from, Vector3 to)
        {
            if ( lerpActive == false || lerpTime <= 0.0f)
            {
                return to-from;
            }
            return (to - from)*(Time.deltaTime / (lerpTime * 0.66f));
        }
        private void OnTwoHandManipulationEnded() { }
        private void OnTwoHandManipulationStarted(State newState)
        {
            if ((newState & State.Rotating) > 0)
            {
                m_rotateLogic.Setup(handPositionMap);
            }
            if ((newState & State.Moving) > 0)
            {
                m_moveLogic.Setup(GetHandsCentroid(), hostTransform);
            }
            if ((newState & State.Scaling) > 0)
            {
                m_scaleLogic.Setup(handPositionMap, hostTransform);
            }
        }
        private void OnOneHandMoveStarted()
        {
            Assert.IsTrue(handPositionMap.Count == 1);
            m_moveLogic.Setup(handPositionMap.Values.First(), hostTransform);
        }
        private void OnManipulationStarted()
        {
            // TODO: If we are on Baraboo, push and pop modal input handler so that we can use old ggv manipulation
            // for Sydney, we don't want to do this
            // MixedRealityToolkit.InputSystem.PushModalInputHandler(gameObject);
        }
        private void OnManipulationEnded()
        {
            // TODO: If we are on Baraboo, push and pop modal input handler so that we can use old ggv manipulation
            // for Sydney, we don't want to do this
            // MixedRealityToolkit.InputSystem.PopModalInputHandler();
        }
        #endregion Private Event Handlers

        #region Unused Event Handlers
        public void OnInputPressed(InputEventData<float> eventData) {}
        public void OnBeforeFocusChange(FocusEventData eventData){}
        public void OnFocusChanged(FocusEventData eventData) {}
        public void OnPointerClicked(MixedRealityPointerEventData eventData){}
        #endregion Unused Event Handlers
    }
}
