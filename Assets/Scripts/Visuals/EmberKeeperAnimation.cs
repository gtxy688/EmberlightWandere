using System;
using UnityEngine;

namespace Emberlight
{
    /// <summary>驱动 Prefab 的外观子节点；根节点的位置与命中规则由移动模块负责。</summary>
    public sealed class EmberKeeperAnimation : MonoBehaviour
    {
        [SerializeField] SpriteRenderer body, glow;
        Func<EmberRunPhase> phase;
        Vector3 previous, restPosition, restScale;
        Quaternion restRotation;
        Color glowColor;
        Vector2 drift;
        float phaseClock, moving;

        public void Initialize(Func<EmberRunPhase> phaseProvider)
        {
            if (body == null || glow == null)
                throw new InvalidOperationException("Keeper prefab requires its body and ground glow references.");
            phase = phaseProvider;
            previous = transform.position;
            restPosition = body.transform.localPosition;
            restScale = body.transform.localScale;
            restRotation = body.transform.localRotation;
            glowColor = glow.color;
        }

        void LateUpdate()
        {
            Vector3 delta = transform.position - previous;
            previous = transform.position;
            float dt = Time.deltaTime;
            if (dt <= 0 || phase == null || phase() != EmberRunPhase.Playing) return;
            Vector2 velocity = delta.sqrMagnitude < 1 ? (Vector2)delta / dt : Vector2.zero;
            float blend = 1 - Mathf.Exp(-dt * 9);
            drift = Vector2.Lerp(drift, Vector2.ClampMagnitude(velocity / EmberKeeperMotion.PlayerSpeed, 1), blend);
            moving = Mathf.Lerp(moving, Mathf.Clamp01(velocity.magnitude / EmberKeeperMotion.PlayerSpeed), blend);
            phaseClock += dt * (2.4f + moving * 2);
            float bob = Mathf.Sin(phaseClock);
            body.transform.localPosition = restPosition + Vector3.up * (.025f * bob);
            body.transform.localRotation = restRotation * Quaternion.Euler(0, 0, -drift.x * 5);
            body.transform.localScale = Vector3.Scale(restScale, new Vector3(1 + .015f * bob, 1 - .01f * bob, 1));
            if (Mathf.Abs(drift.x) > .08f) body.flipX = drift.x < 0;
            var color = glowColor;
            color.a *= 1 + .1f * Mathf.Sin(phaseClock * 2.3f);
            glow.color = color;
        }
    }
}
