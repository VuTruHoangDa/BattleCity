﻿using BattleCity.Items;
using BattleCity.Platforms;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


namespace BattleCity.Tanks
{
	[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
	public abstract class Tank : MonoBehaviour, IBulletCollision
	{
		public abstract Color color { get; set; }

		public abstract Vector3 direction { get; set; }

		public float speed { get; protected set; }

		public Ship ship;

		[SerializeField]
		[HideInInspector]
		protected SpriteRenderer spriteRenderer;

		[SerializeField]
		[HideInInspector]
		protected Animator animator;

		[SerializeField] protected Asset asset;
		public static ReadOnlyArray<ReadOnlyArray<Tank>> array { get; private set; }
		protected static Tank[][] Δarray;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init()
		{
			BattleField.onAwake += () =>
			{
				array = Util.NewReadOnlyArray(Main.level.width * 2,
					Main.level.height * 2, out Δarray);
			};
		}


		protected void Reset()
		{
			spriteRenderer = GetComponent<SpriteRenderer>();
			animator = GetComponent<Animator>();
		}


		protected void OnEnable()
		{
			isMoving = false;
			direction = this is Player ? Vector3.up : Vector3.down;
			animator.runtimeAnimatorController = null;
			index = (transform.position * 2).ToVector3Int();
			if (!array[index.x][index.y]) Δarray[index.x][index.y] = this;
			else AddTankToArray();

			#region Check Item
			if (Item.current && (Setting.enemyCanPickItem || this is Player)
				&& (Item.current.transform.position - transform.position).sqrMagnitude <= 0.5f)
				Item.current.OnCollision(this);
			#endregion

			async void AddTankToArray()
			{
				using var token = CancellationTokenSource.CreateLinkedTokenSource(Token, BattleField.Token);
				while (!token.IsCancellationRequested && !isMoving)
				{
					if (!array[index.x][index.y])
					{
						Δarray[index.x][index.y] = this;
						return;
					}

					await UniTask.Yield();
				}
			}
		}


		private CancellationTokenSource cts = new();
		public CancellationToken Token => cts.Token;
		protected void OnDisable()
		{
			if (this == array[index.x][index.y]) Δarray[index.x][index.y] = null;
			cts.Cancel();
			cts.Dispose();
			cts = new();
		}


		#region CanMove
		public bool CanMove(Vector3 newDir = default)
		{
			newDir = newDir == default ? direction : newDir;
			var origin = transform.position;
			foreach (var v in DIR_VECTORS[newDir])
			{
				var pos = origin + v;
				if (array[(int)(pos.x * 2)][(int)(pos.y * 2)]) return false;
				if (pos.ToVector3Int() != pos) continue;

				var platform = Platform.array[(int)pos.x][(int)pos.y];
				if (!platform) continue;

				if (!platform.AllowMove(this, newDir)) return false;
			}

			return true;
		}


		private static readonly IReadOnlyDictionary<Vector3, ReadOnlyArray<Vector3>>
			DIR_VECTORS = new Dictionary<Vector3, ReadOnlyArray<Vector3>>
			{
				[Vector3.up] = new(new Vector3[] { new(-0.5f, 1), new(0, 1), new(0.5f, 1), new(-0.5f, 0.5f), new(0.5f, 0.5f), new(0, 0.5f) }),
				[Vector3.right] = new(new Vector3[] { new(1, 0.5f), new(1, 0), new(1, -0.5f), new(0.5f, 0.5f), new(0.5f, -0.5f), new(0.5f, 0) }),
				[Vector3.down] = new(new Vector3[] { new(-0.5f, -1), new(0, -1), new(0.5f, -1), new(-0.5f, -0.5f), new(0.5f, -0.5f), new(0, -0.5f) }),
				[Vector3.left] = new(new Vector3[] { new(-1, 0.5f), new(-1, 0), new(-1, -0.5f), new(-0.5f, 0.5f), new(-0.5f, -0.5f), new(-0.5f, 0) }),
			};
		#endregion


		#region Move
		public bool isMoving { get; private set; }

		[Tooltip("Tối đa 0.125")]
		[SerializeField] private float moveSpeed;
		[Tooltip("Thời gian (ms) mỗi bước moveSpeed")]
		[SerializeField] private int delayMoving;
		private Vector3Int index;
		protected abstract RuntimeAnimatorController anim { get; }

		public async UniTask Move()
		{
#if DEBUG
			if (isMoving) throw new Exception("Khong the Move khi dang move !");
#endif
			isMoving = true;
			using var token = CancellationTokenSource.CreateLinkedTokenSource(Token, BattleField.Token);
			if (this == array[index.x][index.y]) Δarray[index.x][index.y] = null;
			index += direction.ToVector3Int();
			Δarray[index.x][index.y] = this;
			var moveDir = direction * moveSpeed;
			animator.runtimeAnimatorController = anim;
			for (float i = 0.5f / moveSpeed; i > 0; --i)
			{
				transform.position += moveDir;
				await UniTask.Delay(delayMoving);
				if (token.IsCancellationRequested) return;
			}

			transform.position = new(index.x * 0.5f, index.y * 0.5f);
			animator.runtimeAnimatorController = null;

			#region Check Item
			if (Item.current && (Setting.enemyCanPickItem || this is Player)
				&& (Item.current.transform.position - transform.position).sqrMagnitude <= 0.5f)
				Item.current.OnCollision(this);
			#endregion

			#region Check Special Platform

			#endregion

			isMoving = false;
		}
		#endregion


		#region Shoot
		[SerializeField] private float delayShooting;
		private float lastShootingTime = float.MinValue;
		[SerializeField] private int maxBullets;
		private readonly ValueWrapper<int> bulletCount = new();


		public Bullet Shoot()
		{
#if DEBUG
			if (isMoving) throw new Exception("Không thể Shoot khi đang Move !");
#endif
			if (bulletCount.value >= maxBullets
				|| Time.time - lastShootingTime < delayShooting) return null;

			++bulletCount.value;
			lastShootingTime = Time.time;
			var data = new Bullet.Data
			{
				origin = transform.position + direction * 0.5f,
				direction = direction,
				color = this is Player ? color : null,
				count = bulletCount,
				owner = this,
				ownerToken = Token
			};

			AddBulletData(ref data);
			return Bullet.New(data);
		}


		protected abstract void AddBulletData(ref Bullet.Data data);
		#endregion


		public abstract bool OnCollision(Bullet bullet);


		public abstract void Explode();
	}
}