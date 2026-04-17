using Photon.Pun;
using UnityEngine;

public class RobloxCameraController : MonoBehaviourPun
{
    [Header("Refs")]
    public Transform target;         // Player root
    public Transform pivot;          // CameraPivot (player child olabilir)
    public Camera cam;

    [Header("Orbit")]
    public float mouseSensitivity = 2.2f;
    public float minPitch = -30f;
    public float maxPitch = 70f;

    [Header("Zoom")]
    public float distance = 6f;          // hedef mesafe (scroll ile deðiþir)
    public float minDistance = 2.5f;
    public float maxDistance = 10f;
    public float zoomSpeed = 4f;

    [Header("Smooth")]
    public float followSmooth = 15f;
    public float rotationSmooth = 18f;

    [Header("Collision")]
    public LayerMask collisionMask = ~0;
    public float collisionRadius = 0.25f;
    public float collisionMinExtra = 0.2f;     // pivot ile duvar arasý tampon
    public float collisionInSpeed = 25f;       // yakýnlaþma hýzý
    public float collisionReturnSpeed = 8f;    // geri açýlma hýzý

    [Header("Vertical smoothing (jump pump fix)")]
    public float pivotYSmooth = 12f;           // zýplarken pivot Y zýplamasýný yumuþatýr

    private float yaw;
    private float pitch;

    private float _currentDistance;
    private Vector3 _smoothedPivotPos;

    private void Awake()
    {
        if (!target) target = transform;
        if (!pivot) pivot = transform.Find("CameraPivot");
        if (!cam && pivot) cam = pivot.GetComponentInChildren<Camera>(true);
    }

    private void Start()
    {
        if (!photonView.IsMine)
        {
            if (cam) cam.enabled = false;
            enabled = false;
            return;
        }

        if (cam) cam.enabled = true;

        // Kamera parent'ta kalmasýn (player rotation jitter'ý bitirir)
        if (cam && cam.transform.parent != null)
            cam.transform.SetParent(null, true);

        _currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yaw = target.eulerAngles.y;
        pitch = 10f;

        _smoothedPivotPos = pivot ? pivot.position : target.position;
    }

    private void LateUpdate()
    {
        if (!pivot || !cam) return;

        // Orbit input
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Zoom input
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        // Pivot world rotation'ý sabitle (player dönerken kamera sarsýlmasýn)
        pivot.rotation = Quaternion.identity;

        // Pivot pozisyonunu yumuþat (özellikle zýplarken Y ekseni pump'ý azaltýr)
        Vector3 desiredPivot = pivot.position;
        _smoothedPivotPos = SmoothPivot(desiredPivot);

        // Kamera rotasyonu (world)
        Quaternion desiredRot = Quaternion.Euler(pitch, yaw, 0f);
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, desiredRot, Time.deltaTime * rotationSmooth);

        // Collision ile hedef mesafeyi bul
        float collidedDist = SolveCollisionDistance(_smoothedPivotPos, cam.transform.forward, distance);

        // Mesafe smoothing: yaklaþma hýzlý, geri açýlma yumuþak
        float dSpeed = (collidedDist < _currentDistance) ? collisionInSpeed : collisionReturnSpeed;
        _currentDistance = Mathf.Lerp(_currentDistance, collidedDist, Time.deltaTime * dSpeed);

        // Kamera hedef pozisyonu
        Vector3 targetPos = _smoothedPivotPos - (cam.transform.forward * _currentDistance);

        // Pozisyon smoothing
        cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, Time.deltaTime * followSmooth);
    }

    private Vector3 SmoothPivot(Vector3 desiredPivot)
    {
        // XZ hýzlý takip, Y biraz daha yumuþak (jump sýrasýnda “zoom gibi” hissi azaltýr)
        Vector3 current = _smoothedPivotPos;

        // XZ
        Vector3 xzCurrent = new Vector3(current.x, 0f, current.z);
        Vector3 xzDesired = new Vector3(desiredPivot.x, 0f, desiredPivot.z);
        Vector3 xz = Vector3.Lerp(xzCurrent, xzDesired, Time.deltaTime * followSmooth);

        // Y
        float y = Mathf.Lerp(current.y, desiredPivot.y, Time.deltaTime * pivotYSmooth);

        return new Vector3(xz.x, y, xz.z);
    }

    private float SolveCollisionDistance(Vector3 from, Vector3 camForward, float wantedDist)
    {
        // pivot'tan kameranýn arkasýna doðru cast
        Vector3 dir = -camForward;
        float dist = Mathf.Clamp(wantedDist, minDistance, maxDistance);

        if (Physics.SphereCast(from, collisionRadius, dir, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
        {
            dist = Mathf.Max(minDistance, hit.distance - collisionMinExtra);
        }

        return dist;
    }

    public float GetYaw() => yaw;
}