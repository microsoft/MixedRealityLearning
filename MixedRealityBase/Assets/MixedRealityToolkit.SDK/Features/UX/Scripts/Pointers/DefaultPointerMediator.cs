// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using System.Collections.Generic;

namespace Microsoft.MixedReality.Toolkit.SDK.UX.Pointers
{
    public class DefaultPointerMediator : IMixedRealityPointerMediator
    {
        private readonly HashSet<IMixedRealityPointer> allPointers = new HashSet<IMixedRealityPointer>();
        private readonly HashSet<IMixedRealityPointer> farInteractPointers = new HashSet<IMixedRealityPointer>();
        private readonly HashSet<IMixedRealityNearPointer> nearInteractPointers = new HashSet<IMixedRealityNearPointer>();
        private readonly HashSet<IMixedRealityTeleportPointer> teleportPointers = new HashSet<IMixedRealityTeleportPointer>();
        private readonly Dictionary<IMixedRealityInputSource, HashSet<IMixedRealityPointer>> pointerByInputSourceParent = new Dictionary<IMixedRealityInputSource, HashSet<IMixedRealityPointer>>();
        private readonly HashSet<IMixedRealityPointer> unPrioritizedPointers = new HashSet<IMixedRealityPointer>();

        public void RegisterPointers(IMixedRealityPointer[] pointers)
        {
            for (int i = 0; i < pointers.Length; i++)
            {
                IMixedRealityPointer pointer = pointers[i];

                allPointers.Add(pointer);

                BaseControllerPointer basePointer = pointer as BaseControllerPointer;
                if (basePointer != null)
                    basePointer.SetActive(true);

                if (pointer is IMixedRealityTeleportPointer)
                {
                    teleportPointers.Add(pointer as IMixedRealityTeleportPointer);
                }
                else if (pointer is IMixedRealityNearPointer)
                {
                    nearInteractPointers.Add(pointer as IMixedRealityNearPointer);
                }
                else
                {
                    farInteractPointers.Add(pointer);
                }

                HashSet<IMixedRealityPointer> children;
                if (!pointerByInputSourceParent.TryGetValue(pointer.InputSourceParent, out children))
                {
                    children = new HashSet<IMixedRealityPointer>();
                    pointerByInputSourceParent.Add(pointer.InputSourceParent, children);
                }
                children.Add(pointer);
            }
        }

        public void UnregisterPointers(IMixedRealityPointer[] pointers)
        {
            for (int i = 0; i < pointers.Length; i++)
            {
                IMixedRealityPointer pointer = pointers[i];

                allPointers.Remove(pointer);
                farInteractPointers.Remove(pointer);
                nearInteractPointers.Remove(pointer as IMixedRealityNearPointer);
                teleportPointers.Remove(pointer as IMixedRealityTeleportPointer);

                foreach (HashSet<IMixedRealityPointer> siblingPointers in pointerByInputSourceParent.Values)
                {
                    siblingPointers.Remove(pointer);
                }
            }
        }

        public void UpdatePointers()
        {
            // If there's any teleportation going on, disable all pointers except the teleporter
            foreach (IMixedRealityTeleportPointer pointer in teleportPointers)
            {
                if (pointer.TeleportRequestRaised)
                {
                    BaseControllerPointer basePointer = pointer as BaseControllerPointer;
                    if (basePointer != null)
                        basePointer.SetActive(true);

                    foreach (IMixedRealityPointer otherPointer in allPointers)
                    {
                        if (otherPointer.PointerId == pointer.PointerId)
                            continue;

                        BaseControllerPointer otherBasePointer = otherPointer as BaseControllerPointer;
                        if (otherBasePointer != null)
                            otherBasePointer.SetActive(false);
                    }
                    // Don't do any further checks
                    return;
                }
            }
            
            unPrioritizedPointers.Clear();
            foreach (IMixedRealityPointer pointer in allPointers)
                unPrioritizedPointers.Add(pointer);
                        
            // If any pointers are locked, they have priority. Other pointers associated with the same controller are subordinate.
            foreach (IMixedRealityPointer pointer in allPointers)
            {
                if (pointer.IsFocusLocked)
                {
                    BaseControllerPointer basePointer = pointer as BaseControllerPointer;
                    if (basePointer != null)
                        basePointer.SetActive(true);

                    unPrioritizedPointers.Remove(pointer);

                    foreach (IMixedRealityPointer otherPointer in pointerByInputSourceParent[pointer.InputSourceParent])
                    {
                        if (!unPrioritizedPointers.Contains(otherPointer))
                            continue;

                        if (otherPointer.PointerId == pointer.PointerId)
                            continue;

                        BaseControllerPointer otherBasePointer = otherPointer as BaseControllerPointer;
                        if (otherBasePointer != null)
                            otherBasePointer.SetActive(false);

                        unPrioritizedPointers.Remove(otherPointer);
                    }
                }
            }

            // Check for near and far interactions
            // Any far interact pointers become disabled when a near pointer is near an object
            foreach (IMixedRealityNearPointer pointer in nearInteractPointers)
            {
                if (!unPrioritizedPointers.Contains(pointer))
                    continue;

                if (pointer.IsNearObject)
                {
                    BaseControllerPointer basePointer = pointer as BaseControllerPointer;
                    if (basePointer != null)
                        basePointer.SetActive(true);

                    unPrioritizedPointers.Remove(pointer);
                    foreach (IMixedRealityPointer otherPointer in pointerByInputSourceParent[pointer.InputSourceParent])
                    {
                        if (otherPointer == pointer)
                            continue;

                        if (!unPrioritizedPointers.Contains(otherPointer))
                            continue;

                        BaseControllerPointer otherBasePointer = otherPointer as BaseControllerPointer;
                        if (otherBasePointer != null)
                            otherBasePointer.SetActive(false);

                        unPrioritizedPointers.Remove(otherPointer);
                    }
                }
            }

            // If we have any pointers whose priority has not been assigned, set them to none
            foreach (IMixedRealityPointer unassignedPointer in unPrioritizedPointers)
            {
                BaseControllerPointer unassignedBasePointer = unassignedPointer as BaseControllerPointer;
                if (unassignedBasePointer != null)
                    unassignedBasePointer.SetActive(true);
            }
        }
    }
}