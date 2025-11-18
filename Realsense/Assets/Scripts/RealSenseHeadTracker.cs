using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;

/// <summary>
/// Python(RealSense)에서 넘어온 눈 위치 (x, y, z in meters)를 받아서
/// - RealSense 좌표계(Y 아래 +) → Unity 센서 로컬(Y 위 +)
/// - sensorOrigin 기준으로 월드 좌표로 변환
/// - targetCamera 위치를 그대로 즉시 적용하는 스크립트 (스무딩 없음)
/// </summary>
public class RealSenseHeadTracker : MonoBehaviour
{
    [Header("Network")]
    public int listenPort = 5005;

    private UdpClient udp;
    private Thread receiveThread;

    private volatile bool running = true;
    private volatile bool hasData = false;

    [Header("Transforms")]
    [Tooltip("RealSense 카메라가 씬 안에서 실제로 위치한 Transform")]
    public Transform sensorOrigin;
    [Tooltip("시점을 따라 움직일 카메라 (보통 Main Camera)")]
    public Camera targetCamera;

    // RealSense에서 받은 raw 좌표 (X 오른쪽+, Y 아래+, Z 앞+)
    private Vector3 latestRawRS = Vector3.zero;

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

        // 2) RealSense → Unity 센서 로컬
        // X: 그대로, Y: -반전(위가 +), Z: 그대로
        Vector3 sensorLocal = new Vector3(x_rs, -y_rs, z_rs);

        // 3) 센서 기준 → 월드 좌표
        Vector3 eyeWorld = sensorOrigin.TransformPoint(sensorLocal);

        // 4) 카메라 위치 즉시 적용 (스무딩 없음)
        targetCamera.transform.position = eyeWorld;
    }

    private void ReceiveLoop()
    {
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

        while (running)
        {
            try
            {
                byte[] data = udp.Receive(ref anyIP);

                // 이진 포맷: 3 floats + 1 double = 20 bytes
                if (data.Length >= 20)
                {
                    float x = System.BitConverter.ToSingle(data, 0);
                    float y = System.BitConverter.ToSingle(data, 4);
                    float z = System.BitConverter.ToSingle(data, 8);
                    // double ts = System.BitConverter.ToDouble(data, 12);

                    latestRawRS = new Vector3(x, y, z);
                    hasData = true;
                }
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
