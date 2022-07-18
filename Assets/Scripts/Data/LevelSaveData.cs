using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames.PictureColoring
{
	public class LevelSaveData
	{
		#region Member Variables

		public HashSet<int> coloredRegions;
		public bool			isCompleted;
		public bool			isUnlocked;		// This does not need to be included in the save dat since the GameManager handles saving/loading unlocked levels

		#endregion

		#region Public Methods

		public LevelSaveData()
		{
			coloredRegions = new HashSet<int>();
		}

		public object ToJson()
		{
			Dictionary<string, object> json = new Dictionary<string, object>();

			string jsonStr = "";

			List<int> coloredRegionValues = new List<int>(coloredRegions);

			for (int i = 0; i < coloredRegionValues.Count; i++)
			{
				if (i != 0)
				{
					jsonStr += ";";
				}

				jsonStr += coloredRegionValues[i];
			}

			json["colored_regions"]	= jsonStr;
			json["is_completed"]	= isCompleted;

			return json;
		}

		public void FromJson(JSONNode json)
		{
			string[] values = json["colored_regions"].Value.Split(';');

			for (int i = 0; i < values.Length; i++)
			{
				coloredRegions.Add(int.Parse(values[i]));
			}

			isCompleted	= json["is_completed"].AsBool;
		}

		#endregion
	}
}
