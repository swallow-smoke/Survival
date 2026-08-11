using System.Collections;
using _001_Scripts.Base;
using _001_Scripts.Data.Message;
using _001_Scripts.Structure;
using _001_Scripts.UI.Component;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _001_Scripts.UI
{
    /// <summary>
    /// Dedicated circular fabrication branch for vehicles. It intentionally does not
    /// reuse the item CraftPanel or WorkbenchPanel data model.
    /// </summary>
    public sealed class SubmarineFabricatorPanel : PanelBase
    {
        public const int CurrentVisualVersion = 1;
        private const string RootName = "SubmarineFabricatorRadialRoot";
        private static readonly Color Cyan = new(.06f, .88f, 1f, 1f);
        private static readonly Color Deep = new(.012f, .12f, .18f, .97f);
        private static readonly Color Orange = new(.95f, .52f, .08f, .98f);

        [SerializeField, HideInInspector] private int visualVersion;
        [SerializeField] private SubmarineFabricator defaultStation;
        [SerializeField] private float openDuration = .28f;

        public int VisualVersion => visualVersion;

        private CanvasGroup _group;
        private RectTransform _tree;
        private Button _hub;
        private Button _category;
        private Button _recipe;
        private Text _status;
        private SubmarineFabricator _station;
        private IPublisher<UIReqMessage> _uiPublisher;
        private Coroutine _animation;
        private bool _wired;

        private new void Awake()
        {
            base.Awake();
            EnsureVisualTree();
            SetHiddenImmediate();
        }

        private void Start() => WireButtons();

        [Inject]
        private void Construct(IPublisher<UIReqMessage> uiPublisher) => _uiPublisher = uiPublisher;

        public void Configure(SubmarineFabricator station) => defaultStation = station;
        public void SetStation(SubmarineFabricator station) => _station = station;

        public override void Open()
        {
            EnsureVisualTree();
            WireButtons();
            if (!_station) _station = defaultStation;
            SetStatus(_station ? "원형 노드에서 잠수함을 선택하세요" : "연결된 잠수함 제작대가 없습니다", _station);
            if (_animation != null) StopCoroutine(_animation);
            isViz = true;
            _group.interactable = true;
            _group.blocksRaycasts = true;
            _animation = StartCoroutine(AnimateOpen());
        }

        public override void Close()
        {
            EnsureVisualTree();
            if (_animation != null) StopCoroutine(_animation);
            SetHiddenImmediate();
        }

        public void RebuildVisualTreeForEditor()
        {
            _group = GetComponent<CanvasGroup>();
            var existing = transform.Find(RootName);
            if (existing)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }
            _wired = false;
            BuildVisualTree();
            SetHiddenImmediate();
        }

        private void Fabricate()
        {
            if (!_station)
            {
                SetStatus("제작대를 다시 사용해 주세요", false);
                return;
            }

            bool success = _station.TryFabricatePrototype(out string message);
            SetStatus(message, success);
            if (success) StartCoroutine(CloseAfterDelay());
        }

        private IEnumerator CloseAfterDelay()
        {
            yield return new WaitForSecondsRealtime(.65f);
            RequestClose();
        }

        private void RequestClose()
        {
            if (_uiPublisher != null)
                _uiPublisher.Publish(new UIReqMessage(UIReqMsgType.Close, "SubmarineFabricator"));
            else Close();
        }

        private IEnumerator AnimateOpen()
        {
            _group.alpha = 0f;
            _tree.localScale = Vector3.one * .68f;
            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / openDuration);
                _group.alpha = t;
                _tree.localScale = Vector3.one * Mathf.Lerp(.68f, 1f, t);
                yield return null;
            }
            _group.alpha = 1f;
            _tree.localScale = Vector3.one;
            _animation = null;
        }

        private void EnsureVisualTree()
        {
            _group = GetComponent<CanvasGroup>();
            if (!_group) _group = gameObject.AddComponent<CanvasGroup>();
            Transform root = transform.Find(RootName);
            if (!root || visualVersion != CurrentVisualVersion) RebuildVisualTreeForEditor();
            else BindVisualTree(root);
        }

        private void BuildVisualTree()
        {
            var root = Rect(RootName, transform);
            Stretch(root);
            _tree = Rect("RadialTree", root);
            Anchor(_tree, new Vector2(.5f, .5f), new Vector2(760, 760), Vector2.zero);

            var connectors = Rect("Connectors", _tree);
            Stretch(connectors);
            CreateConnector(connectors, Vector2.zero, new Vector2(-138f, 112f));
            CreateConnector(connectors, new Vector2(-138f, 112f), new Vector2(260f, 72f));

            _hub = CircleButton("Hub", _tree, 126f, "SUB", "잠수함\n제작대", Deep);
            _hub.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            _category = CircleButton("Category_Submarine", _tree, 104f, "◉", "잠수함", Deep);
            _category.GetComponent<RectTransform>().anchoredPosition = new Vector2(-138f, 112f);

            _recipe = CircleButton("Recipe_PrototypeSubmarine", _tree, 132f, "◆", "임시 잠수함", Orange);
            _recipe.GetComponent<RectTransform>().anchoredPosition = new Vector2(260f, 72f);

            var prototype = Label("PrototypeTag", _recipe.transform, "PROTOTYPE · 재료 무료", 10,
                new Color(1f, .9f, .68f), TextAnchor.MiddleCenter);
            Anchor(prototype.rectTransform, new Vector2(.5f, .5f), new Vector2(190, 24), new Vector2(0, -88));

            _status = Label("Status", _tree, "원형 노드에서 잠수함을 선택하세요", 14,
                new Color(.72f, .93f, .96f), TextAnchor.MiddleCenter);
            Anchor(_status.rectTransform, new Vector2(.5f, .5f), new Vector2(460, 34), new Vector2(0, -170));

            var title = Label("Title", _tree, "SUBMARINE FABRICATION", 18, Color.white, TextAnchor.MiddleCenter);
            Anchor(title.rectTransform, new Vector2(.5f, .5f), new Vector2(420, 34), new Vector2(0, 190));
            visualVersion = CurrentVisualVersion;
            WireButtons();
        }

        private void BindVisualTree(Transform root)
        {
            _tree = root.Find("RadialTree") as RectTransform;
            _hub = _tree.Find("Hub").GetComponent<Button>();
            _category = _tree.Find("Category_Submarine").GetComponent<Button>();
            _recipe = _tree.Find("Recipe_PrototypeSubmarine").GetComponent<Button>();
            _status = _tree.Find("Status").GetComponent<Text>();
        }

        private void WireButtons()
        {
            if (_wired || !_hub || !_recipe) return;
            _hub.onClick.AddListener(RequestClose);
            _category.onClick.AddListener(() => SetStatus("잠수함 설계도", true));
            _recipe.onClick.AddListener(Fabricate);
            _wired = true;
        }

        private void SetHiddenImmediate()
        {
            if (!_group) return;
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
            isViz = false;
        }

        private void SetStatus(string value, bool ready)
        {
            if (!_status) return;
            _status.text = value;
            _status.color = ready ? new Color(.65f, 1f, .94f) : new Color(1f, .52f, .38f);
        }

        private static Button CircleButton(string name, Transform parent, float size, string glyph,
            string label, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(RadialCircleGraphic), typeof(Button));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = Vector2.one * size;
            var graphic = go.GetComponent<RadialCircleGraphic>();
            graphic.color = color;
            var button = go.GetComponent<Button>();
            button.targetGraphic = graphic;
            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, .22f);
            colors.pressedColor = Color.Lerp(color, Color.black, .22f);
            button.colors = colors;

            var glyphText = Label("Glyph", go.transform, glyph, 26, Color.white, TextAnchor.MiddleCenter);
            Anchor(glyphText.rectTransform, new Vector2(.5f, .5f), new Vector2(size, 40), new Vector2(0, 13));
            var labelText = Label("Label", go.transform, label, 13, Color.white, TextAnchor.MiddleCenter);
            Anchor(labelText.rectTransform, new Vector2(.5f, .5f), new Vector2(size + 50, 38), new Vector2(0, -24));
            return button;
        }

        private static void CreateConnector(Transform parent, Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            var line = Rect("Connector", parent);
            line.anchorMin = line.anchorMax = new Vector2(.5f, .5f);
            line.sizeDelta = new Vector2(delta.magnitude, 3f);
            line.anchoredPosition = (from + to) * .5f;
            line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            var image = line.gameObject.AddComponent<Image>();
            image.color = new Color(Cyan.r, Cyan.g, Cyan.b, .42f);
            image.raycastTarget = false;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Text Label(string name, Transform parent, string value, int size, Color color,
            TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
