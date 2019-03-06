#define DISABLE_CHIRA_ON_DEVICE
// USE_PERCEPTION_API uses the perception api to get the head transform
// at the timestamp corresponding to the hand frame, and reproject the hands
// using the proper transform. Without this, hands swim when you move the head.
// NOTE: To use this, we need to build using "Chira" editor menu item.
// See README.txt for more details
// The bug tracking this is MSFT:16730174
#define USE_PERCEPTION_API_UNITY_UNSUPPORTED_FUNCTIONS
using UnityEngine;
using System;
#if !UNITY_EDITOR && UNITY_WSA
using Windows.Foundation.Numerics;
using Windows.Perception;
using Windows.Perception.Spatial;
using System.Runtime.InteropServices;
using Windows.UI.Input.Spatial;
#endif

/// <summary>
/// Provides per-frame data access to data from Chira API
/// 
/// If running on HoloLens, uses chira data.
/// 
/// If running in unity editor, uses mouse and keyboard
/// data to simulate chira data.
/// 
/// Controls for mouse/keyboard simulation:
/// - Press spacebar to turn right hand on/off
/// - Left mouse button brings index and thumb together
/// - Mouse moves left and right hand.
/// </summary>
namespace Chira
{
    public class ChiraDataProvider : Singleton<ChiraDataProvider>
    {
        /// <summary>
        /// This event is raised whenever the hand data changes.
        /// Hand data changes at 45 fps.
        /// </summary>
        public event Action OnChiraDataChanged = delegate { };

        public ChiraDataUnity CurrentFrame;

        public bool IsConnected;
        public string ErrorMessage = "";

        private SimulatedChiraData simulatedChiraData = new SimulatedChiraData();

        [Tooltip("For simulation mode only, apply this noise amount to the data")]
        public float NoiseAmountForSimulation;

        public void Start()
        {
            StartInternal();
            // Update the chira data in OnBeforeRender instead of update to get the data as close as possible to when we will render a frame
            Application.onBeforeRender += Application_onBeforeRender;
        }

        private void Application_onBeforeRender()
        {
            var prevTime = CurrentFrame != null ? CurrentFrame.Timestamp : 0;
            UpdateHandData();
            if (CurrentFrame != null && CurrentFrame.Timestamp != prevTime)
            {
                OnChiraDataChanged();
            }
        }

#if !UNITY_EDITOR && UNITY_WSA && !DISABLE_CHIRA_ON_DEVICE
        private HandTracking.ChiraAPI chiraAPI;

        void StartInternal()
        {
            try
            {
                chiraAPI = new HandTracking.ChiraAPI();
            }
            catch (Exception ex)
            {
                ErrorMessage += String.Format("Failed to get Chira Data:{0}\n", ex.Message.ToString());
                Debug.LogError(ErrorMessage);
            }
            if (chiraAPI == null)
            {
                ErrorMessage += "Failed to initialize Chira API\n";
            }
            else
            {
                IsConnected = chiraAPI.Connect();

                // Sets hand shape: 0 is XL, 1 is L, 2 is M, 3 is S, 4 is XS
                chiraAPI.SetChiraHandShape(4);
            }
            if (!IsConnected)
            {
                ErrorMessage = "Failed to connect to Chira API: \n" + ErrorMessage;
                Debug.LogError(ErrorMessage);
            }

            // Forward prediction is not working, so turn off for now.
            // Turn off Chira smoothing since we will use forward prediction
            // The smoothing will be performed on the predicted data, not original.
            // NOTE: this step is required right now if you use forward predication,
            // that is mean, if you use ChiraAPI.GetChiraData(Uint64) and passing in a
            // non-zero time stamp.
            //chiraAPI.SetChiraSmoothingFactor(0.0f);

        }

