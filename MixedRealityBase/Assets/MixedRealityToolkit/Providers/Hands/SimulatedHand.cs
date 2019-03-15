// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Attributes;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Core.Definitions.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Core.Devices.Hands
{
    [MixedRealityController(
        SupportedControllerType.ArticulatedHand,
        new[] { Handedness.Left, Handedness.Right })]
    public class SimulatedHand : BaseHand
    {
        private static readonly int jointCount = Enum.GetNames(typeof(TrackedHandJoint)).Length;

        private Vector3 currentPointerPosition = Vector3.zero;
        private Quaternion currentPointerRotation = Quaternion.identity;
        private MixedRealityPose lastPointerPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentPointerPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentIndexPose = MixedRealityPose.ZeroIdentity;


        private MixedRealityPose currentGripPose = MixedRealityPose.ZeroIdentity;

        private readonly Quaternion[] jointOrientations = new Quaternion[jointCount];
        private readonly Vector3[] jointPositions = new Vector3[jointCount];
        private readonly Dictionary<TrackedHandJoint, MixedRealityPose> jointPoses = new Dictionary<TrackedHandJoint, MixedRealityPose>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="trackingState"></param>
        /// <param name="controllerHandedness"></param>
        /// <param name="inputSource"></param>
        /// <param name="interactions"></param>
        public SimulatedHand(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
                : base(trackingState, controllerHandedness, inputSource, interactions)
        {
        }

        /// <summary>
        /// The Windows Mixed Reality Controller default interactions.
        /// </summary>
        /// <remarks>A single interaction mapping works for both left and right controllers.</remarks>
        public override MixedRealityInteractionMapping[] DefaultInteractions => new[]
        {
            new MixedRealityInteractionMapping(0, "Spatial Pointer", AxisType.SixDof, DeviceInputType.SpatialPointer, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(1, "Spatial Grip", AxisType.SixDof, DeviceInputType.SpatialGrip, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(2, "Select", AxisType.Digital, DeviceInputType.Select, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(3, "Grab", AxisType.SingleAxis, DeviceInputType.TriggerPress, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(4, "Index Finger Pose", AxisType.SixDof, DeviceInputType.IndexFinger, MixedRealityInputAction.None)
        };

        public override void SetupDefaultInteractions(Handedness controllerHandedness)
        {
            AssignControllerMappings(DefaultInteractions);
        }

        public void UpdateState(SimulatedHandData handData)
        {
            lastPointerPose = currentPointerPose;

            Array.Copy(handData.Joints, jointPositions, jointCount);

            CalculateJointRotations();

            // For convenience of simulating in Unity Editor, make the ray use the index
            // finger position instead of knuckle, since the index finger doesn't move when we press.
            Vector3 pointerPosition = jointPositions[(int)TrackedHandJoint.IndexTip];
            IsPositionAvailable = IsRotationAvailable = pointerPosition != Vector3.zero;

            if (IsPositionAvailable)
            {
                HandRay.Update(pointerPosition, GetPalmNormal(), CameraCache.Main.transform, ControllerHandedness);

                Ray ray = HandRay.Ray;

                currentPointerPose.Position = ray.origin;
                currentPointerPose.Rotation = Quaternion.LookRotation(ray.direction);

                currentGripPose.Position = jointPositions[(int)TrackedHandJoint.Palm];
                currentGripPose.Rotation = jointOrientations[(int)TrackedHandJoint.Palm];

                currentIndexPose.Position = jointPositions[(int)TrackedHandJoint.IndexTip];
                currentIndexPose.Rotation = jointOrientations[(int)TrackedHandJoint.IndexTip];
            }

            if (lastPointerPose != currentPointerPose)
            {
                if (IsPositionAvailable && IsRotationAvailable)
                {
                    MixedRealityToolkit.InputSystem?.RaiseSourcePoseChanged(InputSource, this, currentPointerPose);
                }
                else if (IsPositionAvailable && !IsRotationAvailable)
                {
                    MixedRealityToolkit.InputSystem?.RaiseSourcePositionChanged(InputSource, this, currentPointerPosition);
                }
                else if (!IsPositionAvailable && IsRotationAvailable)
                {
                    MixedRealityToolkit.InputSystem?.RaiseSourceRotationChanged(InputSource, this, currentPointerRotation);
                }
            }

            for (int i = 0; i < Interactions?.Length; i++)
            {
                switch (Interactions[i].InputType)
                {
                    case DeviceInputType.SpatialPointer:
                        Interactions[i].PoseData = currentPointerPose;
                        if (Interactions[i].Changed)
                        {
                            MixedRealityToolkit.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction, currentPointerPose);
                        }
                        break;
                    case DeviceInputType.SpatialGrip:
                        Interactions[i].PoseData = currentGripPose;
                        if (Interactions[i].Changed)
                        {
                            MixedRealityToolkit.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction, currentGripPose);
                        }
                        break;
                    case DeviceInputType.Select:
                        Interactions[i].BoolData = handData.IsPinching;

                        if (Interactions[i].Changed)
                        {
                            if (Interactions[i].BoolData)
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                            else
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                        }
                        break;
                    case DeviceInputType.TriggerPress:
                        Interactions[i].BoolData = handData.IsPinching;

                        if (Interactions[i].Changed)
                        {
                            if (Interactions[i].BoolData)
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                            else
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                        }
                        break;
                    case DeviceInputType.IndexFinger:
                        Interactions[i].PoseData = currentIndexPose;
                        if (Interactions[i].Changed)
                        {
                            MixedRealityToolkit.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction, currentIndexPose);
                        }
                        break;
                }
            }

            for (int i = 0; i < jointPositions.Length; i++)
            {
                TrackedHandJoint handJoint = (TrackedHandJoint)i;

                if (!jointPoses.ContainsKey(handJoint))
                {
                    jointPoses.Add(handJoint, new MixedRealityPose(jointPositions[i], jointOrientations[i]));
                }
                else
                {
                    jointPoses[handJoint] = new MixedRealityPose(jointPositions[i], jointOrientations[i]);
                }
            }

            MixedRealityToolkit.InputSystem?.RaiseHandJointsUpdated(InputSource, ControllerHandedness, jointPoses);

            UpdateVelocity();
            TestForTouching();
        }
 
 
        /// <summary>
        /// Compute the rotation of each joint, with the forward vector of the rotation pointing along the joint bone, 
        /// and the up vector pointing up.
        /// 
        /// The rotation of the base joints (thumb base, pinky base, etc) as well as the wrist joint is set to 
        /// point in the direction of palm forward.
        /// 
        /// Assumption: the position of each joint has been copied from handData joint positions
        /// </summary>
        ///
        /// Notes:
        ///  - SimulatedHandDataUtils.GetPalmUpVector and GetPalmForwardVector appear to be flipped.  GetPalmForwardVector appears
        ///    to return the vector that extends perpendicular from the palm, which might be thought of as 'up'
        private void CalculateJointRotations()
        {

            const int numFingers = 5;
            int[] jointsPerFinger = { 4, 5, 5, 5, 5 }; // thumb, index, middle, right, pinky

            for (int fingerIndex = 0; fingerIndex < numFingers; fingerIndex++)
            {
                int jointsCurrentFinger = jointsPerFinger[fingerIndex];
                int lowIndex = 2 + jointsPerFinger.Take(fingerIndex).Sum();
                int highIndex = lowIndex + jointsCurrentFinger - 1;

                for (int jointStartidx = lowIndex; jointStartidx < highIndex; jointStartidx++)
                {
                    int jointEndidx = jointStartidx + 1;
                    Vector3 boneForward = jointPositions[jointEndidx] - jointPositions[jointStartidx];
                    Vector3 boneUp = Vector3.Cross(boneForward, SimulatedHandDataUtils.GetPalmRightVector(ControllerHandedness));
                    if (boneForward.magnitude > float.Epsilon && boneUp.magnitude > float.Epsilon)
                    {
                        Quaternion jointRotation = Quaternion.LookRotation(boneForward, boneUp);
                        // If we are the thumb, set the up vector to be from pinky to index (right hand) or index to pinky (left hand).
                        if (fingerIndex == 0)
                        {
                            // Rotate the thumb by 90 degrees (-90 if left hand) about thumb forward vector.
                            Quaternion rotateThumb90 = Quaternion.AngleAxis(ControllerHandedness == Handedness.Left ? -90 : 90, boneForward);
                            jointRotation = rotateThumb90 * jointRotation;
                        }
                        jointOrientations[jointStartidx] = jointRotation;
                    }
                    else
                    {
                        jointOrientations[jointStartidx] = Quaternion.identity;
                    }
                }
                jointOrientations[highIndex] = jointOrientations[highIndex - 1];
            }
            jointOrientations[(int)TrackedHandJoint.Palm] = Quaternion.LookRotation(SimulatedHandDataUtils.GetPalmForwardVector(ControllerHandedness), SimulatedHandDataUtils.GetPalmUpVector(ControllerHandedness));
        }
    }
}