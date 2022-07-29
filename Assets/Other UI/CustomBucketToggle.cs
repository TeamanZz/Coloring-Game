using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class CustomBucketToggle : MonoBehaviour
{
    public Color activeColor;
    public Color inactiveColor;

    public Color inactiveBackgroundColor;

    public Image toggleBackground;
    public Image iconColor;

    public Image countView;

    public Toggle Toggle { get; private set; }

    public void Awake()
    {
        Toggle = GetComponent<Toggle>();
        Toggle.onValueChanged.AddListener(ButtonProcessing);

        ButtonProcessing(Toggle.isOn);
    }

    public void ButtonProcessing(bool isSelected)
    {
        countView.gameObject.SetActive(true);

        toggleBackground.color = isSelected ? activeColor : inactiveColor;
        iconColor.color = isSelected ? inactiveColor : activeColor;
    }
}
