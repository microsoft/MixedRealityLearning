// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.EventDatum.Input;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers;
using Microsoft.MixedReality.Toolkit.SDK.UX;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos
{
    public class OpenKeyboard : MonoBehaviour
    {
        // For System Keyboard
        public TouchScreenKeyboard keyboard;
        public static string keyboardText = "";
        public TextMesh debugMessage;

        private void Update()
        {
            // System Keyboard
            if (TouchScreenKeyboard.visible == false && keyboard != null)
            {
                if (keyboard.status == TouchScreenKeyboard.Status.Done)
                {
                    keyboardText = keyboard.text;
                    debugMessage.text = keyboardText;
                    keyboard = null;
                }
            }
        }

        public void OpenSystemKeyboard()
        {
            keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false);
        }
    }
}