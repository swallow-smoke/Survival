using System;
using System.Collections.Generic;
using AstraNope.UI.Base;
using AstraNope.Data.Messages;
using AstraNope.WorldObjects.Items;
using AstraNope.WorldObjects.Structures;
using AstraNope.WorldObjects.Vehicles;
using AstraNope.UI.Components;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace AstraNope.UI.Panels
{
    public sealed class SubmarineFabricatorPanel : PanelBase
    {
        public const int CurrentVisualVersion = 9;
        private const string LegacyRootName = "SubmarineFabricatorRadialRoot";

        [SerializeField, HideInInspector] private int visualVersion;
        [SerializeField] private SubmarineFabricator defaultStation;
        [SerializeField] private string categoryPath = "Vehicles/Submarine";

        private CanvasGroup _group;
        private SimpleRadialMenuView _radial;
        private SubmarineFabricator _station;
        private IPublisher<UIReqMessage> _uiPublisher;
        private string _currentPath = string.Empty;

        public int VisualVersion => visualVersion;

        private new void Awake()
        {
            base.Awake();
            EnsureView();
            SetHidden();
        }

        [Inject]
        private void Construct(IPublisher<UIReqMessage> uiPublisher) => _uiPublisher = uiPublisher;

        public void Configure(SubmarineFabricator station) => defaultStation = station;
        public void SetStation(SubmarineFabricator station) => _station = station;

        public override void Open()
        {
            EnsureView();
            if (!_station) _station = defaultStation;
            if (!_station) _station = FindAnyObjectByType<SubmarineFabricator>(FindObjectsInactive.Include);
            _currentPath = string.Empty;
            BuildCurrentLevel();
            _group.alpha = 1f;
            _group.interactable = true;
            _group.blocksRaycasts = true;
            isViz = true;
            _radial.PlayOpenAnimation();
        }

        public override void Close()
        {
            isViz = false;
            _group.interactable = false;
            _group.blocksRaycasts = false;
            _radial.PlayCloseAnimation(SetHidden);
        }

        public void RebuildVisualTreeForEditor()
        {
            EnsureView();
            _radial.Rebuild("◉");
            visualVersion = CurrentVisualVersion;
            SetHidden();
        }

        private void BuildCurrentLevel()
        {
            var entries = new List<SimpleRadialEntry>();
            string[] path = SplitPath(categoryPath);
            string[] current = SplitPath(_currentPath);

            if (current.Length > 0)
                entries.Add(new SimpleRadialEntry("Back", "←", "Back", GoBack));

            if (current.Length < path.Length && StartsWith(path, current))
            {
                string child = path[current.Length];
                string target = string.IsNullOrEmpty(_currentPath) ? child : $"{_currentPath}/{child}";
                entries.Add(new SimpleRadialEntry($"Category_{child}", "○", child,
                    () => EnterCategory(target)));
            }
            else
            {
                entries.Add(new SimpleRadialEntry("Recipe_PrototypeSubmarine", "▲", "Prototype", Fabricate,
                    _station));
            }

            _radial.SetEntries(entries);
        }

        private void EnterCategory(string path)
        {
            _currentPath = path;
            BuildCurrentLevel();
            _radial.PlayNodeAnimation();
        }

        private void GoBack()
        {
            int separator = _currentPath.LastIndexOf('/');
            _currentPath = separator < 0 ? string.Empty : _currentPath[..separator];
            BuildCurrentLevel();
            _radial.PlayNodeAnimation();
        }

        private void Fabricate()
        {
            if (!_station)
            {
                Debug.LogWarning("[SubmarineFabricator] Station is not assigned.", this);
                return;
            }

            bool success = _station.TryFabricatePrototype(out string message);
            if (success)
            {
                Debug.Log(message, this);
                RequestClose();
            }
            else Debug.LogWarning(message, this);
        }

        private void RequestClose()
        {
            if (_uiPublisher != null)
                _uiPublisher.Publish(new UIReqMessage(UIReqMsgType.Close, "SubmarineFabricator"));
            else Close();
        }

        private void EnsureView()
        {
            _group = GetComponent<CanvasGroup>();
            if (!_group) _group = gameObject.AddComponent<CanvasGroup>();

            var legacy = transform.Find(LegacyRootName);
            if (legacy)
            {
                legacy.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(legacy.gameObject);
                else DestroyImmediate(legacy.gameObject);
            }

            _radial = GetComponent<SimpleRadialMenuView>();
            if (!_radial) _radial = gameObject.AddComponent<SimpleRadialMenuView>();
            _radial.Ensure("◉");
            _radial.SetOutsideClick(RequestClose);
            visualVersion = CurrentVisualVersion;
        }

        private void SetHidden()
        {
            if (!_group) _group = GetComponent<CanvasGroup>();
            if (_group)
            {
                _group.alpha = 0f;
                _group.interactable = false;
                _group.blocksRaycasts = false;
            }
            isViz = false;
        }

        private static string[] SplitPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return Array.Empty<string>();
            string[] raw = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                string value = raw[i].Trim();
                if (value.Length > 0) result.Add(value);
            }
            return result.ToArray();
        }

        private static bool StartsWith(IReadOnlyList<string> path, IReadOnlyList<string> prefix)
        {
            if (path.Count < prefix.Count) return false;
            for (int i = 0; i < prefix.Count; i++)
                if (!string.Equals(path[i], prefix[i], StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }
    }
}
