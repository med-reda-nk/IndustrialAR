using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 2.5f;
    public float speed = 3f;
    private float x, y;

    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            x += Input.GetAxis("Mouse X") * speed;
            y -= Input.GetAxis("Mouse Y") * speed;
        }
        distance -= Input.GetAxis("Mouse ScrollWheel") * 2f;
        Quaternion rot = Quaternion.Euler(y, x, 0);
        transform.position = target.position + rot * new Vector3(0, 0, -distance);
        transform.LookAt(target);
    }
}