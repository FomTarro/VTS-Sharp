using UnityEngine;
using System.Collections;
 
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
 
public class UDPReceiver : IDisposable{
   
    // receiving Thread
    private Thread _receiveThread;
    // udpclient object
    private UdpClient _client;
    private bool _isRunning = false;
 
    // infos
    public string lastReceivedUDPPacket="";
    public string allReceivedUDPPackets=""; // clean up this from time to time!

    // init
    public UDPReceiver(int port)
    {
        // define port
        //port = 47779;
        this._client = new UdpClient(port);
        this._isRunning = true;
        this._receiveThread = new Thread(
            new ThreadStart(ReceiveData));
        _receiveThread.IsBackground = true;
        _receiveThread.Start();
    }

    ~UDPReceiver(){
        this.Dispose();
    }
 
    // receive thread
    private void ReceiveData()
    {
        Debug.Log("receiving udp");
        while (this._isRunning)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);
                lastReceivedUDPPacket=text;
                allReceivedUDPPackets=allReceivedUDPPackets+text;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        Debug.Log("end");
    }

    public void Dispose()
    {
        _client.Close();
        this._isRunning = false;
    }
}