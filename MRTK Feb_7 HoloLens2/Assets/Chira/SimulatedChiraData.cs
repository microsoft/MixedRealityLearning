using UnityEngine;

namespace Chira
{
    class SimulatedChiraData
    {
        public const float CHIRA_SIMULATION_SCROLL_MULTIPLIER = 0.1f;
        public const float CHIRA_SIMULATION_PINCH_LERP_FRACTION = 0.3f;
        public const float CHIRA_SIMULATION_START_HAND_DISTANCE = 0.5f;
        private const float CHIRA_SIMULATION_ROTATE_DELTA = 100.0f;

        public bool IsRightHandVisible { get; private set; }
        public bool IsLeftHandVisible { get; private set; }
        public bool IsRightHandPinching { get; private set; }
        public bool IsLeftHandPinching { get; private set; }
        float mousePositionZ = 0;
        float pinchAmount = 0;
        Vector3 handRotateEulerAngles = Vector3.zero;

        public float NoiseAmount = 0;

        public SimulatedChiraData()
        {
            Reset();
        }

        public void ToggleIsRightVisible()
        {
            IsRightHandVisible = !IsRightHandVisible;
            // Only reset the data if this hand is the only one visible
            if (!IsLeftHandVisible)
            {
                Reset();
            }
        }
        public void ToggleIsLeftVisible()
        {
            IsLeftHandVisible = !IsLeftHandVisible;
            // Only reset the data if this hand is the only one visible
            if (!IsRightHandVisible)
            {
                Reset(); 
            }
        }
        public void Reset()
        {
            mousePositionZ = CHIRA_SIMULATION_START_HAND_DISTANCE;
            pinchAmount = 0;
            handRotateEulerAngles = Vector3.zero;
        }
        public void Update()
        {
            float scrollAmount = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            mousePositionZ += scrollAmount * CHIRA_SIMULATION_SCROLL_MULTIPLIER;
            pinchAmount = Mathf.Lerp(pinchAmount, Input.GetMouseButton(0) ? 1 : 0, CHIRA_SIMULATION_PINCH_LERP_FRACTION);
            float rotateDelta = CHIRA_SIMULATION_ROTATE_DELTA * Time.deltaTime;
            if (Input.GetKey(KeyCode.Q))
            {
                handRotateEulerAngles.y -= rotateDelta;
            }
            if (Input.GetKey(KeyCode.E))
            {
                handRotateEulerAngles.y += rotateDelta;
            }
            if (Input.GetKey(KeyCode.R))
            {
                handRotateEulerAngles.x += rotateDelta;
            }
            if (Input.GetKey(KeyCode.F))
            {
                handRotateEulerAngles.x -= rotateDelta;
            }
            if (Input.GetKey(KeyCode.Z))
            {
                handRotateEulerAngles.z += rotateDelta;
            }
            if (Input.GetKey(KeyCode.X))
            {
                handRotateEulerAngles.z -= rotateDelta;
            }
            IsLeftHandPinching = IsLeftHandVisible && Input.GetMouseButton(0);
            IsRightHandPinching = IsRightHandVisible && Input.GetMouseButton(0);
        }
        public Vector3 MousePositionWorld
        {
            get
            {
                Vector3 mousePositionScreen = UnityEngine.Input.mousePosition;
                mousePositionScreen += Random.insideUnitSphere * NoiseAmount;
                mousePositionScreen.z = mousePositionZ;
                Vector3 result = Camera.main.ScreenToWorldPoint(mousePositionScreen);
                return result;
            }
        }

        // These values obtained from "Demo - HoloLens Leap" scene in folder (launched with Unity 2017.3.0f3)
        // https://microsoft.visualstudio.com/DefaultCollection/Analog/_git/analog.platform.proto.holotouch_museum/
        // 
        // I made the hand pose I wanted, then in CapsuleHandComponent pressed "print relative transfomrs"
        // and copied values from debug console
        private Vector3[] jointOffsetsHandOpened = new Vector3[]
        {
            // Right palm is duplicate of right thumb metacarpal and right pinky metacarpal
            new Vector3(-0.036f,0.10f,0.051f),  // Palm
            new Vector3(-0.036f,0.165f,0.061f),  // Wrist
            new Vector3(-0.020f,0.159f,0.061f), // ThumbMetacarpal
            new Vector3(0.018f,0.126f,0.047f),
            new Vector3(0.044f,0.107f,0.041f),
            new Vector3(0.063f,0.097f,0.040f), // ThumbTip
            new Vector3(-0.027f,0.159f,0.061f), // IndexMetacarpal
            new Vector3(-0.009f,0.075f,0.033f), 
            new Vector3(-0.005f,0.036f,0.017f),
            new Vector3(-0.002f,0.015f,0.007f),
            new Vector3(0.000f,0.000f,0.000f), // IndexTip
            new Vector3(-0.035f,0.159f,0.061f), // MiddleMetacarpal
            new Vector3(-0.032f,0.073f,0.032f),
            new Vector3(-0.017f,0.077f,-0.002f),
            new Vector3(-0.017f,0.104f,-0.001f),
            new Vector3(-0.021f,0.119f,0.008f),
            new Vector3(-0.043f,0.159f,0.061f), // RingMetacarpal
            new Vector3(-0.055f,0.078f,0.032f),
            new Vector3(-0.041f,0.080f,0.001f),
            new Vector3(-0.037f,0.106f,0.003f),
            new Vector3(-0.038f,0.121f,0.012f),
            new Vector3(-0.050f,0.159f,0.061f), // PinkyMetacarpal
            new Vector3(-0.074f,0.087f,0.031f), // PinkyProximal
            new Vector3(-0.061f,0.084f,0.006f), // PinkyIntermediate
            new Vector3(-0.054f,0.101f,0.005f), // PinkyDistal
            new Vector3(-0.054f,0.116f,0.013f), // PinkyTip
        };

