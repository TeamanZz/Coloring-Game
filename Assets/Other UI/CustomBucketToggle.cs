using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomBucketToggle : MonoBehaviour
{
    public Color activeColor;
    public Color inactiveColor;

    public Image toggleBackground;
    public Image iconColor;

    public void Awake()
    { 
        if (TryGetComponent(out Toggle currentToggle))
            ButtonProcessing(currentToggle.isOn);
    }

    public void ButtonProcessing(bool isSelected)
    {
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
}
