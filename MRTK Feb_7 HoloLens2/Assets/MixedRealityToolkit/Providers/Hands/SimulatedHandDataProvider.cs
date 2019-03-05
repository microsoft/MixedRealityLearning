// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using UnityEngine;
using System;

/// <summary>
/// Provides per-frame data access to simulated hand data
/// 
/// Controls for mouse/keyboard simulation:
/// - Press spacebar to turn right hand on/off
/// - Left mouse button brings index and thumb together
/// - Mouse moves left and right hand.
/// </summary>
namespace Microsoft.MixedReality.Toolkit.Core.Devices.Hands
{
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T _Instance;
        public static T Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = FindObjectOfType<T>();
                }
                return _Instance;
            }
        }
    }

    public class SimulatedHandData
    {
        private static readonly int jointCount = Enum.GetNames(typeof(TrackedHandJoint)).Length;

        // Timestamp of hand data, as FileTime, e.g. DateTime.Now.ToFileTime() 
        public long Timestamp;
        public bool IsTracked;
        public Vector3[] Joints = new Vector3[jointCount];
        public bool IsPinching;
    }

    internal class SimulatedHandState
    {
        private Handedness handedness = Handedness.None;
        public Handedness Handedness => handedness;

        // Show a tracked hand device
        public bool IsVisible = false;
        // Hand is simulated
        public bool IsSimulated = false;
        // Activate the pinch gesture
        public bool IsPinching { get; private set; }

        private Vector3 screenPosition;
        // Rotation of the hand
        private Vector3 handRotateEulerAngles = Vector3.zero;
        // Random offset to simulate tracking inaccuracy
        private Vector3 jitterOffset = Vector3.zero;
        // Remaining time until the hand is hidden
        private float timeUntilHide = 0.0f;

        // Interpolation between current pose and target gesture
        private float lastGestureAnim = 0.0f;
        private float currentGestureAnim = 0.0f;
        private SimulatedHandPose.GestureId gesture = SimulatedHandPose.GestureId.None;
        public SimulatedHandPose.GestureId Gesture => gesture;

        private SimulatedHandPose pose = new SimulatedHandPose();

        public SimulatedHandState(Handedness _handedness)
        {
            handedness = _handedness;
        }

        public void Reset(float defaultHandDistance, SimulatedHandPose.GestureId defaultGesture)
        {
            // Start at current mouse position
            Vector3 mousePos = UnityEngine.Input.mousePosition;
            screenPosition = new Vector3(mousePos.x, mousePos.y, defaultHandDistance);

            gesture = defaultGesture;
            lastGestureAnim = 1.0f;
            currentGestureAnim = 1.0f;

            SimulatedHandPose gesturePose = SimulatedHandPose.GetGesturePose(gesture);
            if (gesturePose != null)
            {
                pose.Copy(gesturePose);
            }

            handRotateEulerAngles = Vector3.zero;
            jitterOffset = Vector3.zero;
        }

        // Update hand state
        // If hideTimeout value is null, hands will stay visible after tracking stops
        public void UpdateVisibility(float? hideTimeout)
        {
            if (hideTimeout.HasValue)
            {
                timeUntilHide = IsSimulated ? hideTimeout.Value : timeUntilHide - Time.deltaTime;
                IsVisible = (timeUntilHide > 0.0f);
            }
            else
            {
                IsVisible = true;
            }
        }

        public void SimulateInput(Vector3 mouseDelta, float noiseAmount, Vector3 rotationDeltaEulerAngles)
        {
            if (!IsSimulated)
            {
                return;
            }

            screenPosition += mouseDelta;
            handRotateEulerAngles += rotationDeltaEulerAngles;

            jitterOffset = UnityEngine.Random.insideUnitSphere * noiseAmount;
        }

        public void AnimateGesture(SimulatedHandPose.GestureId newGesture, float gestureAnimDelta)
        {
            if (!IsSimulated)
            {
                return;
            }

            if (newGesture != SimulatedHandPose.GestureId.None && newGesture != gesture)
            {
                gesture = newGesture;
                lastGestureAnim = 0.0f;
                currentGestureAnim = Mathf.Clamp01(gestureAnimDelta);
            }
            else
            {
                lastGestureAnim = currentGestureAnim;
                currentGestureAnim = Mathf.Clamp01(currentGestureAnim + gestureAnimDelta);
            }

            SimulatedHandPose gesturePose = SimulatedHandPose.GetGesturePose(gesture);
            if (gesturePose != null)
            {
                pose.TransitionTo(gesturePose, lastGestureAnim, currentGestureAnim);
            }

            // Pinch is a special gesture that triggers the Select and TriggerPress input actions
            IsPinching = (gesture == SimulatedHandPose.GestureId.Pinch && currentGestureAnim > 0.9f);
        }

        internal void FillCurrentFrame(Vector3[] jointsOut)
        {
            Quaternion rotation = Quaternion.Euler(handRotateEulerAngles);
            Vector3 position = Camera.main.ScreenToWorldPoint(screenPosition + jitterOffset);
            pose.ComputeJointPositions(handedness, rotation, position, jointsOut);
        }
    }

    public class SimulatedHandDataProvider : Singleton<SimulatedHandDataProvider>
    {
        private static readonly int jointCount = Enum.GetNames(typeof(TrackedHandJoint)).Length;

        /// <summary>
        /// This event is raised whenever the hand data changes.
        /// Hand data changes at 45 fps.
        /// </summary>
        public event Action OnHandDataChanged = delegate { };

        [Header("Hand Control Settings")]
        [Tooltip("Persistent mode keeps hands visible for two-hand manipulation")]
        public bool UsePersistentMode = false;
        [Tooltip("Key to toggle persistent mode")]
        public KeyCode TogglePersistentModeKey = KeyCode.T;
        [Tooltip("Time after which uncontrolled hands are hidden")]
        public float HideTimeout = 0.2f;
        [Tooltip("Key to manipulate the left hand")]
        public KeyCode LeftHandManipulationKey = KeyCode.LeftShift;
        [Tooltip("Key to manipulate the right hand")]
        public KeyCode RightHandManipulationKey = KeyCode.Space;

        [Header("Gesture Settings")]
        public SimulatedHandPose.GestureId DefaultGesture = SimulatedHandPose.GestureId.Open;
        public SimulatedHandPose.GestureId LeftMouseGesture = SimulatedHandPose.GestureId.Pinch;
        public SimulatedHandPose.GestureId MiddleMouseGesture = SimulatedHandPose.GestureId.None;
        public SimulatedHandPose.GestureId RightMouseGesture = SimulatedHandPose.GestureId.None;
        [Tooltip("Gesture interpolation per second")]
        public float GestureAnimationSpeed = 8.0f;

        [Header("Hand Placement Settings")]
        [Tooltip("Default distance of the hand from the camera")]
        public float DefaultHandDistance = 0.5f;
        [Tooltip("Depth change when scrolling the mouse wheel")]
        public float ScrollDepthMultiplier = 0.1f;
        [Tooltip("Apply random offset to the hand position")]
        public float NoiseAmountForSimulation;

        [Header("Hand Rotation Settings")]
        [Tooltip("Key to yaw the hand clockwise")]
        public KeyCode YawCWKey = KeyCode.E;
        [Tooltip("Key to yaw the hand counter-clockwise")]
        public KeyCode YawCCWKey = KeyCode.Q;
        [Tooltip("Key to pitch the hand upward")]
        public KeyCode PitchCWKey = KeyCode.F;
        [Tooltip("Key to pitch the hand downward")]
        public KeyCode PitchCCWKey = KeyCode.R;
        [Tooltip("Key to roll the hand right")]
        public KeyCode RollCWKey = KeyCode.X;
        [Tooltip("Key to roll the hand left")]
        public KeyCode RollCCWKey = KeyCode.Z;
        [Tooltip("Angle per second when rotating the hand")]
        public float RotationSpeed = 100.0f;

        public SimulatedHandData CurrentFrameLeft = new SimulatedHandData();
        public SimulatedHandData CurrentFrameRight = new SimulatedHandData();

        private SimulatedHandState stateLeft;
        private SimulatedHandState stateRight;
        // Last frame's mouse position for computing delta
        private Vector3? lastMousePosition = null;

        public void Start()
        {
            // Update the hand data in OnBeforeRender instead of update to get the data as close as possible to when we will render a frame
            Application.onBeforeRender += Application_onBeforeRender;

            stateLeft = new SimulatedHandState(Handedness.Left);
            stateRight = new SimulatedHandState(Handedness.Right);
        }

        private void Application_onBeforeRender()
        {
            bool handDataChanged = false;
            handDataChanged |= UpdateHandDataFromState(CurrentFrameLeft, stateLeft);
            handDataChanged |= UpdateHandDataFromState(CurrentFrameRight, stateRight);

            if (handDataChanged)
            {
                OnHandDataChanged();
            }
        }

        private void Update()
        {
            bool wasLeftVisible = stateLeft.IsVisible;
            bool wasRightVisible = stateRight.IsVisible;

            if (Input.GetKeyDown(TogglePersistentModeKey))
            {
                UsePersistentMode = !UsePersistentMode;
            }

            if (Input.GetKeyDown(LeftHandManipulationKey))
            {
                stateLeft.IsSimulated = true;
            }
            if (Input.GetKeyUp(LeftHandManipulationKey))
            {
                stateLeft.IsSimulated = false;
            }

            if (Input.GetKeyDown(RightHandManipulationKey))
            {
                stateRight.IsSimulated = true;
            }
            if (Input.GetKeyUp(RightHandManipulationKey))
            {
                stateRight.IsSimulated = false;
            }

            // Hide cursor if either of the hands is simulated
            Cursor.visible = !stateLeft.IsSimulated && !stateRight.IsSimulated;

            float? hideTimeout = UsePersistentMode ? null : (float?)HideTimeout;
            stateLeft.UpdateVisibility(hideTimeout);
            stateRight.UpdateVisibility(hideTimeout);
            // Reset when enabling
            if (!wasLeftVisible && stateLeft.IsVisible)
            {
                stateLeft.Reset(DefaultHandDistance, DefaultGesture);
            }
            if (!wasRightVisible && stateRight.IsVisible)
            {
                stateRight.Reset(DefaultHandDistance, DefaultGesture);
            }

            Vector3 mouseDelta = (lastMousePosition.HasValue ? UnityEngine.Input.mousePosition - lastMousePosition.Value : Vector3.zero);
            mouseDelta.z += UnityEngine.Input.GetAxis("Mouse ScrollWheel") * ScrollDepthMultiplier;
            float rotationDelta = RotationSpeed * Time.deltaTime;
            Vector3 rotationDeltaEulerAngles = Vector3.zero;
            if (Input.GetKey(YawCCWKey))
            {
                rotationDeltaEulerAngles.y = -rotationDelta;
            }
            if (Input.GetKey(YawCWKey))
            {
                rotationDeltaEulerAngles.y = rotationDelta;
            }
            if (Input.GetKey(PitchCCWKey))
            {
                rotationDeltaEulerAngles.x = rotationDelta;
            }
            if (Input.GetKey(PitchCWKey))
            {
                rotationDeltaEulerAngles.x = -rotationDelta;
            }
            if (Input.GetKey(RollCCWKey))
            {
                rotationDeltaEulerAngles.z = rotationDelta;
            }
            if (Input.GetKey(RollCWKey))
            {
                rotationDeltaEulerAngles.z = -rotationDelta;
            }
            stateLeft.SimulateInput(mouseDelta, NoiseAmountForSimulation, rotationDeltaEulerAngles);
            stateRight.SimulateInput(mouseDelta, NoiseAmountForSimulation, rotationDeltaEulerAngles);

            float gestureAnimDelta = GestureAnimationSpeed * Time.deltaTime;
            if (UsePersistentMode)
            {
                // Toggle gestures on/off
                stateLeft.AnimateGesture(ToggleGesture(stateLeft), gestureAnimDelta);
                stateRight.AnimateGesture(ToggleGesture(stateRight), gestureAnimDelta);
            }
            else
            {
                // Enable gesture while mouse button is pressed
                stateLeft.AnimateGesture(SelectGesture(), gestureAnimDelta);
                stateRight.AnimateGesture(SelectGesture(), gestureAnimDelta);
            }

            lastMousePosition = UnityEngine.Input.mousePosition;
        }

        private SimulatedHandPose.GestureId SelectGesture()
        {
            if (Input.GetMouseButton(0))
            {
                return LeftMouseGesture;
            }
            else if (Input.GetMouseButton(1))
            {
                return RightMouseGesture;
            }
            else if (Input.GetMouseButton(2))
            {
                return MiddleMouseGesture;
            }
            else
            {
                return DefaultGesture;
            }
        }

        private SimulatedHandPose.GestureId ToggleGesture(SimulatedHandState state)
        {
            if (Input.GetMouseButtonDown(0))
            {
                return (state.Gesture != LeftMouseGesture ? LeftMouseGesture : DefaultGesture);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                return (state.Gesture != RightMouseGesture ? RightMouseGesture : DefaultGesture);
            }
            else if (Input.GetMouseButtonDown(2))
            {
                return (state.Gesture != MiddleMouseGesture ? MiddleMouseGesture : DefaultGesture);
            }
            else
            {
                // 'None' will not change the gesture
                return SimulatedHandPose.GestureId.None;
            }
        }

        private bool UpdateHandDataFromState(SimulatedHandData frame, SimulatedHandState state)
        {
            bool handDataChanged = false;
            bool wasTracked = frame.IsTracked;
            bool wasPinching = frame.IsPinching;

            frame.IsTracked = state.IsVisible;
            frame.IsPinching = state.IsPinching;
            if (wasTracked != frame.IsTracked || wasPinching != frame.IsPinching)
            {
                handDataChanged = true;
            }

            if (frame.IsTracked)
            {
                var prevTime = frame.Timestamp;
                frame.Timestamp = DateTime.Now.Ticks;
                if (frame.Timestamp != prevTime)
                {
                    state.FillCurrentFrame(frame.Joints);
                    handDataChanged = true;
                }
            }
            else
            {
                // If frame is not tracked, set timestamp to zero
                frame.Timestamp = 0;
            }

            return handDataChanged;
        }
    }
}