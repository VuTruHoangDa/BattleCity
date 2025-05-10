using System.Collections.Generic;
using UnityEngine;
using G = UnityEngine.InputSystem.Gamepad;


namespace BattleCity
{
	public interface IGamepad
	{
		void ButtonDpad(Vector3 direction, bool press);

		/// <summary>
		/// Bắn từng viên
		/// </summary>
		void ButtonShoot();

		/// <summary>
		/// Bắn liên tục (có độ trễ) cho đến khi thả nút
		/// </summary>
		/// <param name="press"></param>
		void ButtonShoot(bool press);

		void ButtonSelect();

		void ButtonStart();
	}



	/// <summary>
	/// Sử dụng Gamepad.Add() và Gamepad.Remove() trong hàm Start. Không được sử dụng trong Awake
	/// </summary>
	public sealed class Gamepad : MonoBehaviour
	{
		[SerializeField] private Color color;

		private static readonly IReadOnlyDictionary<Color, Gamepad> gamepads = new Dictionary<Color, Gamepad>();
		private void Awake()
		{
			if (gamepads.ContainsKey(color)) throw new System.Exception();
			DontDestroyOnLoad(((gamepads as Dictionary<Color, Gamepad>)[color] = this).transform.parent);
		}


		private readonly IReadOnlyList<IGamepad> listeners = new List<IGamepad>();

		public static void Add(Color color, IGamepad listener)
		{
			var listeners = gamepads[color].listeners as List<IGamepad>;
			if (!listeners.Contains(listener)) listeners.Add(listener);
		}


		public static void Remove(Color color, IGamepad listener)
		{
			var listeners = gamepads[color].listeners as List<IGamepad>;
			if (listeners.Contains(listener)) listeners.Remove(listener);
		}


		private G g;
		private bool shootContinous;

