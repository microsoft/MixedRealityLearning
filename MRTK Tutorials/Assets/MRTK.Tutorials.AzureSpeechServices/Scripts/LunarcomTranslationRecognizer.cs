using UnityEngine;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Translation;

public class LunarcomTranslationRecognizer : MonoBehaviour
{
    public TranslateToLanguage TargetLanguage = TranslateToLanguage.Russian;

    private string recognizedString = "Select a mode to begin.";
    private string translatedString = "";
    private object threadLocker = new object();

    private TranslationRecognizer translator;

    private bool micPermissionGranted = false;
    ///private bool scanning = false;

    private string fromLanguage = "en-US";
    private string toLanguage = "";

    private LunarcomController lunarcomController;

    void Start()
    {
        lunarcomController = LunarcomController.lunarcomController;

        if (LunarcomController.lunarcomController.outputText == null)
        {
            Debug.LogError("outputText property is null! Assign a UI Text element to it.");
        }
        else
        {
            micPermissionGranted = true;
        }

        lunarcomController.onSelectRecognitionMode += HandleOnSelectRecognitionMode;

        switch (TargetLanguage)
        {
            case TranslateToLanguage.Russian:
                toLanguage = "ru-RU";
                break;
            case TranslateToLanguage.German:
                toLanguage = "de-DE";
                break;
            case TranslateToLanguage.Chinese:
                toLanguage = "zh-HK";
                break; 
        }
    }

    public void HandleOnSelectRecognitionMode(RecognitionMode recognitionMode)
    {
        if (recognitionMode == RecognitionMode.Tralation_Recognizer)
        {
            recognizedString = "Say something...";
            translatedString = "";
            BeginTranslating();
        } else
        {
            if (translator != null)
            {
                translator.StopContinuousRecognitionAsync();
            }
            translator = null;
            recognizedString = "";
            translatedString = "";
        }
    }

    public async void BeginTranslating()
    {
        if (micPermissionGranted)
        {
            CreateTranslationRecognizer();

            if (translator != null)
            {
                await translator.StartContinuousRecognitionAsync().ConfigureAwait(false);
            }
        }
        else
        {
            recognizedString = "This app cannot function without access to the microphone.";
        }
    }

    void CreateTranslationRecognizer()
    {
        if (translator == null)
        {
            SpeechTranslationConfig config = SpeechTranslationConfig.FromSubscription(lunarcomController.SpeechServiceAPIKey, lunarcomController.SpeechServiceRegion);
            config.SpeechRecognitionLanguage = fromLanguage;
            config.AddTargetLanguage(toLanguage);

            translator = new TranslationRecognizer(config);

            if (translator != null)
            {
                translator.Recognizing += HandleTranslatorRecognizing;
                translator.Recognized += HandleTranslatorRecognized;
                translator.Canceled += HandleTranslatorCanceled;
                translator.SessionStarted += HandleTranslatorSessionStarted;
                translator.SessionStopped += HandleTranslatorSessionStopped;
            }
        }
    }

    #region Translation Recognition Event Handlers
    private void HandleTranslatorRecognizing(object s, TranslationRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.TranslatingSpeech)
        {
            if (e.Result.Text != "")
            {
                recognizedString = e.Result.Text;

                foreach (var element in e.Result.Translations)
                {
                    translatedString = element.Value;
                }
            }
        }
    }

    private void HandleTranslatorRecognized(object s, TranslationRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.TranslatedSpeech)
        {
            recognizedString = e.Result.Text;

            foreach (var element in e.Result.Translations)
            {
                translatedString = element.Value;
            }
        }
    }

    private void HandleTranslatorCanceled(object s, TranslationRecognitionEventArgs e)
    {
    }

    private void HandleTranslatorSessionStarted(object s, SessionEventArgs e)
    {
    }

    public void HandleTranslatorSessionStopped(object s, SessionEventArgs e)
    {
    }
    #endregion

    private void Update()
    {
        if (lunarcomController.CurrentRecognitionMode() == RecognitionMode.Tralation_Recognizer)
        {
            if (recognizedString != "")
            {
                lunarcomController.UpdateLunarcomText(recognizedString);

                if (translatedString != "")
                {
                    lunarcomController.outputText.text += "\n\nTranslation:\n" + translatedString;
                }
            }
        }
    }

    void OnDestroy()
    {
        if (translator != null)
        {
            translator.Dispose();
        }
    }
}
