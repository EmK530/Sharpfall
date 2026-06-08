using UnityEngine;
using System.Collections;

public class CameraScript : MonoBehaviour
{
    public float lookSpeed = 10.0f;
    public float moveSpeed = 35.0f;

    float rotationX = 0.0f;
    float rotationY = -24.784f;

    void Update()
    {
        if (!UIMain.playingMIDI)
            return;

        float step = Time.deltaTime;

        if (Input.GetMouseButton(1))
        {
            rotationX += Input.GetAxis("Mouse X") * lookSpeed;
            rotationY += Input.GetAxis("Mouse Y") * lookSpeed;
            rotationY = Mathf.Clamp(rotationY, -90f, 90f);
            transform.localRotation = Quaternion.AngleAxis(rotationX, Vector3.up);
            transform.localRotation *= Quaternion.AngleAxis(rotationY, Vector3.left);
        }

        transform.position += transform.forward * moveSpeed * Input.GetAxisRaw("Vertical") * step;
        transform.position += transform.right * moveSpeed * Input.GetAxisRaw("Horizontal") * step;
    }
}