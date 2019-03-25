// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Devices.Hands;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Services;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.SDK.UX.PressableButtons
{
    ///<summary>
    /// This class handles the glow splash for the Touch Button Cages.
    /// It detects collision events by evaluating either the MeshRenderer or a targeted buttonCollider.
    ///<summary>
    public class ButtonGlowSplashController : MonoBehaviour
    {
        public float Duration = 1.0f;
        public bool Enable_Tap = true;
        public Vector3 Active_Face_Dir = new Vector3(0.0f, 0.0f, 1.0f);
        public float Tip_Offset = 0.02f;
        public float Pulse_Size = 0.1f;
        public AudioClip Tap_Sound = null;

        private bool pulseActiveLeft = false;
        private float startTimeLeft = 0.0f;

        private bool pulseActiveRight = false;
        private float startTimeRight = 0.0f;
        private float initNearSize = 0.0f;

        private bool lastLeftTipInBounds, lastRightTipInBounds;
        private Material material;
        [SerializeField]
        private Collider buttonCollider;

        void Start()
        {
            pulseActiveLeft = false;
            pulseActiveRight = false;
            material = gameObject.GetComponent<Renderer>().material;
            if (buttonCollider == null)
            {
                buttonCollider = gameObject.GetComponent<Collider>();
            }

            lastLeftTipInBounds = false;
            lastRightTipInBounds = false;
            if (material)
            {
                initNearSize = material.GetFloat("_Blob_Near_Size_");
            }
        }

        bool PointInBounds(Vector3 target)
        {
            Vector3 localPosition = transform.InverseTransformPoint(target);
            Mesh mesh = GetComponent<MeshFilter>().mesh;
            if (buttonCollider)
            {
                return buttonCollider.bounds.Contains(target);
            }
            else if (mesh)
            {
                return mesh.bounds.Contains(localPosition);
            }
            else
            {
                return false;
            }
        }

        private bool AnimatePulse(float startTime, string pulseParmName, string fadeParmName, string sizeParmName)
        {
            float t = (Time.time - startTime) / Duration;
            if (t < 0.5f)
            {
                material.SetFloat(pulseParmName, t * 2.0f);
                float s = Mathf.Lerp(initNearSize, Pulse_Size, t * 2.0f);
                material.SetFloat(sizeParmName, s);
            }
            else if (t < 1.0f)
            {
                material.SetFloat(pulseParmName, 0.0f);
                material.SetFloat(fadeParmName, (2.0f * t - 1.0f));
                material.SetFloat(sizeParmName, initNearSize);
            }
            else
            {
                material.SetFloat(fadeParmName, 1.0f);
                return false;
            }
            return true;
        }

        private void PlayTapSound(Vector3 location)
        {
            if (Tap_Sound)
            {
                AudioSource.PlayClipAtPoint(Tap_Sound, location);
            }
        }

        void Update()
        {
            HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Left, out MixedRealityPose leftTip);
            HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Right, out MixedRealityPose rightTip);

            bool leftTipInBounds = false;
            bool rightTipInBounds = false;
            leftTipInBounds = PointInBounds(leftTip.Position);
            rightTipInBounds = PointInBounds(rightTip.Position);

            if (pulseActiveLeft)
            {
                pulseActiveLeft = AnimatePulse(startTimeLeft, "_Blob_Pulse_", "_Blob_Fade_", "_Blob_Near_Size_");
            }
            else
            {
                pulseActiveLeft = (leftTipInBounds && !lastLeftTipInBounds);
                if (pulseActiveLeft)
                {
                    startTimeLeft = Time.time;
                    PlayTapSound(leftTip.Position);
                }
            }

            if (pulseActiveRight)
            {
                pulseActiveRight = AnimatePulse(startTimeRight, "_Blob_Pulse_2_", "_Blob_Fade_2_", "_Blob_Near_Size_2_");
            }
            else
            {
                pulseActiveRight = (rightTipInBounds && !lastRightTipInBounds);
                if (pulseActiveRight)
                {
                    startTimeRight = Time.time;
                    PlayTapSound(rightTip.Position);
                }
            }

            lastLeftTipInBounds = leftTipInBounds;
            lastRightTipInBounds = rightTipInBounds;
        }
    }
}