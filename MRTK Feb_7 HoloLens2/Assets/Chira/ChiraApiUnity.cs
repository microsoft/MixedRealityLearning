using UnityEngine;

// API Version 18226.1000.180821-1700_RS_ANALOG_RUNTIME
namespace Chira
{
    public class ChiraDataUnity
    {
        public long Others;
        // Note: this version of the Chira API is for the latest LKG.
        // Previous LKG versions will need to use an older version of the Chira API

        // Timestamp of hand data, as FileTime, e.g. DateTime.Now.ToFileTime() 
        public long Timestamp;
        public bool[] IsTracked;
        public Vector3[] Joints;
        public Vector3[] Vertices;
        public int[] JointStates;
        public bool[] IsPinching;
        public bool[] IsSystemGestureReady;
        public bool[] IsSystemGestureTriggered;
        public bool[] IsFingerPressed;
        public bool[] IsFingerReleased;

        public static readonly int HAND_ID_RIGHT = 27;
        public static readonly int HAND_ID_LEFT = 28;
        public const int MaxHands = 2;

        public const int MaxVertices = 453 * 2;
    }
    public enum HandSide
    {
        Right,
        Left
    };

    public enum JointIndex
    {
        Palm = 0,
        Wrist = 1,
        ThumbMetacarpal = 2,
        ThumbProximal = 3,
        ThumbDistal = 4,
        ThumbTip = 5,
        IndexMetacarpal = 6,
        IndexProximal = 7,
        IndexIntermediate = 8,
        IndexDistal = 9,
        IndexTip = 10,
        MiddleMetacarpal = 11,
        MiddleProximal = 12,
        MiddleIntermediate = 13,
        MiddleDistal = 14,
        MiddleTip = 15,
        RingMetacarpal = 16,
        RingProximal = 17,
        RingIntermediate = 18,
        RingDistal = 19,
        RingTip = 20,
        PinkyMetacarpal = 21,
        PinkyProximal = 22,
        PinkyIntermediate = 23,
        PinkyDistal = 24,
        PinkyTip = 25,
    }


    public enum Joints
    {
        LeftPalm = 0,
        LeftWrist = 1,
        LeftThumbMetacarpal = 2,
        LeftThumbProximal = 3,
        LeftThumbDistal = 4,
        LeftThumbTip = 5,
        LeftIndexMetacarpal = 6,
        LeftIndexProximal = 7,
        LeftIndexIntermediate = 8,
        LeftIndexDistal = 9,
        LeftIndexTip = 10,
        LeftMiddleMetacarpal = 11,
        LeftMiddleProximal = 12,
        LeftMiddleIntermediate = 13,
        LeftMiddleDistal = 14,
        LeftMiddleTip = 15,
        LeftRingMetacarpal = 16,
        LeftRingProximal = 17,
        LeftRingIntermediate = 18,
        LeftRingDistal = 19,
        LeftRingTip = 20,
        LeftPinkyMetacarpal = 21,
        LeftPinkyProximal = 22,
        LeftPinkyIntermediate = 23,
        LeftPinkyDistal = 24,
        LeftPinkyTip = 25,
        RightPalm = 26,
        RightWrist = 27,
        RightThumbMetacarpal = 28,
        RightThumbProximal = 29,
        RightThumbDistal = 30,
        RightThumbTip = 31,
        RightIndexMetacarpal = 32,
        RightIndexProximal = 33,
        RightIndexIntermediate = 34,
        RightIndexDistal = 35,
        RightIndexTip = 36,
        RightMiddleMetacarpal = 37,
        RightMiddleProximal = 38,
        RightMiddleIntermediate = 39,
        RightMiddleDistal = 40,
        RightMiddleTip = 41,
        RightRingMetacarpal = 42,
        RightRingProximal = 43,
        RightRingIntermediate = 44,
        RightRingDistal = 45,
        RightRingTip = 46,
        RightPinkyMetacarpal = 47,
        RightPinkyProximal = 48,
        RightPinkyIntermediate = 49,
        RightPinkyDistal = 50,
        RightPinkyTip = 51,
        Count = 52
    }
}
