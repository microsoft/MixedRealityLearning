using UnityEngine;
using Microsoft.CognitiveServices.Speech;

public class LunarcomWakeWordRecognizer : MonoBehaviour
{
    public string WakeWord = "";
    public string DismissWord = "";

    private string recognizedString = "Select a mode to begin.";
    private object threadLocker = new object();

    private SpeechRecognizer recognizer;

    private bool micPermissionGranted = false;
    ///private bool scanning = false;

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

        if (GetComponent<LunarcomOfflineRecognizer>())
        {
            LunarcomOfflineRecognizer lunarcomOfflineRecognizer = GetComponent<LunarcomOfflineRecognizer>();
            if (lunarcomOfflineRecognizer.simulateOfflineMode != SimuilateOfflineMode.Enabled)
            {
                if (WakeWord != "" && WakeWord != "*")
                {
                    lunarcomController.ShowTerminal();
                    BeginRecognizing();
                }
            }
        }
        else
        {
            if (WakeWord != "" && WakeWord != "*")
            {
                lunarcomController.ShowTerminal();
                BeginRecognizing();
            }
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
            }
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
            }
        }
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
    }

    private void Update()
    {
        if (lunarcomController.terminal.activeSelf)
        {
            if (recognizedString.ToLower().Contains(DismissWord.ToLower()))
            {
                lunarcomController.HideTerminal();
            }
        }
        else
        {
            if (recognizedString.ToLower().Contains(WakeWord.ToLower()))
            {
                lunarcomController.ShowTerminal();
            }
        }
    }

    void OnDestroy()
    {
        if (recognizer != null)
        {
            recognizer.Dispose();
        }
    }
}
