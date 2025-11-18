using UnityEngine;

public class KeyBoardProvider : MonoBehaviour, ICameraProvider
{


	[SerializeField]
	private float _camSpeed = 0.1f;
	[SerializeField]
	private Camera targetCamera;
	
	private Vector3 camPos;
	public Vector3 GetCameraPosition() => camPos;
	public bool HasData() => true;

	private void Start()
	{
		if (targetCamera == null)
			targetCamera = Camera.main;

		camPos = targetCamera.transform.position;
	}
	private void Update()
	{
		float moveX = Input.GetAxis("Horizontal");
		float moveY = Input.GetAxis("Vertical");
		float moveZ = 0.0f;

		if (Input.GetKey(KeyCode.Q))
			moveZ = 1.0f;
		if (Input.GetKey(KeyCode.E))
			moveZ = -1.0f;

		camPos += new Vector3(moveX, moveY, moveZ) * _camSpeed * Time.deltaTime;
	}
}
