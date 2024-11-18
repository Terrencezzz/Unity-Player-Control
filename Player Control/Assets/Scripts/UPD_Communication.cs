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
                    Debug.Log($"Landmark {i}: X={landmark.x}, Y={landmark.y}, Z={landmark.z}, Visibility={landmark.visibility}");
                }
                // Optionally, perform actions with the landmarks here
            }
        }
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
