using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;


namespace BattleCity.Tanks
{
	public sealed class AI : MonoBehaviour
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init()
		{
			BattleField.onAwake += () =>
			{
				enableEnemy = true;
				dict[true].Clear();
				dict[false].Clear();
			};
		}


		private Tank tank;
		private bool isPlayer;
		private void Awake()
		{
			isPlayer = (tank = GetComponent<Tank>()) is Player;
			dict[isPlayer].Add(this);
		}


		private void OnDestroy()
		{
			dict[isPlayer].Remove(this);
		}


		private static bool enableEnemy;
		private void OnEnable()
		{
			if (isPlayer || enableEnemy) MoveAndShoot();
			else enabled = false;
		}


		private CancellationTokenSource cts = new();
		private void OnDisable()
		{
			cts.Cancel();
			cts.Dispose();
			cts = new();
		}


		private static readonly IReadOnlyDictionary<bool, List<AI>> dict = new Dictionary<bool, List<AI>>
		{
			[true] = new(),
			[false] = new()
		};

		public static void SetActive<T>(bool value) where T : Tank
		{
			bool isPlayer = typeof(T) == typeof(Player);
			if (!isPlayer) enableEnemy = value;
			foreach (var ai in dict[isPlayer]) ai.enabled = value;
		}


		private const int DELAY = 200;
		private async void MoveAndShoot()
		{
			using var token = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, BattleField.Token);
			var vectors = new List<Vector3>
			{
				Vector3.up, Vector3.right, Vector3.down, Vector3.left
			};
			var lastDir = vectors[Random.Range(0, vectors.Count)];
			vectors.Remove(lastDir);

			while (true)
			{
				var dir = vectors[Random.Range(0, vectors.Count)];
				vectors.Add(lastDir);
				vectors.Remove(lastDir = dir);
				tank.direction = dir;
				CheckShoot();
				if (!tank.CanMove())
				{
					await UniTask.Delay(DELAY);
					if (token.IsCancellationRequested) return;
					continue;
				}

				for (int i = Random.Range(1, 21); i > 0; --i)
				{
					await tank.Move();
					if (token.IsCancellationRequested) return;
					CheckShoot();
					if (!tank.CanMove())
					{
						await UniTask.Delay(DELAY);
						if (token.IsCancellationRequested) return;
						break;
					}
				}
			}


			void CheckShoot()
			{
				if (Random.Range(0, 2) == 0) tank.Shoot();
			}
		}


		private async void StandShoot()
		{
			using var token = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, BattleField.Token);

		}
	}
}