using System;
using UnityEngine;

namespace Emberlight
{
    /// <summary>独立 2.5D 美术场景的输入与相机，不接管正式战斗和 UI。</summary>
    public sealed class EmberLowPolyPreview : MonoBehaviour
    {
        [SerializeField] EmberLowPolyKeeper keeper;
        [SerializeField] Camera view;
        [SerializeField, Min(.01f)] float moveSpeed = 1.65f, followResponse = 5;
        [SerializeField] Vector2 walkHalfSize = new Vector2(4.8f, 5.8f);
        Vector3 cameraOffset, screenRight, screenForward;
        bool initialized;
        int previousAntialiasing;
        ShadowQuality previousShadows;
        ShadowResolution previousShadowResolution;
        float previousShadowDistance;
        public bool Paused { get; set; }
        public EmberLowPolyKeeper Keeper => keeper;
        public Camera View => view;

        void Awake() => Initialize();

        public void Initialize()
        {
            if (initialized) return;
            if (keeper == null || view == null) throw new InvalidOperationException("Low-poly sample references are missing.");
            keeper.Initialize();
            cameraOffset = view.transform.position - keeper.transform.position;
            screenRight = Vector3.ProjectOnPlane(view.transform.right, Vector3.up).normalized;
            screenForward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
            // 样板使用自己的预览质量，离开样板后恢复正式工程的质量选择。
            previousAntialiasing = QualitySettings.antiAliasing;
            previousShadows = QualitySettings.shadows;
            previousShadowResolution = QualitySettings.shadowResolution;
            previousShadowDistance = QualitySettings.shadowDistance;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 45;
            initialized = true;
        }

        void OnDestroy()
        {
            if (!initialized) return;
            QualitySettings.antiAliasing = previousAntialiasing;
            QualitySettings.shadows = previousShadows;
            QualitySettings.shadowResolution = previousShadowResolution;
            QualitySettings.shadowDistance = previousShadowDistance;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) Paused = !Paused;
            Step(EmberInput.KeyboardAxis(), Time.deltaTime);
        }

        public void Step(Vector2 input, float dt)
        {
            Initialize();
            if (Paused || dt <= 0) return;
            input = Vector2.ClampMagnitude(input, 1);
            var before = keeper.transform.position;
            var after = before + (screenRight * input.x + screenForward * input.y) * (moveSpeed * dt);
            after.x = Mathf.Clamp(after.x, -walkHalfSize.x, walkHalfSize.x);
            after.z = Mathf.Clamp(after.z, -walkHalfSize.y, walkHalfSize.y);
            after.y = 0;
            keeper.transform.position = after;
            keeper.Step((after - before) / dt, dt);
            view.transform.position = Vector3.Lerp(view.transform.position, after + cameraOffset,
                1 - Mathf.Exp(-followResponse * dt));
        }
    }
}
