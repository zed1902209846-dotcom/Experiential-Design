using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateArround : MonoBehaviour
{
    public float rotationSpeed;
    public GameObject pivotObject;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // pivotObject 在素材包里默认为空，空引用会每帧抛 UnassignedReferenceException；未赋值时直接跳过。
        if (pivotObject == null) return;
        transform.RotateAround(pivotObject.transform.position, new Vector3(0, 1, 0), rotationSpeed * Time.deltaTime);
    }
}
