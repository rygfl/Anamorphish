using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/// <summary>
/// Python(RealSense)에서 넘어온 눈 위치 (x, y, z in meters)를 받아서
/// - RealSense 좌표계(Y 아래 +) → Unity 센서 로컬(Y 위 +)
/// - Z축은 고정 거리 또는 기준값에서의 변화량만 작게 반영
/// - sensorOrigin 기준으로 월드 좌표로 변환
/// - targetCamera 위치를 그 월드 좌표로 맞춰주는 스크립트
/// </summary>
public class RealSenseHeadTracker : MonoBehaviour
{
    [Header("Network")]
    public int listenPort = 5005;

    [Header("Transforms")]
    [Tooltip("RealSense 카메라가 씬 안에서 실제로 위치한 Transform")]
    public Transform sensorOrigin;        // RealSense 위치/방향
    [Tooltip("시점을 따라 움직일 카메라 (보통 Main Camera)")]
    public Camera targetCamera;           // Off-axis 스크립트가 붙어있는 카메라

    [Header("Scale & Smoothing")]
    [Tooltip("1.0이면 1m = 1 Unity 단위, 1000이면 1m = 1000 Unity 단위 (씬 스케일에 맞게 조절)")]
    public float worldScale = 1000.0f;    // Monitor가 (0,1451,1359)이면 1000 정도부터 시작

    [Range(0f, 1f)]
    [Tooltip("카메라 위치 보간 정도 (0이면 바로, 1에 가까울수록 천천히 따라감)")]
    public float positionLerp = 0.25f;

    [Header("Depth(Z) Handling")]
    [Tooltip("true면 거리 변화도 약간 반영, false면 고정 거리만 사용")]
    public bool useDepth = false;

    [Tooltip("센서 기준 고정 거리 (미터). 보통 관객이 서 있는 평균 거리")]
    public float fixedDistanceMeters = 1.0f;

    [Tooltip("useDepth=true일 때, 기준 거리에서 거리 변화에 곱할 민감도 (0~1 정도 권장)")]
    public float depthGain = 0.2f;

    private UdpClient udp;
    private Thread receiveThread;

    private volatile bool running = true;
    private volatile bool hasData = false;

    // RealSense에서 받은 raw 좌표 (그대로: X 오른쪽+, Y 아래+, Z 앞+)
    private Vector3 latestRawRS = Vector3.zero;

    private bool hasBaselineZ = false;
    private float baselineZ = 0f;   // 첫 인식 시 거리 (미터)

    [System.Serializable]
    private class FaceData
    {
        // Python 쪽에서 {"x":..., "y":..., "z":..., "ts":...} 형태로 보내는 것을 가정
        public float x;
        public float y;
        public float z;
        public double ts;
    }

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        udp = new UdpClient(listenPort);
        receiveThread = new Thread(ReceiveLoop);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void Update()
    {
        if (!hasData || sensorOrigin == null || targetCamera == null)
            return;

        // 1) RealSense raw 좌표 (미터 단위)
        float x_rs = latestRawRS.x;
        float y_rs = latestRawRS.y;
        float z_rs = latestRawRS.z;

        if (!hasBaselineZ)
        {
            // 첫 유효 프레임에서 기준 거리 저장
            baselineZ = z_rs;
            hasBaselineZ = true;
        }

        // 2) Z 처리: 고정 거리 + (변화量 * depthGain) (옵션)
        float z_meters;
        if (useDepth)
        {
            float deltaZ = z_rs - baselineZ;       // 기준에서 얼마나 앞/뒤로 갔는가
            z_meters = fixedDistanceMeters + deltaZ * depthGain;
        }
        else
        {
            // 거리 변화 무시, 고정 거리만 사용
            z_meters = fixedDistanceMeters;
        }

        // 3) RealSense → Unity 센서 로컬:
        //    X: 그대로, Y: 부호 반전(위가 +), Z: 위에서 계산한 z_meters
        Vector3 sensorLocalMeters = new Vector3(x_rs, -y_rs, z_meters);

        // 4) 씬 스케일 맞추기
        Vector3 sensorLocalScaled = sensorLocalMeters * worldScale;

        // 5) 센서 기준 → 월드 좌표
        Vector3 eyeWorld = sensorOrigin.TransformPoint(sensorLocalScaled);

        // 6) 카메라 위치 세팅 (절대값, += 아님)
        if (positionLerp > 0f && positionLerp < 1f)
        {
            targetCamera.transform.position = Vector3.Lerp(
                targetCamera.transform.position,
                eyeWorld,
                positionLerp
            );
        }
        else
        {
            targetCamera.transform.position = eyeWorld;
        }
    }

    private void ReceiveLoop()
    {
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

        while (running)
        {
            try
            {
                byte[] data = udp.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);

                FaceData fd = JsonUtility.FromJson<FaceData>(text);
                // raw RealSense 좌표 그대로 저장 (미터)
                latestRawRS = new Vector3(fd.x, fd.y, fd.z);
                hasData = true;
            }
            catch
            {
                // 무시하고 계속
            }
        }
    }

    void OnApplicationQuit()
    {
        Shutdown();
    }

    void OnDisable()
    {
        Shutdown();
    }

    private void Shutdown()
    {
        running = false;
        try { udp?.Close(); } catch { }

        if (receiveThread != null && receiveThread.IsAlive)
        {
            try { receiveThread.Join(100); } catch { }
        }
    }
}
