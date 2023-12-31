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
    public void Pour(CylinderController cylinder)
    {
        tarCylinder = cylinder;
        tarCylinder.cylinders.Add(selCylinder);
        selCylinder.Pouring(true);

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

        InitCylinders();

        


        CylinderMove(selCylinderTemp.transform, targetPos, async () =>
        {
            var dir = Vector3.zero;
            var liquids = selCylinderTemp.liquids;
            if (selCylinderTemp.transform.position.x >= tarCylinderTemp.transform.position.x)
            {
                dir = Vector3.forward;
                for (var i = 0; i < liquids.Length; i++)
                {
                    liquids[i].transform.localPosition += Vector3.left;
                }
            }
            else
            {
                dir = Vector3.back;
                for (var i = 0; i < liquids.Length; i++)
                {
                    liquids[i].transform.localPosition += Vector3.right;
                }
            }

            
            CylinderRotate(selCylinderTemp.transform, dir * 45f, Ease.OutQuad, async () =>
            {
                var duration = 0.25f / templiquidQue.Count;
                while (tempClearCount > 0)
                {
                    if (templiquidQue.Count == 0) break;
                    var selLiquid = templiquidQue.Dequeue();
                    var tarLiquid = tarLiquidQue.Dequeue();
                    LiquidOut(selLiquid, tarLiquid, duration);
                    await UniTask.WaitUntil(() => tarLiquid.transform.localScale.y >= 1);
                    selLiquid.transform.localScale = Vector3.one;
                    tempClearCount--;
                }

                selCylinderTemp.CheckState();
            });

            await UniTask.WaitUntil(() => templiquidQue.Count == 0);

            CylinderRotate(selCylinderTemp.transform, Vector3.zero, Ease.OutQuad, () =>
            {
                if (dir == Vector3.forward)
                {
                    for (var i = 0; i < liquids.Length; i++)
                    {
                        liquids[i].transform.localPosition += Vector3.right;
                    }
                }
                else
                {
                    for (var i = 0; i < liquids.Length; i++)
                    {
                        liquids[i].transform.localPosition += Vector3.left;
                    }
                }

                tarCylinderTemp.cylinders.Remove(selCylinderTemp);

                CylinderMove(selCylinderTemp.transform, selCylinderOriPos, () =>
                {
                    selCylinderTemp.sortingGroup.sortingOrder = 0;
                    selCylinderTemp.Pouring(false);
                });
            });
        });
        
    }

    private void InitCylinders()
    {
        selCylinder.CheckState();
        tarCylinder.CheckState();

        DeSelect();
        tarCylinder = null;
    }

    private void LiquidOut(SpriteRenderer selLiquid, SpriteRenderer tarLiquid, float duration = 0.25f)
    {
        selLiquid.transform.DOScaleY(0f, duration).OnComplete(() =>
        {
            selLiquid.color = Color.clear;
        });
        tarLiquid.transform.DOScaleY(1f, duration);
    }

    private void CylinderRotate(Transform cylinder, Vector3 rot, Ease ease, Action moveEndAction = null)
    {
        var liquids = cylinder.GetComponent<CylinderController>().liquids;
        cylinder.DORotate(rot, 0.5f).OnUpdate(() =>
        {
            for (var i = 0; i < liquids.Length; i++)
            {
                liquids[i].transform.rotation = Quaternion.identity;
            }
        }).SetEase(ease).OnComplete(() =>
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
