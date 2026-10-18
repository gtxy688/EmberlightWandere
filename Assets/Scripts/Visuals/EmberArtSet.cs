using System;
using UnityEngine;

namespace Emberlight
{
    /// <summary>场景外观资源；角色引用留空时使用原版表现，不持有玩法数据。</summary>
    [CreateAssetMenu(menuName = "Emberlight/Art set")]
    public sealed class EmberArtSet : ScriptableObject
    {
        [SerializeField] GameObject keeperPrefab, shadePrefab, courtyardPrefab;
        public GameObject KeeperPrefab => keeperPrefab;
        public GameObject ShadePrefab => shadePrefab;
        public GameObject CourtyardPrefab => courtyardPrefab;

        public void ValidateReferences()
        {
            if (courtyardPrefab == null)
                throw new InvalidOperationException("Art set requires a courtyard prefab.");
            if (keeperPrefab != null && keeperPrefab.GetComponent<EmberKeeperAnimation>() == null)
                throw new InvalidOperationException("Keeper prefab requires its animation component.");
            if (shadePrefab != null && shadePrefab.GetComponent<EmberEnemySilhouette>() == null)
                throw new InvalidOperationException("Shade prefab requires its silhouette component.");
        }
    }
}
