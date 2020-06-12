using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.UI;
using MRTK.Tutorials.AzureCloudPower.Domain;
using MRTK.Tutorials.AzureCloudServices.Scripts.Managers;
using TMPro;
using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Controller
{
    public class ObjectEntryController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GameObject searchObjectPanel;
        [SerializeField]
        private ObjectCardCreationController objectCardCreationPanel;
        [SerializeField]
        private GameObject objectCardQueryPanel;
        [SerializeField]
        private MainSceneManager sceneManager;
        [Header("UI Elements")]
        [SerializeField]
        private Interactable submitButton;
        [SerializeField]
        private ButtonConfigHelper submitButtonConfigHelper;
        [SerializeField]
        private TMP_Text hintLabel;
        [SerializeField]
        private TMP_InputField inputField;
        [SerializeField]
        private string loadingText = "Please wait...";
        
        private bool isInSearchMode;
        
        private void Awake()
        {
            if (sceneManager == null)
            {
                sceneManager = FindObjectOfType<MainSceneManager>();
            }
        }

        private void OnEnable()
        {
            inputField.text = "";
        }

        public void Init()
        {
            submitButtonConfigHelper.MainLabelText = isInSearchMode ? "Search Object" : "Set Object";
        }

        public void SetSearchMode(bool searchModeActive)
        {
            isInSearchMode = searchModeActive;
        }

        public async void OnButtonClick()
        {
            if (string.IsNullOrWhiteSpace(inputField.text))
            {
                hintLabel.SetText("Please type in a name.");
                hintLabel.gameObject.SetActive(true);
                return;
            }

            if (!sceneManager.DataManager.IsReady)
            {
                hintLabel.SetText("No connection to the database!");
                hintLabel.gameObject.SetActive(true);
                return;
            }

            submitButton.IsEnabled = false;
            if (isInSearchMode)
            {
                var project = await FindObject(inputField.text);
                if (project != null)
                {
                    searchObjectPanel.SetActive(false);
                    objectCardQueryPanel.SetActive(true);
                    // TODO handle Init
                }
            }
            else
            {
                var project = await CreateObject(inputField.text);
                if (project != null)
                {
                    searchObjectPanel.SetActive(false);
                    objectCardCreationPanel.gameObject.SetActive(true);
                    objectCardCreationPanel.Init(project);
                }
            }
            submitButton.IsEnabled = true;
        }

        private async Task<TrackedObjectProject> FindObject(string searchName)
        {
            hintLabel.SetText(loadingText);
            hintLabel.gameObject.SetActive(true);
            var projectFromDb = await sceneManager.DataManager.FindByName(searchName);
            if (projectFromDb == null)
            {
                hintLabel.SetText($"No object found with the name '{searchName}'.");
                return null;
            }

            hintLabel.gameObject.SetActive(false);
            return projectFromDb;
        }

        private async Task<TrackedObjectProject> CreateObject(string searchName)
        {
            hintLabel.SetText("Please wait...");
            hintLabel.gameObject.SetActive(true);
            var project = await sceneManager.DataManager.FindByName(searchName);
            if (project == null)
            {
                project = new TrackedObjectProject(searchName);
                var success = await sceneManager.DataManager.UploadOrUpdate(project);
                if (!success)
                {
                    return null;
                }

                var tagName = $"tag_{project.Name}";
                var tagCreation = await sceneManager.ObjectDetectionManager.CreateTag(tagName);
                project.CustomVision.TagName = tagCreation.Name;
                project.CustomVision.TagId = tagCreation.Id;
                await sceneManager.DataManager.UploadOrUpdate(project);
            }

            hintLabel.gameObject.SetActive(false);
            return project;
        }
    }
}
