# Anamorphish

## 목표

관객의 위치에 따라 유니티의 화면의 앵글도 같이 움직여 마치 모니터 안에 공간이 있는 듯한 착시를 유발한다.

## 구현 핵심 플로우

1. intel Realsense depth camera(D435F)에서 관객의 눈 위치를 인식한다.
2. 관객의 눈 좌표를 unity에 보낸다.
3. 유니티에서 카메라를 움직이되 off-axis 투영을 고려해 화면을 왜곡한다.

## 의존성

- Unity 6.2(6000.2.2f1)
- python(3.12.10 이하)
- opencv(pip install opencv-python)
- pyrealsense(pip install pyrealsense2)
- mediapipe
- numpy

## 레퍼런스

- [논문: Generalize perspective projeciton](/References/Kooima,%20Robert.%20Generalized%20perspective%20projection.pdf): off axis 구현을 위한 수식을 도출한 핵심 논문이다.
- [위키: Cg Programming/Unity/Projection for Virtual Reality](/References/Cg_Programming_Unity_Projection_for_Virtual_Reality.pdf): 유니티에서 카메라 C# 파일 하나로만 구현한 예제 위키이다.
- [Medium 글: Off-axis projection in Unity](/References/Off-axis%20projection%20in%20Unity.%20An%20implementation%20of%20off-axis…%20_%20by%20Michel%20de%20Brisis%20_%20TRY%20Creative%20Tech%20_%20Medium.pdf): Michel de Brisis의 블로그 글. 구현에 참고할만 하다.
