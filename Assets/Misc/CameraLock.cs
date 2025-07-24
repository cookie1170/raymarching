using UnityEngine;
using UnityEngine.InputSystem;

namespace Misc
{
	public class CameraLock : MonoBehaviour
	{
		private void Awake()
		{
			InputSystem.actions.FindAction("Click").performed += _ => Cursor.lockState = CursorLockMode.Locked;
			InputSystem.actions.FindAction("Escape").performed += _ => Cursor.lockState = CursorLockMode.None;
		}
	}
}
