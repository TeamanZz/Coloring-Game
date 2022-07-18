using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.PictureColoring
{
    public class DailyScreen : Screen
    {
        #region Inspector Variables

        [SerializeField] private LevelListItem levelListItemPrefab = null;
        [SerializeField] private GridLayoutGroup levelListContainer = null;
        [SerializeField] private ScrollRect levelListScrollRect = null;

        #endregion

        #region Member Variables

        private RecyclableListHandler<LevelData> levelListHandler;
        private int activeCategoryIndex;

        #endregion

        #region Public Methods

        public override void Initialize()
        {
            base.Initialize();

            // Set the cells size based on the width of the screen
            Utilities.SetGridCellSize(levelListContainer);

            SetupLibraryList();

            GameEventManager.Instance.RegisterEventHandler(GameEventManager.LevelPlayedEvent, OnLevelGameEvent);
            GameEventManager.Instance.RegisterEventHandler(GameEventManager.LevelCompletedEvent, OnLevelGameEvent);
            GameEventManager.Instance.RegisterEventHandler(GameEventManager.LevelProgressDeletedEvent, OnLevelGameEvent);
            GameEventManager.Instance.RegisterEventHandler(GameEventManager.LevelUnlockedEvent, OnLevelGameEvent);
        }

        public override void OnShowing()
        {
            if (levelListHandler != null)
            {
                levelListHandler.Refresh();
            }
        }

        #endregion

        #region Private Methods

        private void OnLevelGameEvent(string id, object[] data)
        {
            // Call the Setup method on all visible LevelListItems
            levelListHandler.Refresh();
        }

        /// <summary>
        /// Clears then resets the list of library level items using the current active category index
        /// </summary>
        private void SetupLibraryList()
        {
            activeCategoryIndex = GameManager.Instance.Categories.FindIndex(x => x.displayName == "Daily");

            if (activeCategoryIndex > GameManager.Instance.Categories.Count)
            {
                return;
            }

            List<LevelData> levelDatas = null;


            levelDatas = GameManager.Instance.Categories[activeCategoryIndex].levels;


            if (levelListHandler == null)
            {
                levelListHandler = new RecyclableListHandler<LevelData>(levelDatas, levelListItemPrefab, levelListContainer.transform as RectTransform, levelListScrollRect);

                levelListHandler.OnListItemClicked = GameManager.Instance.LevelSelected;

                levelListHandler.Setup();
            }
            else
            {
                levelListHandler.UpdateDataObjects(levelDatas);
            }
        }

        #endregion
    }
}
