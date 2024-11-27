import cv2
import mediapipe as mp
import numpy as np
import socket
import json

def Pose_And_Hand_Tracking():
    # Initialize MediaPipe Pose and Hands
    mp_pose = mp.solutions.pose
    mp_hands = mp.solutions.hands
    mp_drawing = mp.solutions.drawing_utils
    mp_drawing_styles = mp.solutions.drawing_styles

    # Initialize Pose and Hands models
    with mp_pose.Pose(min_detection_confidence=0.5, min_tracking_confidence=0.8) as pose, \
         mp_hands.Hands(min_detection_confidence=0.5, min_tracking_confidence=0.8, max_num_hands=2) as hands:

        # Open webcam
        cap = cv2.VideoCapture(0)

        # Socket setup for sending data
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        server_address_port = ("127.0.0.1", 5052)

        while True:
            success, image = cap.read()
            if not success:
                print('Error reading video')
                break
            
            # Flip image for better user experience
            image = cv2.flip(image, 1)
            # Convert to RGB once for efficiency
            image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
            image.flags.writeable = False

            # Process Pose
            pose_results = pose.process(image_rgb)

            # Process Hands
            hand_results = hands.process(image_rgb)

            image.flags.writeable = True

            # Draw Pose landmarks
            if pose_results.pose_landmarks:
                mp_drawing.draw_landmarks(
                    image,
                    pose_results.pose_landmarks,
                    mp_pose.POSE_CONNECTIONS,
                    landmark_drawing_spec=mp_drawing_styles.get_default_pose_landmarks_style())

            # Draw Hand landmarks
            if hand_results.multi_hand_landmarks:
                for hand_landmarks in hand_results.multi_hand_landmarks:
                    mp_drawing.draw_landmarks(
                        image,
                        hand_landmarks,
                        mp_hands.HAND_CONNECTIONS,
                        landmark_drawing_spec=mp_drawing_styles.get_default_hand_landmarks_style())
            
            # Collect pose landmarks
            data = {}
            if pose_results.pose_landmarks:
                pose_landmarks = [
                    {'x': lm.x, 'y': lm.y, 'z': lm.z, 'visibility': lm.visibility}
                    for lm in pose_results.pose_landmarks.landmark
                ]
                data['pose_landmarks'] = pose_landmarks

            # Collect hand landmarks
            if hand_results.multi_hand_landmarks:
                hand_landmarks = []
                for hand_landmarks_data in hand_results.multi_hand_landmarks:
                    hand_landmarks.append([
                        {'x': lm.x, 'y': lm.y, 'z': lm.z}
                        for lm in hand_landmarks_data.landmark
                    ])
                data['hand_landmarks'] = hand_landmarks

            # Send data via socket if any landmark is detected
            if data:
                try:
                    json_data = json.dumps(data)
                    sock.sendto(json_data.encode('utf-8'), server_address_port)
                except Exception as e:
                    print(f"Socket error: {e}")

            # Display the image
            cv2.imshow('Pose and Hand Tracking', image)

            # Break on 'q' key press
            if cv2.waitKey(10) & 0xFF == ord('q'):
                break
        
        cap.release()
        cv2.destroyAllWindows()


if __name__ == '__main__':
    Pose_And_Hand_Tracking()
