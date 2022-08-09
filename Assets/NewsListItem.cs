using Assets.Scripts.Network.Models;
using BizzyBeeGames;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewsListItem : RecyclableListItem<Banner>
{

	[SerializeField] private TextMeshProUGUI titleText = null;
	[SerializeField] private TextMeshProUGUI descriptionText = null;
    [SerializeField] private Button goButton;

    [SerializeField] private Image image = null;

    private Banner banner;

    private void Awake()
    {
        goButton.onClick.AddListener(OpenUrl);  
    }

    public override void Initialize(Banner dataObject)
    {
    }

    private void OpenUrl()
    {
        Application.OpenURL(banner.link);
    }

    public override void Removed()
    {
        
    }

    public override void Setup(Banner banner)
    {
        this.banner = banner;

        string url = PanelApiManager.Instance.BannerImage(banner.id);

        Davinci.get().load(url).into(image).start();

        titleText.text = banner.name;
        descriptionText.text = banner.description;
    }
}
