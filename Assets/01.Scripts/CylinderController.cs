using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DEFINES;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.Rendering;
using UniRx;

public class CylinderController : MonoBehaviour
{
    public ColorEnum[] colorEnums;

    [HideInInspector] public SpriteRenderer[] liquids; //액체의 순서는 위에서 아래로 0 ~ 3이다.
    [HideInInspector] public SortingGroup sortingGroup;
    public ReactiveCollection<CylinderController> cylinders = new ReactiveCollection<CylinderController>();
    public bool isFull, isEmpty, isOver, isPoured;

    [SerializeField] private Transform liquidsPar;
    private BoxCollider2D col;

    private void Awake()
    {
        liquids = liquidsPar.GetComponentsInChildren<SpriteRenderer>();
        sortingGroup = GetComponent<SortingGroup>();
        col = GetComponent<BoxCollider2D>();

        cylinders.ObserveAdd().Subscribe(item =>
        {
            if(!isPoured)
                isPoured = true;
        });

        cylinders.ObserveRemove().Subscribe(item =>
        {
            if (cylinders.Count == 0) 
                isPoured = false;
        });
    }

    private void Start()
    {
        for (var i = 0; i < liquids.Length; i++)
        {
            switch (colorEnums[i])
            {
                case ColorEnum.None:
                    liquids[i].color = Color.clear;
                    continue;
                case ColorEnum.Red:
                    liquids[i].color = Color.red;
                    continue;
                case ColorEnum.Blue:
                    liquids[i].color = Color.blue;
                    continue;
                case ColorEnum.Green:
                    liquids[i].color = Color.green;
                    continue;
            }
        }
        CheckState();
    }

    private void OnMouseUp()
    {
        if (isOver) return;

        if (GameController.instance.selCylinder == null)
        {
            if (isEmpty || isPoured) return;
            GameController.instance.Select(this);
        }
        else
        {
            if (GameController.instance.selCylinder == this)
            {
                GameController.instance.DeSelect();
                return;
            }

            GameController.instance.Pour(this)/*.Forget()*/;
        }
    }

    public void CheckState()
    {
        CheckEmpty();
        CheckOver();
        CheckFull();
        for (var i = 0; i < liquids.Length; i++)
        {
            if (liquids[i].color == Color.clear)
                colorEnums[i] = ColorEnum.None;
            else if (liquids[i].color == Color.red)
                colorEnums[i] = ColorEnum.Red;
            else if (liquids[i].color == Color.blue)
                colorEnums[i] = ColorEnum.Blue;
            else if (liquids[i].color == Color.green)
                colorEnums[i] = ColorEnum.Green;
        }
    }

    private void CheckEmpty()
    {
        isEmpty = liquids.Count(x => x.color == Color.clear) == liquids.Length;
    }

    private void CheckOver()
    {
        isOver = liquids.Count(x => x.color != Color.clear && x.color == liquids[liquids.Length - 1].color) == liquids.Length;
    }

    private void CheckFull()
    {
        isFull = liquids.Count(x => x.color != Color.clear) == liquids.Length;
    }

    public void Pouring(bool isOn)
    {
        col.enabled = !isOn;
    }
}
