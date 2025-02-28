﻿using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class Type5Manager : CraneManager
{
    [SerializeField] float[] armLPowerConfig = new float[3]; //アームパワーL(%，未確率時)
    [SerializeField] float[] armLPowerConfigSuccess = new float[3]; //アームパワーL(%，確率時)
    [SerializeField] float[] armRPowerConfig = new float[3]; //アームパワーR(%，未確率時)
    [SerializeField] float[] armRPowerConfigSuccess = new float[3]; //アームパワーR(%，確率時)
    [SerializeField] float armApertures = 80f; //開口率
    [SerializeField] float[] boxRestrictions = new float[2];
    [SerializeField] float downRestriction = 100f;
    public int soundType = 0; //SEの切り替え 0,1: CATCHER 8,9 2: CATCHER 7 Selecterで指定すること
    bool[] isExecuted = new bool[15]; //各craneStatusで1度しか実行しない処理の管理
    public bool buttonPushed = false; //trueならボタンをクリックしているかキーボードを押下している
    public float armLPower;
    public float armRPower;
    [SerializeField] bool player2 = false; //player2の場合true
    [SerializeField] bool button3 = true; //button3の使用可否
    [SerializeField] int[] armSize = new int[2]; // 0:なし，1:S，2:M，3:L
    public Vector2 startPoint; // 開始位置座標定義
    public Vector2 homePoint; // 獲得口座標定義（prizezoneTypeが9のとき使用）
    public int prizezoneType = 9; // 1:左手前，2：左奥，3：右手前，4：右奥，5：左，6：手前，7：右，8：奥，9：特定座標
    TwinArmController armController;
    ArmControllerSupport support;
    ArmUnitLifter lifter;
    ArmNail[] nail = new ArmNail[2];
    [SerializeField] TextMesh credit3d;
    [SerializeField] TextMesh[] preset = new TextMesh[4];
    public Animator[] animator = new Animator[3];
    [HideInInspector] public Type5NetworkV3 net;

    async void Start()
    {
        Transform temp;
        Transform xLimit = transform.Find("Floor").Find("XLimit");
        Transform zLimit = transform.Find("Floor").Find("ZLimit");
        Transform downLimit = transform.Find("Floor").Find("DownLimit");

        craneStatus = -3;
        craneType = 5;
        // 様々なコンポーネントの取得
        //host = transform.root.Find("CP").GetComponent<MachineHost>();
        canvas = transform.Find("Canvas").gameObject;
        creditSystem = transform.Find("CreditSystem").GetComponent<CreditSystem>();
        //sp = transform.Find("SE").GetComponent<SEPlayer>();
        getPoint = transform.Find("Floor").Find("GetPoint").GetComponent<GetPoint>();

        temp = transform.Find("CraneUnit").transform;

        // クレジット情報登録
        creditSystem.rateSet[0, 0] = priceSet[0];
        creditSystem.rateSet[1, 0] = priceSet[1];
        creditSystem.rateSet[0, 1] = timesSet[0];
        creditSystem.rateSet[1, 1] = timesSet[1];
        if (isHibernate)
        {
            creditSystem.SetHibernate();
        }
        else
        {
            preset[0].text = priceSet[0].ToString();
            preset[1].text = priceSet[1].ToString();
            preset[2].text = timesSet[0].ToString();
            preset[3].text = timesSet[1].ToString();
        }

        if (isHibernate | priceSet[1] == 0 || timesSet[1] == 0 || (float)priceSet[0] / timesSet[0] < (float)priceSet[1] / timesSet[1])
        // 未入力の場合，低価格設定を反映 //高額のレートになるとコストが多くなる設定エラーのとき
        {
            if (!player2)
                transform.parent.Find("LCD Component").Find("SegUnit2").gameObject.SetActive(false);// 第2クレジット表示無効に
            else
                transform.parent.Find("LCD Component").Find("SegUnit2 (1)").gameObject.SetActive(false);// 第2クレジット表示無効に
        }

        if (isHibernate)
        {
            if (!player2)
            {
                transform.parent.Find("LCD Component").Find("SegUnit3").gameObject.SetActive(false);
                transform.parent.Find("LCD Component").Find("SegUnit1").gameObject.SetActive(false);
            }
            else
            {
                transform.parent.Find("LCD Component").Find("SegUnit3 (1)").gameObject.SetActive(false);
                transform.parent.Find("LCD Component").Find("SegUnit1 (1)").gameObject.SetActive(false);
            }
        }

        // ロープとアームコントローラに関する処理
        lifter = temp.Find("CraneBox").Find("Tube").Find("TubePoint").GetComponent<ArmUnitLifter>();
        armController = temp.Find("ArmUnit").GetComponent<TwinArmController>();
        support = temp.Find("ArmUnit").Find("Main").GetComponent<ArmControllerSupport>();

        for (int i = 0; i < 2; i++)
        {
            string a = "Arm" + (i + 1).ToString();
            GameObject arm;
            switch (armSize[i])
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
            arm = temp.Find("ArmUnit").Find(a).gameObject;
            nail[i] = arm.transform.Find("Nail").GetComponent<ArmNail>();
            if (armSize[i] != 0) arm.SetActive(true);
            armController.SetArm(i, armSize[i]);
        }

        await Task.Delay(500);
        // CraneBoxに関する処理
        craneBox = temp.Find("CraneBox").GetComponent<CraneBox>();

        // ロープにマネージャー情報をセット
        creditSystem.SetSEPlayer(sp);
        getPoint.SetManager(this);
        switch (soundType)
        {
            case 0:
            case 1:
            case 2:
                getSoundNum = 7;
                break;
            case 3:
                getSoundNum = 15;
                break;
        }
        lifter.Up();

        if (soundType == 0 || soundType == 1 || soundType == 2) creditSystem.SetCreditSound(0);
        else creditSystem.SetCreditSound(8);
        creditSystem.SetSEPlayer(sp);
        support.SetManager(this);
        support.SetLifter(lifter);
        support.pushTime = 300; // 押し込みパワーの調整
        await Task.Delay(500);
        for (int i = 0; i < 2; i++)
        {
            nail[i].SetManager(this);
            nail[i].SetLifter(lifter);
        }

        for (int i = 0; i < 15; i++)
            isExecuted[i] = false;

        if (!button3)
        {
            transform.Find("Canvas").Find("ControlGroup").Find("Button 3").gameObject.SetActive(false);
            transform.Find("Floor").Find("Button3").gameObject.SetActive(false);
        }

        // イニシャル移動とinsertFlagを後に実行
        while (!lifter.UpFinished())
        {
            await Task.Delay(100);
        }
        armController.SetLimit(armApertures);

        if (!player2)
        {
            startPoint = new Vector2(-0.61f + startPoint.x, -0.31f + startPoint.y);
            homePoint = new Vector2(-0.61f + homePoint.x, -0.31f + homePoint.y);
            if (boxRestrictions[0] < 100) xLimit.localPosition = new Vector3(-0.5f + 0.004525f * boxRestrictions[0], xLimit.localPosition.y, xLimit.localPosition.z);
        }
        else
        {
            startPoint = new Vector2(0.61f - startPoint.x, -0.31f + startPoint.y);
            homePoint = new Vector2(0.61f - homePoint.x, -0.31f + homePoint.y);
            if (boxRestrictions[0] < 100) xLimit.localPosition = new Vector3(0.5f - 0.004525f * boxRestrictions[0], xLimit.localPosition.y, xLimit.localPosition.z);
        }
        if (boxRestrictions[1] < 100) zLimit.localPosition = new Vector3(zLimit.localPosition.x, zLimit.localPosition.y, -0.19f + 0.00605f * boxRestrictions[1]);
        if (downRestriction < 100) downLimit.localPosition = new Vector3(downLimit.localPosition.x, 1.4f - 0.005975f * downRestriction, downLimit.localPosition.z);
        craneBox.goPoint = startPoint;

        host.manualCode = 7;
        craneStatus = -2;
    }

    async void Update()
    {
        if (useUI && host.playable && !canvas.activeSelf) canvas.SetActive(true);
        else if (!host.playable && canvas.activeSelf) canvas.SetActive(false);
        if (!player2 && (Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.Alpha0))) InsertCoin();
        else if (player2 && (Input.GetKeyDown(KeyCode.KeypadPeriod) || Input.GetKeyDown(KeyCode.Minus))) InsertCoin();

        if (craneStatus == -2 && ((craneBox.CheckPos(1) && !player2) || (craneBox.CheckPos(3) && player2)))
        {
            craneStatus = -1;
            craneBox.goPositionFlag = true;
        }
        if (craneStatus == -1)
        {
            if (craneBox.CheckPos(9)) craneStatus = 0;
        }

        if (craneStatus > 0)
        {
            if (Input.GetKey(KeyCode.M) && Input.GetKey(KeyCode.Y) && Input.GetKey(KeyCode.C) && !probability) probability = true; // テスト用隠しコマンド
            if (craneStatus == 1)
            {
                //コイン投入有効化;
                //右移動ボタン有効化;
                DetectKey(craneStatus);
            }

            if (craneStatus == 2)
            { //右移動中
              //コイン投入無効化;
                DetectKey(craneStatus);
                //クレーン右移動;
                switch (soundType)
                {
                    case 0:
                    case 1:
                    case 2:
                        sp.Play(1);
                        break;
                    case 3:
                        sp.Play(9);
                        break;
                }
                if (!player2 && craneBox.CheckPos(7))
                {
                    buttonPushed = false;
                    craneStatus = 3;
                }
                if (player2 && craneBox.CheckPos(5))
                {
                    buttonPushed = false;
                    craneStatus = 3;
                }
                //右移動効果音ループ再生;
            }

            if (craneStatus == 3)
            {
                DetectKey(craneStatus);
                switch (soundType)
                {
                    case 0:
                    case 1:
                    case 2:
                        sp.Stop(1);
                        break;
                    case 3:
                        sp.Stop(9);
                        break;
                }
                //右移動効果音ループ再生停止;
                //奥移動ボタン有効化;
            }

            if (craneStatus == 4)
            { //奥移動中
                DetectKey(craneStatus);
                //クレーン奥移動;
                switch (soundType)
                {
                    case 0:
                    case 1:
                    case 2:
                        sp.Play(2);
                        break;
                    case 3:
                        sp.Play(10);
                        break;
                }
                if (craneBox.CheckPos(8))
                {
                    buttonPushed = false;
                    craneStatus = 5;
                }
                //奥移動効果音ループ再生;
            }

            if (craneStatus == 5)
            {
                sp.Stop(1); //奥移動効果音ループ再生停止;
                if (!isExecuted[craneStatus])
                {
                    isExecuted[craneStatus] = true;
                    armController.Open();
                    switch (soundType)
                    {
                        case 0:
                        case 1:
                        case 2:
                            sp.Stop(2);
                            sp.Play(3, 1);
                            break;
                        case 3:
                            sp.Stop(10);
                            sp.Play(11, 1);
                            break;
                    }
                    await Task.Delay(1700);
                    if (craneStatus == 5) craneStatus = 6;
                }
                //アーム開く音再生;
                //アーム開く;
            }

            if (craneStatus == 6)
            {
                if (!isExecuted[craneStatus])
                {
                    isExecuted[craneStatus] = true;
                    switch (soundType)
                    {
                        case 0:
                        case 1:
                        case 2:
                            sp.Play(4);
                            break;
                        case 3:
                            sp.Play(12);
                            break;
                    }
                    if (craneStatus == 6) lifter.Down(); //awaitによる時差実行を防止
                }
                DetectKey(craneStatus);
                if (lifter.DownFinished() && craneStatus == 6) craneStatus = 7;
                //アーム下降音再生
                //アーム下降;
            }

            if (craneStatus == 7)
            {
                switch (soundType)
                {
                    case 0:
                    case 1:
                    case 2:
                        sp.Stop(4);
                        break;
                    case 3:
                        sp.Stop(12);
                        break;
                }
                //アーム下降音再生停止;
                await Task.Delay(1000);
                //アーム掴む音再生;
                if (!isExecuted[craneStatus])
                {
                    isExecuted[craneStatus] = true;
                    switch (soundType)
                    {
                        case 0:
                        case 1:
                        case 2:
                            sp.Play(5, 1);
                            break;
                        case 3:
                            sp.Play(13, 1);
                            break;
                    }
                    armController.Close(30f);

                    await Task.Delay(3000);
                    if (craneStatus == 7)
                    {
                        if (probability)
                        {
                            if (armLPowerConfigSuccess[0] >= 30) armLPower = armLPowerConfigSuccess[0];
                            else armLPower = 30f;
                            if (armRPowerConfigSuccess[0] >= 30) armRPower = armRPowerConfigSuccess[0];
                            else armRPower = 30f;
                        }
                        else
                        {
                            if (armLPowerConfig[0] >= 30) armLPower = armLPowerConfig[0];
                            else armLPower = 30f;
                            if (armRPowerConfig[0] >= 30) armRPower = armRPowerConfig[0];
                            else armRPower = 30f;
                        }
                        armController.SetMotorPower(armLPower, 0);
                        armController.SetMotorPower(armRPower, 1);
                        craneStatus = 8; //awaitによる時差実行を防止
                    }
                }
                //アーム掴む;
            }

            if (craneStatus == 8)
            {
                //アーム上昇音再生;
                if (!isExecuted[craneStatus])
                {
                    isExecuted[craneStatus] = true;
                    switch (soundType)
                    {
                        case 0:
                        case 1:
                        case 2:
                            sp.Play(4);
                            break;
                        case 3:
                            sp.Play(14);
                            break;
                    }
                    lifter.Up();
                    /*await Task.Delay(1000);
                    if (craneStatus < 11)
                    {
                        armController.SetMotorPower(leftCatchArmpower, 0);
                        armController.SetMotorPower(rightCatchArmpower, 1);
                    }*/
                }
                if (lifter.UpFinished() && craneStatus == 8) craneStatus = 9;
                //アーム上昇;
            }

            if (craneStatus == 9)
            {
                //アーム上昇停止音再生;
                //アーム上昇停止;

                if (!isExecuted[craneStatus])
                {
                    isExecuted[craneStatus] = true;
                    switch (soundType)
                    {
                        case 0:
                        case 1:
                        case 2:
                            sp.Stop(4);
                            break;
                        case 3:
                            sp.Stop(14);
                            break;
                    }
                    await Task.Delay(200);
                    if (craneStatus == 9)
                    {
                        if (probability)
                        {
                            armLPower = armLPowerConfigSuccess[1];
                            armRPower = armRPowerConfigSuccess[1];
                        }
                        else
                        {
                            armLPower = armLPowerConfig[1];
                            armRPower = armRPowerConfig[1];
                        }
                        armController.SetMotorPower(armLPower, 0);
                        armController.SetMotorPower(armRPower, 1);
                        if (prizezoneType == 9)
                        {
                            craneBox.goPoint = homePoint;
                            craneBox.goPositionFlag = true;
                        }
                        craneStatus = 10;
                    }
                }
            }

            if (craneStatus == 10)
            {
                if (!isExecuted[craneStatus])
                {
                    isExecuted[craneStatus] = true;
                    switch (soundType)
                    {
                        case 0:
                        case 1:
                        case 2:
                            sp.Play(6);
                            break;
                        case 3:
                            sp.Play(9);
                            break;
                    }
                }
                if (craneBox.CheckPos(prizezoneType)) craneStatus = 11;
                //アーム獲得口ポジション移動音再生;
                //アーム獲得口ポジションへ;
            }

            if (craneStatus == 11)
            {
                if (!isExecuted[craneStatus])
                {
                    isExecuted[craneStatus] = true;
                    switch (soundType)
                    {
                        case 0:
                        case 1:
                        case 2:
                            sp.Stop(6);
                            break;
                        case 3:
                            sp.Stop(9);
                            break;
                    }
                    await Task.Delay(500);
                    switch (soundType)
                    {
                        case 0:
                        case 1:
                        case 2:
                            sp.Play(3, 1);
                            break;
                        case 3:
                            sp.Play(11, 1);
                            break;
                    }
                    armController.SetLimit(100f); // アーム開口度を100に
                    armController.Open();
                    await Task.Delay(2500);
                    if (craneStatus == 11) craneStatus = 12;
                }
                //アーム開く音再生;
                //アーム開く;
                //1秒待機;
            }

            if (craneStatus == 12)
            {
                if (!isExecuted[craneStatus])
                {
                    isExecuted[craneStatus] = true;
                    switch (soundType)
                    {
                        case 0:
                        case 1:
                        case 2:
                            sp.Play(5, 1);
                            break;
                        case 3:
                            sp.Play(13, 1);
                            break;
                    }
                    armController.Close(100f);
                    await Task.Delay(1000);
                    if (craneStatus == 12) craneStatus = 13;
                }
                //アーム閉じる音再生;
                //アーム閉じる;
                //1秒待機;
            }

            if (craneStatus == 13)
            {
                if (!isExecuted[craneStatus])
                {
                    isExecuted[craneStatus] = true;
                    switch (soundType)
                    {
                        case 0:
                        case 1:
                        case 2:
                            sp.Play(6);
                            break;
                    }
                }
                if ((craneBox.CheckPos(1) && !player2) || (craneBox.CheckPos(3) && player2))
                {
                    switch (soundType)
                    {
                        case 0:
                        case 1:
                        case 2:
                            sp.Stop(6);
                            break;
                    }
                    await Task.Delay(1000);
                    if (craneStatus == 13) craneStatus = 14;
                }
                //アーム初期位置帰還
            }
            if (craneStatus == 14)
            {
                if (!isExecuted[craneStatus])
                {
                    isExecuted[craneStatus] = true;
                    switch (soundType)
                    {
                        case 0:
                        case 1:
                        case 2:
                            sp.Play(6);
                            break;
                    }
                    for (int i = 0; i < 14; i++)
                        isExecuted[i] = false;
                    armController.SetLimit(armApertures); //アーム開口度リセット

                    craneBox.goPoint = startPoint;
                    craneBox.goPositionFlag = true;
                }
                if (isExecuted[craneStatus])
                {
                    if (craneBox.CheckPos(9))
                    {
                        switch (soundType)
                        {
                            case 0:
                            case 1:
                            case 2:
                                sp.Stop(6);
                                break;
                        }
                        if (creditSystem.creditDisplayed > 0)
                            craneStatus = 1;
                        else
                            craneStatus = 0;
                    }
                }
                //アームスタート位置へ
            }
        }
    }

    void FixedUpdate()
    {
        if (craneStatus != 0)
        {
            if (craneStatus == -2 || craneStatus == 13)
            {
                if (!player2) craneBox.Left();
                else craneBox.Right();
                craneBox.Forward();
            }
            else if (craneStatus == 8)
            {
                if (probability)
                {
                    if (armLPower > armLPowerConfigSuccess[1]) armLPower -= 0.15f;
                    if (armRPower > armRPowerConfigSuccess[1]) armRPower -= 0.15f;
                }
                else
                {
                    if (armLPower > armLPowerConfig[1]) armLPower -= 0.15f;
                    if (armRPower > armRPowerConfig[1]) armRPower -= 0.15f;
                }
                armController.SetMotorPower(armLPower, 0);
                armController.SetMotorPower(armRPower, 1);
            }
            else if (craneStatus == 10 || craneStatus == 15)
            {
                if (craneStatus == 10)
                {
                    if (probability)
                    {
                        if (armLPower > armLPowerConfigSuccess[2]) armLPower -= 0.15f;
                        if (armRPower > armRPowerConfigSuccess[2]) armRPower -= 0.15f;
                    }
                    else
                    {
                        if (armLPower > armLPowerConfig[2]) armLPower -= 0.15f;
                        if (armRPower > armRPowerConfig[2]) armRPower -= 0.15f;
                    }
                    armController.SetMotorPower(armLPower, 0);
                    armController.SetMotorPower(armRPower, 1);
                }
                switch (prizezoneType) // 1:左手前，2：左奥，3：右手前，4：右奥，5：左，6：手前，7：右，8：奥，9：特定座標
                {
                    case 1:
                        craneBox.Left();
                        craneBox.Forward();
                        break;
                    case 2:
                        craneBox.Left();
                        craneBox.Back();
                        break;
                    case 3:
                        craneBox.Right();
                        craneBox.Forward();
                        break;
                    case 4:
                        craneBox.Right();
                        craneBox.Back();
                        break;
                    case 5:
                        craneBox.Left();
                        break;
                    case 6:
                        craneBox.Forward();
                        break;
                    case 7:
                        craneBox.Right();
                        break;
                    case 8:
                        craneBox.Back();
                        break;
                }
            }
            else if (craneStatus == 2)
            {
                if (!player2) craneBox.Right();
                else craneBox.Left();
            }
            else if (craneStatus == 4) craneBox.Back();
        }
    }

    public override void GetPrize()
    {
        if (net == null) // ネットワークに参加していないとき
            for (int i = 0; i < 3; i++) animator[i].SetTrigger("GetPrize");
        else
            net.CelebrateAll();
        base.GetPrize();
    }

    protected override void DetectKey(int num)
    {
        if (host.playable)
        {
            int credit = 0;
            switch (num)
            {
                case 1:
                    if ((Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Alpha1)) && !buttonPushed && !player2)
                    {
                        buttonPushed = true;
                        if (craneStatus == 1)
                        {
                            creditSystem.ResetPayment();
                            credit = creditSystem.PlayStart();
                            if (credit < 100) credit3d.text = credit.ToString();
                            else credit3d.text = "99.";
                            isExecuted[14] = false;
                            probability = creditSystem.ProbabilityCheck();
                            Debug.Log("Probability:" + probability);
                        }
                        craneStatus = 2;
                    }
                    if ((Input.GetKeyDown(KeyCode.Keypad7) || Input.GetKeyDown(KeyCode.Alpha7)) && !buttonPushed && player2)
                    {
                        buttonPushed = true;
                        if (craneStatus == 1)
                        {
                            creditSystem.ResetPayment();
                            credit = creditSystem.PlayStart();
                            if (credit < 100) credit3d.text = credit.ToString();
                            else credit3d.text = "99.";
                            isExecuted[14] = false;
                            probability = creditSystem.ProbabilityCheck();
                            Debug.Log("Probability:" + probability);
                        }
                        craneStatus = 2;
                    }
                    break;
                //投入を無効化
                case 2:
                    if ((Input.GetKeyUp(KeyCode.Keypad1) || Input.GetKeyUp(KeyCode.Alpha1)) && buttonPushed && !player2)
                    {
                        craneStatus = 3;
                        buttonPushed = false;
                    }
                    if ((Input.GetKeyUp(KeyCode.Keypad7) || Input.GetKeyUp(KeyCode.Alpha7)) && buttonPushed && player2)
                    {
                        craneStatus = 3;
                        buttonPushed = false;
                    }
                    break;
                case 3:
                    if ((Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Alpha2)) && !buttonPushed && !player2)
                    {
                        buttonPushed = true;
                        craneStatus = 4;
                    }
                    if ((Input.GetKeyDown(KeyCode.Keypad8) || Input.GetKeyDown(KeyCode.Alpha8)) && !buttonPushed && player2)
                    {
                        buttonPushed = true;
                        craneStatus = 4;
                    }
                    break;
                case 4:
                    if ((Input.GetKeyUp(KeyCode.Keypad2) || Input.GetKeyUp(KeyCode.Alpha2)) && buttonPushed && !player2)
                    {
                        craneStatus = 5;
                        buttonPushed = false;
                    }
                    if ((Input.GetKeyUp(KeyCode.Keypad8) || Input.GetKeyUp(KeyCode.Alpha8)) && buttonPushed && player2)
                    {
                        craneStatus = 5;
                        buttonPushed = false;
                    }
                    break;
                case 6:
                    if ((Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Alpha3)) && !player2 && button3)
                    {
                        lifter.DownForceStop();
                        craneStatus = 7;
                    }
                    if ((Input.GetKeyDown(KeyCode.Keypad9) || Input.GetKeyDown(KeyCode.Alpha9)) && player2 && button3)
                    {
                        lifter.DownForceStop();
                        craneStatus = 7;
                    }
                    break;
            }
        }
    }

    public override void ButtonDown(int num)
    {
        int credit = 0;
        switch (num)
        {
            case 1:
                if (craneStatus == 1 && !buttonPushed)
                {
                    buttonPushed = true;
                    craneStatus = 2;
                    creditSystem.ResetPayment();
                    credit = creditSystem.PlayStart();
                    if (credit < 100) credit3d.text = credit.ToString();
                    else credit3d.text = "99.";
                    isExecuted[14] = false;
                    probability = creditSystem.ProbabilityCheck();
                    Debug.Log("Probability:" + probability);
                }
                break;
            case 2:
                if ((craneStatus == 3 && !buttonPushed) || (craneStatus == 4 && buttonPushed))
                {
                    buttonPushed = true;
                    craneStatus = 4;
                }
                break;
            case 3:
                if (craneStatus == 6)
                {
                    lifter.DownForceStop();
                    craneStatus = 7;
                }
                break;
            case 4: // player2 case 1:
                if (craneStatus == 1 && !buttonPushed)
                {
                    buttonPushed = true;
                    craneStatus = 2;
                    creditSystem.ResetPayment();
                    credit = creditSystem.PlayStart();
                    if (credit < 100) credit3d.text = credit.ToString();
                    else credit3d.text = "99.";
                    isExecuted[14] = false;
                    probability = creditSystem.ProbabilityCheck();
                    Debug.Log("Probability:" + probability);
                }
                break;
        }
    }

    public override void ButtonUp(int num)
    {
        switch (num)
        {
            case 1:
                if (craneStatus == 2 && buttonPushed)
                {
                    craneStatus = 3;
                    buttonPushed = false;
                }
                break;
            case 2:
                if (craneStatus == 4 && buttonPushed)
                {
                    craneStatus = 5;
                    buttonPushed = false;
                }
                break;
            case 4: // player2 case 1:
                if (craneStatus == 2 && buttonPushed)
                {
                    craneStatus = 3;
                    buttonPushed = false;
                }
                break;
        }
    }

    public override void InsertCoin()
    {
        if (!isHibernate && host.playable && craneStatus >= 0)
        {
            int credit = creditSystem.Pay(100);
            if (credit < 100) credit3d.text = credit.ToString();
            else credit3d.text = "99.";
            if (credit > 0 && craneStatus == 0) craneStatus = 1;
        }
    }

    public override void InsertCoinAuto()
    {
        if (!isHibernate && craneStatus >= 0)
        {
            int credit = creditSystem.Pay(100);
            if (credit < 100) credit3d.text = credit.ToString();
            else credit3d.text = "99.";
            if (credit > 0 && craneStatus == 0) craneStatus = 1;
        }
    }
}
