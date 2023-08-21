// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Subsystems;
using Microsoft.MixedReality.Toolkit.UX;
using MRTK.Tutorials.AzureCloudServices.Scripts.BotDirectLine;
using MRTK.Tutorials.AzureCloudServices.Scripts.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Controller
{
    public class ChatBotController : MonoBehaviour
    {
        
        public bool IsListening { get; private set; }
        public bool IsSpeaking { get; private set; }
        
        [Header("References")]
        [SerializeField]
        private ChatBotManager chatBotManager = default;
        [SerializeField]
        private AudioSource speechAudioSource;

        [Header("UI Elements")]
        [SerializeField]
        private PressableButton dictationButton = default;
        [SerializeField]
        private TMP_Text messageLabel = default;
        
        [Header("Events")]
        [SerializeField]
        private UnityEvent onConversationStarted = default;
        [SerializeField]
        private UnityEvent onConversationFinished = default;

        private bool isPerformingInit;
        private string userId = Guid.NewGuid().ToString().Replace("-", "");
        private string conversationId;
        private List<string> processedMessages = new List<string>();

        private DictationSubsystem dictationSubsystem;
        private TextToSpeechSubsystem textToSpeechSubsystem;
        
        private void Awake()
        {
            chatBotManager.OnConversationStarted += HandleOnConversationStarted;
            chatBotManager.OnMessageSent += HandleOnMessageSent;
            chatBotManager.OnMessagesReceived += HandleOnMessagesReceived;

            dictationSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<DictationSubsystem>();
            if (dictationSubsystem != null)
            {
                dictationSubsystem.Recognized += OnDictationComplete;
            }

            textToSpeechSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<TextToSpeechSubsystem>();
        }

        private void OnEnable()
        {
            StartConversation();
        }

        private void OnDisable()
        {
            StopConversation();
        }

        public void StartConversation()
        {
            if (isPerformingInit)
            {
                return;
            }
            
            Debug.Log("Starting conversation with Bot.");
            isPerformingInit = true;
            dictationButton.enabled = false;
            messageLabel.text = "Starting conversation, please wait.";
            chatBotManager.StartConversation();
            onConversationStarted?.Invoke();
        }

        public void StopConversation()
        {
            IsListening = false;
            conversationId = null;
            processedMessages = new List<string>();

            onConversationFinished?.Invoke();
        }

        public void StartListening()
        {
            if (isPerformingInit || IsListening || IsSpeaking)
            {
                return;
            }

            IsListening = true;
            dictationButton.enabled = false;
            messageLabel.text = "Listening...";
            dictationSubsystem.StartDictation();
        }

        private void HandleOnConversationStarted(object sender, string id)
        {
            conversationId = id;
            isPerformingInit = false;
            var greetingMessage = "Greetings, I can help you with tracked objects. Just ask.";
            textToSpeechSubsystem.TrySpeak(greetingMessage, speechAudioSource);
            messageLabel.text = greetingMessage;
            dictationButton.enabled = true;
        }

        private void HandleOnMessagesReceived(object sender, IList<MessageActivity> messages)
        {
            StartCoroutine(HandleReceivedMessagesCoroutine(messages));
        }

        private void HandleOnMessageSent(object sender, string messageId)
        {
            chatBotManager.ReceiveMessages(conversationId);
        }

        IEnumerator HandleReceivedMessagesCoroutine(IList<MessageActivity> messages)
        {
            Debug.Log("HandleReceivedMessages");
            
            // Wait in case previous message are still being processed.
            while (IsSpeaking)
            {
                yield return new WaitForSeconds(0.5f);
            }
            
            IsSpeaking = true;
            messageLabel.text = String.Empty;

            var textToSpeach = new StringBuilder();

            foreach (var messageActivity in messages.Where(m => m.FromId != userId))
            {
                if (processedMessages.Contains(messageActivity.Id))
                {
                    continue;
                }
                if (messageActivity.Text.Contains("Greetings"))
                {
                    continue;
                }
                
                processedMessages.Add(messageActivity.Id);

                Debug.Log($"Appending message: {messageActivity.Text}");
                textToSpeach.AppendLine(messageActivity.Text);
            }
            
            Debug.Log($"Bot will say: {textToSpeach.ToString()}");
            messageLabel.text = textToSpeach.ToString();
            textToSpeechSubsystem.TrySpeak(textToSpeach.ToString(), speechAudioSource);
            
            do
            {
                yield return new WaitForSeconds(0.5f);
            } while (speechAudioSource.isPlaying);
            
            IsSpeaking = false;
            dictationButton.enabled = true;
        }

        private void OnDictationComplete(DictationResultEventArgs detectedDictation)
        {
            var resultingDictation = SanitizeDictation(detectedDictation.Result);

            Debug.Log($"Dictation received: {resultingDictation}");
            dictationSubsystem.StopDictation();
            messageLabel.text = "Ok, let me process that quickly.";
            Debug.Log($"Sending message to bot: {resultingDictation}");
            IsListening = false;
            chatBotManager.SentMessage(conversationId, userId, resultingDictation);
        }

        private string SanitizeDictation(string dictation)
        {
            dictation = dictation.Replace(".", "");
            if (dictation.EndsWith(" ") && dictation.Length > 2)
            {
                dictation = dictation.Remove(dictation.Length - 1, 1);
            }

            return dictation;
        }
        
    }
}
