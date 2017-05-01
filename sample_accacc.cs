using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using AccAcc;

public class sample_accacc : MonoBehaviour {
    public float a = 1.0f;
    public float v_x = 0.0f;
    public float v_y = 1.0f;
    public float v_z = 0.0f;

    Vector3 pos_origin_;
    float time_;
    float time_last_shake_;
    

	void Start () {
        pos_origin_ = transform.position;
        time_ = 0.0f;
        time_last_shake_ = 0.0f;
	}
	
	// Update is called once per frame
	void Update () {
        // スマフォの振る力
        // shake power
        double power = AccAccServerValue.POWER;
        // スマフォの振る周期
        // shake cycle
        double t = AccAccServerValue.T;

        // 時間更新
        // update time
        time_ += Time.deltaTime;

        if(time_ >= time_last_shake_ + (float)t)
        {
            time_last_shake_ = time_;
        }

        // 周期の正規化 0.0fから1.0fを繰り返す
        // cycle time normalize. time1 range 0.0f ~ 0.1f.
        float time1 = (time_ - time_last_shake_) / (float)t;

        float x = v_x * Mathf.Cos(2 * Mathf.PI * time1) * (float)power;
        float y = v_y * Mathf.Cos(2 * Mathf.PI * time1) * (float)power;
        float z = v_z * Mathf.Cos(2 * Mathf.PI * time1) * (float)power;
        transform.position = pos_origin_ + new Vector3(x, y, z);
	}
}
