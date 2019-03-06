// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.MixedReality.Toolkit.SDK.UX.Pointers
{
    public class DefaultPointerBehavior : IMixedRealityPointerBehavior
    {
        private readonly List<IMixedRealityPointer> farInteractPointers = new List<IMixedRealityPointer>(0);
        private readonly List<IMixedRealityNearPointer> nearInteractPointers = new List<IMixedRealityNearPointer>(0);

        public void RegisterPointers(IMixedRealityPointer[] pointers)
        {
            for (int i = 0; i < pointers.Length; i++)
            {
                if (pointers[i] is IMixedRealityTeleportPointer)
                {
                    continue;
                }
                else if (pointers[i] is IMixedRealityNearPointer)
                {
                    nearInteractPointers.Add(pointers[i] as IMixedRealityNearPointer);
                }
                else
                {
                    farInteractPointers.Add(pointers[i]);
                }
            }
        }

        public void UpdatePointers()
        {
            bool nearPointerNearObject = false;

            bool isAnyPointerLocked = farInteractPointers.Any(p => p.IsFocusLocked) || nearInteractPointers.Any(p => p.IsFocusLocked);

            if (isAnyPointerLocked)
            {
                // Don't disable any far interaction pointers if an interaction is active (the focus is locked)
                // to avoid incorrectly clearing out pointer data while an interaction is in progress.
                return;
            }

            foreach (IMixedRealityNearPointer pointer in nearInteractPointers)
            {
                nearPointerNearObject = nearPointerNearObject || pointer.IsNearObject;
            }

            foreach (IMixedRealityPointer pointer in farInteractPointers)
            {
                if (pointer is BaseControllerPointer)
                {
                    ((BaseControllerPointer)pointer).SetActive(!nearPointerNearObject);
                }
            }
        }
    }
}