using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ArmUnitLifterV3 : BaseLifterV3
{
    public float upSpeed = 0.001f; //上昇速度
    public float downSpeed = 0.001f; //下降速度
    [SerializeField] float upLimit = 1f;
    [SerializeField] float downLimit = 0.7f;
    private Coroutine goPositionCoroutine;
    private Coroutine upCoroutine;
    private Coroutine downCoroutine;

    public override void GoPosition(float height)
    {
        goPositionCoroutine = StartCoroutine(InternalGoPosition(height));
    }

    public override void CancelGoPosition()
    {
        if (goPositionCoroutine != null) StopCoroutine(goPositionCoroutine);
    }

    IEnumerator InternalGoPosition(float height)
    {
        int checker = 0;
        while (true)
        {
            checker = 0;
            if (Mathf.Abs(transform.localPosition.y - height) <= (upSpeed + downSpeed) / 2)
            {
                checker++;
                if (transform.localPosition.y - height != 0)
                {
                    transform.localPosition = new Vector3(transform.localPosition.x, height, transform.localPosition.z);
                }
                goPositionCoroutine = null;
                yield break;
            }
            else
            {
                if (transform.localPosition.y < height) UpEvent();
                else if (transform.localPosition.y > height) DownEvent();
            }

            yield return new WaitForFixedUpdate();
        }
    }

    public override bool CheckPos(int mode) // 1:上，2：下，3：GoPosition用
    {
        int checker = 0; // 復帰チェック用
        if (mode == 1)
        {
            if (upCoroutine == null) checker++;
        }
        else if (mode == 2)
        {
            if (downCoroutine == null) checker++;
        }
        else if (mode == 3)
        {
            if (goPositionCoroutine == null) checker++;
        }

        if (checker == 1) return true;    // 該当箇所に復帰したとみなす
        else return false;                // 復帰していないとみなす
    }

    public override void Up(bool flag)
    {
        if (flag)
        {
            Down(false);
            upCoroutine = StartCoroutine(InternalUp());
        }
        else if (!flag && upCoroutine != null)
        {
            StopCoroutine(upCoroutine);
        }
    }

    private void UpEvent()
    {
        if (transform.localPosition.y < upLimit)
            transform.localPosition += new Vector3(0, upSpeed, 0);
    }

    IEnumerator InternalUp()
    {
        while (true)
        {
            if (transform.localPosition.y >= upLimit)
            {
                upCoroutine = null;
                yield break;
            }
            UpEvent();

            yield return new WaitForFixedUpdate();
        }
    }

    public override void Down(bool flag)
    {
        if (flag)
        {
            Up(false);
            downCoroutine = StartCoroutine(InternalDown());
        }
        else if (!flag && downCoroutine != null)
        {
            StopCoroutine(downCoroutine);
        }
    }

    private void DownEvent()
    {
        if (transform.localPosition.y > downLimit)
            transform.localPosition -= new Vector3(0, downSpeed, 0);
    }

    IEnumerator InternalDown()
    {
        while (true)
        {
            if (transform.localPosition.y <= downLimit)
            {
                downCoroutine = null;
                yield break;
            }
            DownEvent();

            yield return new WaitForFixedUpdate();
        }
    }
}
