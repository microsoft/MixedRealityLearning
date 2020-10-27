using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HoloToolkit.Unity;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
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
        
        [SerializeField]
        private ChatBotManager chatBotManager = default;
        
        [Header("References")]
        [SerializeField]
        private DictationHandler dictationHandler = default;
        [SerializeField]
        private TextToSpeechManager textToSpeechManager = default;

        [Header("UI Elements")]
        [SerializeField]
        private Interactable dictationButton = default;
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
        
        private void Awake()
        {
            chatBotManager.OnConversationStarted += HandleOnConversationStarted;
            chatBotManager.OnMessageSent += HandleOnMessageSent;
            chatBotManager.OnMessagesReceived += HandleOnMessagesReceived;
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
            dictationButton.IsEnabled = false;
            messageLabel.text = "Listening...";
            dictationHandler.StartRecording();
        }

        private void HandleOnConversationStarted(object sender, string id)
        {
            conversationId = id;
            isPerformingInit = false;
            var greetingMessage = "Greetings, I can help you with tracked objects. Just ask.";
            textToSpeechManager.SpeakText(greetingMessage);
            messageLabel.text = greetingMessage;
            dictationButton.IsEnabled = true;
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
            textToSpeechManager.SpeakText(textToSpeach.ToString());
            
            do
            {
                yield return new WaitForSeconds(0.5f);
            } while (textToSpeechManager.AudioSource.isPlaying);
            
            IsSpeaking = false;
            dictationButton.IsEnabled = true;
        }

        private void OnDictationComplete(string detectedDictation)
        {
            detectedDictation = SanitizeDictation(detectedDictation);

            Debug.Log($"Dictation received: {detectedDictation}");
            dictationHandler.StopRecording();
            messageLabel.text = "Ok, let me process that quickly.";
            Debug.Log($"Sending message to bot: {detectedDictation}");
            IsListening = false;
            chatBotManager.SentMessage(conversationId, userId, detectedDictation);
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
