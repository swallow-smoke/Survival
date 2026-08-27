using System;
using System.Collections.Generic;
using AstraNope.Contracts;
using AstraNope.Core.World.Entities.Interfaces;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using VContainer.Unity;
using WorldBuilder.Entities.Creatures;

namespace AstraNope.UI.World
{
    /// <summary>
    /// Projects nearby ECS creatures into a pooled screen-space UI layer.
    /// Creature data stays in ECS; no label GameObjects are attached to entity prefabs.
    /// </summary>
    public sealed class CreatureNameplatePresenter : IStartable, ITickable, IDisposable
    {
        private const float VisibleDistance = 18f;
        private const float FullOpacityDistance = 10f;
        private const float RefreshInterval = 0.08f;
        private const int MaximumLabels = 24;

        private readonly IWorldCreatureGateway creatures;
        private readonly IPlayerTransformProvider player;
        private readonly List<CreatureRecord> records = new List<CreatureRecord>(32);
        private readonly Dictionary<Entity, Nameplate> active = new Dictionary<Entity, Nameplate>();
        private readonly HashSet<Entity> seen = new HashSet<Entity>();
        private readonly List<Entity> removals = new List<Entity>(32);
        private readonly Stack<Nameplate> pool = new Stack<Nameplate>(MaximumLabels);

        private GameObject canvasObject;
        private RectTransform canvasRect;
        private Camera viewCamera;
        private float nextRefreshTime;

        public CreatureNameplatePresenter(IWorldCreatureGateway creatures, IPlayerTransformProvider player)
        {
            this.creatures = creatures;
            this.player = player;
        }

        public void Start()
        {
            CreateCanvas();
            Refresh();
        }

        public void Tick()
        {
            if (!canvasObject) CreateCanvas();
            if (!viewCamera) viewCamera = Camera.main;
            if (Time.unscaledTime < nextRefreshTime) return;

            nextRefreshTime = Time.unscaledTime + RefreshInterval;
            Refresh();
        }

        public void Dispose()
        {
            active.Clear();
            pool.Clear();
            if (canvasObject) UnityEngine.Object.Destroy(canvasObject);
        }

        private void Refresh()
        {
            Transform playerTransform = player?.PlayerTrs;
            if (!creatures.IsReady || !playerTransform || !viewCamera || !canvasRect)
            {
                ReleaseAll();
                return;
            }

            creatures.Collect(records, CreatureFilter.Active);
            seen.Clear();
            Vector3 playerPosition = playerTransform.position;
            float maximumDistanceSquared = VisibleDistance * VisibleDistance;

            for (int i = 0; i < records.Count && seen.Count < MaximumLabels; i++)
            {
                CreatureRecord record = records[i];
                Vector3 worldPosition = record.Position;
                float distanceSquared = (worldPosition - playerPosition).sqrMagnitude;
                if (distanceSquared > maximumDistanceSquared) continue;

                Vector3 anchor = worldPosition + Vector3.up * HeightOffset(record.SizeClass);
                Vector3 screen = viewCamera.WorldToScreenPoint(anchor);
                if (screen.z <= 0f || screen.x < -80f || screen.x > Screen.width + 80f ||
                    screen.y < -40f || screen.y > Screen.height + 40f) continue;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null,
                        out Vector2 canvasPosition)) continue;

                if (!active.TryGetValue(record.Entity, out Nameplate nameplate))
                {
                    nameplate = Acquire();
                    active.Add(record.Entity, nameplate);
                }

                seen.Add(record.Entity);
                nameplate.Root.anchoredPosition = canvasPosition;
                nameplate.Group.alpha = Opacity(Mathf.Sqrt(distanceSquared));
                nameplate.SetContent(record.DisplayName, record.Grade);
            }

            removals.Clear();
            foreach (KeyValuePair<Entity, Nameplate> pair in active)
                if (!seen.Contains(pair.Key)) removals.Add(pair.Key);
            for (int i = 0; i < removals.Count; i++) Release(removals[i]);
        }

        private void CreateCanvas()
        {
            canvasObject = new GameObject("CreatureNameplateCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler));
            UnityEngine.Object.DontDestroyOnLoad(canvasObject);
            canvasRect = canvasObject.GetComponent<RectTransform>();

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            viewCamera = Camera.main;
        }

        private Nameplate Acquire()
        {
            if (pool.Count > 0)
            {
                Nameplate reused = pool.Pop();
                reused.Root.gameObject.SetActive(true);
                return reused;
            }

            GameObject rootObject = new GameObject("CreatureNameplate", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(Outline));
            RectTransform root = rootObject.GetComponent<RectTransform>();
            root.SetParent(canvasRect, false);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(178f, 45f);

            Image background = rootObject.GetComponent<Image>();
            background.color = new Color(0.025f, 0.055f, 0.08f, 0.82f);
            background.raycastTarget = false;

            Outline outline = rootObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.88f, 1f, 0.48f);
            outline.effectDistance = new Vector2(1f, -1f);

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(root, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 3f);
            textRect.offsetMax = new Vector2(-8f, -3f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 15f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.color = Color.white;

            return new Nameplate(root, rootObject.GetComponent<CanvasGroup>(), text);
        }

        private void Release(Entity entity)
        {
            if (!active.TryGetValue(entity, out Nameplate nameplate)) return;
            active.Remove(entity);
            nameplate.Root.gameObject.SetActive(false);
            pool.Push(nameplate);
        }

        private void ReleaseAll()
        {
            removals.Clear();
            foreach (Entity entity in active.Keys) removals.Add(entity);
            for (int i = 0; i < removals.Count; i++) Release(removals[i]);
        }

        private static float HeightOffset(CreatureSizeClass sizeClass)
        {
            switch (sizeClass)
            {
                case CreatureSizeClass.Large: return 2.3f;
                case CreatureSizeClass.Medium: return 1.45f;
                default: return 0.9f;
            }
        }

        private static float Opacity(float distance)
        {
            if (distance <= FullOpacityDistance) return 1f;
            return 1f - Mathf.InverseLerp(FullOpacityDistance, VisibleDistance, distance);
        }

        private sealed class Nameplate
        {
            public readonly RectTransform Root;
            public readonly CanvasGroup Group;
            private readonly TMP_Text text;
            private string displayName;
            private CreatureGrade grade = (CreatureGrade)byte.MaxValue;

            public Nameplate(RectTransform root, CanvasGroup group, TMP_Text text)
            {
                Root = root;
                Group = group;
                this.text = text;
            }

            public void SetContent(string value, CreatureGrade valueGrade)
            {
                value = string.IsNullOrWhiteSpace(value) ? "Creature" : value;
                if (displayName == value && grade == valueGrade) return;
                displayName = value;
                grade = valueGrade;
                int level = CreatureGradeRules.GradeIndex(valueGrade) + 1;
                string color = valueGrade == CreatureGrade.Legendary ? "#FFD36A" :
                    valueGrade == CreatureGrade.Rare ? "#6FD9FF" : "#C9D5DF";
                text.text = $"<b>{value}</b>\n<size=11><color={color}>Lv. {level} · {GradeLabel(valueGrade)}</color></size>";
            }

            private static string GradeLabel(CreatureGrade value)
            {
                switch (value)
                {
                    case CreatureGrade.Legendary: return "전설";
                    case CreatureGrade.Rare: return "희귀";
                    default: return "일반";
                }
            }
        }
    }
}
