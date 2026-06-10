using UnityEngine;

public class CameraFacingDashboard : MonoBehaviour
{
    public Transform cameraOverride;
    public bool matchCameraPlane = true;
    public Vector3 rotationOffsetEuler;

    private Camera cachedCamera;

    private void LateUpdate()
    {
        var cameraTransform = ResolveCameraTransform();
        if (cameraTransform == null) return;

        if (matchCameraPlane)
        {
            transform.rotation = cameraTransform.rotation * Quaternion.Euler(rotationOffsetEuler);
            return;
        }

        Vector3 direction = transform.position - cameraTransform.position;
        if (direction.sqrMagnitude < 0.000001f) return;

        transform.rotation = Quaternion.LookRotation(direction.normalized, cameraTransform.up)
            * Quaternion.Euler(rotationOffsetEuler);
    }

    private Transform ResolveCameraTransform()
    {
        if (cameraOverride != null) return cameraOverride;

        if (cachedCamera == null || !cachedCamera.isActiveAndEnabled)
        {
            cachedCamera = Camera.main;
        }

        return cachedCamera != null ? cachedCamera.transform : null;
    }
}
