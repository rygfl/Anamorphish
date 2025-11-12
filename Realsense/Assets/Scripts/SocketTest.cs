using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class SocketTest : MonoBehaviour
{
    [Header("Position Settings")]
    public float moveScaleX = 2.0f; // 謝辦 檜翕 團馬紫
    public float moveScaleY = 2.0f; // 鼻ж 檜翕 團馬紫

    private float faceX, faceY;
    private UdpClient udp;
    private Thread receiveThread;

    public Vector3 testPosition;

    void Start()
    {
        udp = new UdpClient(5005);
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void Update()
    {
        float moveX = faceX * moveScaleX;
        float moveY = -faceY * moveScaleY;

        Camera.main.transform.localPosition += new Vector3(moveX, moveY, 0);
    }

    void ReceiveData()
    {
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
            try
            {
                byte[] data = udp.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);
                var json = JsonUtility.FromJson<FaceData>(text);

                faceX = json.offset_x;
                faceY = json.offset_y;
            }
            catch { }
        }
    }

    [System.Serializable]
    public class FaceData
    {
        public float offset_x;
        public float offset_y;
    }

    void OnApplicationQuit()
    {
        udp?.Close();
        receiveThread?.Abort();
    }
}
