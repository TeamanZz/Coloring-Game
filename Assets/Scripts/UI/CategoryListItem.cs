using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.PictureColoring
{
    public class CategoryListItem : ClickableListItem
	{
		#region Inspector Variables

		[SerializeField] private Text	categoryText	= null;
		[SerializeField] private Color	normalColor		= Color.white;
		[SerializeField] private Color	selectedColor	= Color.white;

		#endregion

		#region Public Methods

		public void Setup(string displayText)
		{
			categoryText.text = displayText;
		}

		public void SetSelected(bool isSelected)
		{
			categoryText.color		= isSelected ? selectedColor : normalColor;
		}

		#endregion
	}
}
