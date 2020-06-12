using MRTK.Tutorials.AzureCloudPower.Managers;
using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Managers
{
    public class MainSceneManager : MonoBehaviour
    {
        public DataManager DataManager => dataManager;
        public ObjectDetectionManager ObjectDetectionManager => objectDetectionManager;
        
        [SerializeField]
        private DataManager dataManager;
        [SerializeField]
        private ObjectDetectionManager objectDetectionManager;
    }
}
