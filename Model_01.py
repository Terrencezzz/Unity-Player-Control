import cv2
import mediapipe as mp
import json
import socket

def Pose_Images():
    # Initialize MediaPipe solutions
    mp_pose = mp.solutions.pose
    mp_hands = mp.solutions.hands
    mp_face_mesh = mp.solutions.face_mesh
    mp_drawing = mp.solutions.drawing_utils
    mp_drawing_styles = mp.solutions.drawing_styles

    # Initialize pose, hands, and face detection
    pose = mp_pose.Pose(min_detection_confidence=0.5, min_tracking_confidence=0.8)
    hands = mp_hands.Hands(static_image_mode=False, max_num_hands=2, min_detection_confidence=0.5)
    face_mesh = mp_face_mesh.FaceMesh(min_detection_confidence=0.5, min_tracking_confidence=0.8)

    # Set up socket for UDP communication
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    serverAddressPort = ("127.0.0.1", 5052)

    cap = cv2.VideoCapture(0)

    while True:
        hx, image = cap.read()
        if not hx:
            print('Error reading video')
            exit(0)

        image.flags.writeable = False
        rgb_image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)

        # Process pose, hands, and face landmarks
        pose_results = pose.process(rgb_image)
        hand_results = hands.process(rgb_image)
        face_results = face_mesh.process(rgb_image)

        image.flags.writeable = True

        # Initialize storage for all keypoints
        all_landmarks = []

        # Draw and store pose landmarks
        if pose_results.pose_landmarks:
            mp_drawing.draw_landmarks(
                image, pose_results.pose_landmarks, mp_pose.POSE_CONNECTIONS,
                landmark_drawing_spec=mp_drawing_styles.get_default_pose_landmarks_style()
            )
            for landmark in pose_results.pose_landmarks.landmark:
                all_landmarks.append({
                    'type': 'pose',
                    'x': landmark.x,
                    'y': landmark.y,
                    'z': landmark.z,
                    'visibility': landmark.visibility
                })

        # Draw and store hand landmarks
        if hand_results.multi_hand_landmarks:
            for hand_landmarks in hand_results.multi_hand_landmarks:
                mp_drawing.draw_landmarks(
                    image, hand_landmarks, mp_hands.HAND_CONNECTIONS,
                    landmark_drawing_spec=mp_drawing_styles.get_default_hand_landmarks_style()
                )
                for landmark in hand_landmarks.landmark:
                    all_landmarks.append({
                        'type': 'hand',
                        'x': landmark.x,
                        'y': landmark.y,
                        'z': landmark.z
                    })

        # Draw and store face landmarks
        if face_results.multi_face_landmarks:
            for face_landmarks in face_results.multi_face_landmarks:
                mp_drawing.draw_landmarks(
                    image,
                    face_landmarks,
                    connections=mp_face_mesh.FACEMESH_CONTOURS,  # Updated attribute
                    landmark_drawing_spec=None,
                    connection_drawing_spec=mp_drawing_styles.get_default_face_mesh_contours_style()
                )
                for landmark in face_landmarks.landmark:
                    all_landmarks.append({
                        'type': 'face',
                        'x': landmark.x,
                        'y': landmark.y,
                        'z': landmark.z
                    })

        # Serialize and send all keypoints
        if all_landmarks:
            data = json.dumps({'landmarks': all_landmarks})
            sock.sendto(data.encode('utf-8'), serverAddressPort)

        # Display the image with landmarks
        cv2.imshow('Enhanced Pose Estimation', image)

        if cv2.waitKey(10) & 0xFF == ord('q'):
            break

    # Release resources
    cap.release()
    cv2.destroyAllWindows()
    pose.close()
    hands.close()
    face_mesh.close()


if __name__ == '__main__':
    Pose_Images()
