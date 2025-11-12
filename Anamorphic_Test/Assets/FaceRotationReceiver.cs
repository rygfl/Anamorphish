using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class FaceRotationReceiver : MonoBehaviour
{
    public float yawMultiplier = 0.1f;   // 좌우 회전 감도
    public float pitchMultiplier = 0.1f; // 상하 회전 감도
    public float rollMultiplier = 0.1f;  // 롤 회전 감도
    public float smoothTime = 0.1f;    // 부드럽게 보간 시간

    UdpClient client;
    Thread receiveThread;

    private Vector3 currentEuler;
    private Vector3 targetEuler;
    private Vector3 velocity;

    void Start()
    {
        client = new UdpClient(5005);
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

        currentEuler = Camera.main.transform.rotation.eulerAngles;
        targetEuler = currentEuler;
    }

    void ReceiveData()
    {
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            try
            {
                byte[] data = client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);
                var json = JsonUtility.FromJson<RotationData>(text);

                // Unity 좌표계에 맞게 변환
                // Python에서 Yaw가 좌우, Pitch가 상하
                targetEuler = new Vector3(
                    -json.pitch * pitchMultiplier, // 상하 반전 필요시 - 붙임
                    json.yaw * yawMultiplier,
                    -json.roll * rollMultiplier
                );
            }
            catch
            {
                // 예외 무시
            }
        }
    }

    void Update()
    {
        if (Camera.main != null)
        {
            // 부드럽게 보간
            currentEuler = Vector3.SmoothDamp(currentEuler, targetEuler, ref velocity, smoothTime);
            Camera.main.transform.rotation = Quaternion.Euler(currentEuler);
        }
    }

    [System.Serializable]
    public class RotationData
    {
        public float pitch;
        public float yaw;
        public float roll;
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null) receiveThread.Abort();
        if (client != null) client.Close();
    }
}
