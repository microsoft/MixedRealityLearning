// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.Physics;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.Services.InputSystem;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.SDK.UX.Pointers
{
    public class PokePointer : BaseControllerPointer, IMixedRealityNearPointer
    {
        // TODO: Finalize visuals. Model them after Shell.
        // TODO: Prevent back-poking.
        // TODO: Handle fast-poking.
        // TODO: Tweak triggering and debouncing to feel good, ideally model after Shell.

        [SerializeField]
        protected float distBack;

        [SerializeField]
        protected float distFront;

        [SerializeField]
        protected float debounceThreshold;

        [SerializeField]
        protected Transform triggerVisual;

        [SerializeField]
        protected LineRenderer line;

        [SerializeField]
        protected GameObject visuals;

        protected void OnValidate()
        {
            Debug.Assert(distBack > 0, this);
            Debug.Assert(distFront > 0, this);
            Debug.Assert(debounceThreshold > 0, this);
            Debug.Assert(triggerVisual != null, this);
            Debug.Assert(line != null, this);
            Debug.Assert(visuals != null, this);
        }

        public bool IsNearObject { get; set; } = false;

        protected bool isDown = false;

        public override void OnPreRaycast()
        {
            if (Rays == null)
            {
                Rays = new RayStep[1];
            }

            // Get pointer position
            Vector3 pointerPosition;
            TryGetPointerPosition(out pointerPosition);

            // Check proximity
            NearInteractionTouchable closestProximity = null;
            {
                float closestDist = distFront; // NOTE: Start at distFront for cutoff
                foreach (var prox in NearInteractionTouchable.Instances)
                {
                    float dist = prox.DistanceToSurface(pointerPosition);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestProximity = prox;
                    }
                }
            }
            IsNearObject = closestProximity != null;
            SetActive(IsNearObject);
            visuals.SetActive(IsNearObject);

            // Determine ray direction
            Vector3 rayDirection = PointerDirection;
            if (closestProximity != null)
            {
                rayDirection = -closestProximity.Forward;
            }

            // Build ray
            Vector3 start = pointerPosition - distBack * rayDirection;
            Vector3 end = pointerPosition + distFront * rayDirection;

            Rays[0] = new RayStep(start, end);

            triggerVisual.position = pointerPosition;

            line.SetPosition(0, pointerPosition);
            line.SetPosition(1, end);
        }

        public override void OnPostRaycast()
        {
            bool debounceIsDown = false;
            bool debounceIsUp = false;

            // Determine current state of up/down
            if (IsNearObject && (Result?.Details.Object != null))
            {
                float dist = Vector3.Distance(Result.StartPoint, Result.Details.Point) - distBack;

                // Determine if the touch is up or down or unknown
                if (dist > debounceThreshold)
                {
                    debounceIsUp = true;
                }
                else
                {
                    debounceIsDown = true;
                }
            }
            else
            {
                debounceIsUp = true;
            }

            // Determine changes
            bool newIsDown = isDown;

            if (debounceIsDown)
                newIsDown = true;

            if (debounceIsUp)
                newIsDown = false;

            // TODO: consider whether these events should come with configurable input action.
            // Send change events
            if (isDown && !newIsDown)
            {
                MixedRealityToolkit.InputSystem?.RaisePointerUp(this, Core.Definitions.InputSystem.MixedRealityInputAction.None);
            }
            else if (!isDown && newIsDown)
            {
                MixedRealityToolkit.InputSystem?.RaisePointerDown(this, Core.Definitions.InputSystem.MixedRealityInputAction.None);
            }

            isDown = newIsDown;

            if (!IsNearObject)
            {
                line.endColor = line.startColor = new Color(1, 1, 1, 0.25f);
            }
            else if (!isDown)
            {
                line.endColor = line.startColor = new Color(1, 1, 1, 0.75f);
            }
            else
            {
                line.endColor = line.startColor = new Color(0, 0, 1, 0.75f);
            }
        }

        public bool TryGetNearGraspPoint(out Vector3 position)
        {
            position = Vector3.zero;
            return false;
        }
    }
}
