import cv2
import mediapipe as mp
import numpy as np
import socket
import json

UDP_IP = "127.0.0.1"
UDP_PORT = 5005
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Mediapipe 초기화
mp_face_detection = mp.solutions.face_detection
face_det = mp_face_detection.FaceDetection(model_selection=0, min_detection_confidence=0.5)

cap = cv2.VideoCapture(0)

print("✅ 얼굴 위치 추적 시작 (ESC로 종료)")

# 이전 위치 저장 변수
prev_face_x, prev_face_y = None, None

while True:
    ret, frame = cap.read()
    if not ret:
        break

    frame = cv2.flip(frame, 1)
    h, w, _ = frame.shape
    rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

    result_det = face_det.process(rgb)

    face_x, face_y = 0.5, 0.5  # 기본값

    # --- 얼굴 위치 계산 (화면 내 중심점) ---
    if result_det.detections:
        bbox = result_det.detections[0].location_data.relative_bounding_box
        face_x = bbox.xmin + bbox.width / 2
        face_y = bbox.ymin + bbox.height / 2

        # 시각화
        cv2.rectangle(frame,
                      (int(bbox.xmin * w), int(bbox.ymin * h)),
                      (int((bbox.xmin + bbox.width) * w), int((bbox.ymin + bbox.height) * h)),
                      (0, 255, 0), 2)

    # --- 이전 값과의 차이 계산 ---
    offset_x = 0.0
    offset_y = 0.0
    if prev_face_x is not None and prev_face_y is not None:
        offset_x = face_x - prev_face_x
        offset_y = face_y - prev_face_y

    # --- Unity로 전송 ---
    data = json.dumps({
        "offset_x": offset_x,
        "offset_y": offset_y
    })
    sock.sendto(data.encode(), (UDP_IP, UDP_PORT))

    # 시각화
    cv2.putText(frame, f"Offset X:{offset_x:.3f} Offset Y:{offset_y:.3f}", (10, 30),
                cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 0), 2)

    # 이전 값 업데이트
    prev_face_x, prev_face_y = face_x, face_y

    cv2.imshow("Face Tracking (Position)", frame)

    if cv2.waitKey(1) & 0xFF == 27:
        break

cap.release()
cv2.destroyAllWindows()
sock.close()
