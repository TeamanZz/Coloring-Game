using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomBucketToggle : MonoBehaviour
{
    public Color activeColor;
    public Color inactiveColor;

    public Color inactiveBackgroundColor;

    public Image toggleBackground;
    public Image iconColor;

    public Image countView;

    public void Awake()
    { 
        if (TryGetComponent(out Toggle currentToggle))
            ButtonProcessing(currentToggle.isOn);
    }

    public void ButtonProcessing(bool isSelected)
    {
        countView.gameObject.SetActive(true);
        switch(isSelected)
        {
            case false:
                toggleBackground.color = inactiveColor;
                iconColor.color = activeColor;
                break;

            case true:
                toggleBackground.color = activeColor;
                iconColor.color = inactiveColor;
                break;
        }
    }

    [ContextMenu("Inactive")]
    public void InactiveButton()
    {
        Debug.Log("Inactive Button");
        toggleBackground.color = inactiveBackgroundColor;
        iconColor.color = inactiveColor;
        countView.gameObject.SetActive(false);
    }
}
