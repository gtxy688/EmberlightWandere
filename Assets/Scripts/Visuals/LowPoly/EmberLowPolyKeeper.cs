using System;
using UnityEngine;

namespace Emberlight
{
    /// <summary>独立 3D 骨骼小样；位移由调用方驱动，动作按实际速度推进。</summary>
    public sealed class EmberLowPolyKeeper : MonoBehaviour
    {
        [SerializeField] Transform pelvis, head, cape, leftHip, leftKnee, leftFoot, rightHip, rightKnee, rightFoot;
        [SerializeField] Transform leftArm, rightArm, rightForearm, lantern;
        [SerializeField, Min(.01f)] float upperLeg = .36f, lowerLeg = .36f, stride = .60f, stepHeight = .16f;
        [SerializeField, Range(.1f, .9f)] float stance = .6f;
        [SerializeField, Min(0)] float ankleHeight = .10f;
        [SerializeField, Min(.01f)] float response = 12, turnResponse = 10;
        Vector3 restPelvis;
        Quaternion restHead, restCape, restLeftArm, restRightArm, restForearm, restLantern;
        float cycle, blend;
        bool initialized;

        public void Initialize()
        {
            if (initialized) return;
            if (pelvis == null || head == null || cape == null || leftHip == null || leftKnee == null || leftFoot == null ||
                rightHip == null || rightKnee == null || rightFoot == null || leftArm == null || rightArm == null ||
                rightForearm == null || lantern == null)
                throw new InvalidOperationException("Low-poly keeper requires its authored skeleton references.");
            restPelvis = pelvis.localPosition;
            restHead = head.localRotation; restCape = cape.localRotation;
            restLeftArm = leftArm.localRotation; restRightArm = rightArm.localRotation;
            restForearm = rightForearm.localRotation; restLantern = lantern.localRotation;
            cycle = blend = 0;
            initialized = true;
            Pose();
        }

        public void Step(Vector3 velocity, float dt)
        {
            Initialize();
            if (dt <= 0) return;
            velocity.y = 0;
            float speed = velocity.magnitude;
            if (speed > .02f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(velocity),
                    1 - Mathf.Exp(-turnResponse * dt));
            blend = Mathf.Lerp(blend, Mathf.Clamp01(speed / .25f), 1 - Mathf.Exp(-response * dt));
            // 支撑期脚向后走过 stride，抵消同一期间身体向前的位移。
            cycle = Mathf.Repeat(cycle + speed * dt * stance / stride, 1);
            Pose();
        }

        void Pose()
        {
            float angle = cycle * Mathf.PI * 2;
            pelvis.localPosition = restPelvis + Vector3.up * (Mathf.Cos(angle * 2) * .012f * blend);
            head.localRotation = restHead * Quaternion.Euler(Mathf.Sin(angle) * 1.6f * blend, 0, 0);
            cape.localRotation = restCape * Quaternion.Euler(Mathf.Sin(angle - .8f) * 4 * blend,
                Mathf.Sin(angle - .5f) * 3 * blend, 0);
            leftArm.localRotation = restLeftArm * Quaternion.Euler(Mathf.Sin(angle) * 18 * blend, 0, 0);
            rightArm.localRotation = restRightArm * Quaternion.Euler(-Mathf.Sin(angle) * 6 * blend, 0, 0);
            rightForearm.localRotation = restForearm * Quaternion.Euler(Mathf.Sin(angle - .3f) * 4 * blend, 0, 0);
            lantern.localRotation = restLantern * Quaternion.Euler(Mathf.Sin(angle - .7f) * 5 * blend, 0,
                Mathf.Sin(angle - .5f) * 4 * blend);
            Leg(leftHip, leftKnee, leftFoot, cycle);
            Leg(rightHip, rightKnee, rightFoot, cycle + .5f);
        }

        void Leg(Transform hip, Transform knee, Transform foot, float phase)
        {
            var step = EmberLowPolyGait.Foot(phase, stride, stepHeight, stance) * blend;
            var local = new Vector3(hip.localPosition.x, ankleHeight + step.y, step.z);
            var target = transform.TransformPoint(local);
            float scale = transform.lossyScale.y;
            var joint = EmberLowPolyGait.Knee(hip.position, target, upperLeg * scale, lowerLeg * scale, transform.forward);
            var down = transform.rotation * Vector3.down;
            hip.rotation = Quaternion.FromToRotation(down, joint - hip.position) * transform.rotation;
            knee.position = joint;
            knee.rotation = Quaternion.FromToRotation(down, target - joint) * transform.rotation;
            foot.position = target;
            foot.rotation = transform.rotation;
        }
    }
}
