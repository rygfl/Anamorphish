using UnityEngine;

public interface ICameraProvider
{
    Vector3 GetCameraPosition();            //월드 좌표만을 반환해야 함
    bool HasData();
}
