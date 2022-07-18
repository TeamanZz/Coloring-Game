using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames
{
	public class RecyclableListHandler<T>
	{
		#region Classes

		private class Animation
		{
			private RectTransform	target;
			private int				index;
			private float			timer;
			private float			from;
			private float			to;
		}

		#endregion

		#region Inspector Variables

		#endregion

		#region Member Variables

		private List<T>					dataObjects;
		private RecyclableListItem<T>	listItemPrefab;
		private RectTransform			listContainer;
		private ScrollRect				listScrollRect;

		private ObjectPool				listItemPool;
		private List<RectTransform>		listItemPlaceholders;
		private int						topItemIndex;
		private int						bottomItemIndex;

		#endregion

		#region Properties

		public System.Action<RecyclableListItem<T>>	OnListItemCreated { get; set; }
		public System.Action<T>						OnListItemClicked { get; set; }

		private Vector2 ListItemSize { get { return listItemPrefab.RectT.sizeDelta; } }

		#endregion

		#region Constructor

		public RecyclableListHandler(List<T> dataObjects, RecyclableListItem<T> listItemPrefab, RectTransform listContainer, ScrollRect listScrollRect)
		{
			this.dataObjects		= dataObjects;
			this.listItemPrefab		= listItemPrefab;
			this.listContainer		= listContainer;
			this.listScrollRect		= listScrollRect;

			listItemPool			= new ObjectPool(listItemPrefab.gameObject, 0, ObjectPool.CreatePoolContainer(listContainer));
			listItemPlaceholders	= new List<RectTransform>();
		}

		#endregion

		#region Public Methods

		public void UpdateDataObjects(List<T> newDataObjects)
		{
			dataObjects = newDataObjects;

			SyncPlaceholdersObjects();

			Reset();
		}

		public void Setup()
		{
			listScrollRect.onValueChanged.AddListener(OnListScrolled);

			SyncPlaceholdersObjects();

			LayoutRebuilder.ForceRebuildLayoutImmediate(listContainer);

			Reset();
		}

		public void Reset()
		{
			listContainer.anchoredPosition = Vector2.zero;

			RemoveAllListItems();

			LayoutRebuilder.ForceRebuildLayoutImmediate(listContainer);

			UpdateList(true);
		}

		public void Refresh()
		{
			for (int i = topItemIndex; i <= bottomItemIndex && i >= 0 && i < listItemPlaceholders.Count; i++)
			{
				RectTransform placeholder = listItemPlaceholders[i];

				if (placeholder.childCount == 1)
				{
					RecyclableListItem<T> listItem = placeholder.GetChild(0).GetComponent<RecyclableListItem<T>>();

					listItem.Setup(dataObjects[i]);
				}
			}
		}

		#endregion

		#region Private Methods

		private void OnListScrolled(Vector2 pos)
		{
			UpdateList();
		}

		private void SyncPlaceholdersObjects()
		{
			// Set all the placeholders we need that are already created to active
			for (int i = 0; i < dataObjects.Count && i < listItemPlaceholders.Count; i++)
			{
				listItemPlaceholders[i].gameObject.SetActive(true);
			}

			// Create any more placeholders we need to fill the list of data objects
			while (listItemPlaceholders.Count < dataObjects.Count)
			{
				GameObject		placeholder		= new GameObject("list_item");
				RectTransform	placholderRectT	= placeholder.AddComponent<RectTransform>();

				placholderRectT.SetParent(listContainer, false);

				placholderRectT.sizeDelta = ListItemSize;

				listItemPlaceholders.Add(placholderRectT);
			}

			// Set any placeholders we dont need to de-active
			for (int i = dataObjects.Count; i < listItemPlaceholders.Count; i++)
			{
				listItemPlaceholders[i].gameObject.SetActive(false);
			}
		}

		private void RemoveAllListItems()
		{
			for (int i = 0; i < listItemPlaceholders.Count; i++)
			{
				RemoveListItem(listItemPlaceholders[i]);
			}
		}

		private void UpdateList(bool reset = false)
		{
			if (reset)
			{
				topItemIndex	= 0;
				bottomItemIndex = FillList(topItemIndex, 1);
			}
			else
			{
				RecycleList();

				topItemIndex	= FillList(topItemIndex, -1);
				bottomItemIndex	= FillList(bottomItemIndex, 1);
			}
		}

		private int FillList(int startIndex, int indexInc)
		{
			int lastVisibleIndex = startIndex;

			for (int i = startIndex; i >= 0 && i < dataObjects.Count; i += indexInc)
			{
				RectTransform placeholder = listItemPlaceholders[i];

				if (!IsVisible(i, placeholder))
				{
					break;
				}

				lastVisibleIndex = i;

				if (placeholder.childCount == 0)
				{
					AddListItem(i, placeholder, indexInc == -1);
				}
			}

			return lastVisibleIndex;
		}

		private void RecycleList()
		{
			// If there are no items in the list then just return now
			if (listItemPlaceholders.Count == 0)
			{
				return;
			}

			for (int i = topItemIndex; i <= bottomItemIndex; i++)
			{
				RectTransform placeholder = listItemPlaceholders[i];

				if (IsVisible(i, placeholder))
				{
					break;
				}
				else if (placeholder.childCount == 1)
				{
					RemoveListItem(placeholder);

					topItemIndex++;
				}
			}

			for (int i = bottomItemIndex; i >= topItemIndex; i--)
			{
				RectTransform placeholder = listItemPlaceholders[i];

				if (IsVisible(i, placeholder))
				{
					break;
				}
				else if (placeholder.childCount == 1)
				{
					RemoveListItem(placeholder);

					bottomItemIndex--;
				}
			}

			// Check if top index is now greater than bottom index, if so then all elements were recycled so we need to find the new top
			if (topItemIndex > bottomItemIndex)
			{
				int				targetIndex 		= (topItemIndex < dataObjects.Count) ? topItemIndex : bottomItemIndex;
				RectTransform	targetPlaceholder	= listItemPlaceholders[targetIndex];
				float			viewportTop			= listContainer.anchoredPosition.y;

				if (-targetPlaceholder.anchoredPosition.y < viewportTop)
				{
					for (int i = targetIndex; i < dataObjects.Count; i++)
					{
						if (IsVisible(i, listItemPlaceholders[i]))
						{
							topItemIndex	= i;
							bottomItemIndex	= i;
							break;
						}
					}
				}
				else
				{
					for (int i = targetIndex; i >= 0; i--)
					{
						if (IsVisible(i, listItemPlaceholders[i]))
						{
							topItemIndex	= i;
							bottomItemIndex	= i;
							break;
						}
					}
				}
			}
		}

		private bool IsVisible(int index, RectTransform placeholder)
		{
			RectTransform viewport = listScrollRect.viewport as RectTransform;

			float placeholderTop	= -placeholder.anchoredPosition.y - placeholder.rect.height / 2f;
			float placeholderbottom	= -placeholder.anchoredPosition.y + placeholder.rect.height / 2f;

			float viewportTop		= listContainer.anchoredPosition.y;
			float viewportbottom	= listContainer.anchoredPosition.y + viewport.rect.height;

			return placeholderTop < viewportbottom && placeholderbottom > viewportTop;
		}

		private void AddListItem(int index, RectTransform placeholder, bool addingOnTop)
		{
			bool itemInstantiated;

			RecyclableListItem<T> listItem = listItemPool.GetObject<RecyclableListItem<T>>(placeholder, out itemInstantiated);

			T dataObject = dataObjects[index];

			listItem.Index = index;

			if (OnListItemClicked != null)
			{
				listItem.Data				= dataObject;
				listItem.OnListItemClicked	= OnItemClicked;
			}

			if (itemInstantiated)
			{
				if (OnListItemCreated != null)
				{
					OnListItemCreated(listItem);
				}

				listItem.Initialize(dataObject);
			}

			listItem.Setup(dataObject);
		}

		private void RemoveListItem(Transform placeholder)
		{
			if (placeholder.childCount == 1)
			{
				RecyclableListItem<T> listItem = placeholder.GetChild(0).GetComponent<RecyclableListItem<T>>();

				// Return the list item object to the pool
				ObjectPool.ReturnObjectToPool(listItem.gameObject);

				// Notify that it has been removed from the list
				listItem.Removed();
			}
		}

		private void OnItemClicked(int index, object dataObject)
		{
			OnListItemClicked((T)dataObject);
		}

		#endregion
	}
}
