// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem
{
    /// <summary>
    /// Interface for handling groups of pointers and their relationships.
    /// </summary>
    public interface IMixedRealityPointerBehavior
    {
        void RegisterPointers(IMixedRealityPointer[] pointer);

        void UpdatePointers();
    }
}