using System;
using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.UI;
using MRTK.Tutorials.AzureCloudPower.Domain;
using MRTK.Tutorials.AzureCloudPower.Managers;
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
        [SerializeField]
        private DataManager dataManager;
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
            if (dataManager == null)
            {
                dataManager = FindObjectOfType<DataManager>();
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

            if (!dataManager.IsReady)
            {
                hintLabel.SetText("No connection to the database!");
                hintLabel.gameObject.SetActive(true);
                return;
            }

            submitButton.IsEnabled = false;
            if (isInSearchMode)
            {
                var success = await FindObject(inputField.text);
                if (success)
                {
                    searchObjectPanel.SetActive(false);
                    objectCardQueryPanel.SetActive(true);
                }
            }
            else
            {
                var success = await CreateObject(inputField.text);
                if (success)
                {
                    searchObjectPanel.SetActive(false);
                    objectCardCreationPanel.gameObject.SetActive(true);
                    objectCardCreationPanel.Init();
                }
            }
            submitButton.IsEnabled = true;
        }

        private async Task<bool> FindObject(string searchName)
        {
            hintLabel.SetText(loadingText);
            hintLabel.gameObject.SetActive(true);
            var objectFromDb = await dataManager.FindByName(searchName);
            if (objectFromDb == null)
            {
                hintLabel.SetText($"No object found with the name '{searchName}'.");
                return false;
            }

            sceneManager.CurrentProject = objectFromDb;
            hintLabel.gameObject.SetActive(false);
            return true;
        }

        private async Task<bool> CreateObject(string searchName)
        {
            hintLabel.SetText("Please wait...");
            hintLabel.gameObject.SetActive(true);
            var project = await dataManager.FindByName(searchName);
            if (project == null)
            {
                project = new TrackedObjectProject(searchName);
                var success = await dataManager.UploadOrUpdate(project);
                if (!success)
                {
                    return false;
                }
            }

            sceneManager.CurrentProject = project;
            hintLabel.gameObject.SetActive(false);
            return true;
        }
    }
}
