using HoloToolkit.Unity;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;
using UnityEngine.Events;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Managers
{
    public class ChatBotManager : MonoBehaviour
    {
        public bool IsListening { get; private set; }
        
        [Header("References")]
        [SerializeField]
        private SceneController sceneController;
        [SerializeField]
        private DictationHandler dictationHandler;
        [SerializeField]
        private TextToSpeechManager textToSpeechManager;
        [SerializeField]
        private string chatBotEndpoint;

        [Header("References")]
        [SerializeField]
        private UnityEvent onConversationStarted;
        [SerializeField]
        private UnityEvent onConversationFinished;
        
        private void Awake()
        {
            if (sceneController == null)
            {
                sceneController = FindObjectOfType<SceneController>();
            }
            
            dictationHandler.OnDictationComplete.AddListener(OnDictationComplete);
        }

        public void StartConversation()
        {
            if (IsListening)
            {
                return;
            }
            
            Debug.Log("Starting conversation with Bot.");
            IsListening = true;
            dictationHandler.StartRecording();
            onConversationStarted?.Invoke();
        }
        
        private void OnDictationComplete(string detectedDictation)
        {
            dictationHandler.StopRecording();
            IsListening = false;
            HandleDictation(detectedDictation);
        }

        private void HandleDictation(string sentence)
        {
            Debug.Log(sentence);
            ReturnResponse(sentence);
        }

        private void ReturnResponse(string response)
        {
            textToSpeechManager.SpeakText(response);
            onConversationFinished?.Invoke();
        }
    }
}
