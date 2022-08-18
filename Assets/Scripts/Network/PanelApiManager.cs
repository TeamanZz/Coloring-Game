using Assets.Scripts.Network.Models;
using Assets.Scripts.Network.Utils;
using BizzyBeeGames;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using JeffreyLanters.WebRequests;
using UnityEngine;

public class PanelApiManager : SaveableManager<PanelApiManager>
{
    public override string SaveId => nameof(PanelApiManager);

    [SerializeField] private string _apiLink = "http://localhost/api";
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
        RefreshBannersAsync();
    }

    public string BannerImage(int id) => _apiLink + $"/data/banner/image/{id}";

    public async void RefreshBannersAsync()
    {
        string lang = Lang.DeviceLang();

        var geo = new GeoData();

        try
        {
            var response = await new WebRequest(_ipService).Send();
            geo = response.Json<GeoData>();
        }
        catch (WebRequestException exception)
        {
            Debug.Log($"Error {exception.httpStatusCode} while fetching {exception.url}");
        }

        try
        {
            string matchJson = JsonConvert.SerializeObject(new Match()
            {
                Geo = geo.countryCode,
                Lang = lang,
            });

            var response = await new WebRequest(_apiLink + "/data/banner/match") {
                method = RequestMethod.Post,
                contentType = ContentType.ApplicationJson,
                body = matchJson
            }.Send();

            var banners = JsonConvert.DeserializeObject<List<Banner>>(response.webRequestResponseText);

            Banners = banners;

        }
        catch { }

        OnBannersRefreshed?.Invoke();
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


        OnBannersRefreshed?.Invoke();
    }
}
