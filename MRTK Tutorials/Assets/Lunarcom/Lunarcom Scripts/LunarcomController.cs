using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum RecognitionMode { Speech_Recognizer, Intent_Recognizer, Tralation_Recognizer, Disabled, Offline };
public enum SimuilateOfflineMode { Enabled, Disabled };
public enum TranslateToLanguage { Russian, German, Chinese };

public class LunarcomController : MonoBehaviour
{
    public static LunarcomController lunarcomController = null;

    [Header("Speech SDK Credentials")]
    public string SpeechServiceAPIKey = "";
    public string SpeechServiceRegion = "";

    [Space(6)]
    [Header("Object References")]
    public Text outputText;
    public GameObject Terminal;
    public ConnectionLightController connectionLightController;

    [Space(6)]
    [Header("Lunarcom Buttons")]
    public List<LunarcomButtonController> lunarcomButtons;

    public delegate void OnSelectRecognitionMode(RecognitionMode selectedMode);
    public event OnSelectRecognitionMode onSelectRecognitionMode;

    RecognitionMode speechRecognitionMode = RecognitionMode.Disabled;
    LunarcomButtonController activeButton = null;
    LunarcomWakeWordRecognizer lunarcomWakeWordRecognizer = null;
    LunarcomOfflineRecognizer lunarcomOfflineRecognizer = null;

    private void Awake()
    {
        if (lunarcomController == null)
            lunarcomController = this;
        else if (lunarcomController != this)
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (GetComponent<LunarcomWakeWordRecognizer>())
        {
            lunarcomWakeWordRecognizer = GetComponent<LunarcomWakeWordRecognizer>();
        }
        if (GetComponent<LunarcomOfflineRecognizer>())
        {
            lunarcomOfflineRecognizer = GetComponent<LunarcomOfflineRecognizer>();
            if (lunarcomOfflineRecognizer.simulateOfflineMode == SimuilateOfflineMode.Disabled)
            {
                SetupOnlineMode();
            }
            else
            {
                SetupOfflineMode();
            }
        } else
        {
            SetupOnlineMode();
        }
    }

    public bool IsOfflineMode()
    {
        if (lunarcomOfflineRecognizer != null)
        {
            return lunarcomOfflineRecognizer.simulateOfflineMode == SimuilateOfflineMode.Enabled;
        } else
        {
            return false;
        }
    }

    private void SetupOnlineMode()
    {
        if (lunarcomWakeWordRecognizer != null)
        {
            if (lunarcomWakeWordRecognizer.WakeWord == "")
            {
                lunarcomWakeWordRecognizer.WakeWord = "*";
                lunarcomWakeWordRecognizer.DismissWord = "*";
            }

            if (lunarcomWakeWordRecognizer.DismissWord == "")
            {
                lunarcomWakeWordRecognizer.DismissWord = "*";
            }
        }
        

        if (GetComponent<LunarcomTranslationRecognizer>())
        {
            ActivateButtonNamed("Satellite");
        }

        if (GetComponent<LunarcomIntentRecognizer>())
        {
            ActivateButtonNamed("Rocket");
        }

        ShowConnected(true);
    }

    private void SetupOfflineMode()
    {
        if (lunarcomWakeWordRecognizer != null)
        {
            lunarcomWakeWordRecognizer.WakeWord = "*";
            lunarcomWakeWordRecognizer.DismissWord = "*";
        }
        
        if (GetComponent<LunarcomWakeWordRecognizer>())
        {
            GetComponent<LunarcomWakeWordRecognizer>().enabled = false;
        }
        if (GetComponent<LunarcomSpeechRecognizer>())
        {
            GetComponent<LunarcomSpeechRecognizer>().enabled = false;
        }
        if (GetComponent<LunarcomTranslationRecognizer>())
        {
            GetComponent<LunarcomTranslationRecognizer>().enabled = false;
            ActivateButtonNamed("Satellite", false);
        }
        if (GetComponent<LunarcomIntentRecognizer>())
        {
            GetComponent<LunarcomIntentRecognizer>().enabled = false;
            ActivateButtonNamed("Rocket", false);
        }

        ShowConnected(false);
    }

    private void ActivateButtonNamed(string name, bool makeActive = true) {
        foreach (LunarcomButtonController button in lunarcomButtons)
        {
            if (button.gameObject.name == name)
            {
                button.gameObject.SetActive(makeActive);
            }
        }
    }

    public RecognitionMode CurrentRecognitionMode()
    {
        return speechRecognitionMode;
    }

    public void SetActiveButton(LunarcomButtonController buttonToSetActive)
    {
        activeButton = buttonToSetActive;
        foreach (LunarcomButtonController button in lunarcomButtons)
        {
            if (button != activeButton && button.GetIsSelected())
            {
                button.ShowNotSelected();
            }
        }
    }

    public void SelectMode(RecognitionMode speechRecognitionModeToSet)
    {
        speechRecognitionMode = speechRecognitionModeToSet;
        onSelectRecognitionMode(speechRecognitionMode);
        if (speechRecognitionMode == RecognitionMode.Disabled)
        {
            if (outputText.text == "Say something..." || outputText.text == "")
            {
                outputText.text = "Select a mode to begin.";
            }
        }
    }

    public void ShowConnected(bool showConnected)
    {
        connectionLightController.ShowConnected(showConnected);
    }

    public void ShowTerminal()
    {
        Terminal.SetActive(true);
    }

    public void HideTerminal()
    {
        if (Terminal.activeSelf)
        {
            foreach (LunarcomButtonController button in lunarcomButtons)
            {
                if (button.GetIsSelected())
                {
                    button.ShowNotSelected();
                }
            }

            outputText.text = "Select a mode to begin.";
            Terminal.SetActive(false);
            SelectMode(RecognitionMode.Disabled);
        }
    }

    public void UpdateLunarcomText(string textToUpdate)
    {
        if (lunarcomWakeWordRecognizer != null)
        {
            if (!textToUpdate.ToLower().Contains(lunarcomWakeWordRecognizer.DismissWord.ToLower()))
            {
                outputText.text = textToUpdate;
            } else
            {
                HideTerminal();
            }
        }
        else
        {
            outputText.text = textToUpdate;
        }
    }
}
