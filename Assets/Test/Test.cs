using BattleCity;
using UnityEngine;


public class Test : MonoBehaviour, IGamepad
{
	private void Start()
	{
		//Gamepad.Add(BattleCity.Color.Yellow, this);


	}


	public void ButtonDpad(Vector3 direction, bool press)
	{
		print($"dpad( {direction}, {press} )");
	}


	public void ButtonSelect()
	{
		print("select");
	}


	public void ButtonShoot()
	{
		print("shoot");
	}


	public void ButtonShoot(bool press)
	{
		print($"shoot( {press} )");
	}


	public void ButtonStart()
	{
		print("start");
	}


	private void Update()
	{
		var g = UnityEngine.InputSystem.Gamepad.current;
		if (g == null) return;

		print(g.aButton.isPressed);
	}
}
