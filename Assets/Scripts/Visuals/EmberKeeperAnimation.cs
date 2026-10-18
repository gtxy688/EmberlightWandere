using System;
using UnityEngine;

namespace Emberlight
{
    /// <summary>驱动原版或指定 Prefab 的外观；根节点的位置与命中规则由移动模块负责。</summary>
    public sealed class EmberKeeperAnimation : MonoBehaviour
    {
        [SerializeField] SpriteRenderer body, glow;
        [SerializeField] EmberKeeperRig rig;
        [SerializeField] Animator spriteAnimator;
        [SerializeField] bool spriteFacesLeft = true;
        [SerializeField, Min(0)] float spriteMoveThreshold = .025f, spriteFacingThreshold = .08f;
        static readonly int IsMoving = Animator.StringToHash("IsMoving");
        Transform originalBody, originalCloak, originalFace, originalLantern;
        Func<EmberRunPhase> phase;
        Vector3 previous, restPosition, restScale;
        Quaternion restRotation;
        Color glowColor;
        Vector2 drift;
        float phaseClock, moving;
        bool spriteMoving;

        public void Initialize(Func<EmberRunPhase> phaseProvider)
        {
            // 无替换资源时沿用主分支的兜帽、眼睛与提灯，不依赖手绘角色素材。
            if (body == null && glow == null && rig == null && originalBody == null) BuildOriginal();
            phase = phaseProvider;
            previous = transform.position;
            if (originalBody != null) return;
            if (body == null || glow == null)
                throw new InvalidOperationException("Keeper prefab requires its body and ground glow references.");
            restPosition = body.transform.localPosition;
            restScale = body.transform.localScale;
            restRotation = body.transform.localRotation;
            glowColor = glow.color;
            if (rig != null) rig.Initialize();
            if (spriteAnimator != null)
            {
                if (spriteAnimator.runtimeAnimatorController == null || spriteAnimator.GetComponent<SpriteRenderer>() != body)
                    throw new InvalidOperationException("Keeper sprite Animator must target its body renderer and have a controller.");
                // 由现有阶段门控统一推进，避免暂停、选卡时 Animator 仍自动播放。
                spriteAnimator.applyRootMotion = false;
                spriteAnimator.speed = 0;
                spriteAnimator.Rebind();
                spriteAnimator.Play("Idle", 0, 0);
                spriteAnimator.Update(0);
                spriteMoving = false;
                body.flipX = false;
            }
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
            if (originalBody != null)
            {
                AnimateOriginal();
                return;
            }
            if (spriteAnimator != null) AnimateSprites(velocity, dt);
            else if (rig != null) rig.Step(velocity, dt);
            else
            {
                float bob = Mathf.Sin(phaseClock);
                body.transform.localPosition = restPosition + Vector3.up * (.025f * bob);
                body.transform.localRotation = restRotation * Quaternion.Euler(0, 0, -drift.x * 5);
                body.transform.localScale = Vector3.Scale(restScale, new Vector3(1 + .015f * bob, 1 - .01f * bob, 1));
                if (Mathf.Abs(drift.x) > .08f) body.flipX = drift.x < 0;
            }
            var color = glowColor;
            color.a *= 1 + .1f * Mathf.Sin(phaseClock * 2.3f);
            glow.color = color;
        }

        void AnimateSprites(Vector2 velocity, float dt)
        {
            bool isMoving = velocity.sqrMagnitude > spriteMoveThreshold * spriteMoveThreshold;
            if (isMoving != spriteMoving)
            {
                spriteAnimator.SetBool(IsMoving, isMoving);
                spriteMoving = isMoving;
            }
            if (Mathf.Abs(velocity.x) > spriteFacingThreshold)
                body.flipX = spriteFacesLeft ? velocity.x > 0 : velocity.x < 0;
            // 帧内已画出脚步和摆灯，此路径不再给整张角色叠加平移、缩放或倾斜。
            spriteAnimator.speed = 1;
            spriteAnimator.Update(dt);
            spriteAnimator.speed = 0;
        }

