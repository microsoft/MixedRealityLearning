// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Attributes;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Core.Definitions.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Providers;
using Microsoft.MixedReality.Toolkit.Core.Services;

#if UNITY_WSA
using UnityEngine;
using UnityEngine.XR.WSA.Input;
#endif

#if WINDOWS_UWP
using Microsoft.MixedReality.Toolkit.Core.Devices.Hands;
using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers;
using Microsoft.MixedReality.Toolkit.Core.Utilities;
using System;
using System.Collections.Generic;
using Windows.Perception;
using Windows.Perception.People;
using Windows.Perception.Spatial;
using Windows.UI.Input.Spatial;
#endif

namespace Microsoft.MixedReality.Toolkit.Providers.WindowsMixedReality
{
    /// <summary>
    /// A Windows Mixed Reality Controller Instance.
    /// </summary>
    [MixedRealityController(
        SupportedControllerType.WindowsMixedReality,
        new[] { Handedness.Left, Handedness.Right, Handedness.None },
        "Resources/Textures/MotionController")]
    public class WindowsMixedRealityController : BaseController
    {
#if WINDOWS_UWP
        private readonly HandRay handRay = new HandRay();
#endif // WINDOWS_UWP

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="trackingState"></param>
        /// <param name="controllerHandedness"></param>
        /// <param name="inputSource"></param>
        /// <param name="interactions"></param>
        public WindowsMixedRealityController(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
                : base(trackingState, controllerHandedness, inputSource, interactions)
        {
#if WINDOWS_UWP
            UnityEngine.WSA.Application.InvokeOnUIThread(() =>
            {
                spatialInteractionManager = SpatialInteractionManager.GetForCurrentView();

            }, true);
#endif // WINDOWS_UWP
        }

        /// <summary>
        /// The Windows Mixed Reality Controller default interactions.
        /// </summary>
        /// <remarks>A single interaction mapping works for both left and right controllers.</remarks>
        public override MixedRealityInteractionMapping[] DefaultInteractions => new[]
        {
            new MixedRealityInteractionMapping(0, "Spatial Pointer", AxisType.SixDof, DeviceInputType.SpatialPointer, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(1, "Spatial Grip", AxisType.SixDof, DeviceInputType.SpatialGrip, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(2, "Grip Press", AxisType.SingleAxis, DeviceInputType.TriggerPress, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(3, "Trigger Position", AxisType.SingleAxis, DeviceInputType.Trigger, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(4, "Trigger Touch", AxisType.Digital, DeviceInputType.TriggerTouch, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(5, "Trigger Press (Select)", AxisType.Digital, DeviceInputType.Select, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(6, "Touchpad Position", AxisType.DualAxis, DeviceInputType.Touchpad, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(7, "Touchpad Touch", AxisType.Digital, DeviceInputType.TouchpadTouch, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(8, "Touchpad Press", AxisType.Digital, DeviceInputType.TouchpadPress, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(9, "Menu Press", AxisType.Digital, DeviceInputType.Menu, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(10, "Thumbstick Position", AxisType.DualAxis, DeviceInputType.ThumbStick, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(11, "Thumbstick Press", AxisType.Digital, DeviceInputType.ThumbStickPress, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(12, "Index Finger Pose", AxisType.SixDof, DeviceInputType.IndexFinger, MixedRealityInputAction.None),
        };

        public override bool IsInPointingPose
        {
            get
            {
#if WINDOWS_UWP
                if (LastSourceStateReading.source.kind == InteractionSourceKind.Hand)
                {
                    return handRay.ShouldShowRay;
                }
#endif
                return true;
            }
        }

        /// <inheritdoc />
        public override MixedRealityInteractionMapping[] DefaultLeftHandedInteractions => DefaultInteractions;

        /// <inheritdoc />
        public override MixedRealityInteractionMapping[] DefaultRightHandedInteractions => DefaultInteractions;

        /// <inheritdoc />
        public override void SetupDefaultInteractions(Handedness controllerHandedness)
        {
            AssignControllerMappings(DefaultInteractions);
        }
#if UNITY_WSA

        /// <summary>
        /// The last updated source state reading for this Windows Mixed Reality Controller.
        /// </summary>
        public InteractionSourceState LastSourceStateReading { get; private set; }

        private Vector3 currentControllerPosition = Vector3.zero;
        private Quaternion currentControllerRotation = Quaternion.identity;
        private MixedRealityPose lastControllerPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentControllerPose = MixedRealityPose.ZeroIdentity;

        private Vector3 currentPointerPosition = Vector3.zero;
        private Quaternion currentPointerRotation = Quaternion.identity;
        private MixedRealityPose currentPointerPose = MixedRealityPose.ZeroIdentity;

        private Vector3 currentGripPosition = Vector3.zero;
        private Quaternion currentGripRotation = Quaternion.identity;
        private MixedRealityPose currentGripPose = MixedRealityPose.ZeroIdentity;

        private MixedRealityPose currentIndexPose = MixedRealityPose.ZeroIdentity;

#if WINDOWS_UWP
        private SpatialInteractionManager spatialInteractionManager = null;
        private HandMeshObserver handMeshObserver = null;
        private ushort[] handMeshTriangleIndices = null;
        private bool hasRequestedHandMeshObserver = false;
        private Vector2[] handMeshUVs;
#endif // WINDOWS_UWP

        #region Update data functions

        /// <summary>
        /// Update the controller data from the provided platform state
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        public void UpdateController(InteractionSourceState interactionSourceState)
        {
            if (!Enabled) { return; }

            UpdateControllerData(interactionSourceState);

            if (Interactions == null)
            {
                Debug.LogError($"No interaction configuration for Windows Mixed Reality Motion Controller {ControllerHandedness}");
                Enabled = false;
            }

            for (int i = 0; i < Interactions?.Length; i++)
            {
                switch (Interactions[i].InputType)
                {
                    case DeviceInputType.None:
                        break;
                    case DeviceInputType.SpatialPointer:
                        UpdatePointerData(interactionSourceState, Interactions[i]);
                        break;
                    case DeviceInputType.Select:
                    case DeviceInputType.Trigger:
                    case DeviceInputType.TriggerTouch:
                    case DeviceInputType.TriggerPress:
                        UpdateTriggerData(interactionSourceState, Interactions[i]);
                        break;
                    case DeviceInputType.SpatialGrip:
                        UpdateGripData(interactionSourceState, Interactions[i]);
                        break;
                    case DeviceInputType.ThumbStick:
                    case DeviceInputType.ThumbStickPress:
                        UpdateThumbStickData(interactionSourceState, Interactions[i]);
                        break;
                    case DeviceInputType.Touchpad:
                    case DeviceInputType.TouchpadTouch:
                    case DeviceInputType.TouchpadPress:
                        UpdateTouchPadData(interactionSourceState, Interactions[i]);
                        break;
                    case DeviceInputType.Menu:
                        UpdateMenuData(interactionSourceState, Interactions[i]);
                        break;
                    case DeviceInputType.IndexFinger:
                        UpdateIndexFingerData(interactionSourceState, Interactions[i]);
                        break;
                    default:
                        Debug.LogError($"Input [{Interactions[i].InputType}] is not handled for this controller [WindowsMixedRealityController]");
                        Enabled = false;
                        break;
                }
            }

#if WINDOWS_UWP
            TestForTouching();
#endif // WINDOWS_UWP

            LastSourceStateReading = interactionSourceState;
        }

#if WINDOWS_UWP
        protected void InitializeUVs(Vector3[] neutralPoseVertices)
		{
			if (neutralPoseVertices.Length == 0)
			{
				Debug.LogError("Loaded 0 verts for neutralPoseVertices");
			}

			float minY = neutralPoseVertices[0].y;
			float maxY = minY;

			float maxMagnitude = 0.0f;

			for (int ix = 1; ix < neutralPoseVertices.Length; ix++)
			{
				Vector3 p = neutralPoseVertices[ix];

				if (p.y < minY)
				{
					minY = p.y;
				}
				else if (p.y > maxY)
				{
					maxY = p.y;
				}
				float d = p.x * p.x + p.y * p.y;
				if (d > maxMagnitude) maxMagnitude = d;
			}

			maxMagnitude = Mathf.Sqrt(maxMagnitude);
			float scale = 1.0f / (maxY - minY);

			handMeshUVs = new Vector2[neutralPoseVertices.Length];

			for (int ix = 0; ix < neutralPoseVertices.Length; ix++)
			{
				Vector3 p = neutralPoseVertices[ix];

				handMeshUVs[ix] = new Vector2(p.x * scale + 0.5f, (p.y - minY) * scale);
			}
		}

        private async void SetHandMeshObserver(SpatialInteractionSourceState sourceState)
        {
            this.handMeshObserver = await sourceState.Source.TryCreateHandMeshObserverAsync();
        }
#endif

        /// <summary>
        /// Update the "Controller" input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        private void UpdateControllerData(InteractionSourceState interactionSourceState)
        {
            var lastState = TrackingState;
            var sourceKind = interactionSourceState.source.kind;

            lastControllerPose = currentControllerPose;

            if (sourceKind == InteractionSourceKind.Controller && interactionSourceState.source.supportsPointing)
            {
                // The source is a controller that supports pointing.
                // We can now check for position and rotation.
                IsPositionAvailable = interactionSourceState.sourcePose.TryGetPosition(out currentControllerPosition);

                if (IsPositionAvailable)
                {
                    IsPositionApproximate = (interactionSourceState.sourcePose.positionAccuracy == InteractionSourcePositionAccuracy.Approximate);
                }
                else
                {
                    IsPositionApproximate = false;
                }

                IsRotationAvailable = interactionSourceState.sourcePose.TryGetRotation(out currentControllerRotation);

                // Devices are considered tracked if we receive position OR rotation data from the sensors.
                TrackingState = (IsPositionAvailable || IsRotationAvailable) ? TrackingState.Tracked : TrackingState.NotTracked;
            }
            else if (sourceKind == InteractionSourceKind.Hand && interactionSourceState.source.handedness != InteractionSourceHandedness.Unknown)
            {
#if WINDOWS_UWP
                PerceptionTimestamp perceptionTimestamp = PerceptionTimestampHelper.FromHistoricalTargetTime(DateTimeOffset.Now);
                IsPositionAvailable = false;
                IsRotationAvailable = false;
                IReadOnlyList<SpatialInteractionSourceState> sources = spatialInteractionManager?.GetDetectedSourcesAtTimestamp(perceptionTimestamp);
                foreach (SpatialInteractionSourceState sourceState in sources)
                {
                    if (sourceState.Source.Id.Equals(interactionSourceState.source.id))
                    {
                        HandPose handPose = sourceState.TryGetHandPose();
                        if (MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.HandTrackingProfile.EnableHandMeshUpdates)
                        {
                            // Accessing the hand mesh data involves copying quite a bit of data, so only do it if application requests it.
                            if (handMeshObserver == null && !hasRequestedHandMeshObserver)
                            {
                                SetHandMeshObserver(sourceState);
                                hasRequestedHandMeshObserver = true;
                            }

                            if (handMeshObserver != null && this.handMeshTriangleIndices == null)
                            {
                                uint indexCount = handMeshObserver.TriangleIndexCount;
                                ushort[] indices = new ushort[indexCount];
                                handMeshObserver.GetTriangleIndices(indices);
                                handMeshTriangleIndices = indices;
                                
                                // Compute neutral pose
                                Vector3[] neutralPoseVertices = new Vector3[handMeshObserver.VertexCount];
                                HandPose neutralPose = handMeshObserver.NeutralPose;
                                var vertexAndNormals = new HandMeshVertex[handMeshObserver.VertexCount];
                                var handMeshVertexState = handMeshObserver.GetVertexStateForPose(neutralPose);
                                handMeshVertexState.GetVertices(vertexAndNormals);
                                for(int i = 0; i < handMeshObserver.VertexCount; i++)
                                {
                                    neutralPoseVertices[i] = WindowsMixedRealityUtilities.SystemVector3ToUnity(vertexAndNormals[i].Position);
                                }

                                // Compute UV mapping
                                InitializeUVs(neutralPoseVertices);
                            }

                            if (handPose != null && handMeshObserver != null && handMeshTriangleIndices != null)
                            {
                                var vertexAndNormals = new HandMeshVertex[handMeshObserver.VertexCount];
                                var handMeshVertexState = handMeshObserver.GetVertexStateForPose(handPose);
                                handMeshVertexState.GetVertices(vertexAndNormals);

                                var meshTransform = handMeshVertexState.CoordinateSystem.TryGetTransformTo(WindowsMixedRealityUtilities.SpatialCoordinateSystem);
                                if(meshTransform.HasValue)
                                {
                                    System.Numerics.Vector3 scale;
                                    System.Numerics.Quaternion rotation;
                                    System.Numerics.Vector3 translation;
                                    System.Numerics.Matrix4x4.Decompose(meshTransform.Value, out scale, out rotation, out translation);
                                    var handMeshVertices = new Vector3[handMeshObserver.VertexCount];
                                    var handMeshNormals = new Vector3[handMeshObserver.VertexCount];
                                    for(int i = 0; i < handMeshObserver.VertexCount; i++)
                                    {
                                        handMeshVertices[i] = WindowsMixedRealityUtilities.SystemVector3ToUnity(vertexAndNormals[i].Position);
                                        handMeshNormals[i] = WindowsMixedRealityUtilities.SystemVector3ToUnity(vertexAndNormals[i].Normal);
                                    }
                                    HandMeshInfo handMeshInfo = new HandMeshInfo();
                                    handMeshInfo.vertices = handMeshVertices;
                                    handMeshInfo.normals = handMeshNormals;
                                    handMeshInfo.triangles = this.handMeshTriangleIndices;
                                    handMeshInfo.uvs = handMeshUVs;
                                    handMeshInfo.position = WindowsMixedRealityUtilities.SystemVector3ToUnity(translation);
                                    handMeshInfo.rotation = WindowsMixedRealityUtilities.SystemQuaternionToUnity(rotation);
                                    MixedRealityToolkit.InputSystem?.RaiseHandMeshUpdated(InputSource, ControllerHandedness, handMeshInfo);
                                }
                            }
                        }

                        if (handPose != null && handPose.TryGetJoints(WindowsMixedRealityUtilities.SpatialCoordinateSystem, jointIndices, jointPoses))
                        {
                            for (int i = 0; i < jointPoses.Length; i++)
                            {
                                unityJointOrientations[i] = WindowsMixedRealityUtilities.SystemQuaternionToUnity(jointPoses[i].Orientation);
                                unityJointPositions[i] = WindowsMixedRealityUtilities.SystemVector3ToUnity(jointPoses[i].Position);

                                // We want the controller to follow the Playspace, so fold in the playspace transform here to 
                                // put the controller pose into world space.
                                var playspace = MixedRealityToolkit.Instance.MixedRealityPlayspace;
                                if (playspace != null)
                                {
                                    unityJointPositions[i] = playspace.TransformPoint(unityJointPositions[i]);
                                    unityJointOrientations[i] = playspace.rotation * unityJointOrientations[i];
                                }

                                if (jointIndices[i] == HandJointKind.IndexTip)
                                {
                                    lastIndexTipRadius = jointPoses[i].Radius;
                                }

                                TrackedHandJoint handJoint = ConvertHandJointKindToTrackedHandJoint(jointIndices[i]);

                                if (!unityJointPoses.ContainsKey(handJoint))
                                {
                                    unityJointPoses.Add(handJoint, new MixedRealityPose(unityJointPositions[i], unityJointOrientations[i]));
                                }
                                else
                                {
                                    unityJointPoses[handJoint] = new MixedRealityPose(unityJointPositions[i], unityJointOrientations[i]);
                                }
                            }
                            MixedRealityToolkit.InputSystem?.RaiseHandJointsUpdated(InputSource, ControllerHandedness, unityJointPoses);
                        }
                        // MSFT: 19996765 use TryGetSpatialPointerPose to get position and rotation of hand instead of
                        // using the raw data
                        IsPositionAvailable = true;
                        IsPositionApproximate = false;
                        currentControllerPosition = unityJointPositions[(int)HandJointKind.Palm];
                        IsRotationAvailable = true;
                        currentControllerRotation = unityJointOrientations[(int)HandJointKind.Palm];
                        break;
                    }
                    TrackingState = (IsPositionAvailable || IsRotationAvailable) ? TrackingState.Tracked : TrackingState.NotTracked;
                }
#endif // WINDOWS_UWP
            }
            else
            {
                // The input source does not support tracking.
                TrackingState = TrackingState.NotApplicable;
            }

            currentControllerPose.Position = currentControllerPosition;
            currentControllerPose.Rotation = currentControllerRotation;

            // Raise input system events if it is enabled.
            if (lastState != TrackingState)
            {
                MixedRealityToolkit.InputSystem?.RaiseSourceTrackingStateChanged(InputSource, this, TrackingState);
            }

            if (TrackingState == TrackingState.Tracked && lastControllerPose != currentControllerPose)
            {
                if (IsPositionAvailable && IsRotationAvailable)
                {
                    MixedRealityToolkit.InputSystem?.RaiseSourcePoseChanged(InputSource, this, currentControllerPose);
                }
                else if (IsPositionAvailable && !IsRotationAvailable)
                {
                    MixedRealityToolkit.InputSystem?.RaiseSourcePositionChanged(InputSource, this, currentControllerPosition);
                }
                else if (!IsPositionAvailable && IsRotationAvailable)
                {
                    MixedRealityToolkit.InputSystem?.RaiseSourceRotationChanged(InputSource, this, currentControllerRotation);
                }
            }
        }

        /// <summary>
        /// Update the "Spatial Pointer" input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        /// <param name="interactionMapping"></param>
        private void UpdatePointerData(InteractionSourceState interactionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            if (interactionSourceState.source.kind == InteractionSourceKind.Controller)
            {
                interactionSourceState.sourcePose.TryGetPosition(out currentPointerPosition, InteractionSourceNode.Pointer);
                interactionSourceState.sourcePose.TryGetRotation(out currentPointerRotation, InteractionSourceNode.Pointer);

                // We want the controller to follow the Playspace, so fold in the playspace transform here to 
                // put the controller pose into world space.
                var playspace = MixedRealityToolkit.Instance.MixedRealityPlayspace;
                if (playspace != null)
                {
                    currentPointerPose.Position = playspace.TransformPoint(currentPointerPosition);
                    currentPointerPose.Rotation = playspace.rotation * currentPointerRotation;
                }
                else
                {
                    currentPointerPose.Position = currentPointerPosition;
                    currentPointerPose.Rotation = currentPointerRotation;
                }
            }
            else if (interactionSourceState.source.kind == InteractionSourceKind.Hand)
            {
#if WINDOWS_UWP
                Vector3 handPosition = unityJointPositions[(int)HandJointKind.Palm];
                Vector3 palmNormal = currentControllerRotation * (-1 * Vector3.up);
                if (handPosition != Vector3.zero)
                {
                    handRay.Update(handPosition, palmNormal, CameraCache.Main.transform, ControllerHandedness);

                    Ray ray = handRay.Ray;

                    currentPointerPose.Position = ray.origin;
                    currentPointerPose.Rotation = Quaternion.LookRotation(ray.direction);
                }
#endif // WINDOWS_UWP
            }

            // Update the interaction data source
            interactionMapping.PoseData = currentPointerPose;

            // If our value changed raise it.
            if (interactionMapping.Changed)
            {
                // Raise input system Event if it enabled
                MixedRealityToolkit.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, currentPointerPose);
            }
        }

        /// <summary>
        /// Update the "Spatial Grip" input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        /// <param name="interactionMapping"></param>
        private void UpdateGripData(InteractionSourceState interactionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.AxisType)
            {
                case AxisType.SixDof:
                    {
                        interactionSourceState.sourcePose.TryGetPosition(out currentGripPosition, InteractionSourceNode.Grip);
                        interactionSourceState.sourcePose.TryGetRotation(out currentGripRotation, InteractionSourceNode.Grip);

                        if (MixedRealityToolkit.Instance.MixedRealityPlayspace != null)
                        {
                            currentGripPose.Position = MixedRealityToolkit.Instance.MixedRealityPlayspace.TransformPoint(currentGripPosition);
                            currentGripPose.Rotation = Quaternion.Euler(MixedRealityToolkit.Instance.MixedRealityPlayspace.TransformDirection(currentGripRotation.eulerAngles));
                        }
                        else
                        {
                            currentGripPose.Position = currentGripPosition;
                            currentGripPose.Rotation = currentGripRotation;
                        }

                        // Update the interaction data source
                        interactionMapping.PoseData = currentGripPose;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            MixedRealityToolkit.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, currentGripPose);
                        }
                    }
                    break;
            }
        }

        private void UpdateIndexFingerData(InteractionSourceState interactionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            if (interactionSourceState.source.kind == InteractionSourceKind.Hand)
            {
#if WINDOWS_UWP
                UpdateCurrentIndexPose();

                // Update the interaction data source
                interactionMapping.PoseData = currentIndexPose;

                // If our value changed raise it.
                if (interactionMapping.Changed)
                {
                    // Raise input system Event if it enabled
                    MixedRealityToolkit.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, currentIndexPose);
                }
#endif // WINDOWS_UWP
            }
        }

        /// <summary>
        /// Update the Touchpad input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        /// <param name="interactionMapping"></param>
        private void UpdateTouchPadData(InteractionSourceState interactionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.InputType)
            {
                case DeviceInputType.TouchpadTouch:
                    {
                        // Update the interaction data source
                        interactionMapping.BoolData = interactionSourceState.touchpadTouched;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            if (interactionSourceState.touchpadTouched)
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                            else
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                        }
                        break;
                    }
                case DeviceInputType.TouchpadPress:
                    {
                        //Update the interaction data source
                        interactionMapping.BoolData = interactionSourceState.touchpadPressed;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            if (interactionSourceState.touchpadPressed)
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                            else
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                        }
                        break;
                    }
                case DeviceInputType.Touchpad:
                    {
                        // Update the interaction data source
                        interactionMapping.Vector2Data = interactionSourceState.touchpadPosition;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            MixedRealityToolkit.InputSystem?.RaisePositionInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, interactionSourceState.touchpadPosition);
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Update the Thumbstick input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        /// <param name="interactionMapping"></param>
        private void UpdateThumbStickData(InteractionSourceState interactionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.InputType)
            {
                case DeviceInputType.ThumbStickPress:
                    {
                        // Update the interaction data source
                        interactionMapping.BoolData = interactionSourceState.thumbstickPressed;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            if (interactionSourceState.thumbstickPressed)
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                            else
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                        }
                        break;
                    }
                case DeviceInputType.ThumbStick:
                    {
                        // Update the interaction data source
                        interactionMapping.Vector2Data = interactionSourceState.thumbstickPosition;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            MixedRealityToolkit.InputSystem?.RaisePositionInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, interactionSourceState.thumbstickPosition);
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Update the Trigger input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        /// <param name="interactionMapping"></param>
        private void UpdateTriggerData(InteractionSourceState interactionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.InputType)
            {
                case DeviceInputType.TriggerPress:
                    interactionMapping.BoolData = interactionSourceState.grasped;

                    // If our value changed raise it.
                    if (interactionMapping.Changed)
                    {
                        // Raise input system Event if it enabled
                        if (interactionMapping.BoolData)
                        {
                            MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                        }
                        else
                        {
                            MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                        }
                    }
                    break;
                case DeviceInputType.Select:
                    {
                        // Update the interaction data source
                        interactionMapping.BoolData = interactionSourceState.selectPressed;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            if (interactionSourceState.selectPressed)
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                            else
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                        }
                        break;
                    }
                case DeviceInputType.Trigger:
                    {
                        // Update the interaction data source
                        interactionMapping.FloatData = interactionSourceState.selectPressedAmount;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            MixedRealityToolkit.InputSystem?.RaiseOnInputPressed(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, interactionSourceState.selectPressedAmount);
                        }
                        break;
                    }
                case DeviceInputType.TriggerTouch:
                    {
                        // Update the interaction data source
                        interactionMapping.BoolData = interactionSourceState.selectPressedAmount > 0;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            if (interactionSourceState.selectPressedAmount > 0)
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                            else
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Update the Menu button state.
        /// </summary>
        /// <param name="interactionSourceState"></param>
        /// <param name="interactionMapping"></param>
        private void UpdateMenuData(InteractionSourceState interactionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            //Update the interaction data source
            interactionMapping.BoolData = interactionSourceState.menuPressed;

            // If our value changed raise it.
            if (interactionMapping.Changed)
            {
                // Raise input system Event if it enabled
                if (interactionSourceState.menuPressed)
                {
                    MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                }
                else
                {
                    MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                }
            }
        }

        #endregion Update data functions

#if WINDOWS_UWP
        private static readonly HandJointKind[] jointIndices = new HandJointKind[]
        {
            HandJointKind.Palm,
            HandJointKind.Wrist,
            HandJointKind.ThumbMetacarpal,
            HandJointKind.ThumbProximal,
            HandJointKind.ThumbDistal,
            HandJointKind.ThumbTip,
            HandJointKind.IndexMetacarpal,
            HandJointKind.IndexProximal,
            HandJointKind.IndexIntermediate,
            HandJointKind.IndexDistal,
            HandJointKind.IndexTip,
            HandJointKind.MiddleMetacarpal,
            HandJointKind.MiddleProximal,
            HandJointKind.MiddleIntermediate,
            HandJointKind.MiddleDistal,
            HandJointKind.MiddleTip,
            HandJointKind.RingMetacarpal,
            HandJointKind.RingProximal,
            HandJointKind.RingIntermediate,
            HandJointKind.RingDistal,
            HandJointKind.RingTip,
            HandJointKind.LittleMetacarpal,
            HandJointKind.LittleProximal,
            HandJointKind.LittleIntermediate,
            HandJointKind.LittleDistal,
            HandJointKind.LittleTip
        };

        private readonly JointPose[] jointPoses = new JointPose[jointIndices.Length];
        private readonly Vector3[] unityJointPositions = new Vector3[jointIndices.Length];
        private readonly Quaternion[] unityJointOrientations = new Quaternion[jointIndices.Length];
        private readonly Dictionary<TrackedHandJoint, MixedRealityPose> unityJointPoses = new Dictionary<TrackedHandJoint, MixedRealityPose>();
        private float lastIndexTipRadius = 0;

        private TrackedHandJoint ConvertHandJointKindToTrackedHandJoint(HandJointKind handJointKind)
        {
            switch (handJointKind)
            {
                case HandJointKind.Palm: return TrackedHandJoint.Palm;

                case HandJointKind.Wrist: return TrackedHandJoint.Wrist;

                case HandJointKind.ThumbMetacarpal: return TrackedHandJoint.ThumbMetacarpalJoint;
                case HandJointKind.ThumbProximal: return TrackedHandJoint.ThumbProximalJoint;
                case HandJointKind.ThumbDistal: return TrackedHandJoint.ThumbDistalJoint;
                case HandJointKind.ThumbTip: return TrackedHandJoint.ThumbTip;

                case HandJointKind.IndexMetacarpal: return TrackedHandJoint.IndexMetacarpal;
                case HandJointKind.IndexProximal: return TrackedHandJoint.IndexKnuckle;
                case HandJointKind.IndexIntermediate: return TrackedHandJoint.IndexMiddleJoint;
                case HandJointKind.IndexDistal: return TrackedHandJoint.IndexDistalJoint;
                case HandJointKind.IndexTip: return TrackedHandJoint.IndexTip;

                case HandJointKind.MiddleMetacarpal: return TrackedHandJoint.MiddleMetacarpal;
                case HandJointKind.MiddleProximal: return TrackedHandJoint.MiddleKnuckle;
                case HandJointKind.MiddleIntermediate: return TrackedHandJoint.MiddleMiddleJoint;
                case HandJointKind.MiddleDistal: return TrackedHandJoint.MiddleDistalJoint;
                case HandJointKind.MiddleTip: return TrackedHandJoint.MiddleTip;

                case HandJointKind.RingMetacarpal: return TrackedHandJoint.RingMetacarpal;
                case HandJointKind.RingProximal: return TrackedHandJoint.RingKnuckle;
                case HandJointKind.RingIntermediate: return TrackedHandJoint.RingMiddleJoint;
                case HandJointKind.RingDistal: return TrackedHandJoint.RingDistalJoint;
                case HandJointKind.RingTip: return TrackedHandJoint.RingTip;

                case HandJointKind.LittleMetacarpal: return TrackedHandJoint.PinkyMetacarpal;
                case HandJointKind.LittleProximal: return TrackedHandJoint.PinkyKnuckle;
                case HandJointKind.LittleIntermediate: return TrackedHandJoint.PinkyMiddleJoint;
                case HandJointKind.LittleDistal: return TrackedHandJoint.PinkyDistalJoint;
                case HandJointKind.LittleTip: return TrackedHandJoint.PinkyTip;

                default: return TrackedHandJoint.None;
            }
        }

        #region Protected InputSource Helpers

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

        protected enum HandBehaviorType
        {
            Touch = 0,
            Grab
        }

        #region Gesture Definitions

        protected void TestForTouching()
        {
            UpdateCurrentIndexPose();

            if (currentIndexPose.Position == Vector3.zero)
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
                    if (DoesColliderContain(colliders[c], currentIndexPose.Position) == true)
                    {
                        lastTouchedObject = colliders[c].gameObject;
                        lastTouchingPoint = currentIndexPose.Position;
                        if (objectsToTouchUpdate.Contains(colliders[c].gameObject) == false)
                        {
                            // Collider contains a touch point, but is not yet tracked (has not received touch down)
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

                lastPosition = unityJointPositions[(int)HandJointKind.Palm];
                lastPalmNormal = unityJointOrientations[(int)HandJointKind.Palm] * Vector3.up;
            }
            else if (frameOn == velocityUpdateInterval)
            {
                //update linear velocity
                float deltaTime = Time.unscaledTime - deltaTimeStart;
                Vector3 newVelocity = (unityJointPositions[(int)HandJointKind.Palm] - lastPosition) / deltaTime;
                Velocity = (Velocity * 0.8f) + (newVelocity * 0.2f);

                //update angular velocity
                Vector3 currentPalmNormal = unityJointOrientations[(int)HandJointKind.Palm] * Vector3.up;
                Quaternion rotation = Quaternion.FromToRotation(lastPalmNormal, currentPalmNormal);
                Vector3 rotationRate = rotation.eulerAngles * Mathf.Deg2Rad;
                AngularVelocity = rotationRate / deltaTime;
            }

            frameOn++;
            frameOn = frameOn > velocityUpdateInterval ? 0 : frameOn;
        }

        protected void UpdateCurrentIndexPose()
        {
            currentIndexPose.Rotation = unityJointOrientations[(int)HandJointKind.IndexTip];

            var skinOffsetFromBone = (currentIndexPose.Rotation * (-Vector3.forward) * lastIndexTipRadius);
            currentIndexPose.Position = (unityJointPositions[(int)HandJointKind.IndexTip] + skinOffsetFromBone);
        }

        #endregion Gesture Definitions

        private HandTrackingInputEventData GetHandSourceEventData(HandBehaviorType type)
        {
            HandTrackingInputEventData data = new HandTrackingInputEventData(UnityEngine.EventSystems.EventSystem.current);
            data.Initialize(InputSource, this, false,
                            lastPressingState == TouchingState.Pressing, lastTouchingPoint, lastTouchedObject, handRay.Ray);
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

#endif // WINDOWS_UWP
#endif // UNITY_WSA
    }
}