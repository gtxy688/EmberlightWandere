using System;
using UnityEngine;

namespace Emberlight
{
    /// <summary>分件关节视图。动画曲线只驱动外观，步频跟随实际位移。</summary>
    public sealed class EmberKeeperRig : MonoBehaviour
    {
        [SerializeField] AnimationClip walk;
        [SerializeField] Transform[] joints;
        [SerializeField, Min(.01f)] float cyclesPerUnit = .65f;
        [SerializeField, Min(.01f)] float fullStrideSpeed = .35f;
        [SerializeField, Min(.01f)] float response = 12;
        [SerializeField, Min(0)] float turnThreshold = .08f;
        Vector3[] restPositions;
        Quaternion[] restRotations;
        Vector3 restScale;
        float cycle, weight;
        bool facingLeft;

        public void Initialize()
        {
            if (walk == null || joints == null || joints.Length == 0)
                throw new InvalidOperationException("Keeper rig requires its walk clip and joint references.");
            restPositions = new Vector3[joints.Length];
            restRotations = new Quaternion[joints.Length];
            for (int i = 0; i < joints.Length; i++)
            {
                if (joints[i] == null) throw new InvalidOperationException("Keeper rig has a missing joint.");
                restPositions[i] = joints[i].localPosition;
                restRotations[i] = joints[i].localRotation;
            }
            restScale = transform.localScale;
            cycle = weight = 0;
            facingLeft = false;
        }

        // 由阶段门控后的表现控制器调用；停止时收回站姿，暂停时不调用，保持当前动作。
        public void Step(Vector2 velocity, float dt)
        {
            if (dt <= 0) return;
            float speed = velocity.magnitude;
            weight = Mathf.Lerp(weight, Mathf.Clamp01(speed / fullStrideSpeed), 1 - Mathf.Exp(-response * dt));
            cycle = Mathf.Repeat(cycle + speed * dt * cyclesPerUnit, 1);
            walk.SampleAnimation(gameObject, cycle * walk.length);
            for (int i = 0; i < joints.Length; i++)
            {
                var joint = joints[i];
                joint.localPosition = Vector3.Lerp(restPositions[i], joint.localPosition, weight);
                joint.localRotation = Quaternion.Slerp(restRotations[i], joint.localRotation, weight);
            }
            if (Mathf.Abs(velocity.x) > turnThreshold) facingLeft = velocity.x < 0;
            transform.localScale = Vector3.Scale(restScale, new Vector3(facingLeft ? -1 : 1, 1, 1));
        }
    }
}
