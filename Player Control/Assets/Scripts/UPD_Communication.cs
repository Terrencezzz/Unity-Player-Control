using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class UPD_Communication : MonoBehaviour
{
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;

    public List<Landmark> landmarks = new List<Landmark>();

    public GameObject noseObject;

    private int scale = 5;


    // Start is called before the first frame update
    void Start()
    {
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
        headTransform.rotation = Quaternion.Slerp(headTransform.rotation, targetRotation, Time.deltaTime * 5f);
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
