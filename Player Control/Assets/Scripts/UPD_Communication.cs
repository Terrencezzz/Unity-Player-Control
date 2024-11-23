using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

public class UPD_Communication : MonoBehaviour
{
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;

    public List<Landmark> landmarks = new List<Landmark>();

    public GameObject noseObject;
    public GameObject leftArmObject;
    public GameObject rightArmObject;
    public GameObject leftForearmObject;
    public GameObject rightForearmObject;
    public GameObject chestObject;

    private int scale = 10;
    private float smoth = 6f;
    Animator animator;


    // Start is called before the first frame update
    void Start()
    {
        animator = this.GetComponent<Animator>();
        StartReceiver();
    }

    void OnApplicationQuit()
    {
        StopReceiver();
    }

    void StartReceiver()
    {
        udpClient = new UdpClient(5052);
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        isRunning = true;
        receiveThread.Start();
        Debug.Log("UDP Receiver started.");
    }

    void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string json = Encoding.UTF8.GetString(data);

                // Deserialize the JSON data
                PoseData poseData = JsonUtility.FromJson<PoseData>(json);

                // Update landmarks
                lock (landmarks)
                {
                    landmarks = poseData.landmarks;
                }

                Debug.Log("Received pose data.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error receiving data: " + ex.Message);
            }
        }
    }

    void StopReceiver()
    {
        isRunning = false;
        if (udpClient != null)
        {
            udpClient.Close();
        }
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
        Debug.Log("UDP Receiver stopped.");
    }

    void Update()
    {
        lock (landmarks)
        {
            if (landmarks.Count > 0)
            {
                for (int i = 0; i < landmarks.Count; i++)
                {
                    Landmark landmark = landmarks[i];
                }
                RotateHeadBasedOnPreciseAngles(noseObject.transform);
                RotateLeftArm(leftArmObject.transform);
                RotateRightArm(rightArmObject.transform);
                RotateLeftForearm(leftForearmObject.transform);
                RotateRightForearm(rightForearmObject.transform);
                RotateChest(chestObject.transform);
            }
        }
    }
    void RotateHeadBasedOnPreciseAngles(Transform headTransform)
    {
        if (headTransform == null || landmarks.Count < 9) return;

        // Get landmarks for the nose and ears
        Landmark nose = landmarks[0];
        Landmark leftEar = landmarks[7];
        Landmark rightEar = landmarks[8];

        // Calculate the midpoint between the ears (used as the center reference point)
        float earMidpointX = (leftEar.x + rightEar.x) / 2.0f;
        float earMidpointY = (leftEar.y + rightEar.y) / 2.0f;

        // Get the position of the nose
        float noseX = nose.x;
        float noseY = nose.y;

        // Calculate horizontal rotation (Y-axis rotation) based on the X distance from nose to ear midpoint
        float horizontalRotationAngle = Mathf.Atan2(noseX - earMidpointX, 1.0f) * Mathf.Rad2Deg * scale;

        // Calculate vertical rotation (X-axis rotation) based on the Y distance from nose to ear midpoint
        float verticalRotationAngle = Mathf.Atan2(earMidpointY - noseY, 1.0f) * Mathf.Rad2Deg * scale;

        // Apply the calculated rotations to the head transform
        Quaternion targetRotation = Quaternion.Euler(-verticalRotationAngle, -horizontalRotationAngle, 0f);
        headTransform.rotation = Quaternion.Slerp(headTransform.rotation, targetRotation, Time.deltaTime * smoth);
    }

    void RotateLeftArm(Transform armTransform)
    {
        if (armTransform == null || landmarks.Count < 16) return;

        // Get landmarks for the left shoulder (point 11), left elbow (point 13), and left wrist (point 15)
        Landmark leftShoulder = landmarks[11];
        Landmark leftElbow = landmarks[13];

        // Create vectors for shoulder to elbow
        Vector3 shoulderToElbow = new Vector3(leftElbow.x - leftShoulder.x, leftElbow.y - leftShoulder.y, leftElbow.z - leftShoulder.z);

        // Normalize the vector to ensure it only represents direction
        shoulderToElbow.Normalize();

        // Calculate the target rotation based on the shoulder-to-elbow direction
        Quaternion targetRotation = Quaternion.LookRotation(shoulderToElbow, Vector3.up);

        // Adjust the rotation to account for the initial T-pose offset
        Quaternion tPoseCorrection = Quaternion.Euler(0f, -90f, 90f); // Adjust these values to correctly align with the T-pose

        // Combine the target rotation with the T-pose correction
        targetRotation = targetRotation * tPoseCorrection;

        // Smoothly interpolate the current rotation to the target rotation
        armTransform.rotation = Quaternion.Slerp(armTransform.rotation, targetRotation, Time.deltaTime * smoth);
    }

    void RotateRightArm(Transform armTransform)
    {
        if (armTransform == null || landmarks.Count < 16) return;

        // Get landmarks for the right shoulder (point 12), right elbow (point 14), and right wrist (point 16)
        Landmark rightShoulder = landmarks[12];
        Landmark rightElbow = landmarks[14];

        // Create vectors for shoulder to elbow
        Vector3 shoulderToElbow = new Vector3(rightElbow.x - rightShoulder.x, rightElbow.y - rightShoulder.y, rightElbow.z - rightShoulder.z);

        // Normalize the vector to ensure it only represents direction
        shoulderToElbow.Normalize();

        // Calculate the target rotation based on the shoulder-to-elbow direction
        Quaternion targetRotation = Quaternion.LookRotation(shoulderToElbow, Vector3.up);

        // Adjust the rotation to account for the initial T-pose offset
        Quaternion tPoseCorrection = Quaternion.Euler(0f, 90f, -90f); // Adjust these values to correctly align with the T-pose for the right arm

        // Combine the target rotation with the T-pose correction
        targetRotation = targetRotation * tPoseCorrection;

        // Smoothly interpolate the current rotation to the target rotation
        armTransform.rotation = Quaternion.Slerp(armTransform.rotation, targetRotation, Time.deltaTime * smoth);
    }

    void RotateLeftForearm(Transform forearmTransform)
    {
        if (forearmTransform == null || landmarks.Count < 16) return;

        // Get landmarks for the left elbow (point 13) and left wrist (point 15)
        Landmark leftElbow = landmarks[13];
        Landmark leftWrist = landmarks[15];

        // Create a vector for elbow to wrist
        Vector3 elbowToWrist = new Vector3(leftWrist.x - leftElbow.x, leftWrist.y - leftElbow.y, leftWrist.z - leftElbow.z);

        // Normalize the vector to ensure it only represents direction
        elbowToWrist.Normalize();

        // Calculate the target rotation based on the elbow-to-wrist direction
        Quaternion targetRotation = Quaternion.LookRotation(elbowToWrist, Vector3.up);

        // Adjust the rotation to account for the initial T-pose offset
        Quaternion tPoseCorrection = Quaternion.Euler(0f, -90f, 90f); // Adjust these values to correctly align with the T-pose

        // Combine the target rotation with the T-pose correction
        targetRotation = targetRotation * tPoseCorrection;

        // Smoothly interpolate the current rotation to the target rotation
        forearmTransform.rotation = Quaternion.Slerp(forearmTransform.rotation, targetRotation, Time.deltaTime * smoth);
    }

    void RotateRightForearm(Transform forearmTransform)
    {
        if (forearmTransform == null || landmarks.Count < 16) return;

        // Get landmarks for the right elbow (point 14) and right wrist (point 16)
        Landmark rightElbow = landmarks[14];
        Landmark rightWrist = landmarks[16];

        // Create a vector for elbow to wrist
        Vector3 elbowToWrist = new Vector3(rightWrist.x - rightElbow.x, rightWrist.y - rightElbow.y, rightWrist.z - rightElbow.z);

        // Normalize the vector to ensure it only represents direction
        elbowToWrist.Normalize();

        // Calculate the target rotation based on the elbow-to-wrist direction
        Quaternion targetRotation = Quaternion.LookRotation(elbowToWrist, Vector3.up);

        // Adjust the rotation to account for the initial T-pose offset
        Quaternion tPoseCorrection = Quaternion.Euler(0f, 90f, -90f); // Adjust these values to correctly align with the T-pose for the right forearm

        // Combine the target rotation with the T-pose correction
        targetRotation = targetRotation * tPoseCorrection;

        // Smoothly interpolate the current rotation to the target rotation
        forearmTransform.rotation = Quaternion.Slerp(forearmTransform.rotation, targetRotation, Time.deltaTime * smoth);
    }

    void RotateChest(Transform chestTransform)
    {
        if (chestTransform == null || landmarks.Count < 16) return;

        // Get landmarks for the left shoulder (point 11) and right shoulder (point 12)
        Landmark leftShoulder = landmarks[11];
        Landmark rightShoulder = landmarks[12];

        // Calculate the midpoint between the shoulders
        Vector3 shoulderMidpoint = new Vector3(
            (leftShoulder.x + rightShoulder.x) / 2.0f,
            (leftShoulder.y + rightShoulder.y) / 2.0f,
            (leftShoulder.z + rightShoulder.z) / 2.0f
        );

        // Create a vector pointing from the right shoulder to the left shoulder
        Vector3 shoulderLine = new Vector3(
            leftShoulder.x - rightShoulder.x,
            leftShoulder.y - rightShoulder.y,
            leftShoulder.z - rightShoulder.z
        );
        shoulderLine.Normalize(); // Normalize to only get the direction

        // Calculate a reference up vector to define the chest's up direction
        Vector3 referenceUp = Vector3.up;

        // Calculate the target rotation for the chest based on the shoulder line and reference up vector
        Quaternion targetRotation = Quaternion.LookRotation(shoulderLine, referenceUp);

        // Adjust the rotation to account for the initial T-pose offset (if needed)
        Quaternion tPoseCorrection = Quaternion.Euler(0f, -90f, 0f); // Adjust these values to correctly align with the T-pose

        // Combine the target rotation with the T-pose correction
        targetRotation = targetRotation * tPoseCorrection;

        // Smoothly interpolate the current rotation to the target rotation
        chestTransform.rotation = Quaternion.Slerp(chestTransform.rotation, targetRotation, Time.deltaTime * smoth);
    }



    [System.Serializable]
    public class PoseData
    {
        public List<Landmark> landmarks;
    }

    [System.Serializable]
    public class Landmark
    {
        public float x;
        public float y;
        public float z;
        public float visibility;
    }
}
