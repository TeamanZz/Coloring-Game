using Assets.Scripts.Data;
using BizzyBeeGames;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewsListItem : RecyclableListItem<BannerData>
{

	[SerializeField] private TextMeshProUGUI titleText = null;
	[SerializeField] private TextMeshProUGUI descriptionText = null;
    [SerializeField] private Image image = null;

    public override void Initialize(BannerData dataObject)
    {
       
    }

    public override void Removed()
    {
        
    }

    public override void Setup(BannerData dataObject)
    {
        Davinci.get().load(dataObject.ImageUrl).into(image).start();

        titleText.text = dataObject.Title;
        descriptionText.text = dataObject.Description;
    }
}
