using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Managers
{
    public class ChatBotManager : MonoBehaviour
    {
        public bool IsListening { get; private set; }
        
        [SerializeField]
        private SceneController sceneController;
        [SerializeField]
        private string botWebResource;
        
        private void Awake()
        {
            if (sceneController == null)
            {
                sceneController = FindObjectOfType<SceneController>();
            }
        }

        public void StartConversation()
        {
            if (IsListening)
            {
                return;
            }
            
            Debug.Log("Starting conversation with Bot.");
            IsListening = true;
        }
    }
}
