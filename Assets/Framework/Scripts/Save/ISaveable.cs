using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames
{
	public interface ISaveable
	{
		string	SaveId		{ get; }
		bool	ShouldSave	{ get; set; }

		Dictionary<string, object> Save();
	}
}
