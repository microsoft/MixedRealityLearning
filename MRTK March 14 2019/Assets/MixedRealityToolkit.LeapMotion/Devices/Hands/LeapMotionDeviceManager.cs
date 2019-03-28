// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
using Leap;
using Leap.Unity;
using Leap.Unity.Attachments;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.Core.Utilities;
using UnityEngine;
#endif

using Microsoft.MixedReality.Toolkit.Core.Attributes;
using Microsoft.MixedReality.Toolkit.Core.Definitions;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Providers;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.MixedReality.Toolkit.LeapMotion.Devices.Hands
{
    [MixedRealityDataProvider(
        typeof(IMixedRealityInputSystem), 
        SupportedPlatforms.WindowsStandalone | SupportedPlatforms.WindowsEditor, 
        "Profiles/DefaultMixedRealityLeapMotionProfile.asset", "MixedRealityToolkit.LeapMotion")]
    public class LeapMotionDeviceManager : BaseDeviceManager, IMixedRealityExtensionService
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        private HandPool cachedHandPool;
        protected HandPool CachedHandPool
        {
            get
            {
                if (cachedHandPool == null && prefabInstance != null)
                {
                    cachedHandPool = prefabInstance.GetComponentInChildren<HandPool>();
                }
                return cachedHandPool;
            }
        }

        protected GameObject LeapMotionPrefab
        {
            get
            {
                if (ConfigurationProfile is LeapMotionDeviceManagerProfile leapMotionProfile)
                {
                    return leapMotionProfile.LeapMotionPrefab;
                }

                return null;
            }
        }
#endif

        private bool showHands = true;

        public bool ShowHands
        {
            get { return showHands; }
            set
            {
                showHands = value;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                UpdateHandPoolRendering();
#endif
            }
        }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        private GameObject handsInstance;
        private GameObject prefabInstance;
        private AttachmentHands attachmentHands;
        private AttachmentHand leftHand;
        private AttachmentHand rightHand;
        private LeapServiceProvider leapServiceProvider = null;
        private Controller leapController = null;

        private const float DeviceRefreshInterval = 0.01f;
        private float deviceRefreshTimer;

        private bool wasRightHandTracked, wasLeftHandTracked = false;
#endif

        /// <summary>
        /// Dictionary to capture all active hands detected
        /// </summary>
        private readonly Dictionary<Handedness, LeapMotionHand> trackedHands = new Dictionary<Handedness, LeapMotionHand>();

        /// <inheritdoc/>
        public override IMixedRealityController[] GetActiveControllers()
        {
            return trackedHands.Values.ToArray();
        }

        #region BaseDeviceManager Implementation

        public LeapMotionDeviceManager(string name, uint priority, BaseMixedRealityProfile profile) : base(name, priority, profile) { }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        /// <inheritdoc />
        public override void Initialize()
        {
            if (ConfigurationProfile is LeapMotionDeviceManagerProfile leapMotionProfile)
            {
                showHands = leapMotionProfile.ShowLeapCapsuleHands;
            }
        }

        /// <inheritdoc />
        public override void Enable()
        {
            if (LeapMotionPrefab == null)
            {
                Debug.LogError("Tried to use the LeapMotionHandManager, but no prefab was supplied.");
                return;
            }

            if (leapController == null)
            {
                leapController = new Controller();
            }

            if (leapController.IsConnected)
            {
                InternalSystemSetUp();
            }

            leapController.Device += LeapController_DeviceConnect;
            leapController.DeviceLost += LeapController_DeviceLost;
        }

        private void LeapController_DeviceConnect(object sender, DeviceEventArgs e)
        {
            InternalSystemSetUp();
        }

        private void LeapController_DeviceLost(object sender, DeviceEventArgs e)
        {
            InternalSystemCleanUp();
        }

        private void InternalSystemSetUp()
        {
            prefabInstance = Object.Instantiate(LeapMotionPrefab);

            leapServiceProvider = prefabInstance.GetComponentInChildren<LeapServiceProvider>();
            if (leapServiceProvider == null)
            {
                Debug.LogError("LeapMotionHandManager: No LeapServiceProvider found in LeapMotionPrefab.");
                Object.Destroy(prefabInstance);
                prefabInstance = null;
                return;
            }

            // These pieces of the default prefab are already handled by the MixedRealityCamera, so we need to disable them.
            var vrHeightOffsetComponent = prefabInstance.GetComponent<VRHeightOffset>();
            if (vrHeightOffsetComponent != null)
                vrHeightOffsetComponent.enabled = false;

            var leapVrCameraControlComponent = prefabInstance.GetComponentInChildren<LeapVRCameraControl>();
            if (leapVrCameraControlComponent != null)
                leapVrCameraControlComponent.enabled = false;

            var cameraComponent = prefabInstance.GetComponentInChildren<Camera>();
            if (cameraComponent != null)
                cameraComponent.enabled = false;

            var audioListenerComponent = prefabInstance.GetComponentInChildren<AudioListener>();
            if (audioListenerComponent != null)
                audioListenerComponent.enabled = false;

            handsInstance = new GameObject();
            attachmentHands = handsInstance.AddComponent<AttachmentHands>();

            foreach (AttachmentHand hand in attachmentHands.attachmentHands)
            {
                if (hand.chirality == Chirality.Left)
                {
                    leftHand = hand;
                }
                else
                {
                    rightHand = hand;
                }
            }

            handsInstance.name = "Leap Attachment Hands";
            handsInstance.transform.parent = prefabInstance.transform;

            prefabInstance.transform.SetPositionAndRotation(CameraCache.Main.transform.position, CameraCache.Main.transform.rotation);
            prefabInstance.transform.localScale = CameraCache.Main.transform.localScale;
            prefabInstance.transform.parent = CameraCache.Main.transform;
            UpdateHandPoolRendering();
        }

        private void InternalSystemCleanUp()
        {
            if (prefabInstance != null)
            {
                Object.Destroy(prefabInstance);
                prefabInstance = null;
            }

            if (handsInstance != null)
            {
                Object.Destroy(handsInstance);
                handsInstance = null;
            }

            leapServiceProvider = null;
            attachmentHands = null;
        }

        /// <inheritdoc />
        public override void Update()
        {
            if (prefabInstance == null)
            {
                return;
            }

            deviceRefreshTimer += Time.unscaledDeltaTime;

            if (deviceRefreshTimer >= DeviceRefreshInterval)
            {
                deviceRefreshTimer = 0.0f;
                RefreshDevices();
            }

            var hands = leapServiceProvider.CurrentFrame.Hands;

            for (int i = 0; i < hands.Count; i++)
            {
                LeapMotionHand hand = null;

                if (hands[i].IsLeft)
                {
                    hand = GetOrAddHand(Handedness.Left);
                }
                else if (hands[i].IsRight)
                {
                    hand = GetOrAddHand(Handedness.Right);
                }

                if (hand != null)
                {
                    hand.UpdateState(hands[i]);
                }
            }
        }

        /// <inheritdoc />
        public override void Disable()
        {
            InternalSystemCleanUp();

            if (leapController != null)
            {
                leapController.Device -= LeapController_DeviceConnect;
                leapController.DeviceLost -= LeapController_DeviceLost;
            }
        }
#endif // UNITY_EDITOR || UNITY_STANDALONE_WIN

        #endregion BaseDeviceManager Implementation

        #region LeapMotionHandManager Implementation

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        protected virtual void RefreshDevices()
        {
            var currentFrameHands = leapServiceProvider.CurrentFrame.Hands;

            bool isLeftHandTracked = false;
            bool isRightHandTracked = false;

            for (int i = 0; i < currentFrameHands.Count; i++)
            {
                if (currentFrameHands[i].IsLeft)
                {
                    isLeftHandTracked = true;

                    if (!wasLeftHandTracked)
                    {
                        var controller = GetOrAddHand(Handedness.Left);
                        if (controller != null)
                        {
                            MixedRealityToolkit.InputSystem?.RaiseSourceDetected(controller.InputSource, controller);
                            wasLeftHandTracked = true;
                        }
                    }
                }

                if (currentFrameHands[i].IsRight)
                {
                    isRightHandTracked = true;

                    if (!wasRightHandTracked)
                    {
                        var controller = GetOrAddHand(Handedness.Right);
                        if (controller != null)
                        {
                            MixedRealityToolkit.InputSystem?.RaiseSourceDetected(controller.InputSource, controller);
                            wasRightHandTracked = true;
                        }
                    }
                }
            }

            if (!isLeftHandTracked && wasLeftHandTracked)
            {
                var controller = GetOrAddHand(Handedness.Left);
                MixedRealityToolkit.InputSystem?.RaiseSourceLost(controller.InputSource, controller);
                trackedHands.Remove(Handedness.Left);

                wasLeftHandTracked = false;
            }

            if (!isRightHandTracked && wasRightHandTracked)
            {
                var controller = GetOrAddHand(Handedness.Right);
                MixedRealityToolkit.InputSystem?.RaiseSourceLost(controller.InputSource, controller);
                trackedHands.Remove(Handedness.Right);

                wasRightHandTracked = false;
            }
        }

        protected LeapMotionHand GetOrAddHand(Handedness handedness)
        {
            if (trackedHands.ContainsKey(handedness))
            {
                var hand = trackedHands[handedness];
                Debug.Assert(hand != null);
                return hand;
            }

            var pointers = RequestPointers(SupportedControllerType.ArticulatedHand, handedness);
            var inputSource = MixedRealityToolkit.InputSystem?.RequestNewGenericInputSource($"{handedness} Hand", pointers);
            var detectedController = new LeapMotionHand(TrackingState.NotTracked, handedness, inputSource);

            if (detectedController == null)
            {
                Debug.LogError($"Failed to create {typeof(LeapMotionHand).Name} controller");
                return null;
            }

            if (!detectedController.SetupConfiguration(typeof(LeapMotionHand), InputSourceType.Hand))
            {
                // Controller failed to be set up correctly.
                Debug.LogError($"Failed to set up {typeof(LeapMotionHand).Name} controller");
                // Return null so we don't raise the source detected.
                return null;
            }

            var leapController = detectedController as LeapMotionHand;

            if (leapController != null)
            {
                leapController.leapMotionDeviceManager = this;

                if (handedness == Handedness.Left)
                {
                    leapController.attachmentHand = leftHand;
                }
                else
                {
                    leapController.attachmentHand = rightHand;
                }
            }

            for (int i = 0; i < detectedController.InputSource?.Pointers?.Length; i++)
            {
                detectedController.InputSource.Pointers[i].Controller = detectedController;
            }

            trackedHands.Add(handedness, detectedController);
            return detectedController;
        }

        internal void SetLeapAttachmentFlags(AttachmentPointFlags jointToEnable)
        {
            attachmentHands.attachmentPoints = attachmentHands.attachmentPoints | jointToEnable;
        }

        private void UpdateHandPoolRendering()
        {
            if (CachedHandPool != null)
            {
                if (ShowHands)
                {
                    CachedHandPool.EnableGroup("Capsule Hands");
                }
                else
                {
                    CachedHandPool.DisableGroup("Capsule Hands");
                }
            }
        }
#endif

        #endregion LeapMotionHandManager Implementation
    }
}
