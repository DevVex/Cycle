using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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


public class UDP_GyroReceive : MonoBehaviour {
	// receiving Thread
	Thread receiveThread;
	
	// udpclient object
	UdpClient client;
	
	public int port; // define > init
	
	// infos
	public string lastReceivedUDPPacket="";

	//Variable used to pass UDP data to the main thread
	private static Vector3 latestCamPosition;


	private float initialDelay = 0f;
	private float repeatTime = 0.01f;

    private Vector2 touchCoords = Vector2.zero;


    public Transform moveableObj;

    public TextMeshProUGUI ipAddressUi;
    public TextMeshProUGUI tiltUi;
    public TextMeshProUGUI speedUi;
    public TextMeshProUGUI cadenceUi;
    public TextMeshProUGUI distanceUi;
    public TextMeshProUGUI heartRateUi;


    private string tilt = "";
    private float speed = 0f;
    private float cadence = 0f;
    private float distance = 0f;
    private string heartRate = "";

    // start from shell
    private static void Main()
	{
		UDP_GyroReceive receiveObj=new UDP_GyroReceive();
		receiveObj.init();
		
		string text="";
		do
		{
			text = Console.ReadLine();
		}
		while(!text.Equals("exit"));
	}

	// start from unity3d
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

    }

    // init
    private void init()
	{

		receiveThread = new Thread(new ThreadStart(ReceiveData));
		receiveThread.IsBackground = true;
		receiveThread.Start();
		
	}

    // receive thread
    private void ReceiveData()
    {
        client = new UdpClient(port);
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);

                string text = Encoding.UTF8.GetString(data);

                Debug.Log("Text: " + text);

                ClientTransfer clientTransfer = JsonUtility.FromJson<ClientTransfer>(text);

                tilt = clientTransfer.tilt;
                speed = clientTransfer.speed;
                cadence = clientTransfer.cadence;
                distance = clientTransfer.distance;
                heartRate = clientTransfer.heartRate;


                lastReceivedUDPPacket = text;

            }
            catch (Exception err)
            {
                print(err.ToString());
            }
        }
    }
	
	void OnApplicationQuit(){
        if (receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }

        if (receiveThread != null)
		    receiveThread.Abort(); 
		if (client!=null) 
			client.Close(); 
	}

    public string LocalIPAddress()
    {
        return IPManager.GetIP(ADDRESSFAM.IPv4);
    }


}
