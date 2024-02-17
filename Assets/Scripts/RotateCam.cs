using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateCam : MonoBehaviour
{
    float rot = 0f;
    void Update()
    {
        rot += (Time.deltaTime * 16f);
        transform.rotation = Quaternion.Euler(0f, rot, 0f);
    }
}
