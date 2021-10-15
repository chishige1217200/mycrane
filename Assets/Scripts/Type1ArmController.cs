﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Type1ArmController : MonoBehaviour
{
    GameObject[] arm;
    HingeJoint[] joint;
    JointMotor[] motor;
    JointLimits[] limit;
    [SerializeField] float armAperturesBase = 40f;

    void Start()
    {
        arm = new GameObject[2];
        joint = new HingeJoint[2];
        motor = new JointMotor[2];
        limit = new JointLimits[2];
    }

    public void SetArm(int armNum, int size)
    {
        string a = "Arm" + (armNum + 1).ToString();

        switch (size)
        {
            case 0:
            case 1:
                a += "S";
                break;
            case 2:
                a += "M";
                break;
            case 3:
                a += "L";
                break;
        }

        arm[armNum] = transform.Find(a).gameObject;
        if (size != 0) arm[armNum].SetActive(true);

        joint[armNum] = arm[armNum].GetComponent<HingeJoint>();
        motor[armNum] = joint[armNum].motor;
        limit[armNum] = joint[armNum].limits;
    }

    public void Open()
    {
        for (int i = 0; i < 2; i++)
        {
            motor[i].targetVelocity = -150f;
            motor[i].force = 100f;
            joint[i].motor = motor[i];
            joint[i].useMotor = true;
        }
    }

    public void FinalClose()
    {
        for (int i = 0; i < 2; i++)
        {
            motor[i].targetVelocity = 50f;
            motor[i].force = 1f;
            joint[i].motor = motor[i];
            joint[i].useMotor = true;
        }
    }

    public void Close(float power)
    {
        for (int i = 0; i < 2; i++)
        {
            motor[i].targetVelocity = power;
            motor[i].force = 1f;
            joint[i].motor = motor[i];
            joint[i].useMotor = true;
        }
    }

    public void SetMotorPower(float power, int num)
    {
        if (power > 50)
        {
            motor[num].targetVelocity = 100f;
            motor[num].force = power;
            joint[num].motor = motor[num];
        }
        else
        {
            motor[num].targetVelocity = 0.3f * (power - 50);
            motor[num].force = 1f;
            joint[num].motor = motor[num];
        }
    }

    public void SetLimit(float armApertures)
    {
        limit[0].min = -armAperturesBase * armApertures / 100;
        limit[1].min = -armAperturesBase * armApertures / 100;
        for (int i = 0; i < 2; i++)
            joint[i].limits = limit[i];
    }
}
