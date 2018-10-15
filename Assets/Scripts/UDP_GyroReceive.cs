using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TMPro;


public class UDP_GyroReceive : MonoBehaviour {
	// receiving Thread
	Thread receiveThread;
	
	// udpclient object
	UdpClient client;
	
	public int port; // define > init
	
	// infos
	public string lastReceivedUDPPacket="";
	public string allReceivedUDPPackets=""; // clean up this from time to time!

	//Variable used to pass UDP data to the main thread
	private static Vector3 latestCamPosition;


	private float initialDelay = 0f;
	private float repeatTime = 0.01f;

    private bool hasUpdatedTouch = false;
    private bool hasUpdatePlayerNumber = false;
    private bool hasPressedSpecial = false;

    private Vector2 touchCoords = Vector2.zero;
    private int newPlayerNumber = 1;


    public Transform moveableObj;

    public TextMeshProUGUI ipAddressUi;

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

		//Update required to check data changes on separate UDP thread
		InvokeRepeating("UpdateObjectPosition", initialDelay, repeatTime);

        ipAddressUi.text = LocalIPAddress();

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

                //Pulling in information that has been sent to current ip and port
				if (text.Contains("camR:")){ //Change Cam Rotation
					//Remove data type identifier
					string newText = text.Replace(@"camR:","");

					//Parse the camera rotation
					Vector3 parsedRotation = getVector3(newText);

					latestCamPosition = parsedRotation;
				}
                else if (text.Contains("playN:")) //Change Player Number
                {
                    //Remove data type identifier
                    string newText = text.Replace(@"playN:", "");

                    //Parse the camera rotation
                    newPlayerNumber = int.Parse(newText);
                }
                else if (text.Contains("tPos:")) //Touch position
                {
                    //Remove data type identifier
                    string newText = text.Replace(@"tPos:", "");

                    //Parse the camera rotation
                    Vector3 tempCoords = getVector3(newText);

                    touchCoords = new Vector2(tempCoords.x, tempCoords.y);
                    hasUpdatedTouch = true;
                }
                else if (text.Contains("useS:")) //Use Special Button
                {
                    //Remove data type identifier
                    string newText = text.Replace(@"useS:", "");

                    //Parse the camera rotation
                    Vector3 parsedRotation = getVector3(newText);

                    latestCamPosition = parsedRotation;
                }

				lastReceivedUDPPacket=text;
				

				allReceivedUDPPackets=allReceivedUDPPackets+text;
				
			}
			catch (Exception err)
			{
				print(err.ToString());
			}
		}
	}

	public void UpdateObjectPosition(){
		Vector3 tempCamPosition = latestCamPosition;

		if (tempCamPosition != Vector3.zero)
			transform.rotation = Quaternion.Euler(tempCamPosition);


        //Check other possible updates happening on second thread
        if (hasUpdatedTouch){
            hasUpdatedTouch = false;

            //touchCoords

            DoRaycast();

            //Do the function for setting the raycast from the touch value
            Debug.Log("hasUpdatedTouch:");
        }
        if (hasUpdatePlayerNumber){
            hasUpdatePlayerNumber = false;

            //newPlayerNumber

            //Allow the player whos player number matches the new number be controlled
            Debug.Log("hasUpdatePlayerNumber:" + newPlayerNumber);
        }
        if (hasPressedSpecial){
            hasPressedSpecial = false;

            DoSpecialButton();

            //Allow any custom special function to be used here
            Debug.Log("hasPressedSpecial");
        }

	}

    void DoRaycast()
    {
        Ray newRay = Camera.main.ScreenPointToRay(touchCoords);

        Vector3 tempXY = Camera.main.ScreenToWorldPoint(touchCoords);

        RaycastHit hit;
        Vector3 fwd = Camera.main.transform.TransformDirection(Vector3.forward);

        Ray ray = Camera.main.ScreenPointToRay(touchCoords);
        moveableObj.position = ray.direction * 10;

        Debug.DrawRay(ray.origin, ray.direction * 80, Color.yellow,2,false);

        if (Physics.Raycast(ray, out hit, 1000))
        {
            print("Hit something:" + hit.transform.gameObject.name);

        }
    }

    void DoSpecialButton()
    {

    }

	public Vector3 getVector3(string rString){
		string[] temp = rString.Substring(1,rString.Length-2).Split(',');
		float x = float.Parse(temp[0]);
		float y = float.Parse(temp[1]);
		float z = float.Parse(temp[2]);
		Vector3 rValue = new Vector3(x,y,z);
		return rValue;
	}

	
	// getLatestUDPPacket
	// cleans up the rest
	public string getLatestUDPPacket()
	{
		allReceivedUDPPackets="";
		return lastReceivedUDPPacket;
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
