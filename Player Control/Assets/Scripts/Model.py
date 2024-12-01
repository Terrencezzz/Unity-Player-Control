import cv2
import mediapipe as mp
import numpy as np
import socket
import json

def Pose_Images():
    mp_pose = mp.solutions.pose
    mp_hands = mp.solutions.hands
    mp_drawing = mp.solutions.drawing_utils
    mp_drawing_styles = mp.solutions.drawing_styles

    with mp_pose.Pose(
        min_detection_confidence=0.5,
        min_tracking_confidence=0.8) as pose, \
        mp_hands.Hands(
        max_num_hands=2,
        min_detection_confidence=0.5,
        min_tracking_confidence=0.5) as hands:

        cap = cv2.VideoCapture(0)
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        serverAddressPort = ("127.0.0.1", 5052)

        while True:
            hx, image = cap.read()
            if hx is False:
                print('read video error')
                exit(0)
            image.flags.writeable = False

            image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
            pose_results = pose.process(image_rgb)
            hand_results = hands.process(image_rgb)

            image.flags.writeable = True

            if pose_results.pose_landmarks:
                # Draw pose landmarks
                mp_drawing.draw_landmarks(
                    image,
                    pose_results.pose_landmarks,
                    mp_pose.POSE_CONNECTIONS,
                    landmark_drawing_spec=mp_drawing_styles.get_default_pose_landmarks_style())

                # Serialize and send pose data
                landmarks = []
                for landmark in pose_results.pose_landmarks.landmark:
                    landmarks.append({
                        'x': landmark.x,
                        'y': landmark.y,
                        'z': landmark.z,
                        'visibility': landmark.visibility
                    })
                data = json.dumps({'type': 'pose', 'landmarks': landmarks})
                sock.sendto(data.encode('utf-8'), serverAddressPort)

            if hand_results.multi_hand_landmarks and hand_results.multi_handedness:
                for hand_landmarks, hand_handedness in zip(hand_results.multi_hand_landmarks, hand_results.multi_handedness):
                    mp_drawing.draw_landmarks(
                        image,
                        hand_landmarks,
                        mp_hands.HAND_CONNECTIONS,
                        mp_drawing_styles.get_default_hand_landmarks_style(),
                        mp_drawing_styles.get_default_hand_connections_style())

                    # Get handedness ('Left' or 'Right')
                    handedness = hand_handedness.classification[0].label

                    # Serialize and send hand data
                    hand_landmarks_list = []
                    for landmark in hand_landmarks.landmark:
                        hand_landmarks_list.append({
                            'x': landmark.x,
                            'y': landmark.y,
                            'z': landmark.z
                        })
                    data = json.dumps({
                        'type': 'hand',
                        'handedness': handedness,
                        'hand_landmarks': hand_landmarks_list
                    })
                    sock.sendto(data.encode('utf-8'), serverAddressPort)

            cv2.imshow('Pose and Hand Estimation', image)

            if cv2.waitKey(10) & 0xFF == ord('q'):
                break
        cap.release()
        cv2.destroyAllWindows()

if __name__ == '__main__':
    Pose_Images()
