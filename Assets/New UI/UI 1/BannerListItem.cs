using Assets.Scripts.Network.Models;
using BizzyBeeGames;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BannerListItem : RecyclableListItem<Banner>
{

    [SerializeField] private TextMeshProUGUI titleText = null;
    [SerializeField] private TextMeshProUGUI descriptionText = null;
    [SerializeField] private Button goButton;

    [SerializeField] private Image image = null;

    public Banner BannerInfo { get; private set; }

    private void Awake()
    {
        goButton.onClick.AddListener(OpenUrl);
    }

    public override void Initialize(Banner dataObject)
    {
    }

    private void OpenUrl()
    {
        if (BannerInfo != null)
            Application.OpenURL(BannerInfo.link);
    }

    public override void Removed()
    {

    }

    public override void Setup(Banner banner)
    {
        this.BannerInfo = banner;

        string url = PanelApiManager.Instance.BannerImage(banner.id);

        Davinci.get().load(url).into(image).start();

        if (titleText)
            titleText.text = banner.name;
        if (descriptionText)
            descriptionText.text = banner.description;
    }

    public void SetupPlaceholder(Sprite sprite)
    {
        image.sprite = sprite;
    }
}
