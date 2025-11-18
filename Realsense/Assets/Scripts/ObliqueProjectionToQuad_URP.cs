using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class ObliqueProjectionToQuad_URP : MonoBehaviour
{
    public GameObject projectionScreen;
    public bool estimateViewFrustum = false;
    public bool setNearClipPlane = false;
    public float minNearClipDistance = 0.0001f;
    public float nearClipDistanceOffset = -0.01f;

    private Camera cam;

    void OnEnable()
    {
        cam = GetComponent<Camera>();

        // SRP (URP/HDRP) 전용 렌더 이벤트
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    // ───────────────────────────────────────
    // URP/HDRP용 렌더링 이벤트
    void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera c)
    {
        if (c == cam)
        {
            ApplyObliqueProjection();
        }
    }

    // ───────────────────────────────────────
    // 실제 projectionMatrix 계산
    void ApplyObliqueProjection()
    {
        if (projectionScreen == null || cam == null)
            return;

        Vector3 pa = projectionScreen.transform.TransformPoint(new Vector3(-0.5f, -0.5f, 0));
        Vector3 pb = projectionScreen.transform.TransformPoint(new Vector3(0.5f, -0.5f, 0));
        Vector3 pc = projectionScreen.transform.TransformPoint(new Vector3(-0.5f, 0.5f, 0));

        Vector3 pe = cam.transform.position;
        float n = cam.nearClipPlane;
        float f = cam.farClipPlane;

        Vector3 vr = (pb - pa).normalized;
        Vector3 vu = (pc - pa).normalized;
        Vector3 vn = -Vector3.Cross(vr, vu).normalized;

        Vector3 va = pa - pe;
        Vector3 vb = pb - pe;
        Vector3 vc = pc - pe;

        float d = -Vector3.Dot(va, vn);

        if (setNearClipPlane)
        {
            n = Mathf.Max(minNearClipDistance, d + nearClipDistanceOffset);
            cam.nearClipPlane = n;
        }

        float l = Vector3.Dot(vr, va) * n / d;
        float r = Vector3.Dot(vr, vb) * n / d;
        float b = Vector3.Dot(vu, va) * n / d;
        float t = Vector3.Dot(vu, vc) * n / d;

        Matrix4x4 p = new Matrix4x4();
        p[0, 0] = 2.0f * n / (r - l);
        p[0, 2] = (r + l) / (r - l);
        p[1, 1] = 2.0f * n / (t - b);
        p[1, 2] = (t + b) / (t - b);
        p[2, 2] = (f + n) / (n - f);
        p[2, 3] = 2.0f * f * n / (n - f);
        p[3, 2] = -1.0f;

        Matrix4x4 rm = Matrix4x4.identity;
        rm.SetRow(0, new Vector4(vr.x, vr.y, vr.z, 0));
        rm.SetRow(1, new Vector4(vu.x, vu.y, vu.z, 0));
        rm.SetRow(2, new Vector4(vn.x, vn.y, vn.z, 0));

        Matrix4x4 tm = Matrix4x4.identity;
        tm.SetColumn(3, new Vector4(-pe.x, -pe.y, -pe.z, 1));

        cam.projectionMatrix = p;
        cam.worldToCameraMatrix = rm * tm;

        if (estimateViewFrustum)
        {
            Quaternion q = Quaternion.LookRotation((0.5f * (pb + pc) - pe), vu);
            cam.transform.rotation = q;
        }
    }
}
