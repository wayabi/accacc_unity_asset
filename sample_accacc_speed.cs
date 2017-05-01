using UnityEngine;
using System.Collections;
using System;

namespace AccAcc
{
    public class sample_accacc_speed : MonoBehaviour
    {
        // Animatorの変数名。モーションのスピード倍率。
        public string name_param_motion_speed_ = "node_speed_aa";
        // 1振りと認識する加速度の最小値
        public double thre_power_shake_ = 0.7;
        // 停止判定用の値。直近の1振りの加速度がこの値以下ならば停止と判定する。
        public double thre_ave_power_one_cycle_ = 1.0;
        // 再生するモーションの周期
        public float duration_motion_ = 2.875f;

        // 現在の振り数。スマートフォンが1振りされる度にインクリメントされる。
        int count_rhythm_;
        // 操作するAnimator
        Animator anim_;

        double x_r_old_ = 1.0;
        double y_r_old_ = 0.0;
        double z_r_old_ = 0.0;

        // 振る方向が変更されたか
        bool flag_side_shake_ = false;

        int count_twerk_ = 0;

        void Start()
        {
            count_rhythm_ = 0;

            anim_ = GetComponent<Animator>();
            anim_.SetFloat(name_param_motion_speed_, 0.0f);
        }

        // 振る方向が大きく変わったかチェックする
        bool check_side_shake()
        {
            // 現在の振る方向と前回の降る方向３DベクトルのCosを計算する
            double cos_v = get_cos(x_r_old_, y_r_old_, z_r_old_, AccAccServerValue.X_R, AccAccServerValue.Y_R, AccAccServerValue.Z_R);
            //Debug.Log(string.Format("cos_v = {0}, {1}, {2}, {3}, {4}, {5}, {6}", x_r_old_, y_r_old_, z_r_old_, AccAccServerValue.X_R, AccAccServerValue.Y_R, AccAccServerValue.Z_R, cos_v));
            // だいたい20度くらい変わったらフラグをtrueにする
            if (cos_v < Math.Cos(Math.PI * 20 / 180))
            {
                x_r_old_ = AccAccServerValue.X_R;
                y_r_old_ = AccAccServerValue.Y_R;
                z_r_old_ = AccAccServerValue.Z_R;
                return true;
            }
            return false;
        }

        void Update()
        {
            if (check_side_shake())
            {
                // 振る方向が大きく変わったらヴァイブレーション
                vibration();
            }

            // 振られた回数が更新されているかチェックする
            if (AccAccServerValue.ON_RHYTHM != count_rhythm_)
            {
                // 振る力（加速度）が一定以上かチェックする
                if (AccAccServerValue.POWER > thre_power_shake_ && AccAccServerValue.AVE_POWER_ONE_CYCLE > thre_ave_power_one_cycle_)
                {
                    // スマフォの振り周期が20回/秒以上は速すぎるので無視する
                    if (AccAccServerValue.T > 1.0 / 20)
                    {
                        // 振り回数を更新
                        count_rhythm_ = AccAccServerValue.ON_RHYTHM;

                        // 振る周期とモーションの再生時間から再生速度を計算
                        float speed = (float)(duration_motion_ * (1 / AccAccServerValue.T));
                        // Animatorの状態遷移などで再生が若干遅れることがあるので少しだけ再生速度を上げておく
                        // アニメーションがスムーズになることがある
                        speed = speed * 1.05f;

                        anim_.SetFloat(name_param_motion_speed_, speed);
                    }
                }
                else
                {
                    // 振る力が弱いのでモーションのスピードを0にする
                    anim_.SetFloat(name_param_motion_speed_, 0.0f);
                }
            }
        }

        private double get_cos(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            double a = Math.Sqrt(x1 * x1 + y1 * y1 + z1 * z1) * Math.Sqrt(x2 * x2 + y2 * y2 + z2 * z2);
            if (Math.Abs(a) < 0.0000001) return 1.0;
            return (x1 * x2 + y1 * y2 + z1 * z2) / a;
        }

        // スマフォにヴァイブレーション要求を送信するサンプル関数
        private void vibration()
        {
            lock (AccAccServerValue.SEND_QUEUE)
            {
                // コマンドフォーマット "vib,<wait1>,<vib1>,<wait2>,<vib2>, ...\n"
                // iPhoneは1度短くヴァイブレーションするのみ。(iPhoneのAPIに細かいヴァイブレーション制御がない)
                // iPhoneは後ろの<wait>や<vib>指定を無視する。

                // 0ミリ秒間 待機後に 200ミリ秒間 1回振動させる
                AccAccServerValue.SEND_QUEUE.Add("vib,0,200\n");

                // 100ミリ秒間隔で 50ミリ秒間3回短く振動させる(Androidのみ)
                //AccAccServerValue.SEND_QUEUE.Add("vib,0,50,100,50,100,50\n");
            }
        }

    }
}