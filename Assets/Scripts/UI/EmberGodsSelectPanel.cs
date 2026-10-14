using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Emberlight
{
    /// <summary>先选槽位数 N，再单选 1 把起始武器；开局不可跳过，剩余 N-1 个空槽在局内补。</summary>
    public sealed class EmberGodsSelectPanel : MonoBehaviour
    {
        [SerializeField] GameObject slotsRoot, rosterRoot;
        int selectedSlots = 2;
        readonly List<int> filled = new List<int>();
        [SerializeField] Image[] slotCards, rosterCards;
        [SerializeField] TextMeshProUGUI[] rosterDescriptions;
        [SerializeField] Button slotConfirm, rosterConfirm;
        [SerializeField] TextMeshProUGUI rosterConfirmLabel;
        Action<int> confirmSlots;
        Action<int[]> confirmRoster;
        bool slotsBound, rosterBound;
        static readonly int[] Roster = EmberWeaponCatalog.RosterCopy();
        public void Initialize()
        {
            if (slotsRoot == null || rosterRoot == null || slotCards == null || slotCards.Length != 5 ||
                rosterCards == null || rosterCards.Length != Roster.Length || rosterDescriptions == null ||
                slotConfirm == null || rosterConfirm == null || rosterConfirmLabel == null)
                throw new InvalidOperationException("Gods select prefab references are incomplete.");
            if (slotsRoot != null && !slotsBound)
            {
                for (int i = 0; i < slotCards.Length; i++) BindSlot(slotCards[i].GetComponent<Button>(), i + 1);
                slotConfirm.onClick.AddListener(FinishSlots);
                slotsBound = true;
            }
            if (rosterRoot != null && !rosterBound)
            {
                for (int i = 0; i < rosterCards.Length; i++) BindRoster(rosterCards[i].GetComponent<Button>(), Roster[i]);
                rosterConfirm.onClick.AddListener(FinishRoster);
                rosterBound = true;
            }
            Hide();
        }

        public void Hide()
        {
            if (slotsRoot != null) slotsRoot.SetActive(false);
            if (rosterRoot != null) rosterRoot.SetActive(false);
            confirmSlots = null;
            confirmRoster = null;
        }

        /// <summary>UI 入口：N 确定后开始选起始武器。</summary>
        public void ShowRosterSelect(int slots, Action<int[]> onConfirm)
        {
            ShowRoster(slots, onConfirm);
        }

        public void ShowSlots(Action<int> onConfirm)
        {
            Hide();
            selectedSlots = 2;
            confirmSlots = onConfirm;
            RefreshSlotHighlight();
            slotsRoot.SetActive(true);
        }

        public void ShowRoster(int slots, Action<int[]> onConfirm)
        {
            Hide();
            selectedSlots = slots < 1 ? 1 : (slots > 5 ? 5 : slots);
            filled.Clear();
            confirmRoster = onConfirm;
            RefreshRoster();
            rosterRoot.SetActive(true);
        }

        void BindSlot(Button button, int count)
        {
            button.onClick.AddListener(() => { EmberAudio.Ensure().PlayUiClick(); selectedSlots = count; RefreshSlotHighlight(); });
        }

        void FinishSlots()
        {
            var callback = confirmSlots;
            Hide();
            callback?.Invoke(selectedSlots);
        }

        void BindRoster(Button button, int id)
        {
            button.onClick.AddListener(() => { EmberAudio.Ensure().PlayUiClick(); ToggleRoster(id); });
        }

        void FinishRoster()
        {
            if (!rosterConfirm.interactable || filled.Count == 0) return;
            rosterConfirm.interactable = false;
            var chosen = filled.ToArray();
            var callback = confirmRoster;
            EmberAudio.Ensure().PlayUiConfirm();
            Hide();
            callback?.Invoke(chosen);
        }

        void ToggleRoster(int id)
        {
            // 选择保留到显式确认为止。
            filled.Clear();
            filled.Add(id);
            RefreshRoster();
        }

        void RefreshRoster()
        {
            for (int i = 0; i < rosterCards.Length; i++)
            {
                bool selected = filled.Contains(Roster[i]);
                rosterCards[i].color = selected ? new Color(1f, .91f, .65f) : Color.white;
                rosterDescriptions[i].text = selected ? "已选中" : EmberWeaponCatalog.Find(Roster[i]).Description;
            }
            rosterConfirm.interactable = filled.Count > 0;
            rosterConfirmLabel.text = filled.Count > 0 ? "确认 · " + EmberWeaponCatalog.Find(filled[0]).Name : "请先选择武器";
        }

        void RefreshSlotHighlight()
        {
            if (slotCards == null)
                return;
            for (int i = 0; i < slotCards.Length; i++)
            {
                slotCards[i].sprite = EmberUiArt.Get(i + 1 == selectedSlots ? EmberUiArt.Piece.Primary : EmberUiArt.Piece.Secondary);
                slotCards[i].color = Color.white;
            }
        }

    }
}
