using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Transforms")]

    [Tooltip("시점을 따라 움직일 카메라 (보통 Main Camera)")]
    public Camera targetCamera;

    [SerializeField]
    [Tooltip("카메라 입력 값 설정 : KeyBoardProvider(키보드 입력) or RealSenseProvider(리얼센스 값)")]
    private MonoBehaviour providerComponent;                    //직접 사용할 컴포넌트 드래그 앤 드롭

    private ICameraProvider provider;

	private void Start()
	{
        if (providerComponent != null)
            provider = providerComponent as ICameraProvider;

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

	private void Update()
	{
        if (!provider.HasData()) { Debug.Log($"{provider.GetType().Name} does not have data"); }
        if (targetCamera == null) { Debug.Log("targetCamera is null"); }

        targetCamera.transform.position = provider.GetCameraPosition();
	}
}
