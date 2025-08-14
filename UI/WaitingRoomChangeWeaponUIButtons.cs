using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaitingRoomChangeWeaponUIButtons : MonoBehaviour
{
    public Dictionary<int,Button> buttons= new Dictionary<int, Button>();
    
    
    private Color OnabledColor = new Color(1, 1, 1, 0.5f);
    private Color disabledColor = new Color(0.2f, 0.2f, 0.2f);


    private void Awake()
    {
        Button[] foundButtons = GetComponentsInChildren<Button>();

        for (int i = 0; i < foundButtons.Length; i++)
        {
            buttons.Add(i, foundButtons[i]);
        }
    }

    public void OnReadyToggle(bool isOn)
    {
        //Debug.Log("OnReadyToggle");
        if (isOn)
        {
            DisableButtons();
            //Debug.Log("DisableButtons");
        }
        else
        {
            OnableButtons();
            //Debug.Log("OnableButtons");
        }
    }
    
    private void DisableButtons()
    {
        foreach (Button button in buttons.Values)
        {
            var block = button.colors;
            block.normalColor = disabledColor;
            button.colors = block;
            button.interactable = false;
        }
    }
    
    private void OnableButtons()
    {
        foreach (Button button in buttons.Values)
        {
            var block = button.colors;
            block.normalColor = OnabledColor;
            button.colors = block;
            button.interactable = true;
        }
    }
}
