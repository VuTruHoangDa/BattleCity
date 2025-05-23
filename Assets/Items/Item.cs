using BattleCity.Tanks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;


namespace BattleCity.Items
{
	[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
	public abstract class Item : MonoBehaviour
	{
		public static Item current { get; protected set; }
		private static readonly ReadOnlyArray<string> PREFAB_NAMES;

		static Item()
		{
			var types = Assembly.GetAssembly(typeof(Item)).GetTypes()
				.Where(t => t.IsSubclassOf(typeof(Item)));

			var array = new string[types.Count()];
			int i = 0;
			foreach (var type in types) array[i++] = type.Name.Split('.')[^1];
			PREFAB_NAMES = new(array);
		}


		private static readonly List<Tank> tmp = new();
		private static readonly ReadOnlyArray<Vector2Int> VECTORS = new(new Vector2Int[]
		{
			new (-1,1), new(0,1), new(1,1),
			new (-1,0), default, new(1,0),
			new(-1,-1), new(0,-1), new(1,-1)
		});

		public static void New()
		{
			if (current) Destroy(current.gameObject);
			var origin = new Vector2Int(
				Random.Range(2, (Main.level.width - 2) * 2 + 1),
				Random.Range(2, (Main.level.height - 2) * 2 + 1));
			current = Addressables.InstantiateAsync("Assets/Items/Prefab" +
				$"/{PREFAB_NAMES[Random.Range(0, PREFAB_NAMES.Length)]}.prefab",
				origin.ToVector3() / 2f,
				Quaternion.identity).WaitForCompletion().GetComponent<Item>();

			#region Check Tank collision
			tmp.Clear();
			for (int v = 0; v < VECTORS.Length; ++v)
			{
				var index = origin + VECTORS[v];
				var tank = Tank.array[index.x][index.y];
				if (tank && (tank is Player || Setting.enemyCanPickItem)) tmp.Add(tank);
			}

			if (tmp.Count != 0) current.OnCollision(tmp[Random.Range(0, tmp.Count)]);
			#endregion
		}


		public static T New<T>(in Vector3 position = default) where T : Item
			=> Addressables.InstantiateAsync($"Assets/Items/Prefab/{typeof(T).Name}.prefab", position, Quaternion.identity)
			.WaitForCompletion().GetComponent<T>();


		public abstract void OnCollision(Tank tank);


		protected void OnDisable()
		{
			if (this == current) current = null;
		}
	}
}