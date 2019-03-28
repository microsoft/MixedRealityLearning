// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using System;
using UnityEngine.EventSystems;

namespace Microsoft.MixedReality.Toolkit.Core.EventDatum.Input
{
    /// <summary>
    /// Describes an input event that involves keyword recognition.
    /// </summary>
    public class SpeechEventData : BaseInputEventData
    {
        /// <summary>
        /// The time it took for the phrase to be uttered.
        /// </summary>
        public TimeSpan PhraseDuration { get; private set; }

        /// <summary>
        /// The moment in UTC time when uttering of the phrase began.
        /// </summary>
        public DateTime PhraseStartTime { get; private set; }

        /// <summary>
        /// The text that was recognized.
        /// </summary>
        public SpeechCommands Command { get; private set; }

        /// <summary>
        /// A measure of correct recognition certainty.
        /// </summary>
        public RecognitionConfidenceLevel Confidence { get; private set; }

        /// <inheritdoc />
        public SpeechEventData(EventSystem eventSystem) : base(eventSystem) { }

        /// <summary>
        /// Populates the event with data.
        /// </summary>
        /// <param name="inputSource"></param>
        /// <param name="confidence"></param>
        /// <param name="phraseDuration"></param>
        /// <param name="phraseStartTime"></param>
        /// <param name="command"></param>
        public void Initialize(IMixedRealityInputSource inputSource, RecognitionConfidenceLevel confidence, TimeSpan phraseDuration, DateTime phraseStartTime, SpeechCommands command)
        {
            BaseInitialize(inputSource, command.Action);
            Confidence = confidence;
            PhraseDuration = phraseDuration;
            PhraseStartTime = phraseStartTime;
            Command = command;
        }
    }
}