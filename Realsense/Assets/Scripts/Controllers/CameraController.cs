using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private float _cameraSpeed = 100.0f;

    [SerializeField]
    private Vector3 _intialCamPos = new Vector3(0, 1641, 1038);       //초기 카메라 위치 => 전시장 입구로 지정?, 추후 사람을 인식받지 않을 때 되돌아 가도록 구현해야 함.

    void Start()
    {
        transform.position = _intialCamPos;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        OnKeyboard();
    }

    //상하좌우 카메라 위치 이동
    void OnKeyboard()
	{
        if (Input.GetKey(KeyCode.W))
		{
            transform.position += Vector3.up * Time.deltaTime * _cameraSpeed;

        }

        if (Input.GetKey(KeyCode.S))
		{
            transform.position += Vector3.down * Time.deltaTime * _cameraSpeed;
        }

        if (Input.GetKey(KeyCode.A))
        {
            transform.position += Vector3.left * Time.deltaTime * _cameraSpeed;
        }

        if (Input.GetKey(KeyCode.D))
        {
            transform.position += Vector3.right * Time.deltaTime * _cameraSpeed;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            transform.position += Vector3.forward * Time.deltaTime * _cameraSpeed;
        }

        if (Input.GetKey(KeyCode.E))
        {
            transform.position += Vector3.back * Time.deltaTime * _cameraSpeed;
        }


        //처음 위치로
        if (Input.GetKey(KeyCode.R))
        {
            transform.position = _intialCamPos;
        }
    }
}
