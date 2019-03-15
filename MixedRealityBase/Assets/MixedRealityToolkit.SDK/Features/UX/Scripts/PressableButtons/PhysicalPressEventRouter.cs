using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers;
using Microsoft.MixedReality.Toolkit.SDK.UX;
using System;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.SDK.UX.Interactable;
using UnityEngine.Events;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos
{
    ///<summary>
    /// This class exists to route <see cref="PhysicalButtonMovement"/>/<see cref="HandInteractionPress"/> events through to Interactable.
    /// The result is being able to have physical touch call Interactable.OnPointerClicked.
    /// If this class does not suit your needs, it is recommended to copy it and write a custom one with better handling for your product's use cases.
    ///</summary>
    public class PhysicalPressEventRouter : MonoBehaviour, IMixedRealityHandPressTriggerHandler
    {
        public Interactable routingTarget;

        /// This enum/the behavior it supports is far from finished.
        /// I HIGHLY recommend you only use EventOnClickCompletion.
        public enum PhysicalPressEventBehavior
        {
            EventOnClickCompletion = 0,
            EventOnPress,
            EventOnTouch
        }
        public PhysicalPressEventBehavior InteractableOnClick = PhysicalPressEventBehavior.EventOnClickCompletion;

        public AudioSource source;
        public AudioClip touchClip;
        public AudioClip pressClip;
        public AudioClip clickClip;

        public void OnHandPressTouched()
        {
            PlayClip(touchClip);

            routingTarget.SetPhysicalTouch(true);
            if (InteractableOnClick == PhysicalPressEventBehavior.EventOnTouch)
            {
                routingTarget.SetPress(true);
                routingTarget.OnPointerClicked(null);
                routingTarget.SetPress(false);
            }
        }

        public void OnHandPressUntouched()
        {
            if (InteractableOnClick == PhysicalPressEventBehavior.EventOnTouch)
            {
                routingTarget.SetPhysicalTouch(false);
                routingTarget.SetPress(true);
            }
        }

        public void OnHandPressTriggered()
        {
            PlayClip(pressClip);

            routingTarget.SetPhysicalTouch(true);
            routingTarget.SetPress(true);
            if (InteractableOnClick == PhysicalPressEventBehavior.EventOnPress)
            {
                routingTarget.OnPointerClicked(null);
            }
        }

        public void OnHandPressCompleted()
        {
            PlayClip(clickClip);

            routingTarget.SetPhysicalTouch(true);
            routingTarget.SetPress(true);
            if (InteractableOnClick == PhysicalPressEventBehavior.EventOnClickCompletion)
            {
                routingTarget.OnPointerClicked(null);
            }
            routingTarget.SetPress(false);
        }

        private void PlayClip(AudioClip clip)
        {
            if (source != null && clip != null)
            {
                source.PlayOneShot(clip);
            }
        }
    }
}