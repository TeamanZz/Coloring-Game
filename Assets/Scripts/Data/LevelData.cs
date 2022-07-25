using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames.PictureColoring
{
    [System.Serializable]
    public class LevelData
    {
        #region Inspector Variables

        public TextAsset levelFile;
        public int coinsToAward;
        public bool locked;
        public int coinsToUnlock;

        #endregion

        #region Member Variables

        private bool levelFileParsed;
        private string id;
        private string resourcesPath;
        private LevelSaveData levelSaveData;

        #endregion

        #region Properties

        public string Id
        {
            get
            {
                if (!levelFileParsed)
                {
                    ParseLevelFile();
                }

                return id;
            }
        }

        public string ResourcesPath
        {
            get
            {
                if (!levelFileParsed)
                {
                    ParseLevelFile();
                }

                return resourcesPath;
            }
        }

        public LevelSaveData LevelSaveData
        {
            get
            {
                if (levelSaveData == null)
                {
                    // Get the LevelSaveData from the GameManager
                    levelSaveData = GameManager.Instance.GetLevelSaveData(Id);
                }

                return levelSaveData;
            }
        }

        // Level data
        public LevelFileData LevelFileData { get; set; }
        public Texture2D ShareTexture { get; set; }

        #endregion

        #region Public Methods

        public bool IsColorComplete(int colorIndex)
        {
            if (LevelFileData == null)
            {
                Debug.LogError("[LevelData] IsColorRegionComplete | LevelFileData has not been loaded.");

                return false;
            }

            if (colorIndex < 0 || colorIndex >= LevelFileData.regions.Count)
            {
                Debug.LogErrorFormat("[LevelData] IsColorComplete | Given colorIndex ({0}) is out of bounds for the regions list of size {1}.", colorIndex, LevelFileData.regions.Count);

                return false;
            }

            LevelSaveData levelSaveData = LevelSaveData;
            List<Region> regions = LevelFileData.regions;

            for (int i = 0; i < regions.Count; i++)
            {
                Region region = regions[i];

                if (region.colorIndex == colorIndex && !levelSaveData.coloredRegions.Contains(region.id))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if all regions have been colored
        /// </summary>
        public bool AllRegionsColored()
        {
            if (LevelFileData == null)
            {
                Debug.LogError("[LevelData] AllRegionsColored | LevelFileData has not been loaded.");

                return false;
            }

            LevelSaveData levelSaveData = LevelSaveData;
            List<Region> regions = LevelFileData.regions;

            for (int i = 0; i < regions.Count; i++)
            {
                Region region = regions[i];

                if (region.colorIndex > -1 && !levelSaveData.coloredRegions.Contains(region.id))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets a random region index in the given ColorRegion that has not been colored in
        /// </summary>
        public int GetSmallestUncoloredRegion(int colorIndex)
        {
            if (LevelFileData == null)
            {
                Debug.LogError("[LevelData] GetRandomUncoloredRegion | LevelFileData has not been loaded.");

                return -1;
            }

            if (colorIndex < 0 || colorIndex >= LevelFileData.regions.Count)
            {
                Debug.LogErrorFormat("[LevelData] GetRandomUncoloredRegion | Given colorRegionIndex ({0}) is out of bounds for the colorRegions list of size {1}.", colorIndex, LevelFileData.regions.Count);

                return -1;
            }

            LevelSaveData levelSaveData = LevelSaveData;
            List<Region> regions = LevelFileData.regions;

            int minRegionSize = int.MaxValue;
            int index = -1;

            for (int i = 0; i < regions.Count; i++)
            {
                Region region = regions[i];

                if (colorIndex == region.colorIndex && !levelSaveData.coloredRegions.Contains(region.id))
                {
                    if (minRegionSize > region.numberSize)
                    {
                        minRegionSize = region.numberSize;
                        index = i;
                    }
                }
            }

            return index;
        }

        #endregion

        #region Private Methods

        private void ParseLevelFile()
        {
            string[] fileContents = levelFile.text.Split('\n');

            if (fileContents.Length != 2)
            {
                Debug.LogError(levelFile.name);
            }

            id = fileContents[0];
            resourcesPath = fileContents[1];

            levelFileParsed = true;
        }

        #endregion
    }
}
