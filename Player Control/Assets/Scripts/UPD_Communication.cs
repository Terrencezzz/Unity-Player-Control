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
    public GameObject leftUpLeg;
    public GameObject rightUpLeg;
    public GameObject leftWristObject;
    public GameObject rightWristObject;
    public GameObject waistObject;
    public GameObject waist2Object;
    public GameObject hipObject;

    private int scale = 10;
    private float smoth = 5f;
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
                RotateHead(noseObject.transform);
                RotateLeftArm(leftArmObject.transform);
                RotateRightArm(rightArmObject.transform);
                RotateLeftForearm(leftForearmObject.transform);
                RotateRightForearm(rightForearmObject.transform);
                RotateLeftWrist(leftWristObject.transform);
                RotateRightWrist(rightWristObject.transform);
                RotateWaist(waistObject.transform);
                //RotateWaist(waist2Object.transform);
                RotateChest(chestObject.transform);
                RotateHip(hipObject.transform);
                RotateLeftUpLeg(leftUpLeg.transform);
                RotateRightUpLeg(rightUpLeg.transform);
            }
        }
    }
    void RotateHead(Transform headTransform)
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

    void RotateLeftWrist(Transform wristTransform)
    {
        if (wristTransform == null || landmarks.Count < 21) return;

        // Get landmarks for wrist (point 15) and index finger tip (point 19)
        Landmark wrist = landmarks[15];
        Landmark indexFingerTip = landmarks[19];

        // Validate visibility of the landmarks
        if (wrist.visibility < 0.5f || indexFingerTip.visibility < 0.5f) return;

        // Create a vector from wrist to index finger tip
        Vector3 wristToIndex = new Vector3(
            indexFingerTip.x - wrist.x,
            indexFingerTip.y - wrist.y,
            indexFingerTip.z - wrist.z
        ).normalized;

        // Use an up vector from the elbow to the wrist for a better reference
        Landmark elbow = landmarks[13]; // Left elbow landmark
        if (elbow.visibility < 0.5f) return;
        Vector3 elbowToWrist = new Vector3(
            wrist.x - elbow.x,
            wrist.y - elbow.y,
            wrist.z - elbow.z
        ).normalized;

        // Calculate the target rotation based on the direction from wrist to index finger tip
        Quaternion targetRotation = Quaternion.LookRotation(wristToIndex, elbowToWrist);

        // Adjust the rotation to account for the initial T-pose offset
        Quaternion tPoseCorrection = Quaternion.Euler(-90f, 0f, 0f);
        targetRotation = targetRotation * tPoseCorrection;

        // Smoothly interpolate the current rotation to the target rotation
        wristTransform.rotation = Quaternion.Slerp(
            wristTransform.rotation, targetRotation, Time.deltaTime * smoth
        );
    }



    void RotateRightWrist(Transform wristTransform)
    {
        if (wristTransform == null || landmarks.Count < 21) return;

        // Get landmarks for wrist (point 16) and index finger tip (point 20)
        Landmark wrist = landmarks[16];
        Landmark indexFingerTip = landmarks[20];

        // Validate visibility of the landmarks
        if (wrist.visibility < 0.5f || indexFingerTip.visibility < 0.5f) return;

        // Create a vector from wrist to index finger tip
        Vector3 wristToIndex = new Vector3(
            indexFingerTip.x - wrist.x,
            indexFingerTip.y - wrist.y,
            indexFingerTip.z - wrist.z
        ).normalized;

        // Use an up vector from the elbow to the wrist for a better reference
        Landmark elbow = landmarks[14]; // Right elbow landmark
        if (elbow.visibility < 0.5f) return;
        Vector3 elbowToWrist = new Vector3(
            wrist.x - elbow.x,
            wrist.y - elbow.y,
            wrist.z - elbow.z
        ).normalized;

        // Calculate the target rotation based on the direction from wrist to index finger tip
        Quaternion targetRotation = Quaternion.LookRotation(wristToIndex, elbowToWrist);

        // Adjust the rotation to account for the initial T-pose offset
        Quaternion tPoseCorrection = Quaternion.Euler(-90f, 0f, 0f);
        targetRotation = targetRotation * tPoseCorrection;

        // Smoothly interpolate the current rotation to the target rotation
        wristTransform.rotation = Quaternion.Slerp(
            wristTransform.rotation, targetRotation, Time.deltaTime * smoth
        );
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

    void RotateWaist(Transform waistTransform)
    {
        if (waistTransform == null || landmarks.Count < 25) return;

        // Get landmarks
        Landmark leftShoulder = landmarks[11];
        Landmark rightShoulder = landmarks[12];
        Landmark leftHip = landmarks[23];
        Landmark rightHip = landmarks[24];

        // Validate visibility of the landmarks
        if (leftShoulder.visibility < 0.5f || rightShoulder.visibility < 0.5f ||
            leftHip.visibility < 0.5f || rightHip.visibility < 0.5f) return;

        // Calculate the centers of the shoulders and hips
        Vector3 shoulderCenter = new Vector3(
            (leftShoulder.x + rightShoulder.x) / 2.0f,
            (leftShoulder.y + rightShoulder.y) / 2.0f,
            (leftShoulder.z + rightShoulder.z) / 2.0f
        );

        Vector3 hipCenter = new Vector3(
            (leftHip.x + rightHip.x) / 2.0f,
            (leftHip.y + rightHip.y) / 2.0f,
            (leftHip.z + rightHip.z) / 2.0f
        );

        // Compute the vector from hip center to shoulder center (torso vector)
        Vector3 torsoVector = shoulderCenter - hipCenter;
        torsoVector.Normalize();

        // Calculate the angle between the torso vector and the vertical axis (Y-axis)
        float bowAngle = Vector3.SignedAngle(Vector3.up, torsoVector, Vector3.right);

        // Create a rotation around the X-axis (sideways axis) based on the bow angle
        Quaternion bowRotation = Quaternion.AngleAxis(bowAngle, Vector3.right);

        // Compute the right vector from left hip to right hip
        Vector3 hipRight = new Vector3(
            rightHip.x - leftHip.x,
            rightHip.y - leftHip.y,
            rightHip.z - leftHip.z
        ).normalized;

        // Compute the forward vector as the cross product of the up vector and hipRight
        Vector3 forwardVector = Vector3.Cross(Vector3.up, hipRight).normalized;

        // Create the base rotation without bowing
        Quaternion baseRotation = Quaternion.LookRotation(forwardVector, Vector3.up);

        // Combine the base rotation with the bow rotation
        Quaternion targetRotation = baseRotation * bowRotation;

        // Adjust the rotation to account for the initial T-pose offset (adjust these values as needed)
        Quaternion tPoseCorrection = Quaternion.Euler(160f, 0f, 0f); // Adjust if necessary
        targetRotation = targetRotation * tPoseCorrection;

        // Smoothly interpolate the current rotation to the target rotation
        waistTransform.rotation = Quaternion.Slerp(
            waistTransform.rotation,
            targetRotation,
            Time.deltaTime * smoth
        );
    }

    void RotateHip(Transform hipTransform)
    {
        if (hipTransform == null || landmarks.Count < 25) return;

        // Get landmarks for the left hip (point 23) and right hip (point 24)
        Landmark leftHip = landmarks[23];
        Landmark rightHip = landmarks[24];

        // Validate visibility of the landmarks
        if (leftHip.visibility < 0.5f || rightHip.visibility < 0.5f) return;

        // Calculate the midpoint between the hips
        Vector3 hipMidpoint = new Vector3(
            (leftHip.x + rightHip.x) / 2.0f,
            (leftHip.y + rightHip.y) / 2.0f,
            (leftHip.z + rightHip.z) / 2.0f
        );

        // Create a vector pointing from the right hip to the left hip
        Vector3 hipLine = new Vector3(
            leftHip.x - rightHip.x,
            leftHip.y - rightHip.y,
            leftHip.z - rightHip.z
        );
        hipLine.Normalize(); // Normalize to get the direction

        // Calculate a reference up vector to define the hip's up direction
        Vector3 referenceUp = Vector3.up;

        // Calculate the target rotation for the hip based on the hip line and reference up vector
        Quaternion targetRotation = Quaternion.LookRotation(hipLine, referenceUp);

        // Adjust the rotation to account for the initial T-pose offset (adjust these values as needed)
        Quaternion tPoseCorrection = Quaternion.Euler(0f, -90f, 0f); // Adjust as necessary for your model

        // Combine the target rotation with the T-pose correction
        targetRotation = targetRotation * tPoseCorrection;

        // Smoothly interpolate the current rotation to the target rotation
        hipTransform.rotation = Quaternion.Slerp(
            hipTransform.rotation,
            targetRotation,
            Time.deltaTime * smoth
        );
    }



    void RotateLeftUpLeg(Transform upperLegTransform)
    {
        if (upperLegTransform == null || landmarks.Count < 27) return;

        // Get landmarks for the left hip (point 23), right hip (point 24), and left knee (point 25)
        Landmark leftHip = landmarks[23];
        Landmark rightHip = landmarks[24];
        Landmark leftKnee = landmarks[25];

        // Validate visibility of the landmarks, ensuring all points are visible enough
        if (leftHip.visibility < 0.5f || rightHip.visibility < 0.5f || leftKnee.visibility < 0.5f) return;

        // Create a vector for hip to knee and reverse the direction
        Vector3 hipToKnee = new Vector3(
            leftKnee.x - leftHip.x,
            leftKnee.y - leftHip.y,
            leftKnee.z - leftHip.z
        );

        // Reverse the direction to fix the opposite rotation issue
        hipToKnee = -hipToKnee;

        // Normalize the vector to ensure it only represents direction
        hipToKnee.Normalize();

        // Calculate a reference up vector using the left and right hips
        Vector3 hipLine = new Vector3(
            rightHip.x - leftHip.x,
            rightHip.y - leftHip.y,
            rightHip.z - leftHip.z
        );

        // Calculate the cross product to find a stable "up" vector for the leg
        Vector3 referenceUp = Vector3.Cross(hipLine, hipToKnee);
        if (referenceUp == Vector3.zero)
        {
            referenceUp = Vector3.up; // Fallback if cross product is zero
        }

        // Calculate the target rotation based on the hip-to-knee direction and the stable up vector
        Quaternion targetRotation = Quaternion.LookRotation(hipToKnee, referenceUp);

        // Adjust the rotation to account for the initial T-pose offset (adjust these values to correctly align with the T-pose)
        Quaternion tPoseCorrection = Quaternion.Euler(-90f, 0f, -180f);  // Modified to fix the opposite direction issue
        targetRotation = targetRotation * tPoseCorrection;

        // Smoothly interpolate the current rotation to the target rotation
        upperLegTransform.rotation = Quaternion.Slerp(upperLegTransform.rotation, targetRotation, Time.deltaTime * smoth);
    }



    void RotateRightUpLeg(Transform upperLegTransform)
    {
        if (upperLegTransform == null || landmarks.Count < 27) return;

        // Get landmarks for the right hip (point 24), left hip (point 23), and right knee (point 26)
        Landmark rightHip = landmarks[24];
        Landmark leftHip = landmarks[23];
        Landmark rightKnee = landmarks[26];

        // Validate visibility of the landmarks, ensuring all points are visible enough
        if (rightHip.visibility < 0.5f || leftHip.visibility < 0.5f || rightKnee.visibility < 0.5f) return;

        // Create a vector for hip to knee and reverse the direction
        Vector3 hipToKnee = new Vector3(
            rightKnee.x - rightHip.x,
            rightKnee.y - rightHip.y,
            rightKnee.z - rightHip.z
        );

        // Reverse the direction to fix the opposite rotation issue
        hipToKnee = -hipToKnee;

        // Normalize the vector to ensure it only represents direction
        hipToKnee.Normalize();

        // Calculate a reference up vector using the right and left hips
        Vector3 hipLine = new Vector3(
            leftHip.x - rightHip.x,
            leftHip.y - rightHip.y,
            leftHip.z - rightHip.z
        );

        // Calculate the cross product to find a stable "up" vector for the leg
        Vector3 referenceUp = Vector3.Cross(hipToKnee, hipLine);
        if (referenceUp == Vector3.zero)
        {
            referenceUp = Vector3.up; // Fallback if cross product is zero
        }

        // Calculate the target rotation based on the hip-to-knee direction and the stable up vector
        Quaternion targetRotation = Quaternion.LookRotation(hipToKnee, referenceUp);

        // Adjust the rotation to account for the initial T-pose offset (adjust these values to correctly align with the T-pose)
        Quaternion tPoseCorrection = Quaternion.Euler(-90f, 0f, 180f);  // Modified to fix the opposite direction issue
        targetRotation = targetRotation * tPoseCorrection;

        // Smoothly interpolate the current rotation to the target rotation
        upperLegTransform.rotation = Quaternion.Slerp(upperLegTransform.rotation, targetRotation, Time.deltaTime * smoth);
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
