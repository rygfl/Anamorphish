using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class FaceTrackedCamera : MonoBehaviour
{
    UdpClient udp;
    Thread receiveThread;
    float pitch, yaw, roll, faceX, faceY;

    [Header("Target Object (카메라가 바라볼 대상)")]
    public Transform target;

    [Header("기본 설정")]
    public float distance = 2.0f;

    [Header("회전 민감도")]
    public float yawSensitivity = 0.4f;   // 좌우 회전 속도
    public float pitchSensitivity = 0.3f; // 상하 회전 속도

    [Header("이동 민감도")]
    public float moveXSensitivity = 0.5f; // 좌우 이동 속도
    public float moveYSensitivity = 0.8f; // 상하 이동 속도

    [Header("보간(부드럽게 움직임)")]
    public float smoothSpeed = 5.0f;

    private float smoothedYaw, smoothedPitch;
    private Vector3 smoothedOffset;

    void Start()
    {
        udp = new UdpClient(5005);
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void Update()
    {
        // 회전 각도 계산
        float targetYaw = Mathf.Clamp(-yaw * yawSensitivity, -30f, 30f);
        float targetPitch = Mathf.Clamp(-pitch * pitchSensitivity, -20f, 20f);

        // 부드럽게 보간
        smoothedYaw = Mathf.Lerp(smoothedYaw, targetYaw, Time.deltaTime * smoothSpeed);
        smoothedPitch = Mathf.Lerp(smoothedPitch, targetPitch, Time.deltaTime * smoothSpeed);

        // 회전 쿼터니언 계산
        Quaternion rotation = Quaternion.Euler(smoothedPitch, smoothedYaw, 0);

        // 카메라 기본 위치 (뒤로 distance만큼 떨어짐)
        Vector3 baseOffset = rotation * Vector3.back * distance;

        // 얼굴의 좌우/상하 이동값을 카메라 패럴럭스로 변환
        Vector3 moveOffset = new Vector3(
            faceX * moveXSensitivity,
            faceY * moveYSensitivity,
            0
        );

        // 최종 카메라 위치 = target 기준점 + offset
        Vector3 targetPosition = target.position + baseOffset + moveOffset;
        smoothedOffset = Vector3.Lerp(smoothedOffset, targetPosition, Time.deltaTime * smoothSpeed);

        transform.position = smoothedOffset;
        transform.LookAt(target);
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

                pitch = json.pitch;
                yaw = json.yaw;
                roll = json.roll;
                faceX = json.x;
                faceY = json.y;
            }
            catch { }
        }
    }

    [System.Serializable]
    public class FaceData
    {
        public float pitch;
        public float yaw;
        public float roll;
        public float x;
        public float y;
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null && receiveThread.IsAlive)
            receiveThread.Abort();
        if (udp != null)
            udp.Close();
    }
}
