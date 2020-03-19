using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LunarcomButtonController : MonoBehaviour
{
    [Header("Reference Objects")]
    public RecognitionMode speechRecognitionMode = RecognitionMode.Disabled;

    [Space(6)]
    [Header("Button States")]
    public Sprite Default;
    public Sprite Activated;

    private Button button;
    private bool isSelected = false;

    private LunarcomController lunarcomController;

    private void Start()
    {
        lunarcomController = LunarcomController.lunarcomController;
        button = GetComponent<Button>();
    }

    public bool GetIsSelected()
    {
        return isSelected;
    }

    public void ToggleSelected()
    {
        if (isSelected)
        {
            DeselectButton();
        }
        else
        {
            button.image.sprite = Activated;
            isSelected = true;
            lunarcomController.SetActiveButton(GetComponent<LunarcomButtonController>());

            if (lunarcomController.IsOfflineMode())
            {
                lunarcomController.SelectMode(RecognitionMode.Offline);
            } else
            {
                lunarcomController.SelectMode(speechRecognitionMode);
            }
        }
    }

    public void ShowNotSelected()
    {
        button.image.sprite = Default;
        isSelected = false;
    }

    public void DeselectButton()
    {
        ShowNotSelected();
        lunarcomController.SelectMode(RecognitionMode.Disabled);
    }
}
