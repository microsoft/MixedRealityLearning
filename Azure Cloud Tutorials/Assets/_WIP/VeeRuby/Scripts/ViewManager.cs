using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewManager : MonoBehaviour
{
    int collapseMenuID;
    public static bool searchObjectFlow;
    public static int menuId;
    public GameObject mainMenu;
    public GameObject enterNameMenu;
    public GameObject saveObjectButton;
    public GameObject searchObjectButton;
    public GameObject computerVisionMenu;
    public GameObject objectCardMenu;
    //public GameObject computerVisionMenuMessge;

    void Update()
    {
        if(mainMenu.activeSelf)
        {
            menuId = 1;
        }
        else if(enterNameMenu.activeSelf && saveObjectButton.activeSelf)
        {
            menuId = 2;
        }
        else if(enterNameMenu.activeSelf && searchObjectButton.activeSelf)
        {
            menuId = 3;
        }
        else if(objectCardMenu.activeSelf)
        {
            menuId = 4;
        }
        else if(computerVisionMenu.activeSelf)
        {
            menuId = 5;
        }
        else{}
    }
   
    public void CancelMenuHandler()
    {
        
        if(menuId==5)
        {
            objectCardMenu.SetActive(true);
        }
        else
        {
            
            mainMenu.SetActive(true);
            
        }

    }

    public void ExpandMenu()
    {
        switch (menuId)
      {
          case 1:
              mainMenu.SetActive(true);
              break;
          case 2:
              enterNameMenu.SetActive(true);
              saveObjectButton.SetActive(true);
              searchObjectButton.SetActive(false);
              break;
          case 3:
                enterNameMenu.SetActive(true);
                saveObjectButton.SetActive(false);
                searchObjectButton.SetActive(true);
              break;
           case 4:
                objectCardMenu.SetActive(true);
              break;
            case 5:
              computerVisionMenu.SetActive(true);
            break;
          default:
                mainMenu.SetActive(true);
              break;
      }
    }

    public void SetSearchObjectFlow(bool searchObject)
    {
        searchObjectFlow = searchObject;
    }

    public void ComputerVisionMenuHandler()
    {
        Debug.Log(searchObjectFlow);
        if(searchObjectFlow)
        {
            objectCardMenu.SetActive(false);
            computerVisionMenu.SetActive(true);
        }
        else
        {
           // computerVisionMenuMessge.GetComponent<Text>.text="Looking for the object";
        }
    }
}
