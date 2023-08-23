// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.Windows.Speech;
using MixedReality.Toolkit.UX;
using System.Text;

public class LunarcomIntentRecognizer : MonoBehaviour
{
    [Header("CLU Credentials")]
    [SerializeField]
    private string cluEndpoint = string.Empty;
    [SerializeField]
    private string cluProjectName = string.Empty;
    [SerializeField]
    private string cluDeploymentName = string.Empty;
    [SerializeField]
    private string languageServiceAPIKey = string.Empty;

    [Header("Rocket Launcher Buttons")]
    public GameObject HintsButton;
    public GameObject ResetButton;
    public GameObject LaunchButton;

    DictationRecognizer dictationRecognizer;
    LunarcomController lunarcomController;
    string recognizedString;
    bool capturingAudio = false;
    bool commandCaptured = false;

    void Start()
    {
        lunarcomController = LunarcomController.lunarcomController;

        if (lunarcomController.outputText == null)
        {
            Debug.LogError("outputText property is null! Assign a UI Text element to it.");
        }

        lunarcomController.onSelectRecognitionMode += HandleOnSelectRecognitionMode;
    }

    public void HandleOnSelectRecognitionMode(RecognitionMode recognitionMode)
    {
        if (recognitionMode == RecognitionMode.Intent_Recognizer)
        {
            recognizedString = "Say something...";
            BeginRecognizing();
        }
        else
        {
            if (capturingAudio)
            {
                StopCapturingAudio();
            }
            recognizedString = string.Empty;
            commandCaptured = false;
        }
    }

    private void BeginRecognizing()
    {
        if (Microphone.devices.Length > 0)
        {
            if (dictationRecognizer == null)
            {
                dictationRecognizer = new DictationRecognizer
                {
                    InitialSilenceTimeoutSeconds = 60,
                    AutoSilenceTimeoutSeconds = 5
                };

                dictationRecognizer.DictationResult += DictationRecognizer_DictationResult;
                dictationRecognizer.DictationError += DictationRecognizer_DictationError;
            }


            if (dictationRecognizer.Status == SpeechSystemStatus.Stopped)
            {
                dictationRecognizer.Start();
            }
            capturingAudio = true;
        }
    }

    public void StopCapturingAudio()
    {
        if (dictationRecognizer != null && dictationRecognizer.Status != SpeechSystemStatus.Stopped)
        {
            dictationRecognizer.DictationResult -= DictationRecognizer_DictationResult;
            dictationRecognizer.DictationError -= DictationRecognizer_DictationError;
            dictationRecognizer.Stop();
            dictationRecognizer = null;
            capturingAudio = false;
        }
    }

    private void DictationRecognizer_DictationResult(string dictationCaptured, ConfidenceLevel confidence)
    {
        StartCoroutine(SubmitRequestToClu(dictationCaptured, BeginRecognizing));
        recognizedString = dictationCaptured;
    }

    private void DictationRecognizer_DictationError(string error, int hresult)
    {
        Debug.Log("Dictation exception: " + error);
    }

    [Serializable]
    class RequestQuery
    {
        public string kind = "Conversation";
        public AnalysisInput analysisInput = new AnalysisInput();
        public QueryParameters parameters = new QueryParameters();
    }

    [Serializable]
    class AnalysisInput
    {
        public ConversationItem conversationItem = new ConversationItem();
    }

    [Serializable]
    class ConversationItem
    {
        public string id = "1";
        public string participantId = "1";
        public string text = string.Empty;
    }

    [Serializable]
    class QueryParameters
    {
        public string projectName = string.Empty;
        public string deploymentName = string.Empty;
        public string stringIndexType = "TextElement_V8";
    }

    [Serializable]
    class AnalysedQuery
    {
        public string kind = default;
        public ResultData result = default;
    }

    [Serializable]
    class ResultData
    {
        public string query = default;
        public TopScoringIntentData prediction = default;
    }

    [Serializable]
    class TopScoringIntentData
    {
        public string topIntent = default;
        public string projectKind = default;
        public EntityData[] entities = default;
        public IntentData[] intents = default;
    }