        void UpdateHandData()
        {
            if (!IsConnected)
            {
                return;
            }

            DateTime now = DateTime.Now;
            // Get the forward predicted chira result by passing in current time
            //ChiraData chiraData = chiraAPI.GetChiraData((ulong)now.ToFileTime());
            HandTracking.ChiraData chiraData = chiraAPI.GetChiraData();
            if (CurrentFrame == null)
            {
                CurrentFrame = new ChiraDataUnity();
                CurrentFrame.IsTracked = new bool[chiraData.IsTracked.Length];
                CurrentFrame.Joints = new Vector3[chiraData.Joints.Length];
                CurrentFrame.Vertices = new Vector3[chiraData.Vertices.Length];
                CurrentFrame.JointStates = new int[chiraData.Joints.Length];
                CurrentFrame.IsPinching = new bool[chiraData.IsPinching.Length];
                CurrentFrame.IsSystemGestureReady = new bool[chiraData.IsSystemGestureReady.Length];
                CurrentFrame.IsSystemGestureTriggered = new bool[chiraData.IsSystemGestureTriggered.Length];
            }
            if (chiraData.Timestamp != CurrentFrame.Timestamp)
            {
                CurrentFrame.Timestamp = chiraData.Timestamp;
                CurrentFrame.Others = chiraData.Others;
                for (int i = 0; i < chiraData.IsTracked.Length; i++)
                {
                    CurrentFrame.IsTracked[i] = chiraData.IsTracked[i];
                }
                for (int i = 0; i < chiraData.IsPinching.Length; i++)
                {
                    CurrentFrame.IsPinching[i] = chiraData.IsPinching[i];
                }
                for (int i = 0; i < chiraData.IsSystemGestureReady.Length; i++)
                {
                    CurrentFrame.IsSystemGestureReady[i] = chiraData.IsSystemGestureReady[i];
                }
                for (int i = 0; i < chiraData.IsSystemGestureTriggered.Length; i++)
                {
                    CurrentFrame.IsSystemGestureTriggered[i] = chiraData.IsSystemGestureTriggered[i];
                }
            }

            DateTime handframeTimestamp = DateTime.FromFileTime(CurrentFrame.Timestamp);
#if USE_PERCEPTION_API_UNITY_UNSUPPORTED_FUNCTIONS
            // We get the joints in camera-space. Get the world space position of joints using
            // the head transform corresponding to the timestamp for the hand frame.
            PerceptionTimestamp perceptionTimestamp = PerceptionTimestampHelper.FromHistoricalTargetTime(handframeTimestamp);
            SpatialCoordinateSystem spatialCoordinateSystem = GetSpatialCoordinateSystem();
            SpatialPointerPose headPose = SpatialPointerPose.TryGetAtTimestamp(spatialCoordinateSystem, perceptionTimestamp);
            if (headPose != null)
            {
                Matrix4x4 transform = Matrix4x4.TRS(
                    WindowsVectorToUnityVector(headPose.Head.Position),
                    Quaternion.LookRotation(WindowsVectorToUnityVector(headPose.Head.ForwardDirection), WindowsVectorToUnityVector(headPose.Head.UpDirection)),
                    Vector3.one);
                UpdateJointsWindowsMatrix(transform, chiraData);
                UpdateVerticesWindowsMatrix(transform, chiraData);
            }
#else
            Matrix4x4? transform = TransformManager.GetCameraAtTimestamp(handframeTimestamp);
            if (transform.HasValue)
            {
                UpdateJointsUnityMatrix(transform.Value, chiraData);
                UpdateVerticesUnityMatrix(transform.Value, chiraData);
            }
#endif
        }

        private void UpdateJointsWindowsMatrix(Matrix4x4 transform, HandTracking.ChiraData chiraData)
        {
            for (int i = 0; i < chiraData.Joints.Length; i++)
            {
                CurrentFrame.Joints[i] = transformPoint(HandTrackingFloat3ToUnityVector(chiraData.Joints[i]), transform);
                CurrentFrame.JointStates[i] = chiraData.JointStates[i];
            }
        }

        private void UpdateVerticesWindowsMatrix(Matrix4x4 transform, HandTracking.ChiraData chiraData)
        {
            for (int i = 0; i < chiraData.Vertices.Length; i++)
            {
                CurrentFrame.Vertices[i] = transformPoint(HandTrackingFloat3ToUnityVector(chiraData.Vertices[i]), transform);
            }
        }

