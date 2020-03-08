using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Windows.Speech;
using Microsoft.MixedReality.Toolkit.UI;

public class LunarcomIntentRecognizer : MonoBehaviour
{
    [Header("LUIS Credentials")]
    public string luisEndpoint = "";

    [Header("Rocket Launcher Buttons")]
    public GameObject HintsButton;
    public GameObject ResetButton;
    public GameObject LaunchButton;

    DictationRecognizer dictationRecognizer;
    LunarcomController lunarcomController;
    ///bool micPermissionGranted = false;
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
        else
        {
            ///micPermissionGranted = true;
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
            recognizedString = "";
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


            if (dictationRecognizer.Status == SpeechSystemStatus.Stopped) {
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
        //StopCapturingAudio();
        StartCoroutine(SubmitRequestToLuis(dictationCaptured, BeginRecognizing));
        recognizedString = dictationCaptured;
    }

    private void DictationRecognizer_DictationError(string error, int hresult)
    {
        Debug.Log("Dictation exception: " + error);
    }

    [Serializable]
    class AnalysedQuery
    {
        public TopScoringIntentData topScoringIntent = default;
        public EntityData[] entities = default;
        ///public string query;
    }

    [Serializable]
    class TopScoringIntentData
    {
        public string intent = default;
        ///public float score;
    }

    [Serializable]
    class EntityData
    {
        public string entity = default;
        public string type = default;
        ///public int startIndex;
        ///public int endIndex;
        ///public float score;
    }

    public IEnumerator SubmitRequestToLuis(string dictationResult, Action done)
    {
        string queryString = string.Concat(Uri.EscapeDataString(dictationResult));

        using (UnityWebRequest unityWebRequest = UnityWebRequest.Get(luisEndpoint + queryString))
        {
            yield return unityWebRequest.SendWebRequest();

            if (unityWebRequest.isNetworkError || unityWebRequest.isHttpError)
            {
                Debug.Log(unityWebRequest.error);
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
                    Debug.Log("Luis Request Exception Message: " + exception.Message);
                }
            }

            done();
            yield return null;
        }
    }

    private void UnpackResults(AnalysedQuery aQuery)
    {
        string topIntent = aQuery.topScoringIntent.intent;

        Dictionary<string, string> entityDic = new Dictionary<string, string>();

        foreach (EntityData ed in aQuery.entities)
        {
            entityDic.Add(ed.type, ed.entity);
        }

        switch (aQuery.topScoringIntent.intent)
        {
            case "PressButton":
                string actionToTake = null;
                string targetButton = null;

                foreach (var pair in entityDic)
                {
                    if (pair.Key == "Target")
                    {
                        targetButton = pair.Value;
                    }
                    else if (pair.Key == "Action")
                    {
                        actionToTake = pair.Value;
                    }
                }
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
            buttonToPush.GetComponent<Interactable>().OnClick.Invoke();
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
