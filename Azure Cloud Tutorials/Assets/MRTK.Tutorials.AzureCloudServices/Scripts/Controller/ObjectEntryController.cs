using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.UI;
using MRTK.Tutorials.AzureCloudServices.Scripts.Domain;
using MRTK.Tutorials.AzureCloudServices.Scripts.Managers;
using TMPro;
using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Controller
{
    public class ObjectEntryController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private SceneController sceneController;
        [SerializeField]
        private GameObject searchObjectPanel;
        [SerializeField]
        private ObjectEditController objectEditPanel;
        [SerializeField]
        private ObjectCardViewController objectCardPrefab;
        [Header("UI Elements")]
        [SerializeField]
        private ButtonConfigHelper submitButtonConfigHelper;
        [SerializeField]
        private TMP_Text hintLabel;
        [SerializeField]
        private TMP_InputField inputField;
        [SerializeField]
        private string loadingText = "Please wait...";
        [SerializeField]
        private Interactable[] buttons;
        
        private bool isInSearchMode;
        
        private void Awake()
        {
            if (sceneController == null)
            {
                sceneController = FindObjectOfType<SceneController>();
            }
        }

        private void OnEnable()
        {
            inputField.text = "";
        }

        public void SetSearchMode(bool searchModeActive)
        {
            isInSearchMode = searchModeActive;
            submitButtonConfigHelper.MainLabelText = isInSearchMode ? "Search Object" : "Set Object";
        }

        public async void SubmitQuery()
        {
            if (string.IsNullOrWhiteSpace(inputField.text))
            {
                hintLabel.SetText("Please type in a name.");
                hintLabel.gameObject.SetActive(true);
                return;
            }

            if (!sceneController.DataManager.IsReady)
            {
                hintLabel.SetText("No connection to the database!");
                hintLabel.gameObject.SetActive(true);
                return;
            }

            SetButtonsInteractiveState(false);
            if (isInSearchMode)
            {
                var project = await FindObject(inputField.text);
                if (project != null)
                {
                    searchObjectPanel.SetActive(false);
                    var objectCard = Instantiate(objectCardPrefab, transform.position, transform.rotation);
                    objectCard.Init(project);
                }
            }
            else
            {
                var project = await CreateObject(inputField.text);
                if (project != null)
                {
                    searchObjectPanel.SetActive(false);
                    objectEditPanel.gameObject.SetActive(true);
                    objectEditPanel.Init(project);
                }
            }
            SetButtonsInteractiveState(true);
        }

        private async Task<TrackedObject> FindObject(string searchName)
        {
            hintLabel.SetText(loadingText);
            hintLabel.gameObject.SetActive(true);
            var projectFromDb = await sceneController.DataManager.FindTrackedObjectByName(searchName);
            if (projectFromDb == null)
            {
                hintLabel.SetText($"No object found with the name '{searchName}'.");
                return null;
            }

            hintLabel.gameObject.SetActive(false);
            return projectFromDb;
        }

        private async Task<TrackedObject> CreateObject(string searchName)
        {
            hintLabel.SetText(loadingText);
            hintLabel.gameObject.SetActive(true);
            var trackedObject = await sceneController.DataManager.FindTrackedObjectByName(searchName);
            if (trackedObject == null)
            {
                trackedObject = new TrackedObject(searchName);
                var success = await sceneController.DataManager.UploadOrUpdate(trackedObject);
                if (!success)
                {
                    return null;
                }
                
                await sceneController.DataManager.UploadOrUpdate(trackedObject);
            }

            hintLabel.gameObject.SetActive(false);
            return trackedObject;
        }
        
        private void SetButtonsInteractiveState(bool state)
        {
            foreach (var interactable in buttons)
            {
                interactable.IsEnabled = state;
            }
        }
    }
}
