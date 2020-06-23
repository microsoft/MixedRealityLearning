using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HoloToolkit.Unity;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using MRTK.Tutorials.AzureCloudServices.Scripts.BotDirectLine;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Managers
{
    public class ChatBotManager : MonoBehaviour
    {
        public bool IsListening { get; private set; }
        public bool IsSpeaking { get; private set; }
        
        [SerializeField]
        private string chatBotDirectLineSecretKey;
        
        [Header("References")]
        [SerializeField]
        private DictationHandler dictationHandler;
        [SerializeField]
        private TextToSpeechManager textToSpeechManager;

        [Header("UI Elements")]
        [SerializeField]
        private Interactable dictationButton;
        [SerializeField]
        private TMP_Text messageLabel;
        
        [Header("Events")]
        [SerializeField]
        private UnityEvent onConversationStarted;
        [SerializeField]
        private UnityEvent onConversationFinished;

        private bool isPerformingInit;
        private string userId = Guid.NewGuid().ToString().Replace("-", "");
        private string conversationId;
        private List<string> processedMessages = new List<string>();
        
        private void Awake()
        {
            BotDirectLineManager.Initialize(chatBotDirectLineSecretKey);
            BotDirectLineManager.Instance.BotResponse += HandleBotResponse;
            dictationHandler.OnDictationComplete.AddListener(OnDictationComplete);
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
            dictationButton.IsEnabled = false;
            messageLabel.text = "Starting conversation, please wait.";
            StartCoroutine(BotDirectLineManager.Instance.StartConversationCoroutine());
            onConversationStarted?.Invoke();
        }

        public void StopConversation()
        {
            // if (IsListening)
            // {
            //     return;
            // }
            
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
            dictationButton.IsEnabled = false;
            messageLabel.text = "Listening...";
            dictationHandler.StartRecording();
        }

        private void HandleBotResponse(object sender, BotResponseEventArgs e)
        {
            Debug.Log($"Response from Bot of type: {e.EventType}");

            switch (e.EventType)
            {
                case EventTypes.None:
                    break;
                case EventTypes.ConversationStarted:
                    conversationId = e.ConversationId;
                    isPerformingInit = false;
                    StartListening();
                    break;
                case EventTypes.MessageSent:
                    StartCoroutine(BotDirectLineManager.Instance.GetMessagesCoroutine(conversationId));
                    break;
                case EventTypes.MessageReceived:
                    StartCoroutine(HandleReceivedMessagesCoroutine(e.Messages));
                    break;
                case EventTypes.Error:
                    break;
            }
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

            foreach (var messageActivity in messages.Where(m => m.FromId != userId))
            {
                if (processedMessages.Contains(messageActivity.Id))
                {
                    continue;
                }
                processedMessages.Add(messageActivity.Id);

                while (textToSpeechManager.AudioSource.isPlaying)
                {
                    yield return new WaitForSeconds(0.5f);
                }
                
                Debug.Log($"Start speaking message: {messageActivity.Text}");
                textToSpeechManager.SpeakText(messageActivity.Text);
                messageLabel.text += $"\nBot: {messageActivity.Text}";

                while (textToSpeechManager.AudioSource.isPlaying)
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }
            
            IsSpeaking = false;
            dictationButton.IsEnabled = true;
        }

        private void OnDictationComplete(string detectedDictation)
        {
            Debug.Log($"Dictation received: {detectedDictation}");
            dictationHandler.StopRecording();
            messageLabel.text = "Ok, let me process that quickly.";
            Debug.Log($"Sending message to bot: {detectedDictation}");
            IsListening = false;
            StartCoroutine(BotDirectLineManager.Instance.SendMessageCoroutine(conversationId, userId, detectedDictation));
        }
    }
}
