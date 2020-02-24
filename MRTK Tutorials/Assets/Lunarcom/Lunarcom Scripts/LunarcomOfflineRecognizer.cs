using UnityEngine;
using Microsoft.CognitiveServices.Speech;

public class LunarcomOfflineRecognizer : MonoBehaviour
{
    [Header("Lunarcom Settings")]
    public SimuilateOfflineMode simulateOfflineMode = SimuilateOfflineMode.Disabled;

    private string recognizedString = "Select a mode to begin.";
    private object threadLocker = new object();

    private SpeechRecognizer recognizer;

    private bool micPermissionGranted = false;
    private bool scanning = false;

    private string fromLanguage = "en-US";

    private LunarcomController lunarcomController;

    void Start()
    {
        lunarcomController = LunarcomController.lunarcomController;
        
        if (lunarcomController.outputText == null)
        {
            Debug.LogError("outputText property is null! Assign a UI Text element to it.");
        }
        else
        {
            micPermissionGranted = true;
        }

        lunarcomController.onSelectRecognitionMode += HandleOnSelectRecognitionMode;
    }

    public void HandleOnSelectRecognitionMode(RecognitionMode recognitionMode)
    {
        if (recognitionMode == RecognitionMode.Offline)
        {
            BeginRecognizing();
        }
        else
        {
            if (recognizer != null)
            {
                recognizer.StopContinuousRecognitionAsync();
            }
            recognizer = null;
            recognizedString = "";
        }
    }

    public async void BeginRecognizing()
    {
        if (micPermissionGranted)
        {
            CreateSpeechRecognizer();

            if (recognizer != null)
            {
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
                recognizedString = "Say something...";
            }
        }
        else
        {
            Debug.Log("This app cannot function without access to the microphone.");
        }
    }

    void CreateSpeechRecognizer()
    {
        if (recognizer == null)
        {
            SpeechConfig config = SpeechConfig.FromSubscription(lunarcomController.SpeechServiceAPIKey, lunarcomController.SpeechServiceRegion);
            config.SpeechRecognitionLanguage = fromLanguage;
            recognizer = new SpeechRecognizer(config);
            if (recognizer != null)
            {
                recognizer.Recognizing += RecognizingHandler;
                recognizer.Recognized += RecognizedHandler;
                recognizer.SpeechStartDetected += SpeechStartDetected;
                recognizer.SpeechEndDetected += SpeechEndDetectedHandler;
                recognizer.Canceled += CancelHandler;
                recognizer.SessionStarted += SessionStartedHandler;
                recognizer.SessionStopped += SessionStoppedHandler;
            }
        }
    }

    #region Speech Recognition Event Handlers
    private void SessionStartedHandler(object sender, SessionEventArgs e)
    {
        Debug.Log("SessionStartedHandler called");
    }
    private void SessionStoppedHandler(object sender, SessionEventArgs e)
    {
        Debug.Log("SessionStoppedHandler called");
        recognizer = null;
    }

    private void RecognizingHandler(object sender, SpeechRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.RecognizingSpeech)
        {
            lock (threadLocker)
            {
                recognizedString = $"{e.Result.Text}";
            }
        }
        Debug.Log("Recognizing Handler called");
    }

    private void RecognizedHandler(object sender, SpeechRecognitionEventArgs e)
    {
        Debug.Log("Recognized Handler called");
    }

    private void SpeechStartDetected(object sender, RecognitionEventArgs e)
    {
        Debug.Log("SpeechStart Handler called");
    }

    private void SpeechEndDetectedHandler(object sender, RecognitionEventArgs e)
    {
        Debug.Log("SpeechStart Handler called");
    }

    private void CancelHandler(object sender, RecognitionEventArgs e)
    {
        Debug.Log("SpeechEndDetectedHandler called");
    }
    #endregion

    private void Update()
    {
        Debug.Log(lunarcomController.CurrentRecognitionMode());
        if (lunarcomController.CurrentRecognitionMode() == RecognitionMode.Offline)
        {
            Debug.Log("Now offline mode");
            if (recognizedString != "" && recognizedString != "Offline Transcription:\n")
            {
                Debug.Log("recognized string isn't blank and it's not Offline Transcription");
                if (recognizedString != "Say something..." && recognizedString != "Select a mode to begin.")
                {
                    string combinedString = "Offline Transcription:\n" + recognizedString;
                    lunarcomController.UpdateLunarcomText(combinedString);
                } else
                {
                    lunarcomController.UpdateLunarcomText(recognizedString);
                }
            }
        }
    }
}
