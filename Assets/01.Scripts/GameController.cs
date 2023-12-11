using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System;

public class GameController : MonoBehaviour
{
    public static GameController instance;
    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public CylinderController selCylinder;
    public CylinderController tarCylinder;

    public void Select(CylinderController cylinder)
    {
        cylinder.transform.position += Vector3.up;
        selCylinder = cylinder;

        var stage = cylinder.GetComponentInParent<StageController>();
        var topLayerId = stage.cylinders.Max(x => x.sortingGroup.sortingLayerID);

        selCylinder.sortingGroup.sortingOrder = topLayerId + 1;
    }
    public void DeSelect()
    {
        selCylinder.transform.position += Vector3.down;
        selCylinder = null;
    }
    public async UniTask Pour(CylinderController cylinder)
    {
        tarCylinder = cylinder;
        selCylinder.isPouring = true;

        var liquidQue = new Queue<SpriteRenderer>();

        for (var i = 0; i < selCylinder.liquids.Length; i++)
        {
            if (selCylinder.liquids[i].color == Color.clear) continue;

            if (liquidQue.Count > 0)
            {
                var lastSprite = liquidQue.Peek();
                if (lastSprite.color == selCylinder.liquids[i].color)
                {
                    liquidQue.Enqueue(selCylinder.liquids[i]);
                }
                else break;
            }
            else
            {
                liquidQue.Enqueue(selCylinder.liquids[i]);
            }
        }

        var templiquidQue = new Queue<SpriteRenderer>(liquidQue);
        var tarLiquidQue = new Queue<SpriteRenderer>();

        for (var i = 0; i < tarCylinder.liquids.Length; i++)
        {
            if (tarCylinder.liquids[i].color == Color.clear) continue;

            if (tarCylinder.liquids[i].color != liquidQue.Peek().color)
            {
                InitCylinders();
                return;
            }
        }

        var clearCount = tarCylinder.liquids.Count(x => x.color == Color.clear);
        var tempClearCount = clearCount;

        for (var i = tarCylinder.liquids.Length - 1; i >= 0; i--)
        {
            if (tarCylinder.liquids[i].color != Color.clear) continue;
            clearCount--;
            var lastSprite = liquidQue.Dequeue();

            tarCylinder.liquids[i].color = lastSprite.color;

            tarCylinder.liquids[i].transform.localScale += Vector3.down;
            tarLiquidQue.Enqueue(tarCylinder.liquids[i]);

            if (liquidQue.Count == 0 || clearCount == 0) break;
        }

        var targetPos = Vector3.zero;

        if (selCylinder.transform.position.x >= tarCylinder.transform.position.x)
            targetPos = tarCylinder.transform.position + Vector3.up * 2f + Vector3.right * 0.5f;
        else
            targetPos = tarCylinder.transform.position + Vector3.up * 2f + Vector3.left * 0.5f;

        var selCylinderTemp = selCylinder;
        var tarCylinderTemp = tarCylinder;

        var selCylinderOriPos = selCylinder.transform.position + Vector3.down;

        CylinderMove(selCylinderTemp.transform, targetPos, async () =>
        {
            var dir = Vector3.zero;
            if (selCylinderTemp.transform.position.x >= tarCylinderTemp.transform.position.x)
                dir = Vector3.forward;
            else
                dir = Vector3.back;
            CylinderRotate(selCylinderTemp.transform, dir * 45f);

            while (tempClearCount > 0)
            {
                var selLiquid = templiquidQue.Dequeue();
                var tarLiquid = tarLiquidQue.Dequeue();
                LiquidOut(selLiquid, tarLiquid);
                await UniTask.WaitUntil(() => selLiquid.transform.localScale.y <= 0);
                selLiquid.transform.localScale = Vector3.one;
                tempClearCount--;
            }

            CylinderRotate(selCylinderTemp.transform, Vector3.zero, () =>
            {
                CylinderMove(selCylinderTemp.transform, selCylinderOriPos, () =>
                {
                    selCylinderTemp.sortingGroup.sortingOrder = 0;
                    selCylinderTemp.isPouring = false;
                });
            });
        });

        InitCylinders();
    }

    private void InitCylinders()
    {
        selCylinder.CheckState();
        tarCylinder.CheckState();

        DeSelect();
        tarCylinder = null;
    }

    private void LiquidOut(SpriteRenderer selLiquid, SpriteRenderer tarLiquid)
    {
        selLiquid.transform.DOScaleY(0f, 0.25f).OnComplete(() =>
        {
            selLiquid.color = Color.clear;
        });
        tarLiquid.transform.DOScaleY(1f, 0.25f);
    }

    private void CylinderRotate(Transform cylinder, Vector3 rot, Action moveEndAction = null)
    {
        cylinder.DORotate(rot, 0.5f/*, RotateMode.FastBeyond360*/).SetEase(Ease.InQuad).OnComplete(() =>
        {
            moveEndAction?.Invoke();
        });
    }

    private void CylinderMove(Transform cylinder, Vector3 targetPos, Action moveEndAction = null)
    {
        cylinder.DOMove(targetPos, 0.5f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            moveEndAction?.Invoke();
        });
    }
}
