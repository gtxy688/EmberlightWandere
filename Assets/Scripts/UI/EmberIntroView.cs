using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Emberlight
{
    /// <summary>加载页 Prefab 的视图引用；布局和静态图片由资产维护。</summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class EmberIntroView : MonoBehaviour
    {
        public TextMeshProUGUI ChineseTitle, EnglishTitle, Prompt, Percent, Tagline;
        public GameObject ProgressRoot;
        public Image ProgressFill;

        public void ValidateReferences()
        {
            if (ChineseTitle == null || EnglishTitle == null || Prompt == null || Percent == null ||
                Tagline == null || ProgressRoot == null || ProgressFill == null)
                throw new InvalidOperationException("IntroRoot references are incomplete. Run Emberlight/UI/Connect UIRoot Runtime in the Editor.");
        }
    }
}
