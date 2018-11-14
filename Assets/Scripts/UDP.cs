using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TMPro;

[Serializable]
public class ClientTransfer{
    public float speed;
    public float distance;
    public float cadence;
    public string heartRate;
    public string tilt;
}


public class UDP : MonoBehaviour {
	// receiving Thread
	Thread receiveThread;

    // udpclient object
    UdpClient receiver;
    UdpClient sender;

    // infos
    private DateTime lastReceivedUDPPacket;

    public int sendPort;
    public int receivePort;

    IPEndPoint remoteEndPoint;

    public TextMeshProUGUI ipAddressUi;
    public TextMeshProUGUI tiltUi;
    public TextMeshProUGUI speedUi;
    public TextMeshProUGUI cadenceUi;
    public TextMeshProUGUI distanceUi;
    public TextMeshProUGUI heartRateUi;
    public TextMeshProUGUI logUi;


    private string tilt = "";
    private float speed = 0f;
    private float cadence = 0f;
    private float distance = 0f;
    private string heartRate = "";
    private string log = "";
    
    private float searchSpeed = 10f;
    private float healthCheckSpeed = 5f;
    private float disconnectTimeCheck = 15f;

    private string companionIp;
    private bool companionFound = false;

	public void Start()
	{
		
		init();
        ipAddressUi.text = LocalIPAddress();

    }

    public void Update()
    {
        tiltUi.text = tilt;
        speedUi.text = speed.ToString();
        cadenceUi.text = cadence.ToString();
        distanceUi.text = distance.ToString();
        heartRateUi.text = heartRate;
        logUi.text = log;

        if(companionIp != null && !companionFound)
        {
            CancelInvoke("Searching");
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(companionIp), sendPort);
            companionFound = true;
            InvokeRepeating("HealthCheck", 0.0f, healthCheckSpeed);
        }


        //Disconnected from companion, start searching
        if (companionFound && lastReceivedUDPPacket != null)
        {
            double timeDifference = (DateTime.Now - lastReceivedUDPPacket).TotalSeconds;
            if(timeDifference > disconnectTimeCheck)
            {
                CancelInvoke("HealthCheck");
                log += "\n Disconnected from companion";
                companionFound = false;
                companionIp = null;
                remoteEndPoint = null;
                InvokeRepeating("Searching", 0.0f, searchSpeed);
            }
        }
    }

    // init
    private void init()
	{

		receiveThread = new Thread(new ThreadStart(ReceiveData));
		receiveThread.IsBackground = true;
		receiveThread.Start();
        sender = new UdpClient();
        InvokeRepeating("Searching", 0.0f, searchSpeed);

    }

    // receive thread
    private void ReceiveData()
    {
        receiver = new UdpClient(receivePort);
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = receiver.Receive(ref anyIP);

                string text = Encoding.UTF8.GetString(data);

                Debug.Log("Text: " + text);

                if(companionIp == null)
                {
                    log += "\n Found";
                    companionIp = anyIP.Address.ToString();
                }

                ClientTransfer clientTransfer = JsonUtility.FromJson<ClientTransfer>(text);

                tilt = clientTransfer.tilt;
                speed = clientTransfer.speed;
                cadence = clientTransfer.cadence;
                distance = clientTransfer.distance;
                heartRate = clientTransfer.heartRate;

                lastReceivedUDPPacket = DateTime.Now;
            }
            catch (Exception err)
            {
                print(err.ToString());
            }
        }
    }

    private void Searching()
    {
        string message = "Searching";
        log += "\n Searching";

        for (int i = 0; i < 255; i++)
        {
            if (companionFound)
            {
                break;
            }
            else
            {
                remoteEndPoint = new IPEndPoint(IPAddress.Parse(LocalSubnet() + "." + i), sendPort);
                Send(message);
            }
        }

    }

    private void HealthCheck()
    {
        string message = "ping";
        log += "\n health";

        if (companionFound)
        {
            Send(message);
        }
    }

    private void Send(string message)
    {
        try
        {
            if (remoteEndPoint != null)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                sender.Send(data, data.Length, remoteEndPoint);
                Debug.Log("Sending: " + message);
            }
        }
        catch (Exception err)
        {
            Debug.Log(err.ToString());
        }
    }


    private void KillThreads()
    {
        if (receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }

        if (receiveThread != null)
            receiveThread.Abort();

        if (sender != null)
            sender.Close();

        if (receiver != null)
            receiver.Close();

    }

    void OnApplicationQuit()
    {
        KillThreads();

    }

    private void OnDestroy()
    {
        KillThreads();
    }

    private string LocalSubnet()
    {
        string[] ip = LocalIPAddress().Split('.');
        return ip[0] + "." + ip[1] + "." + ip[2];
    }

    public string LocalIPAddress()
    {
        return IPManager.GetIP(ADDRESSFAM.IPv4);
    }


}
