import cv2
import mediapipe as mp
import numpy as np
import socket
import json

def Pose_Images():
    mp_pose = mp.solutions.pose
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
            print(results.pose_landmarks)

            # Serialize and send pose data
            if results.pose_landmarks:
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

            cv2.imshow('image', image)

            if cv2.waitKey(10) & 0xFF == ord('q'):
                break
        cap.release()



if __name__ == '__main__':
    Pose_Images() 