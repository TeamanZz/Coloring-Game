using BizzyBeeGames;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class CustomBucketToggle : MonoBehaviour
{
    public static CustomBucketToggle Instance;
    [Header("Main Settings")]
    public bool isActive = true;

    [Header("View Settings")]
    public Color activeColor;
    public Color inactiveColor;

    public Color inactiveBackgroundColor;

    public Color lastClickedColor;

    public Image toggleBackground;
    public Image iconColor;
    public Image countView;

    public Toggle Toggle { get; private set; }

    public void Awake()
    {
        Instance = this;
        Toggle = GetComponent<Toggle>();
        Toggle.onValueChanged.AddListener(ButtonProcessing);

        ButtonProcessing(Toggle.isOn);
    }

    public void ButtonProcessing(bool isSelected)
    {
        if (!isActive)
        {
            PopupManager.Instance.Show("store");
            return;
        }

        toggleBackground.color = isSelected ? lastClickedColor : inactiveColor;
        iconColor.color = isSelected ? inactiveColor : activeColor;
    }

    public void SetToggleBackgroundColor(Color newColor)
    {
        if (Toggle.isOn)
            toggleBackground.color = newColor;
    }

    public void InactiveButton()
    {
        toggleBackground.color = inactiveBackgroundColor;
        iconColor.color = inactiveColor;
        countView.gameObject.SetActive(false);
    }
}