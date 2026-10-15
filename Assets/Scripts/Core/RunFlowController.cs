namespace Emberlight
{
    /// <summary>
    /// 选卡流程控制器：清波之后"发几张、选几次、什么时候补偿一次、什么时候回到战斗"的推进规则。
    ///
    /// 它**不认识** `EmberGame`，也不碰 UI 与音频：所需状态（本局构筑、当前阶段）与要触发的动作
    /// （重绘面板、开始下一波、回到战斗）全部通过 <see cref="Host"/> 回调，因此选卡次数规则
    /// 可以脱离场景与帧推进单独验证。
    ///
    /// 行为口径（`requirements.md` §3.6 / §4.3）：波次选卡次数由 `LevelConfig.PicksAfterWave` 给出；
    /// 每次选卡各自允许一次"拿到新武器后的补偿选卡"，补偿**不消耗**后续次数且**不连锁**；
    /// 只有出现在当前候选里的卡才能被选中。
    /// </summary>
    public sealed class RunFlowController
    {
        /// <summary>宿主契约：EmberGame 提供状态与各阶段的呈现动作。</summary>
        public interface Host
        {
            /// <summary>本局构筑；尚未开局时为 null。无需世界、相机等场景句柄。</summary>
            RunProgress Progress { get; }

            int PicksAfterWave(int clearedWave);

            /// <summary>当前阶段，由组合根持有。</summary>
            EmberRunPhase Phase { get; }

            /// <summary>生成一组新候选（内部会置 Pending = 1）。</summary>
            void RequestChoices();

            /// <summary>当前候选，用于校验玩家点的那张确实在候选里。</summary>
            EmberOffer[] Offers { get; }

            /// <summary>进入 Upgrade 阶段并把候选画出来。</summary>
            void OnUpgradeOffered(EmberOffer[] offers);

            /// <summary>选卡序列走完，开始下一波并回到 Playing。</summary>
            void OnUpgradeSequenceFinished();
        }

        readonly Host host;
        readonly System.Random random;
        int pendingPicks;
        bool compensationPick;

        /// <summary>当前这一轮还剩几次计划内选卡。</summary>
        public int PendingPicks { get { return pendingPicks; } }

        /// <summary>当前这次选卡是否为"新武器补偿"。</summary>
        public bool IsCompensationPick { get { return compensationPick; } }

        public RunFlowController(Host runHost, System.Random rng)
        {
            host = runHost;
            random = rng;
        }

        /// <summary>开一局/回营地：清空选卡序列。</summary>
        public void Reset()
        {
            pendingPicks = 0;
            compensationPick = false;
        }

        /// <summary>清波入口：算出这一波该选几次，然后开始第一次选卡。</summary>
        public void OnWaveCleared(int clearedWave)
        {
            if (host.Phase != EmberRunPhase.Playing || host.Progress == null)
                return;
            compensationPick = false;
            pendingPicks = host.PicksAfterWave(clearedWave);
            if (pendingPicks == 0)
            {
                // 最终波或未达选卡条件：直接推进，不打扰玩家。
                host.OnUpgradeSequenceFinished();
                return;
            }

            OfferUpgrade();
        }

        /// <summary>发一次候选并让宿主把面板画出来。</summary>
        public void OfferUpgrade()
        {
            host.RequestChoices();
            host.OnUpgradeOffered(host.Offers);
        }

        /// <summary>
        /// 玩家点了一张卡。返回 false 表示这次点击无效（卡不在候选里，或构筑拒绝了这次选择）。
        /// 成功时由本方法决定"继续发卡 / 转补偿 / 收尾"，宿主只负责呈现。
        /// </summary>
        public bool Choose(EmberOffer offer)
        {
            if (host.Phase != EmberRunPhase.Upgrade)
                return false;
            if (!IsCurrentlyOffered(offer))
                return false;
            bool grantedNew;
            if (!host.Progress.Choose(offer, out grantedNew))
                return false;
            if (!compensationPick)
                pendingPicks = pendingPicks > 0 ? pendingPicks - 1 : 0;
            // 拿到新武器 → 立刻补发一次，不推进波次，且不会再次触发补偿。
            if (grantedNew && !compensationPick)
            {
                compensationPick = true;
                OfferUpgrade();
                return true;
            }

            compensationPick = false;
            if (pendingPicks > 0)
                OfferUpgrade();
            else
                host.OnUpgradeSequenceFinished();
            return true;
        }

        /// <summary>刷新：扣一次免费次数并重发候选。返回 false 表示次数已用尽或不在选卡阶段。</summary>
        public bool TryRefresh()
        {
            if (host.Phase != EmberRunPhase.Upgrade)
                return false;
            EmberOffer[] next;
            if (!host.Progress.TryRefresh(random, out next) || next == null)
                return false;
            host.OnUpgradeOffered(next);
            return true;
        }

        /// <summary>同屏唯一键为 (词条Id, 品质, WeaponId)，与候选生成时的口径一致。</summary>
        bool IsCurrentlyOffered(EmberOffer offer)
        {
            var offers = host.Offers;
            if (offers == null)
                return false;
            for (int i = 0; i < offers.Length; i++)
            {
                if (offers[i].Id == offer.Id && offers[i].Rarity == offer.Rarity && offers[i].WeaponId == offer.WeaponId)
                    return true;
            }

            return false;
        }
    }
}
