using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageController : MonoBehaviour
{
    public CylinderController[] cylinders;

    private void Awake()
    {
        cylinders = GetComponentsInChildren<CylinderController>();
    }
}
