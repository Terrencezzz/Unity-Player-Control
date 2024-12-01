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
    public List<HandLandmark> handLandmarks = new List<HandLandmark>();

    public GameObject root;
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
    public GameObject leftLowLegObject;
    public GameObject rightLowLegObject;

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

                // Deserialize the JSON data to determine the type
                ReceivedData receivedData = JsonUtility.FromJson<ReceivedData>(json);

                if (receivedData.type == "pose")
                {
                    // Deserialize as PoseData
                    PoseData poseData = JsonUtility.FromJson<PoseData>(json);

                    // Update pose landmarks
                    lock (landmarks)
                    {
                        landmarks = poseData.landmarks;
                    }

                    Debug.Log("Received pose data.");
                }
                else if (receivedData.type == "hand")
                {
                    // Deserialize as HandData
                    HandData handData = JsonUtility.FromJson<HandData>(json);

                    // Update hand landmarks
                    lock (handLandmarks)
                    {
                        handLandmarks = handData.hand_landmarks;
                    }

                    Debug.Log("Received hand data.");
                }
                else
                {
                    Debug.LogWarning("Unknown data type received: " + receivedData.type);
                }
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
                MoveModelRootBasedOnHip(root.transform);
                RotateHead(noseObject.transform);
                RotateLeftArm(leftArmObject.transform);
                RotateRightArm(rightArmObject.transform);
                RotateLeftForearm(leftForearmObject.transform);
                RotateRightForearm(rightForearmObject.transform);
                RotateLeftWrist(leftWristObject.transform);
                RotateRightWrist(rightWristObject.transform);
                RotateWaist(waistObject.transform);
                RotateChest(chestObject.transform);
                RotateHip(hipObject.transform);
                RotateLeftUpLeg(leftUpLeg.transform);
                RotateRightUpLeg(rightUpLeg.transform);
                RotateLeftLowerLeg(leftLowLegObject.transform);
                RotateRightLowerLeg(rightLowLegObject.transform);
            }
        }
    }

    // Declare variables to store initial positions
    private Vector3 initialHipMidpointNormalized;
    private Vector3 initialRootPosition;
    private bool initialPositionSet = false;

    void MoveModelRootBasedOnHip(Transform rootTransform)
    {
        if (rootTransform == null || landmarks.Count < 25) return; // Ensure landmarks are available

        // Get landmarks for the left and right hips (points 23 and 24 in MediaPipe Pose)
        Landmark leftHip = landmarks[23];
        Landmark rightHip = landmarks[24];

        // Calculate the midpoint between the hips (normalized space)
        Vector3 hipMidpointNormalized = new Vector3(
            (leftHip.x + rightHip.x) / 2.0f,
            (leftHip.y + rightHip.y) / 2.0f,
            (leftHip.z + rightHip.z) / 2.0f
        );

        // If initial position is not set, store the initial hip midpoint and root position
        if (!initialPositionSet)
        {
            initialHipMidpointNormalized = hipMidpointNormalized;
            initialRootPosition = rootTransform.position;
            initialPositionSet = true;
        }

        // Compute the offset from the initial hip midpoint
        Vector3 hipOffset = hipMidpointNormalized - initialHipMidpointNormalized;

        // Ignore vertical movement by setting the y-component to zero
        hipOffset.y = 0f;

        // Apply scale factor if needed
        float scaleFactor = 2f; // Adjust as necessary
        Vector3 worldHipOffset = new Vector3(
            hipOffset.x * scaleFactor * -1,
            0f, // Ensure y-component remains zero
            hipOffset.z * scaleFactor * -1
        );

        // Apply the offset to the initial root position
        Vector3 targetPosition = initialRootPosition + worldHipOffset;

        // Smoothly move the root transform to the new position
        rootTransform.position = Vector3.Lerp(rootTransform.position, targetPosition, Time.deltaTime * 5f);
    }




    void RotateHead(Transform headTransform)
    {
        if (headTransform == null || landmarks.Count < 9) return;

        // Get landmarks for the left ear (point 7), right ear (point 8), and nose (point 0)
        Landmark leftEar = landmarks[7];
        Landmark rightEar = landmarks[8];
        Landmark nose = landmarks[0];

        // Calculate the midpoint between the ears (optional, but can be useful for other calculations)
        Vector3 earMidpoint = new Vector3(
            (leftEar.x + rightEar.x) / 2.0f,
            (leftEar.y + rightEar.y) / 2.0f,
            (leftEar.z + rightEar.z) / 2.0f
        );

        // Create a vector pointing from the right ear to the left ear (this defines the head's rotation direction)
        Vector3 earLine = new Vector3(
            leftEar.x - rightEar.x,
            leftEar.y - rightEar.y,
            leftEar.z - rightEar.z
        );
        earLine.Normalize(); // Normalize to get just the direction

        // Calculate the horizontal (yaw) rotation using the X and Z components (left-right rotation)
        float horizontalRotationAngle = Mathf.Atan2(earLine.x, earLine.z) * Mathf.Rad2Deg;

        // **Corrected vertical rotation (pitch) calculation**:
        // Calculate the difference in Y positions between the ear midpoint and the nose
        float verticalDiff = earMidpoint.y - nose.y;

        // Use the difference to calculate the pitch angle
        // This is a more straightforward calculation for pitch, as it's simply the Y difference
        float verticalRotationAngle = Mathf.Atan2(verticalDiff, earLine.magnitude) * Mathf.Rad2Deg * 10;

        // Calculate the target rotation based on horizontal (yaw) and vertical (pitch) angles
        Quaternion targetRotation = Quaternion.Euler(0f, horizontalRotationAngle, -verticalRotationAngle);

        // Adjust the rotation to account for any initial T-pose offset (if needed)
        Quaternion tPoseCorrection = Quaternion.Euler(-20f, -90f, 0f);  // Adjust these values as needed
        targetRotation = targetRotation * tPoseCorrection;

        // Smoothly interpolate from the current rotation to the target rotation
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

        // Create a vector from wrist to index finger tip
        Vector3 wristToIndex = new Vector3(
            indexFingerTip.x - wrist.x,
            indexFingerTip.y - wrist.y,
            indexFingerTip.z - wrist.z
        ).normalized;

        // Use an up vector from the elbow to the wrist for a better reference
        Landmark elbow = landmarks[13]; // Left elbow landmark
        //if (elbow.visibility < 0.5f) return;
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

        // Create a vector from wrist to index finger tip
        Vector3 wristToIndex = new Vector3(
            indexFingerTip.x - wrist.x,
            indexFingerTip.y - wrist.y,
            indexFingerTip.z - wrist.z
        ).normalized;

        // Use an up vector from the elbow to the wrist for a better reference
        Landmark elbow = landmarks[14]; // Right elbow landmark
        //if (elbow.visibility < 0.5f) return;
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

    void RotateLeftLowerLeg(Transform lowerLegTransform)
    {
        if (lowerLegTransform == null || landmarks.Count < 33) return;

        // Get landmarks for the left hip (point 23), left knee (point 25), left ankle (point 27), and left foot index (point 31)
        Landmark leftHip = landmarks[23];
        Landmark leftKnee = landmarks[25];
        Landmark leftAnkle = landmarks[27];
        Landmark leftFootIndex = landmarks[31];

        // Convert landmark positions to Vector3
        Vector3 hipPos = new Vector3(leftHip.x, leftHip.y, leftHip.z);
        Vector3 kneePos = new Vector3(leftKnee.x, leftKnee.y, leftKnee.z);
        Vector3 anklePos = new Vector3(leftAnkle.x, leftAnkle.y, leftAnkle.z);
        Vector3 footIndexPos = new Vector3(leftFootIndex.x, leftFootIndex.y, leftFootIndex.z);

        // Calculate the direction vectors
        Vector3 kneeToAnkle = anklePos - kneePos;
        Vector3 ankleToFootIndex = footIndexPos - anklePos;

        // Calculate the normal of the plane formed by the hip, knee, and ankle
        Vector3 legPlaneNormal = Vector3.Cross(kneePos - hipPos, anklePos - kneePos);
        legPlaneNormal.Normalize();

        // Use the leg plane normal as the up vector
        Vector3 referenceUp = legPlaneNormal;

        // Calculate the target rotation
        Quaternion targetRotation = Quaternion.LookRotation(kneeToAnkle, referenceUp);

        // Adjust the rotation to account for the initial T-pose offset
        Quaternion tPoseCorrection = Quaternion.Euler(0f, 60f, -90f); // Modify as necessary for alignment
        targetRotation = targetRotation * tPoseCorrection;

        // Smoothly interpolate the current rotation to the target rotation
        lowerLegTransform.rotation = Quaternion.Slerp(lowerLegTransform.rotation, targetRotation, Time.deltaTime * smoth);
    }

    void RotateRightLowerLeg(Transform lowerLegTransform)
    {
        if (lowerLegTransform == null || landmarks.Count < 33) return;

        // Get landmarks for the right hip (point 24), right knee (point 26), right ankle (point 28), and right foot index (point 32)
        Landmark rightHip = landmarks[24];
        Landmark rightKnee = landmarks[26];
        Landmark rightAnkle = landmarks[28];
        Landmark rightFootIndex = landmarks[32];

        // Convert landmark positions to Vector3
        Vector3 hipPos = new Vector3(rightHip.x, rightHip.y, rightHip.z);
        Vector3 kneePos = new Vector3(rightKnee.x, rightKnee.y, rightKnee.z);
        Vector3 anklePos = new Vector3(rightAnkle.x, rightAnkle.y, rightAnkle.z);
        Vector3 footIndexPos = new Vector3(rightFootIndex.x, rightFootIndex.y, rightFootIndex.z);

        // Calculate the direction vectors
        Vector3 kneeToAnkle = anklePos - kneePos;
        Vector3 ankleToFootIndex = footIndexPos - anklePos;

        // Calculate the normal of the plane formed by the hip, knee, and ankle
        Vector3 legPlaneNormal = Vector3.Cross(kneePos - hipPos, anklePos - kneePos);
        legPlaneNormal.Normalize();

        // Use the leg plane normal as the up vector
        Vector3 referenceUp = legPlaneNormal;

        // Calculate the target rotation
        Quaternion targetRotation = Quaternion.LookRotation(kneeToAnkle, referenceUp);

        // Adjust the rotation to account for the initial T-pose offset
        Quaternion tPoseCorrection = Quaternion.Euler(0f, 60f, -90f); // Modify as necessary for alignment
        targetRotation = targetRotation * tPoseCorrection;

        // Smoothly interpolate the current rotation to the target rotation
        lowerLegTransform.rotation = Quaternion.Slerp(lowerLegTransform.rotation, targetRotation, Time.deltaTime * smoth);
    }

    [System.Serializable]
    public class ReceivedData
    {
        public string type;
    }

    [System.Serializable]
    public class PoseData
    {
        public List<Landmark> landmarks;
    }

    [System.Serializable]
    public class HandData : ReceivedData
    {
        public List<HandLandmark> hand_landmarks;
    }

    [System.Serializable]
    public class Landmark
    {
        public float x;
        public float y;
        public float z;
        public float visibility;
    }

    [System.Serializable]
    public class HandLandmark
    {
        public float x;
        public float y;
        public float z;
    }
}
