// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
using Chira;
#endif // UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
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
    [MixedRealityController(SupportedControllerType.ArticulatedHand, new[] { Handedness.Left, Handedness.Right })]
    public class ChiraHand : BaseHand
    {
        private Vector3 currentPointerPosition = Vector3.zero;
        private Quaternion currentPointerRotation = Quaternion.identity;
        private MixedRealityPose lastPointerPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentPointerPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentGripPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentIndexPose = MixedRealityPose.ZeroIdentity;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
        private static readonly int jointCount = (int)Joints.Count / 2;

        private Chira.HandSide HandSide => ControllerHandedness == Handedness.Left ? Chira.HandSide.Left : Chira.HandSide.Right;
#endif // UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="trackingState"></param>
        /// <param name="controllerHandedness"></param>
        /// <param name="inputSource"></param>
        /// <param name="interactions"></param>
        public ChiraHand(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
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
            new MixedRealityInteractionMapping(4, "Index Finger Pose", AxisType.SixDof, DeviceInputType.IndexFinger, MixedRealityInputAction.None),
        };

        public override void SetupDefaultInteractions(Handedness controllerHandedness)
        {
            AssignControllerMappings(DefaultInteractions);
        }

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
        public void UpdateState(ChiraDataUnity chiraData)
        {
            lastPointerPose = currentPointerPose;

            if (ControllerHandedness == Handedness.Left)
            {
                Array.Copy(chiraData.Joints, jointPositions, jointCount);
            }
            else if (ControllerHandedness == Handedness.Right)
            {
                Array.Copy(chiraData.Joints, (int)Joints.RightPalm, jointPositions, 0, jointCount);
            }

            CalculateJointRotations();

            // For convenience of simulating in Unity Editor with chira, make the ray use the index
            // finger position instead of knuckle, since the index finger doesn't move when we press.
            Vector3 pointerPosition = jointPositions[Application.isEditor ? (int)JointIndex.IndexTip : (int)JointIndex.IndexProximal];
            IsPositionAvailable = IsRotationAvailable = pointerPosition != Vector3.zero;

            if (IsPositionAvailable)
            {
                HandRay.Update(pointerPosition, GetPalmNormal(), CameraCache.Main.transform, ControllerHandedness);

                Ray ray = HandRay.Ray;

                currentPointerPose.Position = ray.origin;
                currentPointerPose.Rotation = Quaternion.LookRotation(ray.direction);

                currentGripPose.Position = jointPositions[(int)JointIndex.Palm];
                currentGripPose.Rotation = jointOrientations[(int)JointIndex.Palm];

                currentIndexPose.Position = jointPositions[(int)JointIndex.IndexTip];
                currentIndexPose.Rotation = jointOrientations[(int)JointIndex.IndexTip];
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
                        Interactions[i].BoolData = chiraData.IsPinching[ControllerHandedness == Handedness.Left ? 0 : 1];

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
                        Interactions[i].BoolData = chiraData.IsPinching[ControllerHandedness == Handedness.Left ? 0 : 1];

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
                TrackedHandJoint handJoint = ConvertChiraJointIndexToTrackedHandJoint((JointIndex)i);

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

        private readonly Quaternion[] jointOrientations = new Quaternion[jointCount];
        private readonly Vector3[] jointPositions = new Vector3[jointCount];
        private readonly Dictionary<TrackedHandJoint, MixedRealityPose> jointPoses = new Dictionary<TrackedHandJoint, MixedRealityPose>();

        /// <summary>
        /// Compute the rotation of each joint, with the forward vector of the rotation pointing along the joint bone, 
        /// and the up vector pointing up.
        /// 
        /// The rotation of the base joints (thumb base, pinky base, etc) as well as the wrist joint is set to 
        /// point in the direction of palm forward.
        /// 
        /// Assumption: the position of each joint has been copied from chiraData joint positions
        /// </summary>
        ///
        /// Notes:
        ///  - ChiraDataUtils.GetPalmUpVector and GetPalmForwardVector appear to be flipped.  GetPalmForwardVector appears
        ///    to return the vector that extends perpendicular from the palm, which might be thought of as 'up'
        private void CalculateJointRotations()
        {
            jointOrientations[(int)JointIndex.Palm] = Quaternion.LookRotation(ChiraDataUtils.GetPalmForwardVector(HandSide), ChiraDataUtils.GetPalmUpVector(HandSide));

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
                    Vector3 boneUp = Vector3.Cross(boneForward, ChiraDataUtils.GetPalmRightVector(HandSide));
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
        }

        private TrackedHandJoint ConvertChiraJointIndexToTrackedHandJoint(JointIndex joint)
        {
            switch (joint)
            {
                default:
                case JointIndex.Wrist:
                    return TrackedHandJoint.Wrist;
                case JointIndex.Palm:
                    return TrackedHandJoint.Palm;
                case JointIndex.ThumbMetacarpal:
                    return TrackedHandJoint.ThumbMetacarpalJoint;
                case JointIndex.ThumbProximal:
                    return TrackedHandJoint.ThumbProximalJoint;
                case JointIndex.ThumbDistal:
                    return TrackedHandJoint.ThumbDistalJoint;
                case JointIndex.ThumbTip:
                    return TrackedHandJoint.ThumbTip;
                case JointIndex.IndexMetacarpal:
                    return TrackedHandJoint.IndexMetacarpal;
                case JointIndex.IndexProximal:
                    return TrackedHandJoint.IndexKnuckle; 
                case JointIndex.IndexIntermediate:
                    return TrackedHandJoint.IndexMiddleJoint;
                case JointIndex.IndexDistal:
                    return TrackedHandJoint.IndexDistalJoint;
                case JointIndex.IndexTip:
                    return TrackedHandJoint.IndexTip;
                case JointIndex.MiddleMetacarpal:
                    return TrackedHandJoint.MiddleMetacarpal;
                case JointIndex.MiddleProximal:
                    return TrackedHandJoint.MiddleKnuckle;
                case JointIndex.MiddleIntermediate:
                    return TrackedHandJoint.MiddleMiddleJoint;
                case JointIndex.MiddleDistal:
                    return TrackedHandJoint.MiddleDistalJoint;
                case JointIndex.MiddleTip:
                    return TrackedHandJoint.MiddleTip;
                case JointIndex.RingMetacarpal:
                    return TrackedHandJoint.RingMetacarpal;
                case JointIndex.RingProximal:
                    return TrackedHandJoint.RingKnuckle;
                case JointIndex.RingIntermediate:
                    return TrackedHandJoint.RingMiddleJoint;
                case JointIndex.RingDistal:
                    return TrackedHandJoint.RingDistalJoint;
                case JointIndex.RingTip:
                    return TrackedHandJoint.RingTip;
                case JointIndex.PinkyMetacarpal:
                    return TrackedHandJoint.PinkyMetacarpal;
                case JointIndex.PinkyProximal:
                    return TrackedHandJoint.PinkyKnuckle;
                case JointIndex.PinkyIntermediate:
                    return TrackedHandJoint.PinkyMiddleJoint;
                case JointIndex.PinkyDistal:
                    return TrackedHandJoint.PinkyDistalJoint;
                case JointIndex.PinkyTip:
                    return TrackedHandJoint.PinkyTip;
            }
        }
#endif // UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
    }
}