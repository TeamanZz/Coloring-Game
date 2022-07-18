using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames.PictureColoring
{
	public class MainScreen : Screen
	{
		#region Classes

		[System.Serializable]
		private class SubScreen
		{
			public Screen					screen		= null;
			public MainScreenSubNavButton	navButton	= null;
		}

		#endregion

		#region Inspector Variables

		[Space]

		[SerializeField] private List<SubScreen> subScreens = null;

		#endregion

		#region Member Variables

		private SubScreen currentSubScreen;

		#endregion

		#region Public Methods

		public override void Initialize()
		{
			base.Initialize();

			if (subScreens.Count > 0)
			{
				for (int i = 0; i < subScreens.Count; i++)
				{
					SubScreen subScreen = subScreens[i];

					subScreen.screen.Initialize();
					subScreen.screen.gameObject.SetActive(true);
					subScreen.screen.Hide(false, true);
				}

				ShowSubScreen(subScreens[0], true);
			}
		}

		public void ShowSubScreen(string subScreenId)
		{
			SubScreen subScreen = GetSubScreen(subScreenId);

			if (subScreen != null && currentSubScreen != subScreen)
			{
				ShowSubScreen(subScreen, false);
			}
		}

		public override void OnShowing()
		{
			if (currentSubScreen != null)
			{
				currentSubScreen.screen.OnShowing();
			}
		}

		public override void OnHiding()
		{
			if (currentSubScreen != null)
			{
				currentSubScreen.screen.OnHiding();
			}
		}

		#endregion

		#region Private Methods

		private SubScreen GetSubScreen(string screenId)
		{
			for (int i = 0; i < subScreens.Count; i++)
			{
				SubScreen subScreen = subScreens[i];

				if (subScreen.screen.Id == screenId)
				{
					return subScreen;
				}
			}

			return null;
		}

		private void ShowSubScreen(SubScreen subScreen, bool immediate)
		{
			bool transitionLeft = currentSubScreen == null || subScreens.IndexOf(currentSubScreen) > subScreens.IndexOf(subScreen);

			if (currentSubScreen != null)
			{
				currentSubScreen.screen.Hide(transitionLeft, immediate);
				currentSubScreen.navButton.SetSelected(false);
			}

			subScreen.screen.Show(transitionLeft, immediate);
			subScreen.navButton.SetSelected(true);

			currentSubScreen = subScreen;
		}

		#endregion
	}
}
