import cv2
import mediapipe as mp
import pyrealsense2 as rs
import numpy as np

# RealSense 설정
pipeline = rs.pipeline()
config = rs.config()
config.enable_stream(rs.stream.depth, 640, 480, rs.format.z16, 30)
config.enable_stream(rs.stream.color, 640, 480, rs.format.bgr8, 30)
pipeline.start(config)

mp_face = mp.solutions.face_mesh.FaceMesh(min_detection_confidence=0.5)

try:
    while True:
        frames = pipeline.wait_for_frames()
        depth_frame = frames.get_depth_frame()
        color_frame = frames.get_color_frame()
        if not depth_frame or not color_frame:
            continue

        color_image = np.asanyarray(color_frame.get_data())
        rgb_image = cv2.cvtColor(color_image, cv2.COLOR_BGR2RGB)
        results = mp_face.process(rgb_image)

        if results.multi_face_landmarks:
            for face_landmarks in results.multi_face_landmarks:
                # 코 끝 (landmark index 1)
                h, w, _ = color_image.shape
                x = int(face_landmarks.landmark[1].x * w)
                y = int(face_landmarks.landmark[1].y * h)
                depth = depth_frame.get_distance(x, y)
                intrin = depth_frame.profile.as_video_stream_profile().intrinsics
                X, Y, Z = rs.rs2_deproject_pixel_to_point(intrin, [x, y], depth)
                cv2.circle(color_image, (x, y), 3, (0,255,0), -1)
                cv2.putText(color_image, f"Head: {X:.2f}, {Y:.2f}, {Z:.2f}m", (x, y-10),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,0), 2)

        cv2.imshow('Head Tracking', color_image)
        if cv2.waitKey(1) == 27:
            break
finally:
    pipeline.stop()
