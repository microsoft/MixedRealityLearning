using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Controller
{
    public class CloseDialogController : MonoBehaviour
    {
        [SerializeField]
        private GameObject mainMenu;

        private GameObject originMenu;
        private GameObject targetMenu;

        public void SetOriginMenu(GameObject menu)
        {
            originMenu = menu;
        }
        
        public void SetTargetMenu(GameObject menu)
        {
            targetMenu = menu;
        }

        public void OnOkButtonClick()
        {
            if (targetMenu == null)
            {
                mainMenu.SetActive(true);
            }
            else
            {
                targetMenu.SetActive(true);
                targetMenu = null;
            }
            gameObject.SetActive(false);
        }

        public void OnCancelButtonClick()
        {
            if (originMenu == null)
            {
                return;
            }
            
            originMenu.SetActive(true);
            originMenu = null;
            gameObject.SetActive(false);
        }
    }
}
