using Cysharp.Threading.Tasks;
using UnityEngine;


public class Test : MonoBehaviour
{
	public GameObject obj;
	private async void Start()
	{
		await UniTask.Delay(2000);
		obj.SetActive(true);
	}
}
