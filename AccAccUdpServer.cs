using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;

public class AccAccUdpServer : MonoBehaviour {

    static public int port_udp_receive_ = 12346;
    static public int port_udp_send_ = 12347;
    static public string host_;
    static UdpClient udp_;
    Thread thread_;
    static public int port_tcp_ = 12345;

    void Start () {

        host_ = get_ip_address();
        udp_ = new UdpClient(new IPEndPoint(IPAddress.Parse(host_), port_udp_receive_));
        thread_ = new Thread(new ThreadStart(ThreadMethod));
        thread_.Start();
    }

    // Update is called once per frame
    void Update () {
       // var data = Encoding.UTF8.GetString(result.Buffer);
    }

    string get_ip_address()
    {
        string hostName = Dns.GetHostName();    // 自身のホスト名を取得
        IPAddress[] addresses = Dns.GetHostAddresses(hostName);

        foreach (IPAddress address in addresses)
        {
            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return address.ToString();
            }
        }
        return null;
    }

    private static void ThreadMethod()
    {
        while (true)
        {
            IPEndPoint remoteEP = null;
            byte[] data = udp_.Receive(ref remoteEP);
            string text = Encoding.ASCII.GetString(data);
            Debug.Log(text);
            if (text == "accacc_tcp_search\n" || text == "accacc_tcp_search")
            {
                string host = remoteEP.Address.ToString();
                string send_data = host_ + ":" + port_tcp_;
                byte[] byte_data = System.Text.Encoding.ASCII.GetBytes(send_data);
                send(host, port_udp_send_, byte_data);
                Debug.Log("send:" + host + ":" + port_udp_send_+", data:"+send_data);
            }
        }
    }

    private static void send(string host, int port, byte[] data)
    {
        UdpClient client = new UdpClient(host, port);
        client.Send(data, data.Length);
        client.Close();
    }

    void OnApplicationQuit()
    {
        udp_.Close();
        thread_.Abort();
    }
}
