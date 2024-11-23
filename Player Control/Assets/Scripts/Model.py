import cv2
import mediapipe as mp
import numpy as np
import socket
import json

def Pose_Images():
    mp_pose = mp.solutions.pose
    mp_drawing = mp.solutions.drawing_utils
    mp_drawing_styles = mp.solutions.drawing_styles
    
    with mp_pose.Pose(
        min_detection_confidence=0.5,
        min_tracking_confidence=0.8) as pose:

        cap = cv2.VideoCapture(0)
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        serverAddressPort = ("127.0.0.1", 5052)

        while(True):
            hx, image = cap.read()
            if hx is False:
                print('read video error')
                exit(0)
            image.flags.writeable = False

            results = pose.process(cv2.cvtColor(image, cv2.COLOR_BGR2RGB))
            
            image.flags.writeable = True
            
            if results.pose_landmarks:
                # Draw landmarks and connections on the image.
                mp_drawing.draw_landmarks(
                    image,
                    results.pose_landmarks,
                    mp_pose.POSE_CONNECTIONS,
                    landmark_drawing_spec=mp_drawing_styles.get_default_pose_landmarks_style())
                
                # Serialize and send pose data
                landmarks = []
                for landmark in results.pose_landmarks.landmark:
                    landmarks.append({
                        'x': landmark.x,
                        'y': landmark.y,
                        'z': landmark.z,
                        'visibility': landmark.visibility
                    })
                data = json.dumps({'landmarks': landmarks})
                sock.sendto(data.encode('utf-8'), serverAddressPort)

            cv2.imshow('Pose Estimation', image)

            if cv2.waitKey(10) & 0xFF == ord('q'):
                break
        cap.release()
        cv2.destroyAllWindows()


if __name__ == '__main__':
    Pose_Images() 