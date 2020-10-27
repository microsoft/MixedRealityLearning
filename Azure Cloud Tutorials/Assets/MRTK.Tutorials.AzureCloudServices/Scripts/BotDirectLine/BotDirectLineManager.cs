using System;
using System.Collections;
using System.Text;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.BotDirectLine
{
    public class BotDirectLineManager
    {
        private const string DirectLineV3ApiUriPrefix = "https://directline.botframework.com/v3/directline";
        private const string DirectLineConversationsApiUri = DirectLineV3ApiUriPrefix + "/conversations";
        private const string DirectLineActivitiesApiUriPostfix = "activities";
        private const string DirectLineChannelId = "directline";

        private enum WebRequestMethods
        {
            Get,
            Post
        }

        public event EventHandler<BotResponseEventArgs> BotResponse;

        private static BotDirectLineManager _instance;
        public static BotDirectLineManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BotDirectLineManager();
                }

                return _instance;
            }
        }

        public bool IsInitialized
        {
            get;
            private set;
        }

        private string SecretKey
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        private BotDirectLineManager()
        {
            IsInitialized = false;
        }

        /// <summary>
        /// Initializes this instance by setting the bot secret.
        /// </summary>
        /// <param name="secretKey">The secret key of the bot.</param>
        public static void Initialize(string secretKey)
        {
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new ArgumentException("Secret key cannot be null or empty");
            }

            BotDirectLineManager instance = Instance;
            instance.SecretKey = secretKey;
            instance.IsInitialized = true;
        }

        /// <summary>
        /// Starts a new conversation with the bot.
        /// </summary>
        /// <returns></returns>
        public IEnumerator StartConversationCoroutine()
        {
            if (IsInitialized)
            {
                UnityWebRequest webRequest = CreateWebRequest(WebRequestMethods.Post, DirectLineConversationsApiUri);

                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError)
                {
                    Debug.Log("Web request failed: " + webRequest.error);
                }
                else
                {
                    string responseAsString = webRequest.downloadHandler.text;

                    if (!string.IsNullOrEmpty(responseAsString))
                    {
                        BotResponseEventArgs eventArgs = CreateBotResponseEventArgs(responseAsString);

                        if (BotResponse != null)
                        {
                            BotResponse.Invoke(this, eventArgs);
                        }
                    }
                    else
                    {
                        Debug.Log("Received an empty response");
                    }
                }
            }
            else
            {
                Debug.Log("Bot Direct Line manager is not initialized");
                yield return null;
            }
        }

        /// <summary>
        /// Sends the given message to the given conversation.
        /// </summary>
        /// <param name="conversationId">The conversation ID.</param>
        /// <param name="fromId">The ID of the sender.</param>
        /// <param name="message">The message to sent.</param>
        /// <param name="fromName">The name of the sender (optional).</param>
        /// <returns></returns>
        public IEnumerator SendMessageCoroutine(string conversationId, string fromId, string message, string fromName = null)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentException("Conversation ID cannot be null or empty");
            }

            if (IsInitialized)
            {
                Debug.Log("SendMessageCoroutine: " + conversationId + "; " + message);

                var body = new MessageActivity(fromId, message, DirectLineChannelId, null, fromName).ToJsonString();
                body = body.Replace("\"channelId\": \"directline\", ", "\"locale\": \"en-EN\", ");
                UnityWebRequest webRequest = CreateWebRequest(
                    WebRequestMethods.Post,
                    DirectLineConversationsApiUri
                    + "/" + conversationId
                    + "/" + DirectLineActivitiesApiUriPostfix,
                    body);

                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError)
                {
                    Debug.Log("Web request failed: " + webRequest.error);
                }
                else
                {
                    string responseAsString = webRequest.downloadHandler.text;

                    if (!string.IsNullOrEmpty(responseAsString))
                    {
                        //Debug.Log("Received response:\n" + responseAsString);
                        BotResponseEventArgs eventArgs = CreateBotResponseEventArgs(responseAsString);

                        if (BotResponse != null)
                        {
                            BotResponse.Invoke(this, eventArgs);
                        }
                    }
                    else
                    {
                        Debug.Log("Received an empty response");
                    }
                }
            }
            else
            {
                Debug.Log("Bot Direct Line manager is not initialized");
                yield return null;
            }
        }

        /// <summary>
        /// Retrieves the activities of the given conversation.
        /// </summary>
        /// <param name="conversationId">The conversation ID.</param>
        /// <param name="watermark">Indicates the most recent message seen (optional).</param>
        /// <returns></returns>
        public IEnumerator GetMessagesCoroutine(string conversationId, string watermark = null)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentException("Conversation ID cannot be null or empty");
            }

            if (IsInitialized)
            {
                Debug.Log("GetMessagesCoroutine: " + conversationId);

                string uri = DirectLineConversationsApiUri
                             + "/" + conversationId
                             + "/" + DirectLineActivitiesApiUriPostfix;

                if (!string.IsNullOrEmpty(watermark))
                {
                    uri += "?" + BotJsonProtocol.KeyWatermark + "=" + watermark;
                }

                UnityWebRequest webRequest = CreateWebRequest(WebRequestMethods.Get, uri);
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError)
                {
                    Debug.Log("Web request failed: " + webRequest.error);
                }
                else
                {
                    string responseAsString = webRequest.downloadHandler.text;

                    if (!string.IsNullOrEmpty(responseAsString))
                    {
                        //Debug.Log("Received response:\n" + responseAsString);
                        BotResponseEventArgs eventArgs = CreateBotResponseEventArgs(responseAsString);

                        if (BotResponse != null)
                        {
                            BotResponse.Invoke(this, eventArgs);
                        }
                    }
                    else
                    {
                        Debug.Log("Received an empty response");
                    }
                }
            }
            else
            {
                Debug.Log("Bot Direct Line manager is not initialized");
                yield return null;
            }
        }

        /// <summary>
        /// Creates a new UnityWebRequest instance initialized with bot authentication and JSON content type.
        /// </summary>
        /// <param name="webRequestMethod">Defines whether to use GET or POST method.</param>
        /// <param name="uri">The request URI.</param>
        /// <param name="content">The content to post (expecting JSON as UTF-8 encoded string or null).</param>
        /// <returns>A newly created UnityWebRequest instance.</returns>
        private UnityWebRequest CreateWebRequest(WebRequestMethods webRequestMethod, string uri, string content = null)
        {
            Debug.Log("CreateWebRequest: " + webRequestMethod + "; " + uri + (string.IsNullOrEmpty(content) ? "" : ("; " + content)));

            UnityWebRequest webRequest = new UnityWebRequest();

            webRequest.url = uri;
            webRequest.SetRequestHeader("Authorization", "Bearer " + SecretKey);

            if (webRequestMethod == WebRequestMethods.Get)
            {
                webRequest.method = "GET";
            }
            else
            {
                webRequest.method = "POST";
            }

            if (!string.IsNullOrEmpty(content))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(Utf8StringToByteArray(content));
            }

            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            return webRequest;
        }

        /// <summary>
        /// Creates a new BotResponseEventArgs instance based on the given response.
        /// </summary>
        /// <param name="responseAsString"></param>
        /// <returns></returns>
        private BotResponseEventArgs CreateBotResponseEventArgs(string responseAsString)
        {
            if (string.IsNullOrEmpty(responseAsString))
            {
                throw new ArgumentException("Response cannot be null or empty");
            }

            JSONNode responseJsonRootNode = JSONNode.Parse(responseAsString);
            JSONNode jsonNode = null;
            BotResponseEventArgs eventArgs = new BotResponseEventArgs();

            if ((jsonNode = responseJsonRootNode[BotJsonProtocol.KeyError]) != null)
            {
                eventArgs.EventType = EventTypes.Error;
                eventArgs.Code = jsonNode[BotJsonProtocol.KeyCode];
                string message = jsonNode[BotJsonProtocol.KeyMessage];

                if (!string.IsNullOrEmpty(message))
                {
                    eventArgs.Message = message;
                }
            }
            else if (responseJsonRootNode[BotJsonProtocol.KeyConversationId] != null)
            {
                eventArgs.EventType = EventTypes.ConversationStarted;
                eventArgs.ConversationId = responseJsonRootNode[BotJsonProtocol.KeyConversationId];
            }
            else if (responseJsonRootNode[BotJsonProtocol.KeyId] != null)
            {
                eventArgs.EventType = EventTypes.MessageSent;
                eventArgs.SentMessageId = responseJsonRootNode[BotJsonProtocol.KeyId];
            }
            else if ((jsonNode = responseJsonRootNode[BotJsonProtocol.KeyActivities]) != null)
            {
                eventArgs.EventType = EventTypes.MessageReceived;
                eventArgs.Watermark = responseJsonRootNode[BotJsonProtocol.KeyWatermark];
                JSONArray jsonArray = jsonNode.AsArray;

                foreach (JSONNode activityNode in jsonArray)
                {
                    MessageActivity messageActivity = MessageActivity.FromJson(activityNode);
                    eventArgs.Messages.Add(messageActivity);
                }
            }

            return eventArgs;
        }

        private byte[] Utf8StringToByteArray(string stringToBeConverted)
        {
            return Encoding.UTF8.GetBytes(stringToBeConverted);
        }
    }
}