        private void UpdateJointsUnityMatrix(Matrix4x4 transform, HandTracking.ChiraData chiraData)
        {
            for (int i = 0; i < chiraData.Joints.Length; i++)
            {
                CurrentFrame.Joints[i] = transformPoint(HandTrackingFloat3ToUnityVector3FlipX(chiraData.Joints[i]), transform);
            }
        }

        private void UpdateVerticesUnityMatrix(Matrix4x4 transform, HandTracking.ChiraData chiraData)
        {
            for (int i = 0; i < chiraData.Vertices.Length; i++)
            {
                CurrentFrame.Vertices[i] = transformPoint(HandTrackingFloat3ToUnityVector3FlipX(chiraData.Vertices[i]), transform);
            }
        }

        private SpatialCoordinateSystem GetSpatialCoordinateSystem()
        {
            IntPtr spatialCoordinateSystemPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
            object baseObject = Marshal.GetObjectForIUnknown(spatialCoordinateSystemPtr);

            return baseObject as SpatialCoordinateSystem;
        }

        /// <summary>
        /// Converts a vector in window's coordinate system (which is right handed)
        /// to Unity's coordinate system (which is left handed)
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        UnityEngine.Vector3 WindowsVectorToUnityVector(System.Numerics.Vector3 v)
        {
            return new Vector3(v.X, v.Y, -v.Z);
        }

        UnityEngine.Vector3 HandTrackingFloat3ToUnityVector(HandTracking.Float3 v)
        {
            return new Vector3(v.x, v.y, -v.z);
        }

        UnityEngine.Vector3 HandTrackingFloat3ToUnityVector3FlipX(HandTracking.Float3 f)
        {
            return new Vector3(-f.x, f.y, f.z);
        }

        UnityEngine.Vector3 transformPoint(UnityEngine.Vector3 pos, Matrix4x4? transform)
        {
            if (!transform.HasValue)
            {
                throw new Exception("Couldn't find transform with appropriate timestamp");
            }
            else
            {
                return transform.Value.MultiplyPoint(pos);
            }
        }

        // Timespan is a long representation of a UTC file time
        public static string t2s(long timespan)
        {
            return String.Format("{0,10:F2}", timespan / 10000.0f);
        }
#elif !UNITY_EDITOR && UNITY_WSA
        public void StartInternal()
        {
        }
        private void UpdateHandData()
        {
        }
#elif UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    simulatedChiraData.ToggleIsLeftVisible();
                }
                else
                {
                    simulatedChiraData.ToggleIsRightVisible();
                }
            }
        }
        public void StartInternal()
        {
            IsConnected = true;
        }
        private void UpdateHandData()
        {
            if (CurrentFrame == null)
            {
                CurrentFrame = new ChiraDataUnity();
                CurrentFrame.IsTracked = new bool[(int)Joints.Count];
                CurrentFrame.Joints = new Vector3[(int)Joints.Count];
                CurrentFrame.IsPinching = new bool[2];
                CurrentFrame.Vertices = new Vector3[ChiraDataUnity.MaxVertices];
            }
            ZeroJoints();
            UpdateFrameMouseKeyboard();
        }

        private void UpdateFrameMouseKeyboard()
        {
            simulatedChiraData.Update();
            CurrentFrame.IsTracked[0] = simulatedChiraData.IsLeftHandVisible;
            CurrentFrame.IsTracked[1] = simulatedChiraData.IsRightHandVisible;
            CurrentFrame.IsPinching[0] = simulatedChiraData.IsLeftHandPinching;
            CurrentFrame.IsPinching[1] = simulatedChiraData.IsRightHandPinching;
            simulatedChiraData.NoiseAmount = NoiseAmountForSimulation;
            simulatedChiraData.FillCurrentFrame(CurrentFrame.Joints);

            // If neighter frame is tracked, set timestamp to zero
            CurrentFrame.Timestamp = 0;
            for (int i = 0; i < 2; i++)
            {
                if (CurrentFrame.IsTracked[i])
                {
                    CurrentFrame.Timestamp = DateTime.Now.Ticks;
                }
            }
        }

        private void ZeroJoints()
        {
            for (int i = 0; i < CurrentFrame.Joints.Length; i++)
            {
                CurrentFrame.Joints[i] = Vector3.zero;
            }
        }
#endif

    }
}