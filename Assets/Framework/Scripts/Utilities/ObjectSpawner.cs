using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames
{
	public class ObjectSpawner : UIMonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] protected SpawnObject	spawnObjectPrefab;
		[SerializeField] protected float		spawnRate;

		#endregion

		#region Member Variables

		protected ObjectPool	spawnObjectPool;
		protected float			timer;

		#endregion

		#region Unity Methods

		protected virtual void Start()
		{
			spawnObjectPool = new ObjectPool(spawnObjectPrefab.gameObject, 0, transform, ObjectPool.PoolBehaviour.CanvasGroup);
		}

		protected virtual void Update()
		{
			timer -= Time.deltaTime;

			if (timer <= 0)
			{
				SpawnObject();

				timer = spawnRate;
			}
		}

		#endregion

		#region Protected Methods

		protected virtual void SpawnObject()
		{
			spawnObjectPool.GetObject<SpawnObject>().Spawned();
		}

		#endregion
	}
}
