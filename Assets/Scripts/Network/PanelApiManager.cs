using Assets.Scripts.Network.Models;
using Assets.Scripts.Network.Utils;
using BizzyBeeGames;
using Newtonsoft.Json;
using Proyecto26;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PanelApiManager : SaveableManager<PanelApiManager>
{
    public override string SaveId => nameof(PanelApiManager);

    [SerializeField] private string _apiLink = "http://165.227.147.239/api/";
    [SerializeField] private string _ipService = "http://ip-api.com/json";

    public List<Banner> Banners { get; private set; }
    public event Action OnBannersRefreshed;

    protected override void Awake()
    {
        base.Awake();

        Banners = new List<Banner>();

        InitSave();
        Refresh();

    }

    public void Refresh()
    {
        RefreshBanners();
    }

    public string BannerImage(int id) => _apiLink + $"/banner/get_file?id={id}";

    public void RefreshBanners()
    {
        string lang = Lang.DeviceLang();

        RestClient.Get(_ipService).Then(response =>
        {
            var geo = JsonUtility.FromJson<GeoData>(response.Text);

            Banner[] banners = new Banner[0];

            RestClient.GetArray<Banner>(_apiLink + $"/banner/get?lang={lang}&country={geo.countryCode}").Then(response =>
            {
                var newBanners = Banners != null ? response.Where(x => !Banners.Any(b => b.id == x.id)).ToList() : response.ToList();

                for (int i = 0; i < newBanners.Count; i++)
                {
                    newBanners[i].getDate = DateTime.UtcNow;
                    Banners.Add(newBanners[i]);
                }

                OnBannersRefreshed?.Invoke();
            });
        });
    }

    public override Dictionary<string, object> Save()
    {
        Dictionary<string, object> saveData = new Dictionary<string, object>();

        saveData["banners"] = JsonConvert.SerializeObject(Banners);

        return saveData;
    }

    protected override void LoadSaveData(bool exists, JSONNode saveData)
    {
        if (!exists) return;

        Banners = JsonConvert.DeserializeObject<List<Banner>>(saveData["banners"]);

        if (Banners == null)
            Banners = new List<Banner>();
    }
}
