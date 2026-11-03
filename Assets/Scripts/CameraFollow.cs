using UnityEngine;

public class CameraFollowRotate : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Pengaturan")]
    public float positionSmoothTime = 0.15f;
    public float rotationSmoothSpeed = 5f;

    private Vector3 localOffset;
    private Quaternion localRotation;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (target == null) return;

        // Simpan posisi & rotasi kamera relatif terhadap target saat start
        localOffset = target.InverseTransformPoint(transform.position);
        localRotation = Quaternion.Inverse(target.rotation) * transform.rotation;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.TransformPoint(localOffset);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, positionSmoothTime);

        Quaternion desiredRotation = target.rotation * localRotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed * Time.deltaTime);
    }
}