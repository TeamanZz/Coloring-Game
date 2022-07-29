using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace BizzyBeeGames.PictureColoring
{
    public class GameManager : SaveableManager<GameManager>
    {
        #region Inspector Variables

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI bucketsCountText;
        [SerializeField] private CustomBucketToggle customBucketToggle;

        [Header("Data")]
        [SerializeField] private List<CategoryData> categories = null;

        [Header("Values")]
        [SerializeField] private bool awardHints = false;
        [SerializeField] private int numLevelsBetweenAds = 0;
        [SerializeField] private int bucketsCount;

        #endregion

        #region Member Variables

        private List<LevelData> allLevels;

        private bool isLevelLoading;
        private int loadId;

        private int numLevelsStarted;

        // Contains all LevelSaveDatas which have atleast one region colored in but have not been completed yet
        private Dictionary<string, LevelSaveData> playedLevelSaveDatas;

        /// <summary>
        /// Contains all level ids which have been completed by the player
        /// </summary>
        private HashSet<string> unlockedLevels;

        /// <summary>
        /// Levels that have been completed atleast one and the player has been awarded the coins/hints
        /// </summary>
        private HashSet<string> awardedLevels;

        //public bool BucketActive { get; private set; }

        #endregion

        #region Properties

        public override string SaveId { get { return "game_manager"; } }

        public List<CategoryData> Categories { get { return categories; } }
        public LevelData ActiveLevelData { get; private set; }

        public List<LevelData> AllLevels
        {
            get
            {
                if (allLevels == null)
                {
                    allLevels = new List<LevelData>();

                    for (int i = 0; i < categories.Count; i++)
                    {
                        allLevels.AddRange(categories[i].levels);
                    }
                }

                return allLevels;
            }
        }

        #endregion

        #region Unity Methods

        protected override void Awake()
        {
            base.Awake();

            playedLevelSaveDatas = new Dictionary<string, LevelSaveData>();
            awardedLevels = new HashSet<string>();
            unlockedLevels = new HashSet<string>();

            InitSave();

            ScreenManager.Instance.OnSwitchingScreens += OnSwitchingScreens;

            bucketsCountText.text = bucketsCount.ToString();
        }

        private void Start()
        {
            customBucketToggle.Toggle.onValueChanged.AddListener(SetBuckketActive);
        }

        #endregion

        #region Public Methods

        #region Bucket
        [ContextMenu("Add Bucket Point")]
        public void AddBucketPoint()
        {
            bucketsCount++;
            UpdateBucketUI();
        }

        public void UpdateBucketUI()
        {
            if (bucketsCount > 0)
            {
                customBucketToggle.countView.gameObject.SetActive(true);
                bucketsCountText.text = bucketsCount.ToString();

                customBucketToggle.isActive = true;
                customBucketToggle.ButtonProcessing(false);
            }
            else
            {
                customBucketToggle.countView.gameObject.SetActive(false);
                bucketsCountText.text = "";

                customBucketToggle.isActive = false;
                customBucketToggle.InactiveButton();
            }
        }

        public void SetBuckketActive(bool active)
        {
            if (active && bucketsCount > 0)
            {
                customBucketToggle.isActive = true;
                bucketsCountText.text = bucketsCount.ToString();
            }
            else
            {
                if (bucketsCount == 0)
                {
                    customBucketToggle.Toggle.isOn = false;
                    customBucketToggle.InactiveButton();

                    customBucketToggle.isActive = false;
                }
            }
        }
        #endregion

        /// <summary>
        /// Shows the level selected popup
        /// </summary>
        public void LevelSelected(LevelData levelData)
        {
            bool isLocked = levelData.locked && !levelData.LevelSaveData.isUnlocked;

            // Check if the level has been played or if its locked
            if (IsLevelPlaying(levelData.Id) || isLocked)
            {
                PopupManager.Instance.Show("level_selected", new object[] { levelData, isLocked }, (bool cancelled, object[] outData) =>
                {
                    if (!cancelled)
                    {
                        string action = outData[0] as string;

                        // Check what button the player selected
                        switch (action)
                        {
                            case "continue":
                                StartLevel(levelData);
                                break;
                            case "delete":
                                DeleteLevelSaveData(levelData);
                                break;
                            case "restart":
                                DeleteLevelSaveData(levelData);
                                StartLevel(levelData);
                                break;
                            case "unlock":
                                // Try and spend the coins required to unlock the level
                                if (CurrencyManager.Instance.TrySpend("coins", levelData.coinsToUnlock))
                                {
                                    UnlockLevel(levelData);
                                    StartLevel(levelData);
                                }
                                break;
                        }
                    }
                });
            }
            else
            {
                StartLevel(levelData);
            }
        }

        public void StartLevel(LevelData levelData)
        {
            // Check if there is already a level being loaded and if so cancel it
            if (isLevelLoading && ActiveLevelData != null)
            {
                LoadManager.Instance.Cancel(ActiveLevelData.Id, loadId);

                isLevelLoading = false;
            }

            // Set the new active LevelData
            ActiveLevelData = levelData;

            // Start loading everything needed to play the level
            loadId = LoadManager.Instance.LoadLevel(levelData, OnLevelLoaded);

            if (loadId == 0)
            {
                // If loadId is 0 then the level is already loaded
                OnLevelLoaded(levelData, true);
            }
            else
            {
                isLevelLoading = true;

                GameEventManager.Instance.SendEvent(GameEventManager.LevelLoadingEvent);
            }

            // Show the game screen now
            ScreenManager.Instance.Show("game");

            // Increate the number of levels started since the last ad was shown
            numLevelsStarted++;

            // Check if it's time to show an interstitial ad
            if (numLevelsStarted > numLevelsBetweenAds)
            {
                if (MobileAdsManager.Instance.ShowInterstitialAd(null))
                {
                    // If an ad was successfully shown then reset the num levels started
                    numLevelsStarted = 0;
                }
            }
        }

        private List<Region> GetRegionListByColorIndex(int colorIndex)
        {
            List<Region> regionList = new List<Region>(); // list with regions.id

            List<Region> regions = ActiveLevelData.LevelFileData.regions;

            // Check all regions and pushing regionId's with needed colorIndex to array
            for (int i = 0; i < regions.Count; i++)
            {
                Region region = regions[i];

                // Check if this region.colorIndex == colorIndex
                if (region.colorIndex == colorIndex)
                {
                    regionList.Add(region);
                }
            }

            return regionList;
        }

        public bool TryColorRegion(int x, int y, int colorIndex, out bool levelCompleted, out bool hintAwarded, out bool coinsAwarded)
        {
            List<Region> regionList = new List<Region>(); // list with regions should to be colored!
            LevelData activeLevelData = ActiveLevelData;

            levelCompleted = false;
            hintAwarded = false;
            coinsAwarded = false;

            if (activeLevelData != null)
            {
                Region region = GetRegionAt(x, y);


                if (region != null && region.colorIndex == colorIndex && !activeLevelData.LevelSaveData.coloredRegions.Contains(region.id))
                {
                    if (customBucketToggle.Toggle.isOn && bucketsCount > 0)
                    {
                        regionList = GetRegionListByColorIndex(colorIndex);
                        bucketsCount--;
                        bucketsCountText.text = bucketsCount.ToString();

                        customBucketToggle.Toggle.isOn = false;

                        //if (bucketsCount == 0)
                        //{
                        //    //customBucketToggle.isActive = false;
                        //    //customBucketToggle.Toggle.interactable = false
                            
                        //}

                        UpdateBucketUI();

                    }
                    else
                    {
                        regionList.Add(region);
                    }

                    foreach (Region regionS in regionList)
                    {
                        region = regionS;

                        if (region != null && region.colorIndex == colorIndex && !activeLevelData.LevelSaveData.coloredRegions.Contains(region.id)) // for exrta safety check
                        {
                            // Color the region
                            ColorRegion(region);

                            // Set the region as colored in the level save data
                            activeLevelData.LevelSaveData.coloredRegions.Add(region.id);

                            // Check if the level is not in the playedLevelSaveDatas dictionary, it not then this is the first region to be colored
                            if (!playedLevelSaveDatas.ContainsKey(activeLevelData.Id))
                            {
                                // Set the LevelSaveData of the active LevelData in the playedLevelSaveDatas so will will saved now that a region has been colored
                                playedLevelSaveDatas.Add(activeLevelData.Id, activeLevelData.LevelSaveData);

                                GameEventManager.Instance.SendEvent(GameEventManager.LevelPlayedEvent, activeLevelData);
                            }

                            // Check if all regions have been colored
                            levelCompleted = activeLevelData.AllRegionsColored();

                            if (levelCompleted)
                            {
                                // Check if this level has not been awarded hints / coins yet (ie. first time the level is completed)
                                if (!awardedLevels.Contains(activeLevelData.Id))
                                {
                                    awardedLevels.Add(activeLevelData.Id);

                                    if (awardHints)
                                    {
                                        // Award the player 1 hint for completing the level
                                        CurrencyManager.Instance.Give("hints", 1);

                                        hintAwarded = true;
                                    }

                                    if (activeLevelData.coinsToAward > 0)
                                    {
                                        // Award coins to the player for completing this level
                                        CurrencyManager.Instance.Give("coins", activeLevelData.coinsToAward);

                                        coinsAwarded = true;
                                    }
                                }

                                // The level is now complete
                                activeLevelData.LevelSaveData.isCompleted = true;

                                // Notify a lwevel has been compelted
                                GameEventManager.Instance.SendEvent(GameEventManager.LevelCompletedEvent, activeLevelData);
                            }
                        }
                    } // my foreach
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Returns a LevelSaveData for the given level id
        /// </summary>
        public LevelSaveData GetLevelSaveData(string levelId)
        {
            LevelSaveData levelSaveData = null;

            if (playedLevelSaveDatas.ContainsKey(levelId))
            {
                levelSaveData = playedLevelSaveDatas[levelId];
            }
            else
            {
                levelSaveData = new LevelSaveData();
            }

            if (unlockedLevels.Contains(levelId))
            {
                levelSaveData.isUnlocked = true;
            }

            return levelSaveData;
        }

        /// <summary>
        /// Returns true if the level was completed atleast once by the player
        /// </summary>
        public bool IsLevelPlaying(string levelId)
        {
            return playedLevelSaveDatas.ContainsKey(levelId);
        }

        /// <summary>
        /// Gets all level datas that are beening played or have been completed
        /// </summary>
        public void GetMyWorksLevelDatas(out List<LevelData> myWorksLeveDatas)
        {
            myWorksLeveDatas = new List<LevelData>();

            int completeInsertIndex = 0;

            for (int i = 0; i < categories.Count; i++)
            {
                List<LevelData> levelDatas = categories[i].levels;

                for (int j = 0; j < levelDatas.Count; j++)
                {
                    LevelData levelData = levelDatas[j];
                    string levelId = levelData.Id;

                    if (playedLevelSaveDatas.ContainsKey(levelId))
                    {
                        LevelSaveData levelSaveData = playedLevelSaveDatas[levelId];

                        if (levelSaveData.isCompleted)
                        {
                            myWorksLeveDatas.Insert(completeInsertIndex++, levelData);
                        }
                        else
                        {
                            myWorksLeveDatas.Add(levelData);
                        }
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Invoked when the LevelLoadManager has finished loading the active LevelData
        /// </summary>
        private void OnLevelLoaded(LevelData levelData, bool success)
        {
            isLevelLoading = false;

            GameEventManager.Instance.SendEvent(GameEventManager.LevelLoadFinishedEvent, success);
        }

        /// <summary>
        /// Gets the Region which contains the given pixel
        /// </summary>
        public Region GetRegionAt(int x, int y)
        {
            List<Region> regions = ActiveLevelData.LevelFileData.regions;

            // Check all regions for the one that contains the pixel
            for (int i = 0; i < regions.Count; i++)
            {
                Region region = regions[i];

                // Check if this region contains the pixel
                if (IsPixelInRegion(region, x, y))
                {
                    return region;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true if one of the triangles in this region contains the given pixel point
        /// </summary>
        private bool IsPixelInRegion(Region region, int pixelX, int pixelY)
        {
            Vector2 pixelPoint = new Vector2(pixelX, pixelY);

            // Check if the bounding box of the region contains the pixel
            if (!Math.RectangleContainsPoint(pixelPoint, region.bounds.minX, region.bounds.minY, region.bounds.maxX, region.bounds.maxY))
            {
                return false;
            }

            // The triangle vertices are store based on an origin point of 0,0 so we need to adjust the pixel point
            pixelPoint.x -= region.bounds.minX;
            pixelPoint.y -= region.bounds.minY;

            // Check all the triangles in the region to see if one of them contains the pixel point
            for (int i = 0; i < region.triangles.Count; i += 3)
            {
                Vector2 p1 = region.points[region.triangles[i]];
                Vector2 p2 = region.points[region.triangles[i + 1]];
                Vector2 p3 = region.points[region.triangles[i + 2]];

                if (Math.TriangleContainsPoint(pixelPoint, p1, p2, p3))
                {
                    // The pixel is in this region
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Colors the regions pixels on the levelData ColoredTexture
        /// </summary>
        private void ColorRegion(Region region)
        {
            // Color		regionColor		= ActiveLevelData.LevelFileData.colors[region.colorIndex];
            // Texture2D	coloredTexture	= ActiveLevelData.ColoredTexture;
            // Color[]		coloredPixels	= ActiveLevelData.ColoredTexture.GetPixels();
            // int			textureWidth	= coloredTexture.width;

            // int min = (region.pixelsByX ? region.bounds.minX : region.bounds.minY);

            // for (int j = 0; j < region.pixelsInRegion.Count; j++)
            // {
            // 	List<int[]> pixelSections = region.pixelsInRegion[j];

            // 	for (int k = 0; k < pixelSections.Count; k++)
            // 	{
            // 		int[]	startEnd	= pixelSections[k];
            // 		int		start		= startEnd[0];
            // 		int		end			= startEnd[1];

            // 		for (int i = start; i <= end; i++)
            // 		{
            // 			int x = region.pixelsByX ? min + j : i;
            // 			int y = region.pixelsByX ? i : min + j;

            // 			coloredPixels[y * textureWidth + x] = regionColor;
            // 		}
            // 	}
            // }

            // coloredTexture.SetPixels(coloredPixels);
            // coloredTexture.Apply();
        }

        /// <summary>
        /// Clears any progress from the level and sets the level as not completed
        /// </summary>
        private void DeleteLevelSaveData(LevelData levelData)
        {
            LevelSaveData levelSaveData = levelData.LevelSaveData;

            // Clear the colored regions
            levelSaveData.coloredRegions.Clear();

            // Make sure the completed flag is false
            levelSaveData.isCompleted = false;

            // Remove the level from the played and completed levels
            playedLevelSaveDatas.Remove(levelData.Id);

            // Cancel all loading operations that may be active for the current level
            LoadManager.Instance.Cancel(levelData.Id);

            GameEventManager.Instance.SendEvent(GameEventManager.LevelProgressDeletedEvent, levelData);
        }

        /// <summary>
        /// Unlocks the level
        /// </summary>
        private void UnlockLevel(LevelData levelData)
        {
            if (!unlockedLevels.Contains(levelData.Id))
            {
                unlockedLevels.Add(levelData.Id);
            }

            levelData.LevelSaveData.isUnlocked = true;

            GameEventManager.Instance.SendEvent(GameEventManager.LevelUnlockedEvent, new object[] { levelData });
        }

        /// <summary>
        /// Invoked by ScreenManager when screens are transitioning
        /// </summary>
        private void OnSwitchingScreens(string fromScreen, string toScreen)
        {
            // If we are moving away from the game screen then we need to unload all the game textures in the active level since they are no longer needed
            // and are taking up space
            if (fromScreen == "game" && ActiveLevelData != null)
            {
                if (isLevelLoading)
                {
                    LoadManager.Instance.Cancel(ActiveLevelData.Id, loadId);

                    isLevelLoading = false;
                }

                ActiveLevelData = null;
            }
        }

        #endregion

        #region Save Methods

        public override Dictionary<string, object> Save()
        {
            Dictionary<string, object> saveData = new Dictionary<string, object>();
            List<object> levelSaveDatas = new List<object>();

            foreach (KeyValuePair<string, LevelSaveData> pair in playedLevelSaveDatas)
            {
                Dictionary<string, object> levelSaveData = new Dictionary<string, object>();

                levelSaveData["key"] = pair.Key;
                levelSaveData["data"] = pair.Value.ToJson();

                levelSaveDatas.Add(levelSaveData);
            }

            saveData["levels"] = levelSaveDatas;
            saveData["awarded"] = SaveHashSetValues(awardedLevels);
            saveData["unlocked"] = SaveHashSetValues(unlockedLevels);

            return saveData;
        }

        protected override void LoadSaveData(bool exists, JSONNode saveData)
        {
            if (!exists)
            {
                return;
            }

            // Load all the levels that have some progress
            JSONArray levelSaveDatasJson = saveData["levels"].AsArray;

            for (int i = 0; i < levelSaveDatasJson.Count; i++)
            {
                JSONNode levelSaveDataJson = levelSaveDatasJson[i];
                string key = levelSaveDataJson["key"].Value;

                if (playedLevelSaveDatas.ContainsKey(key))
                    continue;

                JSONNode data = levelSaveDataJson["data"];

                LevelSaveData levelSaveData = new LevelSaveData();

                levelSaveData.FromJson(data);

                if (playedLevelSaveDatas.ContainsKey(key))
                    continue;

                playedLevelSaveDatas.Add(key, levelSaveData);
            }

            LoadHastSetValues(saveData["awarded"].Value, awardedLevels);
            LoadHastSetValues(saveData["unlocked"].Value, unlockedLevels);
        }

        /// <summary>
        /// Saves all values in the HashSet hash as a single string
        /// </summary>
        private string SaveHashSetValues(HashSet<string> hashSet)
        {
            string jsonStr = "";

            List<string> list = new List<string>(hashSet);

            for (int i = 0; i < list.Count; i++)
            {
                if (i != 0)
                {
                    jsonStr += ";";
                }

                jsonStr += list[i];
            }

            return jsonStr;
        }

        /// <summary>
        /// Loads the hast set values.
        /// </summary>
        private void LoadHastSetValues(string str, HashSet<string> hashSet)
        {
            string[] values = str.Split(';');

            for (int i = 0; i < values.Length; i++)
            {
                hashSet.Add(values[i]);
            }
        }

        #endregion

        #region Menu Items

#if UNITY_EDITOR

        [UnityEditor.MenuItem("Tools/Bizzy Bee Games/Clean Categories", priority = 201)]
        private static void CleanCategoryList()
        {
            GameManager gameManager = GameManager.Instance;

            int numRemoved = 0;
            int numUpdated = 0;

            if (gameManager.Categories == null)
            {
                Debug.LogWarning("Could not find a GameManager in the current open scene");

                return;
            }

            for (int i = 0; i < gameManager.Categories.Count; i++)
            {
                CategoryData categoryData = gameManager.Categories[i];

                if (categoryData.levels == null)
                {
                    continue;
                }

                for (int j = categoryData.levels.Count - 1; j >= 0; j--)
                {
                    LevelData levelData = categoryData.levels[j];

                    if (levelData.levelFile == null)
                    {
                        categoryData.levels.RemoveAt(j);
                        numRemoved++;

                        continue;
                    }

                    string levelFileContents = levelData.levelFile.text;
                    string[] levelFileLines = levelFileContents.Split('\n');

                    if (levelFileLines.Length < 2)
                    {
                        categoryData.levels.RemoveAt(j);
                        numRemoved++;

                        continue;
                    }

                    string levelFileResourcsePath = levelFileLines[1];
                    string levelFileAssetPath = UnityEditor.AssetDatabase.GetAssetPath(levelData.levelFile);

                    int resourcesIndex = levelFileAssetPath.IndexOf("/Resources/", System.StringComparison.Ordinal);

                    if (resourcesIndex == -1)
                    {
                        Debug.LogWarningFormat("The level file \"{0}\" in the \"{1}\" category is not in a Resources folder. This may cause the level to not" +
                                               "load when the game runs. If you want to move level files make sure you also more the .png file with the same name" +
                                               "and that they are located in a Resources folder.", levelData.levelFile.name, categoryData.displayName);

                        continue;
                    }

                    string currentLevelFileResourcesPath = levelFileAssetPath.Remove(0, resourcesIndex + "/Resources/".Length).Replace(".txt", "");

                    // Check if the asset path is still the same and if not update the path
                    if (levelFileResourcsePath != currentLevelFileResourcesPath)
                    {
                        levelFileLines[1] = currentLevelFileResourcesPath;

                        // Get the full file path of the level file and overwrite it with the new contents
                        string fullLevelFilePath = Application.dataPath + levelFileAssetPath.Remove(0, "Assets".Length);
                        string newContents = "";

                        for (int k = 0; k < levelFileLines.Length; k++)
                        {
                            if (k != 0) newContents += "\n";
                            newContents += levelFileLines[k];
                        }

                        Debug.LogFormat("Level file \"{0}\" in category \"{1}\" has been moved to \"{2}\", updating resources path in level file to \"{3}\"",
                                        levelData.levelFile.name, categoryData.displayName, levelFileAssetPath, currentLevelFileResourcesPath);

                        System.IO.File.WriteAllText(fullLevelFilePath, newContents);

                        numUpdated++;
                    }
                }
            }

            if (numRemoved == 0 && numUpdated == 0)
            {
                Debug.Log("Finished cleaning, everything looks good.");
            }
            else
            {
                Debug.LogFormat("Finished cleaning | Removed {0} levels with a missing/null level file | Updated {1} level files with new resources path", numRemoved, numUpdated);
            }
        }

        [UnityEditor.MenuItem("Tools/Bizzy Bee Games/Clean Categories", validate = true)]
        private static bool CleanCategoryListValidate()
        {
            return GameManager.Instance != null;
        }

        [UnityEditor.MenuItem("Tools/Bizzy Bee Games/Give 10000 Coins", priority = 300)]
        private static void Give1000Coins()
        {
            CurrencyManager.Instance.Give("coins", 10000);
        }

        [UnityEditor.MenuItem("Tools/Bizzy Bee Games/Give 10000 Coins", validate = true)]
        private static bool Give1000CoinsValidate()
        {
            return Application.isPlaying;
        }

        [UnityEditor.MenuItem("Tools/Bizzy Bee Games/Give 1000 Hints", priority = 301)]
        private static void Give1000Hints()
        {
            CurrencyManager.Instance.Give("hints", 1000);
        }

        [UnityEditor.MenuItem("Tools/Bizzy Bee Games/Give 1000 Hints", validate = true)]
        private static bool Give1000HintsValidate()
        {
            return Application.isPlaying;
        }

#endif

        #endregion
    }
}
