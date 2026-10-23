using UnityEngine;

public class Panel_controler_historia : MonoBehaviour
{
    
    [SerializeField] private GameObject panel;

   
    [SerializeField] private MonoBehaviour playerMovementScript;

   
    [SerializeField] private MonoBehaviour cameraLookScript;

    private void Start()
    {
        
        if (panel != null && panel.activeSelf)
        {
            SetPanelState(true);
        }
        else
        {
            SetPanelState(false);
        }
    }

    
    public void TogglePanel()
    {
        if (panel == null) return;

        bool newState = !panel.activeSelf;
        SetPanelState(newState);
    }

   
    public void SetPanelState(bool isOpen)
    {
        if (panel != null)
        {
            panel.SetActive(isOpen);
        }

       
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = !isOpen;
        }

        
        if (cameraLookScript != null)
        {
            cameraLookScript.enabled = !isOpen;
        }

       
        if (isOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

    }

}


