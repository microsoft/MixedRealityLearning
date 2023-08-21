// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Controller
{
    public class CloseDialogController : MonoBehaviour
    {
        [SerializeField]
        private GameObject mainMenu = default;

        private GameObject originMenu;
        private GameObject targetMenu;

        /// <summary>
        /// Menu to back when cancel button is clicked.
        /// </summary>
        /// <param name="menu">target menu</param>
        public void SetOriginMenu(GameObject menu)
        {
            originMenu = menu;
        }
        
        /// <summary>
        /// Menu to back when ok button is clicked.
        /// </summary>
        /// <param name="menu">target menu</param>
        public void SetTargetMenu(GameObject menu)
        {
            targetMenu = menu;
        }

        public void HandleOkButtonClick()
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

        public void HandleCancelButtonClick()
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
