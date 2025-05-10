using BattleCity.Items;
using BattleCity.Platforms;
using Cysharp.Threading.Tasks;
using RotaryHeart.Lib.SerializableDictionary;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;


namespace BattleCity.Tanks
{
	public sealed class Player : Tank, IGamepad
	{
		public static readonly IReadOnlyDictionary<Color, Player> players = new Dictionary<Color, Player>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init()
		{
			var dict = players as Dictionary<Color, Player>;
			var anchor = new GameObject { name = "Players" }.transform;
			anchor.gameObject.SetActive(false);
			DontDestroyOnLoad(anchor);
			foreach (var color in new Color[] { Color.Green, Color.Yellow })
			{
				var player = dict[color] = Addressables.InstantiateAsync("Assets/Tanks/Prefab/Player.prefab", anchor)
					.WaitForCompletion().GetComponent<Player>();
				player.gameObject.SetActive(false);
				player.name = color.ToString();
				player.color = color;
			}

			anchor.gameObject.SetActive(true);
		}


		public static async UniTask<Player> New(Color color, bool borrowLife = false)
		{
			var token = BattleField.Token;
			await UniTask.Delay(1000); // Animation
			if (token.IsCancellationRequested) return null;

			if (BattleField.count == 1 || players[color].isExploded) goto CHECK_LIFE;
			goto SPAWN_PLAYER;

		CHECK_LIFE:
			if (BattleField.playerLifes[color] != 0)
			{
				--BattleField.playerLifes[color];
				goto SPAWN_PLAYER;
			}

			if (!borrowLife) return null;

			// Kiểm tra đồng đội còn mạng không để mượn mạng
			var allyColor = color == Color.Yellow ? Color.Green : Color.Yellow;
			if (BattleField.playerLifes[allyColor] != 0)
			{
				--BattleField.playerLifes[allyColor];
				goto SPAWN_PLAYER;
			}

			return null;

		SPAWN_PLAYER:
			var pos = Main.level.playerIndexes[color].ToVector3();
			try { Destroy(Platform.array[(int)pos.x][(int)pos.y].gameObject); }
			catch { }

			var player = players[color];
			player.transform.position = pos;
			player.gameObject.SetActive(true);
			return player;
		}


		[SerializeField]
		private SerializableDictionaryBase<int, SerializableDictionaryBase<Color,
			SerializableDictionaryBase<Vector3, Sprite>>> sprites;

		[SerializeField]
		private SerializableDictionaryBase<int, SerializableDictionaryBase<Color,
			SerializableDictionaryBase<Vector3, RuntimeAnimatorController>>> anims;

		protected override RuntimeAnimatorController anim
			=> star == 3 ? asset.gunAnims[color][direction]
			: anims[star][color][direction];


		private Color Δcolor;
		public override Color color
		{
			get => Δcolor;

			set
			{
				Δcolor = value;
				spriteRenderer.sprite = star == 3 ? asset.gunSprites[color][direction]
					: sprites[star][color][direction];
			}
		}


		private Vector3 Δdirection = Vector3.up;
		public override Vector3 direction
		{
			get => Δdirection;
			set
			{
				Δdirection = value;
				spriteRenderer.sprite = star == 3 ? asset.gunSprites[color][Δdirection = value]
					: sprites[star][color][direction];
			}
		}


		private byte star, fireStar;
		public void IncreaseStar(byte value)
		{
			star += value;
			if (star < 3) spriteRenderer.sprite = sprites[star][color][direction];
			else
			{
				star = 3;
				spriteRenderer.sprite = asset.gunSprites[color][direction];
			}

			fireStar += value;
			if (fireStar >= 7)
			{
				canBurnForest = false;
				fireStar = star;
			}
			else if (fireStar >= 5) canBurnForest = true;
		}


		public Helmet helmet;
		private bool canBurnForest;
		public override bool OnCollision(Bullet bullet)
		{
			if (helmet) return true;

			if (bullet.data.color != null)
			{
				// Player bullet
				if (!Setting.playerCanFreezePlayer) return false;
				Freeze();
				return true;
			}

			// Enemy bullet
			if (ship)
			{
				Destroy(ship.gameObject);
				ship = null;
			}
			else
			{
				canBurnForest = false;
				if (star == 3)
				{
					fireStar = star = 2;
					spriteRenderer.sprite = sprites[star][color][direction];
				}
				else
				{
					fireStar = 0;
					Explode();
				}
			}

			return true;
		}


		private async void Freeze()
		{
			throw new NotImplementedException();
		}


		private new void OnEnable()
		{
			base.OnEnable();
			isExploded = false;
			helmet = null;
			Item.New<Helmet>().OnCollision(this);
			if (!GetComponent<AI>()) Gamepad.Add(color, this);
		}


		private new void OnDisable()
		{ 
			Gamepad.Remove(color, this);
			base.OnDisable();
		}

		
		public bool isExploded { get; private set; }
		public override async void Explode()
		{
			isExploded = true;
			spriteRenderer.sprite = sprites[star = 0][color][direction];
			gameObject.SetActive(false);

			// Animation

			var token = BattleField.Token;
			await UniTask.Delay(1000);
			if (token.IsCancellationRequested) return;

			if (BattleField.playerLifes[color] != 0) New(color).Forget();
			else if (BattleField.playerLifes[3 - color] == 0 && !players[3 - color].gameObject.activeSelf) BattleField.End();
		}


		protected override void AddBulletData(ref Bullet.Data data)
		{
			data.canBurnForest = canBurnForest;
			data.canDestroySteel = star == 3;
		}


		#region Gamepad
		private Vector3 moveRequest;
		public void ButtonDpad(Vector3 direction, bool press)
		{
			moveRequest = press ? direction : default;
			if (press && !task.isRunning()) task = Task();
		}


		private bool shootRequest;
		public void ButtonShoot()
		{
			if (!isMoving)
			{
				Shoot();
				return;
			}

			shootRequest = true;
			if (!task.isRunning()) task = Task();
		}


		private bool shootContinousRequest;
		public void ButtonShoot(bool press)
		{
			shootContinousRequest = press;
			if (press && !task.isRunning()) task = Task();
		}


		public void ButtonSelect()
		{
		}


		public void ButtonStart()
		{
		}


		private UniTask task = UniTask.CompletedTask;
		private async UniTask Task()
		{
			using var token = CancellationTokenSource.CreateLinkedTokenSource(Token, BattleField.Token);

			while (!token.IsCancellationRequested &&
				(moveRequest != default || shootContinousRequest || shootRequest))
			{
				if (shootContinousRequest) Shoot();
				else if (shootRequest)
				{
					shootRequest = false;
					Shoot();
				}

				if (moveRequest != default)
				{
					if (direction != moveRequest)
					{
						direction = moveRequest;
						await UniTask.DelayFrame(5);
						continue;
					}

					if (CanMove()) await Move(); else await UniTask.Yield();
				}
				else await UniTask.Yield();
			}
		}
		#endregion
	}
}