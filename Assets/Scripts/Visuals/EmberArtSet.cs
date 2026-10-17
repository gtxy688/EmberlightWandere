using System;
using UnityEngine;

namespace Emberlight
{
    /// <summary>场景使用的外观资源；只保存 Prefab 引用，不持有玩法数据。</summary>
    [CreateAssetMenu(menuName = "Emberlight/Art set")]
    public sealed class EmberArtSet : ScriptableObject
    {
        [SerializeField] GameObject keeperPrefab, shadePrefab, courtyardPrefab;
        public GameObject KeeperPrefab => keeperPrefab;
        public GameObject ShadePrefab => shadePrefab;
        public GameObject CourtyardPrefab => courtyardPrefab;

        public void ValidateReferences()
        {
            if (keeperPrefab == null || shadePrefab == null || courtyardPrefab == null ||
                keeperPrefab.GetComponent<EmberKeeperAnimation>() == null ||
                shadePrefab.GetComponent<EmberEnemySilhouette>() == null)
                throw new InvalidOperationException("Art set requires configured keeper, shade and courtyard prefabs.");
        }
    }
}
