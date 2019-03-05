// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces;
using Microsoft.MixedReality.Toolkit.Core.Providers;
using Microsoft.MixedReality.Toolkit.Core.Services;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Core.Devices.Hands
{
    public class SimulatedHandDeviceManager : BaseDeviceManager, IMixedRealityExtensionService
    {
        private GameObject prefabInstance;

        private bool wasRightHandTracked, wasLeftHandTracked = false;

        /// <summary>
        /// Dictionary to capture all active hands detected
        /// </summary>
        private readonly Dictionary<Handedness, SimulatedHand> trackedHands = new Dictionary<Handedness, SimulatedHand>();

        #region BaseDeviceManager Implementation

        public SimulatedHandDeviceManager(string name, uint priority, BaseMixedRealityProfile profile) : base(name, priority, profile)
        {
        }

        /// <inheritdoc />
        public override void Initialize()
        {
        }

        /// <inheritdoc />
        public override void Enable()
        {
            if (MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.HandTrackingProfile.SimulatedHandPrefab == null)
            {
                Debug.LogError("Tried to use the SimulatedHandDeviceManager, but no prefab was supplied.");
                return;
            }

            prefabInstance = Object.Instantiate(MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.HandTrackingProfile.SimulatedHandPrefab);

            if (SimulatedHandDataProvider.Instance != null)
            {
                SimulatedHandDataProvider.Instance.OnHandDataChanged += OnHandDataChanged;
            }
        }

        /// <inheritdoc />
        public override void Disable()
        {
            if (prefabInstance != null)
            {
                Object.Destroy(prefabInstance);
            }

            if (SimulatedHandDataProvider.Instance != null)
            {
                SimulatedHandDataProvider.Instance.OnHandDataChanged -= OnHandDataChanged;
            }
        }

        #endregion BaseDeviceManager Implementation

        private void OnHandDataChanged()
        {
            SimulatedHandData handDataLeft = SimulatedHandDataProvider.Instance?.CurrentFrameLeft;
            SimulatedHandData handDataRight = SimulatedHandDataProvider.Instance?.CurrentFrameRight;

            if (handDataLeft != null && handDataLeft.IsTracked)
            {
                SimulatedHand hand = GetOrAddHand(Handedness.Left);

                if (hand != null)
                {
                    if (!wasLeftHandTracked)
                    {
                        MixedRealityToolkit.InputSystem?.RaiseSourceDetected(hand.InputSource, hand);
                        wasLeftHandTracked = true;
                    }

                    hand.UpdateState(handDataLeft);
                }
            }
            else if (wasLeftHandTracked)
            {
                SimulatedHand hand = GetOrAddHand(Handedness.Left);

                if (hand != null)
                {
                    MixedRealityToolkit.InputSystem?.RaiseSourceLost(hand.InputSource, hand);
                    trackedHands.Remove(Handedness.Left);
                }

                wasLeftHandTracked = false;
            }

            if (handDataRight != null && handDataRight.IsTracked)
            {
                SimulatedHand hand = GetOrAddHand(Handedness.Right);

                if (hand != null)
                {
                    if (!wasRightHandTracked)
                    {
                        MixedRealityToolkit.InputSystem?.RaiseSourceDetected(hand.InputSource, hand);
                        wasRightHandTracked = true;
                    }

                    hand.UpdateState(handDataRight);
                }
            }
            else if (wasRightHandTracked)
            {
                SimulatedHand hand = GetOrAddHand(Handedness.Right);

                if (hand != null)
                {
                    MixedRealityToolkit.InputSystem?.RaiseSourceLost(hand.InputSource, hand);
                    trackedHands.Remove(Handedness.Right);
                }

                wasRightHandTracked = false;
            }
        }

        protected SimulatedHand GetOrAddHand(Handedness handedness)
        {
            if (trackedHands.ContainsKey(handedness))
            {
                var hand = trackedHands[handedness];
                Debug.Assert(hand != null);
                return hand;
            }

            var pointers = RequestPointers(typeof(SimulatedHand), handedness);
            var inputSource = MixedRealityToolkit.InputSystem?.RequestNewGenericInputSource($"{handedness} Hand", pointers);
            var detectedController = new SimulatedHand(TrackingState.NotTracked, handedness, inputSource);

            if (detectedController == null)
            {
                Debug.LogError($"Failed to create {typeof(SimulatedHand).Name} controller");
                return null;
            }

            if (!detectedController.SetupConfiguration(typeof(SimulatedHand), InputSourceType.Hand))
            {
                // Controller failed to be setup correctly.
                Debug.LogError($"Failed to Setup {typeof(SimulatedHand).Name} controller");
                // Return null so we don't raise the source detected.
                return null;
            }

            for (int i = 0; i < detectedController.InputSource?.Pointers?.Length; i++)
            {
                detectedController.InputSource.Pointers[i].Controller = detectedController;
            }

            trackedHands.Add(handedness, detectedController);
            return detectedController;
        }
    }
}