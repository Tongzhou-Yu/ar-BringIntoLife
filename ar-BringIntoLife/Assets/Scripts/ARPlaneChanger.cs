using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARPlaneChanger : MonoBehaviour
{

public void ChangePlane(Material PlaneMaterial)
    {
        GetComponentInChildren<MeshRenderer>().material = PlaneMaterial;
    }
}