        private Vector3[] jointOffsetsHandPinch = new Vector3[]
        {
            // Right palm is duplicate of right thumb metacarpal and right pinky metacarpal
            new Vector3(-0.042f,0.051f,0.060f), // Palm
            new Vector3(-0.042f,0.111f,0.060f), // Wrist
            new Vector3(-0.032f,0.091f,0.060f), // ThumbMetacarpal
            new Vector3(-0.013f,0.052f,0.044f),
            new Vector3(0.002f,0.026f,0.030f),
            new Vector3(0.007f,0.007f,0.017f),  // ThumbTip
            new Vector3(-0.038f,0.091f,0.060f), // IndexMetacarpal
            new Vector3(-0.029f,0.008f,0.050f),
            new Vector3(-0.009f,-0.016f,0.025f),
            new Vector3(-0.002f,-0.011f,0.008f),
            new Vector3(0.000f,0.000f,0.000f),
            new Vector3(-0.042f,0.091f,0.060f), // MiddleMetacarpal
            new Vector3(-0.050f,0.004f,0.046f),
            new Vector3(-0.026f,0.004f,0.014f),
            new Vector3(-0.028f,0.031f,0.014f),
            new Vector3(-0.034f,0.048f,0.020f),
            new Vector3(-0.048f,0.091f,0.060f), // RingMetacarpal
            new Vector3(-0.071f,0.008f,0.041f),
            new Vector3(-0.048f,0.009f,0.012f),
            new Vector3(-0.046f,0.036f,0.014f),
            new Vector3(-0.050f,0.052f,0.022f),
            new Vector3(-0.052f,0.091f,0.060f), // PinkyMetacarpal
            new Vector3(-0.088f,0.014f,0.034f),
            new Vector3(-0.067f,0.012f,0.013f),
            new Vector3(-0.061f,0.031f,0.014f),
            new Vector3(-0.062f,0.046f,0.021f),
        };

        /// <summary>
        /// Returns root + offset, where offset is inverseley lerped (when value is zero, apply offset fully
        /// and when value is 1, don't apply offset).
        /// </summary>
        /// <param name="rootInWorldSpace"></param>
        /// <param name="offset"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private Vector3 LerpOffsetInv(Vector3 rootInWorldSpace, Vector3 offsetStart, Vector3 offsetEnd, float value, Quaternion handRotation)
        {
            // first rotate the joint according to the hand rotation.
            offsetStart = handRotation * offsetStart;
            offsetEnd = handRotation * offsetEnd;
            return rootInWorldSpace - Vector3.Lerp(
                Camera.main.transform.TransformDirection(offsetStart),
                Camera.main.transform.TransformDirection(offsetEnd),
                pinchAmount);
        }

        internal void FillCurrentFrame(Vector3[] joints)
        {
            FillCurrentFrameHelper(joints, 0, (int)Joints.Count / 2, MousePositionWorld + -0.3f * Vector3.right);
            FillCurrentFrameHelper(joints, (int)Joints.Count / 2, (int)Joints.Count, MousePositionWorld);
        }

        internal void FillCurrentFrameHelper(Vector3[] joints, int startJointIndex, int endJointIndex, Vector3 handOrigin)
        {
            for (int i = startJointIndex; i < endJointIndex; i++)
            {
                int i2 = i - (int)startJointIndex;
                joints[i] = LerpOffsetInv(handOrigin, jointOffsetsHandOpened[i2], jointOffsetsHandPinch[i2], pinchAmount, Quaternion.Euler(handRotateEulerAngles));
            }
        }
    }
}