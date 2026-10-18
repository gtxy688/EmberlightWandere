using UnityEngine;

namespace Emberlight
{
    /// <summary>3D 小样的落脚轨迹与两段腿求解，不读取场景或输入。</summary>
    public static class EmberLowPolyGait
    {
        public static Vector3 Foot(float phase, float stride, float lift, float stance)
        {
            float t = Mathf.Repeat(phase, 1);
            if (t < stance)
                return new Vector3(0, 0, Mathf.Lerp(stride * .5f, -stride * .5f, t / stance));
            float swing = (t - stance) / (1 - stance);
            float arc = Mathf.Sin(swing * Mathf.PI);
            float tangent = -stride * (1 - stance) / stance;
            float forward = Mathf.Lerp(-stride * .5f, stride * .5f, Mathf.SmoothStep(0, 1, swing)) +
                tangent * swing * (1 - swing) * (1 - 2 * swing);
            return new Vector3(0, arc * arc * lift, forward);
        }

        public static Vector3 Knee(Vector3 hip, Vector3 foot, float upper, float lower, Vector3 forward)
        {
            var offset = foot - hip;
            var axis = offset.sqrMagnitude > .000001f ? offset.normalized : Vector3.down;
            float distance = Mathf.Clamp(offset.magnitude, .0001f, upper + lower - .0001f);
            float along = (upper * upper - lower * lower + distance * distance) / (2 * distance);
            var bend = Vector3.ProjectOnPlane(forward, axis);
            if (bend.sqrMagnitude < .000001f) bend = Vector3.ProjectOnPlane(Vector3.right, axis);
            float height = Mathf.Sqrt(Mathf.Max(0, upper * upper - along * along));
            return hip + axis * along + bend.normalized * height;
        }
    }
}
