using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Chira
{
    public class ChiraDataUtils
    {
        [Obsolete("This is for Chira 0908, don't use it in ChiraLatest or any version newer than 0908.")]
        public static Vector3 GetWristPosition(Chira.HandSide handSide)
        {
            EnsureChiraDataProvider();

            Vector3 thumbMetacarpal = GetJoint(handSide, JointIndex.ThumbMetacarpal);
            Vector3 pinkyMetacarpal = GetJoint(handSide, JointIndex.PinkyMetacarpal);

            return (thumbMetacarpal + pinkyMetacarpal) / 2.0f;
        }

        public static Vector3 GetJoint(Chira.HandSide side, JointIndex jointIndex)
        {
            int offset = 0;
            if (side == HandSide.Right)
            {
                offset = (int)Joints.Count / 2;
            }
            return ChiraDataProvider.Instance.CurrentFrame.Joints[offset + (int)jointIndex];
        }

        private static void EnsureChiraDataProvider()
        {
            if (ChiraDataProvider.Instance == null)
            {
                Debug.LogError("ChiraDataProvider is null.");
                throw new Exception("ChiraDataProvider is null.");
            }
        }

        /// <summary>
        /// Gets vector corresponding to +z using the same coordinate space
        /// as Leap Motion does.
        /// In Leap motion, the forward vecotr moves from the ThumbMetaCarpal to the index finger.
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        public static Vector3 GetPalmForwardVector(Chira.HandSide side)
        {
            Vector3 indexBase = GetJoint(side, JointIndex.IndexProximal);
            Vector3 thumbMetaCarpal = GetJoint(side, JointIndex.ThumbMetacarpal);

            Vector3 thumbMetaCarpalToIndex = indexBase - thumbMetaCarpal;
            return thumbMetaCarpalToIndex.normalized;
        }

        /// <summary>
        /// Gets the vector corresponding to +y using same coordinate space as leap motion
        /// In Leap Motion the up vector moves out of the palm.
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        public static Vector3 GetPalmUpVector(Chira.HandSide side)
        {
            Vector3 indexBase = GetJoint(side, JointIndex.IndexProximal);
            Vector3 pinkyBase = GetJoint(side, JointIndex.PinkyProximal);
            Vector3 ThumbMetaCarpal = GetJoint(side, JointIndex.ThumbMetacarpal);

            Vector3 ThumbMetaCarpalToPinky = pinkyBase - ThumbMetaCarpal;
            Vector3 ThumbMetaCarpalToIndex = indexBase - ThumbMetaCarpal;
            if (side == HandSide.Left)
            {
                return Vector3.Cross(ThumbMetaCarpalToPinky, ThumbMetaCarpalToIndex).normalized;
            }
            else
            {
                return Vector3.Cross(ThumbMetaCarpalToIndex, ThumbMetaCarpalToPinky).normalized;
            }
        }


        public static Vector3 GetPalmRightVector(Chira.HandSide side)
        {
            Vector3 indexBase = GetJoint(side, JointIndex.IndexProximal);
            Vector3 pinkyBase = GetJoint(side, JointIndex.PinkyProximal);
            Vector3 thumbMetaCarpal = GetJoint(side, JointIndex.ThumbMetacarpal);

            Vector3 thumbMetaCarpalToPinky = pinkyBase - thumbMetaCarpal;
            Vector3 thumbMetaCarpalToIndex = indexBase - thumbMetaCarpal;
            Vector3 thumbMetaCarpalUp = Vector3.zero;
            if (side == HandSide.Left)
            {
                thumbMetaCarpalUp = Vector3.Cross(thumbMetaCarpalToPinky, thumbMetaCarpalToIndex).normalized;
            }
            else
            {
                thumbMetaCarpalUp = Vector3.Cross(thumbMetaCarpalToIndex, thumbMetaCarpalToPinky).normalized;
            }

            return Vector3.Cross(thumbMetaCarpalUp, thumbMetaCarpalToIndex).normalized;
        }


        public static bool IsChiraDataValid()
        {
            return ChiraDataProvider.Instance != null && ChiraDataProvider.Instance.CurrentFrame != null && ChiraDataProvider.Instance.CurrentFrame.Timestamp > 0;
        }

        public static bool IsHandTracked(HandSide side)
        {
            if (!IsChiraDataValid())
            {
                return false;
            }

            return ChiraDataProvider.Instance.CurrentFrame.IsTracked != null
                && ChiraDataProvider.Instance.CurrentFrame.IsTracked[side == HandSide.Left ? 0 : 1];
        }
    }

}