        void BuildOriginal()
        {
            originalBody = new GameObject("Floating keeper").transform;
            originalBody.SetParent(transform, false);
            EmberVisuals.Shape("Shadow", transform, new Vector2(0, -.50f), new Vector2(.90f, .28f), new Color(0, 0, 0, .30f), 1);
            var blue = new Color(.20f, .35f, .45f);
            var cloak = EmberVisuals.Shape("Cloak", originalBody, new Vector2(0, -.08f), new Vector2(.66f, .78f), blue, 3);
            cloak.sprite = EmberCharacterArt.Cloak;
            originalCloak = cloak.transform;
            EmberVisuals.Shape("Hood rim", originalBody, new Vector2(0, .23f), new Vector2(.69f, .62f), new Color(.26f, .43f, .52f), 4).sprite = EmberCharacterArt.Hood;
            EmberVisuals.Shape("Hood", originalBody, new Vector2(0, .25f), new Vector2(.61f, .55f), blue, 5).sprite = EmberCharacterArt.Hood;
            EmberVisuals.Shape("Collar", originalBody, new Vector2(0, .025f), new Vector2(.54f, .13f), new Color(.16f, .28f, .36f), 6).sprite = EmberCharacterArt.Hood;
            EmberVisuals.Shape("Brass clasp", originalBody, new Vector2(.025f, -.005f), new Vector2(.07f, .085f), new Color(.76f, .55f, .26f), 7);
            originalFace = EmberVisuals.Shape("Hidden face", originalBody, new Vector2(0, .21f), new Vector2(.44f, .32f), new Color(.035f, .06f, .085f), 6).transform;
            for (int i = -1; i <= 1; i += 2)
            {
                var eye = EmberVisuals.Shape("Warm eye", originalFace, new Vector2(i * .22f, .02f), new Vector2(.13f, .25f), new Color(1f, .79f, .35f), 7);
                EmberVisuals.Shape("Eye glint", eye.transform, new Vector2(-.12f, .18f), new Vector2(.24f, .24f), new Color(1, .96f, .76f), 8);
            }
            originalLantern = new GameObject("Swinging lantern").transform;
            originalLantern.SetParent(originalBody, false);
            originalLantern.localPosition = new Vector2(.37f, .10f);
            var loop = EmberVisuals.Shape("Handle", originalLantern, new Vector2(0, -.04f), new Vector2(.15f, .15f), new Color(.72f, .51f, .22f), 7);
            loop.sprite = EmberArt.Ring;
            var housing = EmberVisuals.Shape("Lantern case", originalLantern, new Vector2(0, -.18f), new Vector2(.27f, .34f), new Color(.76f, .51f, .23f), 7);
            housing.sprite = EmberCharacterArt.Lantern;
            var glass = EmberVisuals.Shape("Warm glass", originalLantern, new Vector2(0, -.18f), new Vector2(.15f, .22f), new Color(1f, .68f, .22f), 8);
            glass.sprite = EmberArt.Panel;
            EmberArt.Fire(originalLantern, new Vector2(0, -.18f), .11f, 9);
            for (int i = -1; i <= 1; i += 2)
                EmberVisuals.Shape("Lantern rib", originalLantern, new Vector2(i * .055f, -.18f), new Vector2(.014f, .23f), new Color(.43f, .25f, .10f), 10).sprite = EmberArt.Panel;
            glow = EmberVisuals.Shape("Lantern light", originalLantern, new Vector2(0, -.18f), Vector2.one * .65f, new Color(1f, .5f, .1f, .12f), 2);
            glow.sprite = EmberArt.Glow;
        }

        void AnimateOriginal()
        {
            originalBody.localPosition = new Vector3(0, .025f * Mathf.Sin(phaseClock), 0);
            originalBody.localRotation = Quaternion.Euler(0, 0, -drift.x * 5);
            originalCloak.localScale = new Vector3(.66f * (1 + .025f * Mathf.Sin(phaseClock)), .78f * (1 - .018f * Mathf.Sin(phaseClock)), 1);
            originalFace.localPosition = new Vector3(drift.x * .035f, .21f + drift.y * .016f, 0);
            originalLantern.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(phaseClock * .85f) * (3 + moving * 9) + drift.x * 9);
            glow.color = new Color(1f, .5f, .1f, .12f + .015f * Mathf.Sin(phaseClock * 2.3f));
        }
    }
}
