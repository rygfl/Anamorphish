using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class RealSenseProvider : MonoBehaviour, ICameraProvider
{

    [Header("Network")]
    public int listenPort = 5005;
    private UdpClient udp;
    private Thread receiveThread;
    private volatile bool running = true;
    private volatile bool hasData = false;

    [Tooltip("RealSense 카메라가 씬 안에서 실제로 위치한 Transform")]
    public Transform sensorOrigin;                                  //추후 시간 남으면 자동 연동?

    // RealSense에서 받은 raw 좌표 (X 오른쪽+, Y 아래+, Z 앞+)
    private Vector3 latestRawRS = Vector3.zero;
    Vector3 eyeWorld;

    void Start()
    {
        udp = new UdpClient(listenPort);
        receiveThread = new Thread(ReceiveLoop);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void Update()
    {
        if (hasData == false) return;

        // 2) RealSense → Unity 센서 로컬
        // X: 그대로, Y: -반전(위가 +), Z: 그대로
        Vector3 sensorLocal = new Vector3(latestRawRS.x, -latestRawRS.y, latestRawRS.z);

        // 3) 센서 기준 → 월드 좌표
        eyeWorld = sensorOrigin.TransformPoint(sensorLocal);
    }

    //센서 위치 기준으로 사용자의 위치를 월드 좌표계로 변환한 eyeWorld 반환
    public Vector3 GetCameraPosition() => eyeWorld;

    public bool HasData() => true;

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
