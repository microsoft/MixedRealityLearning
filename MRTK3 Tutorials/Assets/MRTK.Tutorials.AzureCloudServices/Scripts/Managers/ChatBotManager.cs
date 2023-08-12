using System;
using System.Collections.Generic;
using MRTK.Tutorials.AzureCloudServices.Scripts.BotDirectLine;
using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Managers
{
    public class ChatBotManager : MonoBehaviour
    {
        /// <summary>
        /// Conversation started event with conversation id.
        /// </summary>
        public event EventHandler<string> OnConversationStarted;
        
        /// <summary>
        /// Message sent event with message id.
        /// </summary>
        public event EventHandler<string> OnMessageSent;
        
        /// <summary>
        /// Messages received event with MessageActivity objects.
        /// </summary>
        public event EventHandler<IList<MessageActivity>> OnMessagesReceived;
        
        [SerializeField]
        private string directLineSecretKey = default;
        
        private void Awake()
        {
            BotDirectLineManager.Initialize(directLineSecretKey);
            BotDirectLineManager.Instance.BotResponse += HandleBotResponse;
        }

        public void StartConversation()
        {
            StartCoroutine(BotDirectLineManager.Instance.StartConversationCoroutine());
        }

        public void ReceiveMessages(string conversationId)
        {
            StartCoroutine(BotDirectLineManager.Instance.GetMessagesCoroutine(conversationId));
        }

        public void SentMessage(string conversationId, string userId, string message)
        {
            StartCoroutine(BotDirectLineManager.Instance.SendMessageCoroutine(conversationId, userId, message));
        }
        
        private void HandleBotResponse(object sender, BotResponseEventArgs e)
        {
            Debug.Log($"Response from Bot of type: {e.EventType}");

            switch (e.EventType)
            {
                case EventTypes.None:
                    break;
                case EventTypes.ConversationStarted:
                    OnConversationStarted?.Invoke(this, e.ConversationId);
                    break;
                case EventTypes.MessageSent:
                    OnMessageSent?.Invoke(this, e.SentMessageId);
                    break;
                case EventTypes.MessageReceived:
                    OnMessagesReceived?.Invoke(this, e.Messages);
                    break;
                case EventTypes.Error:
                    break;
            }
        }
    }
}
