﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Type6Selecter : CelebrationSync
{
    [SerializeField] Type6Manager[] manager = new Type6Manager[2];
    [SerializeField] Animator[] animator = new Animator[3];
    BGMPlayer bp;
    [SerializeField] int soundType = 0; //0:U9_1，1:U9_2
    [SerializeField] float audioPitch = 1.0f;
    [SerializeField] int lightColor = 1; //1:Red, 2:Green, 3:Blue, 4:Yellow, 5:Sky, 6:Purple, 7:Pink(255,128,255), 8:Orange(255,128,0), 9:Forest(128,255,0)

    void Start()
    {
        bp = this.transform.Find("BGM").GetComponent<BGMPlayer>();
        bp.SetAudioPitch(audioPitch);
        manager[0].soundType = soundType;
        manager[1].soundType = soundType;
        manager[0].sp.SetAudioPitch(audioPitch);
        manager[1].sp.SetAudioPitch(audioPitch);

        for (int i = 0; i < 3; i++)
        {
            animator[i].SetInteger("ColorNumber", lightColor);
            manager[0].animator[i] = animator[i];
            manager[1].animator[i] = animator[i];
        }
    }

    void Update()
    {
        if (manager[0].GetStatus() == 0 && manager[1].GetStatus() == 0)
        {
            switch (soundType)
            {
                case 0:
                    bp.Stop(1);
                    bp.Play(0);
                    break;
                case 1:
                    bp.Stop(3);
                    bp.Play(2);
                    break;
            }
        }
        else if (manager[0].GetStatus() > 0 || manager[1].GetStatus() > 0)
        {
            switch (soundType)
            {
                case 0:
                    bp.Stop(0);
                    bp.Play(1);
                    break;
                case 1:
                    bp.Stop(2);
                    bp.Play(3);
                    break;
            }
        }
    }

    public Type6Manager GetManager(int num)
    {
        return manager[num - 1];
    }

    public override void SetupNetWork(Type5NetworkV3 net) // 獲得連動用
    {
        manager[0].net = net;
        manager[1].net = net;
    }

    public override void Celebrate() // 獲得連動用
    {
        for (int i = 0; i < 3; i++) animator[i].SetTrigger("GetPrize");
    }
}
