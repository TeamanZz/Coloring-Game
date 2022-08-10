using Assets.Scripts.Network.Models;
using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class TopBanners : MonoBehaviour
{
    [SerializeField] private BannerListItem _bannerItemPrefab = null;
    [SerializeField] private float _changeDelay = 2f;

    [SerializeField] private float _moveTime = .5f;

    private ScrollRect _rect;

    private float _t = 0f;
    private int _selectedIndex = 0;

    private void Awake()
    {
        _rect = GetComponent<ScrollRect>();
    }

    private void Update()
    {
        _t += Time.deltaTime;

        if(_t >= _changeDelay)
        {
            ScrollNext();
            _t = 0f;
        }
    }

    private void ScrollNext()
    {
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
        if (_selectedIndex > _rect.content.childCount)
            _selectedIndex = 0;

        for (int i = 0; i < _rect.content.childCount; i++)
        {
            Destroy(_rect.content.GetChild(i).gameObject);
        }

        var banners = PanelApiManager.Instance.Banners.Where(b => b.Type == BannerType.Top && (DateTime.UtcNow - b.getDate).Days <= b.lifetime).ToArray();

        for (int i = 0; i < banners.Length; i++)
        {
            Instantiate(_bannerItemPrefab, _rect.content).Setup(banners[i]);
        }
    }
}
