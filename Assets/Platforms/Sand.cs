using BattleCity.Tanks;
using UnityEngine;


namespace BattleCity.Platforms
{
	[RequireComponent(typeof(SpriteRenderer))]
	public sealed class Sand : Platform
	{
		public override bool AllowMove(Tank tank, Vector3 newDir) => true;


		public override bool OnCollision(Bullet bullet) => false;

	}
}