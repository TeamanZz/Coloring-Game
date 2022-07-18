using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames.PictureColoring
{
	public class GameEventManager : EventManager<GameEventManager>
	{
		#region Inspector Variables

		#endregion

		#region Member Variables

		public const string LevelLoadingEvent			= "LevelLoading";
		public const string LevelLoadFinishedEvent		= "LevelLoadFinished";
		public const string LevelPlayedEvent			= "LevelPlayed";
		public const string LevelCompletedEvent			= "LevelCompleted";
		public const string LevelProgressDeletedEvent	= "LevelProgressDeleted";
		public const string LevelUnlockedEvent			= "LevelUnlocked";

		#endregion

		#region Protected Methods

		protected override Dictionary<string, List<Type>> GetEventDataTypes()
		{
			return new Dictionary<string, List<Type>>()
			{
				{ LevelLoadingEvent, new List<Type>() },
				{ LevelLoadFinishedEvent, new List<Type>() { typeof(bool) } },
				{ LevelPlayedEvent, new List<Type>() { typeof(LevelData) } },
				{ LevelCompletedEvent, new List<Type>() { typeof(LevelData) } },
				{ LevelProgressDeletedEvent, new List<Type>() { typeof(LevelData) } },
				{ LevelUnlockedEvent, new List<Type>() { typeof(LevelData) } },
			};
		}

		#endregion
	}
}
