import cv2
import mediapipe as mp
import pyrealsense2 as rs
import numpy as np
import socket
import json
import time

# ─────────────────────────────────────
# UDP 설정
# ─────────────────────────────────────
UDP_IP = "127.0.0.1"   # Unity가 도는 PC라면 localhost
UDP_PORT = 5005
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# ─────────────────────────────────────
# RealSense 파이프라인 설정
# ─────────────────────────────────────
pipeline = rs.pipeline()
config = rs.config()

# 해상도/프레임레이트는 필요에 따라 조절
config.enable_stream(rs.stream.depth, 640, 480, rs.format.z16, 30)
config.enable_stream(rs.stream.color, 640, 480, rs.format.bgr8, 30)

pipeline.start(config)

# depth를 color 기준 좌표계로 align
align = rs.align(rs.stream.color)

# ─────────────────────────────────────
# MediaPipe FaceMesh 설정
# ─────────────────────────────────────
mp_face = mp.solutions.face_mesh
face_mesh = mp_face.FaceMesh(
    max_num_faces=1,
    refine_landmarks=True,           # 홍채까지 포함
    min_detection_confidence=0.5,
    min_tracking_confidence=0.5,
)

# ─────────────────────────────────────
# 지수 이동 평균(EMA)으로 좌표 스무딩
# ─────────────────────────────────────
ema = None
alpha = 0.3  # 0.2~0.4 사이 정도로 조절

try:
    while True:
        try:
            # 프레임 획득 + 컬러 기준 정렬
            frames = pipeline.wait_for_frames()
            frames = align.process(frames)
            depth_frame = frames.get_depth_frame()
            color_frame = frames.get_color_frame()

            if not depth_frame or not color_frame:
                continue

            color_image = np.asanyarray(color_frame.get_data())
            h, w, _ = color_image.shape

            # BGR → RGB
            rgb_image = cv2.cvtColor(color_image, cv2.COLOR_BGR2RGB)
            rgb_image.flags.writeable = False
            results = face_mesh.process(rgb_image)
            rgb_image.flags.writeable = True

        except Exception as e:
            print(f"프레임 처리 중 오류: {e}")
            continue

        if results.multi_face_landmarks:
            face = results.multi_face_landmarks[0]

            # 눈/홍채 랜드마크 선택
            # 468, 473: 왼/오 홍채 중심
            # fallback: 33, 263 (눈 가장자리)
            if len(face.landmark) >= 474:
                l = face.landmark[468]
                r = face.landmark[473]
            else:
                l = face.landmark[33]
                r = face.landmark[263]

            lx, ly = int(l.x * w), int(l.y * h)
            rx, ry = int(r.x * w), int(r.y * h)

            # 눈 좌표 클리핑
            lx = max(0, min(lx, w - 1))
            ly = max(0, min(ly, h - 1))
            rx = max(0, min(rx, w - 1))
            ry = max(0, min(ry, h - 1))

            cx, cy = (lx + rx) // 2, (ly + ry) // 2  # 눈 사이 중앙 = cyclopean eye

            # 중심 좌표 클리핑 (이미 클리핑된 좌표로 계산되었지만 안전을 위해)
            cx = max(0, min(cx, w - 1))
            cy = max(0, min(cy, h - 1))

            # (cx, cy) 픽셀의 depth[m]
            depth_m = depth_frame.get_distance(cx, cy)

            # depth가 0이거나 너무 말도 안 되면 스킵
            if depth_m <= 0 or depth_m > 5.0:
                cv2.imshow("Head Tracking (Eye Center)", color_image)
                if cv2.waitKey(1) == 27:
                    break
                continue

            # 컬러 스트림 intrinsics (align을 color 기준으로 했으므로)
            intr = color_frame.profile.as_video_stream_profile().intrinsics

            # 픽셀 + 깊이 → 카메라 좌표계 3D (X,Y,Z) [m]
            X, Y, Z = rs.rs2_deproject_pixel_to_point(intr, [cx, cy], depth_m)

            # EMA로 스무딩
            v = np.array([X, Y, Z], dtype=np.float32)
            if ema is None:
                ema = v
            else:
                ema = (1.0 - alpha) * ema + alpha * v

            Xs, Ys, Zs = ema.tolist()

            # UDP 페이로드 (RealSense 좌표 그대로: X 오른쪽+, Y 아래+, Z 앞+)
            payload = {
                "x": float(Xs),
                "y": float(Ys),
                "z": float(Zs),
                "ts": time.time()
            }
            sock.sendto(json.dumps(payload).encode("utf-8"), (UDP_IP, UDP_PORT))

            # 디버그용 시각화
            cv2.circle(color_image, (lx, ly), 3, (0, 255, 0), -1)
            cv2.circle(color_image, (rx, ry), 3, (0, 255, 0), -1)
            cv2.circle(color_image, (cx, cy), 4, (0, 0, 255), -1)
            cv2.putText(
                color_image,
                f"XYZ(m): {Xs:.2f}, {Ys:.2f}, {Zs:.2f}",
                (max(0, cx - 120), max(20, cy - 10)),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.5,
                (0, 255, 255),
                2
            )
        else:
            # 얼굴이 감지되지 않을 때 메시지 표시
            cv2.putText(
                color_image,
                "Face not detected - Waiting...",
                (10, 30),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.7,
                (0, 0, 255),
                2
            )

        cv2.imshow("Head Tracking (Eye Center)", color_image)
        if cv2.waitKey(1) == 27:  # ESC
            break

finally:
    pipeline.stop()
    face_mesh.close()
    cv2.destroyAllWindows()
    sock.close()
