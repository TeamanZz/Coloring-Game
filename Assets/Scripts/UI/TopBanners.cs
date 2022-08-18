using Assets.Scripts.Network.Models;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(ScrollRect))]
public class TopBanners : MonoBehaviour
{
    [Header("Banner")]
    [SerializeField] private Sprite _placeholderSprite;
    [SerializeField] private BannerListItem _bannerItemPrefab = null;

    [Header("Swipe settings")]
    [SerializeField] private float _changeDelay = 2f;
    [SerializeField] private float _moveTime = .5f;

    private ScrollRect _rect;

    private List<BannerListItem> _bannerList = new List<BannerListItem>();

    private float _t = 0f;
    private int _selectedIndex = 0;

    private void Awake()
    {
        _rect = GetComponent<ScrollRect>();
    }

    private void Update()
    {
        if (_bannerList != null || _bannerList.Count == 0 || _rect.content.childCount == 0)
            return;

        _t += Time.deltaTime;

        if(_t >= _changeDelay)
        {
            ScrollNext();
            _t = 0f;
        }
    }

    private void ScrollNext()
    {
        if (_bannerList != null || _bannerList.Count == 0 || _rect.content.childCount == 0)
            return;

        _selectedIndex++;

        if (_selectedIndex > (_rect.content.childCount - 1))
            _selectedIndex = 0;

        var positions = _rect.GetScrollPositions(_rect.content.GetChild(_selectedIndex) as RectTransform);

        DOVirtual.Float(_rect.horizontalNormalizedPosition, positions.Item1, _moveTime, v => _rect.horizontalNormalizedPosition = v);
    }

    private void OnEnable()
    {
        PanelApiManager.Instance.OnBannersRefreshed += SetupBannerList;
    }


    private void SetupBannerList()
    {
        _bannerList.Clear();

        if (_selectedIndex > _rect.content.childCount)
            _selectedIndex = 0;

        for (int i = 0; i < _rect.content.childCount; i++)
        {
            Destroy(_rect.content.GetChild(i).gameObject);
        }

        var banners = PanelApiManager.Instance.Banners.Where(b => b.Type == BannerType.Top).ToArray();

        if(banners.Length > 0)
        {
            for (int i = 0; i < banners.Length; i++)
            {
                 var banner = Instantiate(_bannerItemPrefab, _rect.content);
                 banner.Setup(banners[i]);
                _bannerList.Add(banner);
            }
        }
        else
        {
            Instantiate(_bannerItemPrefab, _rect.content).SetupPlaceholder(_placeholderSprite);
        }
    }
}
