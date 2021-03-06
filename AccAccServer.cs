﻿using UnityEngine;
using System.Collections;
using System;
using System.Net;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace AccAcc
{
    static class AccAccServerValue
    {
        // 振り周期（秒）
        // shake cycle
        public static double T = 1.0;
        // 振り回数。1振りごとにインクリメントされる。
        // shaking num. incremented on a shake.
        public static int ON_RHYTHM = 0;
        // 振り力。加速度から算出。
        // shaking power. calculated by acceleration.
        public static double POWER = 0.0;
        // 直近の１振りの周期の力。加速度から算出。ストップ判定に使う。
        // average shaking power in a latest shake(one wave). use for stop check.
        public static double AVE_POWER_ONE_CYCLE = 0.0;
        // スマフォX方向の振り力
        // phone x axis shake power
        public static double X_R = 0.0;
        // スマフォY方向の振り力
        // phone y axis shake power
        public static double Y_R = 0.0;
        // スマフォZ方向の振り力
        // phone z axis shake power
        public static double Z_R = 0.0;
        // スマフォへ送信するコマンドのキュー。stringをAdd()することでスマフォに送信する。
        // sending queue to phone. SEND_QUEUE.Add("command you want to send to the phone");
        public static ArrayList SEND_QUEUE;
    }

    public class AccAccServer : MonoBehaviour
    {
        public bool auto_ip_address = true;
        public string ip_address_manual_;
        public int port_;

        AccAccTcpServer server_;
        DateTime time_rhythm_last_;

        DateTime time_receive_per_sec_;
        int count_receive_per_sec_ = 0;
        bool is_connect_ = false;

        // Use this for initialization
        void Start()
        {
            AccAccServerValue.SEND_QUEUE = new ArrayList();
            server_ = new AccAccTcpServer();
            string host = ip_address_manual_;
            if (auto_ip_address)
            {
                host = get_ip_address();
                Debug.Log("AccAccServer auto_ip_address = " + host);
            }
            server_.StartListening(host, port_, on_connect, on_disconnect);

            time_rhythm_last_ = DateTime.Now;
            time_receive_per_sec_ = DateTime.Now;

        }

        void Update()
        {
            if (AccAccServerValue.SEND_QUEUE.Count > 0)
            {
                lock (AccAccServerValue.SEND_QUEUE)
                {
                    for (int i = 0; i < AccAccServerValue.SEND_QUEUE.Count; ++i)
                    {
                        String s = (String)AccAccServerValue.SEND_QUEUE[i];
                        server_.Send(s);
                    }
                    AccAccServerValue.SEND_QUEUE.Clear();
                }
            }


            if (server_.queue_.Count >= 7 * sizeof(float))
            {
                double hz = 1.0;
                double power = 0.0;
                double ave_power_one_cycle = 0.0;
                double x_rotate = 0.0;
                double y_rotate = 0.0;
                double z_rotate = 0.0;
                double reserved = 0.0;
                lock (server_.lock_)
                {
                    byte[] buf = new byte[sizeof(float)];

                    while (server_.queue_.Count >= 7 * sizeof(float))
                    {
                        ++count_receive_per_sec_;
                        for (int i = 0; i < sizeof(float); ++i)
                        {
                            buf[i] = server_.queue_.RemoveFromFront();
                        }
                        Array.Reverse(buf);
                        hz = (double)BitConverter.ToSingle(buf, 0);
                        for (int i = 0; i < sizeof(float); ++i)
                        {
                            buf[i] = server_.queue_.RemoveFromFront();
                        }
                        Array.Reverse(buf);
                        power = (double)BitConverter.ToSingle(buf, 0);
                        for (int i = 0; i < sizeof(float); ++i)
                        {
                            buf[i] = server_.queue_.RemoveFromFront();
                        }
                        Array.Reverse(buf);
                        ave_power_one_cycle = (double)BitConverter.ToSingle(buf, 0);
                        for (int i = 0; i < sizeof(float); ++i)
                        {
                            buf[i] = server_.queue_.RemoveFromFront();
                        }
                        Array.Reverse(buf);
                        x_rotate = (double)BitConverter.ToSingle(buf, 0);
                        for (int i = 0; i < sizeof(float); ++i)
                        {
                            buf[i] = server_.queue_.RemoveFromFront();
                        }
                        Array.Reverse(buf);
                        y_rotate = (double)BitConverter.ToSingle(buf, 0);
                        for (int i = 0; i < sizeof(float); ++i)
                        {
                            buf[i] = server_.queue_.RemoveFromFront();
                        }
                        Array.Reverse(buf);
                        z_rotate = (double)BitConverter.ToSingle(buf, 0);
                        for (int i = 0; i < sizeof(float); ++i)
                        {
                            buf[i] = server_.queue_.RemoveFromFront();
                        }
                        Array.Reverse(buf);
                        reserved = (double)BitConverter.ToSingle(buf, 0);

                    }
                }


                AccAccServerValue.T = 1.0 / hz;
                AccAccServerValue.POWER = power;
                AccAccServerValue.AVE_POWER_ONE_CYCLE = ave_power_one_cycle;
                AccAccServerValue.X_R = x_rotate;
                AccAccServerValue.Y_R = y_rotate;
                AccAccServerValue.Z_R = z_rotate;
            }

            DateTime t = DateTime.Now;
            TimeSpan ts = t - time_rhythm_last_;
            if (ts.TotalMilliseconds >= AccAccServerValue.T * 1000)
            {
                double t_add = -((long)(ts.TotalMilliseconds) % (long)(AccAccServerValue.T * 1000));
                time_rhythm_last_ = t.Add(TimeSpan.FromMilliseconds(t_add));
                ++(AccAccServerValue.ON_RHYTHM);
            }

            ts = t - time_receive_per_sec_;
            if (ts.TotalMilliseconds >= 1000)
            {
                time_receive_per_sec_ = t;
                count_receive_per_sec_ = 0;
            }

            if (!is_connect_)
            {
                AccAccServerValue.T = 1.0;
                AccAccServerValue.POWER = 0.0;
                AccAccServerValue.X_R = 0.0;
                AccAccServerValue.Y_R = 0.0;
                AccAccServerValue.Z_R = 0.0;
            }
        }

        public void on_disconnect(string s)
        {
            Debug.LogFormat("accacc_AccAccServerValue : client disconnected:" + s);
            is_connect_ = false;
        }
        public void on_connect()
        {
            Debug.LogFormat("accacc_AccAccServerValue : client connected");
            is_connect_ = true;
        }

        void OnDestroy()
        {
            server_.stop();
        }

        string get_ip_address()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            // 各インターフェースごとの情報を調べる
            foreach (var adapter in interfaces)
            {
                // 有効なインターフェースのみを対象とする
                if (adapter.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                // インターフェースに設定されたIPアドレス情報を取得
                var properties = adapter.GetIPProperties();

                // 設定されているすべてのユニキャストアドレスについて
                foreach (var unicast in properties.UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        // IPv4アドレス
                        return unicast.Address.ToString();
                    }
                    else if (unicast.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        // IPv6アドレス
                    }
                }
            }
            return null;
        }
    }
}
