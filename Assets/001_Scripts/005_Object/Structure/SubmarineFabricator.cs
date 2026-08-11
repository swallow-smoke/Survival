using _001_Scripts.Entities;
using _001_Scripts.UI;
using UnityEngine;

namespace _001_Scripts.Structure
{
    public sealed class SubmarineFabricator : Fabricator
    {
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float fabricateCooldown = 1f;

        private GameObject _lastPrototype;
        private float _lastFabricatedAt = -10f;

        protected override void Awake()
        {
            base.Awake();
            Configure("SubmarineFabricator", "잠수함 제작대 사용");
        }

        public void Configure(Transform point) => spawnPoint = point;

        protected override void BeforePanelOpen()
        {
            var panel = FindAnyObjectByType<SubmarineFabricatorPanel>(FindObjectsInactive.Include);
            if (panel) panel.SetStation(this);
        }

        public bool TryFabricatePrototype(out string message)
        {
            if (Time.unscaledTime - _lastFabricatedAt < fabricateCooldown)
            {
                message = "제작 장치 냉각 중";
                return false;
            }

            if (_lastPrototype)
            {
                message = "임시 잠수함이 이미 배치되어 있습니다";
                return false;
            }

            Transform point = spawnPoint ? spawnPoint : transform;
            _lastPrototype = PrototypeSubmarineBuilder.Create(point.position, point.rotation);
            _lastFabricatedAt = Time.unscaledTime;
            message = "임시 잠수함 제작 완료";
            return true;
        }
    }

    internal static class PrototypeSubmarineBuilder
    {
        private static readonly Color HullColor = new(.88f, .52f, .08f);
        private static readonly Color DarkColor = new(.055f, .09f, .12f);
        private static readonly Color GlassColor = new(.04f, .62f, .82f);

        public static GameObject Create(Vector3 position, Quaternion rotation)
        {
            var root = new GameObject("PrototypeSubmarine");
            root.transform.SetPositionAndRotation(position, rotation);

            var entity = root.AddComponent<Entity>();
            entity.Configure("prototype_submarine", "Prototype Submarine", EntityKind.Submarine);
            root.AddComponent<Health>();
            root.AddComponent<_001_Scripts.Entities.Vehicle>();
            root.AddComponent<Submarine>();
            root.AddComponent<PrototypeSubmarine>();

            RemoveCollider(Primitive("Hull", PrimitiveType.Capsule, root.transform,
                new Vector3(0, 1.1f, 0), new Vector3(1.35f, 2.65f, 1.35f),
                Quaternion.Euler(90f, 0f, 0f), HullColor));
            RemoveCollider(Primitive("ViewDome", PrimitiveType.Sphere, root.transform,
                new Vector3(0, 1.1f, 2.45f), new Vector3(1.08f, 1.08f, .72f),
                Quaternion.identity, GlassColor));
            RemoveCollider(Primitive("ConningTower", PrimitiveType.Cube, root.transform,
                new Vector3(0, 2.05f, -.2f), new Vector3(.8f, .48f, 1.15f),
                Quaternion.identity, DarkColor));

            CreateFin("FinLeft", root.transform, new Vector3(-1.28f, .95f, -.15f), new Vector3(1.25f, .12f, 1.25f));
            CreateFin("FinRight", root.transform, new Vector3(1.28f, .95f, -.15f), new Vector3(1.25f, .12f, 1.25f));
            CreateFin("TailFin", root.transform, new Vector3(0, 1.72f, -2.45f), new Vector3(.12f, 1.3f, 1.1f));

            var prop = new GameObject("Propeller");
            prop.transform.SetParent(root.transform, false);
            prop.transform.localPosition = new Vector3(0, 1.1f, -2.85f);
            Primitive("BladeA", PrimitiveType.Cube, prop.transform, Vector3.zero,
                new Vector3(2.2f, .12f, .12f), Quaternion.identity, DarkColor);
            Primitive("BladeB", PrimitiveType.Cube, prop.transform, Vector3.zero,
                new Vector3(.12f, 2.2f, .12f), Quaternion.identity, DarkColor);

            var collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 2;
            collider.center = new Vector3(0, 1.1f, 0);
            collider.radius = 1.2f;
            collider.height = 5.6f;
            var bodyPhysics = root.AddComponent<Rigidbody>();
            bodyPhysics.isKinematic = true;
            root.AddComponent<PrototypeSubmarineVisual>();
            return root;
        }

        private static void CreateFin(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            var fin = Primitive(name, PrimitiveType.Cube, parent, position, scale, Quaternion.identity, HullColor);
            RemoveCollider(fin);
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position,
            Vector3 scale, Quaternion rotation, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material.color = color;
            return go;
        }

        private static void RemoveCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider) UnityEngine.Object.Destroy(collider);
        }
    }

    internal sealed class PrototypeSubmarineVisual : MonoBehaviour
    {
        private Transform _propeller;
        private Vector3 _origin;

        private void Awake()
        {
            _propeller = transform.Find("Propeller");
            _origin = transform.position;
        }

        private void Update()
        {
            if (_propeller) _propeller.Rotate(0f, 0f, 90f * Time.deltaTime, Space.Self);
            transform.position = _origin + Vector3.up * (Mathf.Sin(Time.time * 1.25f) * .08f);
        }
    }
}