    [Serializable]
    class EntityData
    {
        public string category = default;
        public string text = default;
        public int offset = default;
        public int length = default;
        public float confidenceScore = default;
    }

    [Serializable]
    class IntentData
    {
        public string category = default;
        public float confidenceScore = default;
    }

    public IEnumerator SubmitRequestToClu(string dictationResult, Action done)
    {
        RequestQuery requestQuery = new RequestQuery();
        requestQuery.parameters.deploymentName = cluDeploymentName;
        requestQuery.parameters.projectName = cluProjectName;
        requestQuery.analysisInput.conversationItem.text = dictationResult;

        string postData = JsonUtility.ToJson(requestQuery);

        using (UnityWebRequest unityWebRequest = new UnityWebRequest(cluEndpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);
            unityWebRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
            unityWebRequest.SetRequestHeader("Content-Type", "application/json");
            unityWebRequest.SetRequestHeader("Ocp-Apim-Subscription-Key", languageServiceAPIKey);

            yield return unityWebRequest.SendWebRequest();

            if (unityWebRequest.result == UnityWebRequest.Result.ConnectionError || 
                unityWebRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log(unityWebRequest.error + "\n" + unityWebRequest.downloadHandler.error);
            }
            else
            {
                try
                {
                    AnalysedQuery analysedQuery = JsonUtility.FromJson<AnalysedQuery>(unityWebRequest.downloadHandler.text);                                     
                    UnpackResults(analysedQuery);
                }
                catch (Exception exception)
                {
                    Debug.Log("Clu Request Exception Message: " + exception.Message);
                }
            }

            done();
            yield return null;
        }
    }

    private void UnpackResults(AnalysedQuery aQuery)
    {
        bool commandWasRecognized = aQuery.result.prediction.entities.Length > 0;
        if (!commandWasRecognized)
        {
            ProcessResults();
            return;
        }

        string topIntent = aQuery.result.prediction.topIntent;

        switch (topIntent)
        {
            case "PressButton":
                string actionToTake = aQuery.result.prediction.entities[0].text;//Save action intent
                string targetButton = aQuery.result.prediction.entities[1].text;//Save target intent

                ProcessResults(targetButton, actionToTake);
                break;
            default:
                ProcessResults();
                break;
        }
    }

    public void ProcessResults(string targetButton = null, string actionToTake = null)
    {
        switch (targetButton)
        {
            case "launch":
                CompleteButtonPress(actionToTake, targetButton, LaunchButton);
                break;
            case "reset":             
                CompleteButtonPress(actionToTake, targetButton, ResetButton);
                break;
            case "hint":
                CompleteButtonPress(actionToTake, targetButton, HintsButton);
                break;
            case "hints":
                CompleteButtonPress(actionToTake, targetButton, HintsButton);
                break;
            default:
                CompleteButtonPress();
                break;
        }
    }

    private void CompleteButtonPress(string actionToTake = null, string buttonName = null, GameObject buttonToPush = null)
    {
        recognizedString += (actionToTake != null) ? "\n\nAction: " + actionToTake : "\n\nAction: -";
        recognizedString += (buttonName != null) ? "\nTarget: " + buttonName : "\nTarget: -";

        if (actionToTake != null && buttonName != null && buttonToPush != null)
        {
            recognizedString += "\n\nCommand recognized, pushing the '" + buttonName + "' button because I was told to '" + actionToTake + "'";
            buttonToPush.GetComponent<PressableButton>().OnClicked.Invoke();
        }
        else
        {
            recognizedString += "\n\nCommand not recognized";
        }
        commandCaptured = true;
    }

    private void Update()
    {
        if (lunarcomController.CurrentRecognitionMode() == RecognitionMode.Intent_Recognizer)
        {
            lunarcomController.UpdateLunarcomText(recognizedString);

            if (commandCaptured)
            {
                foreach (LunarcomButtonController button in lunarcomController.buttons)
                {
                    if (button.GetIsSelected())
                    {
                        button.DeselectButton();
                    }
                }
                commandCaptured = false;
            }
        }
    }

    void OnDestroy()
    {
        if (dictationRecognizer != null)
        {
            dictationRecognizer.Dispose();
        }
    }
}