		private void Update()
		{
			#region Keyboard
			if (color == Color.Yellow)
			{
				#region Yellow
				// Dpad
				if (Input.GetKeyDown(KeyCode.UpArrow)) PressDpad(Vector3.up);
				else if (Input.GetKeyUp(KeyCode.UpArrow)) ReleaseDpad(Vector3.up);

				if (Input.GetKeyDown(KeyCode.RightArrow)) PressDpad(Vector3.right);
				else if (Input.GetKeyUp(KeyCode.RightArrow)) ReleaseDpad(Vector3.right);

				if (Input.GetKeyDown(KeyCode.DownArrow)) PressDpad(Vector3.down);
				else if (Input.GetKeyUp(KeyCode.DownArrow)) ReleaseDpad(Vector3.down);

				if (Input.GetKeyDown(KeyCode.LeftArrow)) PressDpad(Vector3.left);
				else if (Input.GetKeyUp(KeyCode.LeftArrow)) ReleaseDpad(Vector3.left);

				// Bắn từng viên
				if (Input.GetKeyDown(KeyCode.Slash)) foreach (var listener in listeners) listener.ButtonShoot();

				// Bắn liên tục
				if (Input.GetKeyDown(KeyCode.RightAlt)) foreach (var listener in listeners) listener.ButtonShoot(true);
				else if (Input.GetKeyUp(KeyCode.RightAlt)) foreach (var listener in listeners) listener.ButtonShoot(false);

				// Select
				if (Input.GetKeyDown(KeyCode.RightShift)) foreach (var listener in listeners) listener.ButtonSelect();

				// Start
				if (Input.GetKeyDown(KeyCode.Return)) foreach (var listener in listeners) listener.ButtonStart();
				#endregion
			}
			else if (color == Color.Green)
			{
				#region Green
				// Dpad
				if (Input.GetKeyDown(KeyCode.W)) PressDpad(Vector3.up);
				else if (Input.GetKeyUp(KeyCode.W)) ReleaseDpad(Vector3.up);

				if (Input.GetKeyDown(KeyCode.D)) PressDpad(Vector3.right);
				else if (Input.GetKeyUp(KeyCode.D)) ReleaseDpad(Vector3.right);

				if (Input.GetKeyDown(KeyCode.S)) PressDpad(Vector3.down);
				else if (Input.GetKeyUp(KeyCode.S)) ReleaseDpad(Vector3.down);

				if (Input.GetKeyDown(KeyCode.A)) PressDpad(Vector3.left);
				else if (Input.GetKeyUp(KeyCode.A)) ReleaseDpad(Vector3.left);

				// Bắn từng viên
				if (Input.GetKeyDown(KeyCode.Z)) foreach (var listener in listeners) listener.ButtonShoot();

				// Bắn liên tục
				if (Input.GetKeyDown(KeyCode.LeftAlt)) foreach (var listener in listeners) listener.ButtonShoot(true);
				else if (Input.GetKeyUp(KeyCode.LeftAlt)) foreach (var listener in listeners) listener.ButtonShoot(false);

				// Select
				if (Input.GetKeyDown(KeyCode.LeftShift)) foreach (var listener in listeners) listener.ButtonSelect();

				// Start
				if (Input.GetKeyDown(KeyCode.Tab)) foreach (var listener in listeners) listener.ButtonStart();
				#endregion
			}
			#endregion

			#region Gamepad
			switch (G.all.Count)
			{
				case 0:
					gamepads[Color.Yellow].g = gamepads[Color.Green].g = null;
					break;

				case 1:
					gamepads[Color.Yellow].g = G.current;
					gamepads[Color.Green].g = null;
					break;

				default:
					gamepads[Color.Yellow].g = G.all[0];
					gamepads[Color.Green].g = G.all[1];
					break;
			}

			if (g != null)
			{
				// Dpad
				if (g.dpad.up.wasPressedThisFrame) PressDpad(Vector3.up);
				else if (g.dpad.up.wasReleasedThisFrame) ReleaseDpad(Vector3.up);

				if (g.dpad.right.wasPressedThisFrame) PressDpad(Vector3.right);
				else if (g.dpad.right.wasReleasedThisFrame) ReleaseDpad(Vector3.right);

				if (g.dpad.down.wasPressedThisFrame) PressDpad(Vector3.down);
				else if (g.dpad.down.wasReleasedThisFrame) ReleaseDpad(Vector3.down);

				if (g.dpad.left.wasPressedThisFrame) PressDpad(Vector3.left);
				else if (g.dpad.left.wasReleasedThisFrame) ReleaseDpad(Vector3.left);

				// Bắn từng viên
				if (g.aButton.wasPressedThisFrame || g.bButton.wasPressedThisFrame)
					foreach (var listener in listeners) listener.ButtonShoot();

				// Bắn liên tục
				bool pressX = g.xButton.isPressed, pressY = g.yButton.isPressed;
				if (shootContinous)
				{
					if (!pressX && !pressY)
					{
						shootContinous = false;
						foreach (var listener in listeners) listener.ButtonShoot(false);
					}
				}
				else
				{
					if (pressX || pressY)
					{
						shootContinous = true;
						foreach (var listener in listeners) listener.ButtonShoot(true);
					}
				}

				// Select
				if (g.selectButton.wasPressedThisFrame) foreach (var listener in listeners) listener.ButtonSelect();

				// Start
				if (g.startButton.wasPressedThisFrame) foreach (var listener in listeners) listener.ButtonStart();
			}
			#endregion
		}


		private readonly List<Vector3> dpads = new();
		private void PressDpad(Vector3 dir)
		{
			if (dpads.Contains(dir)) return;

			dpads.Add(dir);
			foreach (var listener in listeners) listener.ButtonDpad(dir, true);
		}


		private void ReleaseDpad(Vector3 dir)
		{
			if (!dpads.Contains(dir)) return;

			var last = dpads[^1];
			dpads.Remove(dir);
			if (dir != last) return;

			foreach (var listener in listeners) listener.ButtonDpad(dir, false);
			if (dpads.Count > 0) foreach (var listener in listeners) listener.ButtonDpad(dpads[^1], true);
		}


		public static void Rumble(Color color, float x, float y)
		{
			var g = gamepads[color].g;
			if (g == null) return;

			g.SetMotorSpeeds(x, y);
		}
	}
}