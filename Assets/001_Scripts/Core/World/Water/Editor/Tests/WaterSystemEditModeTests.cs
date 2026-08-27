#if UNITY_EDITOR
using AstraNope.Core.World.Water.Interfaces;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace AstraNope.Core.World.Water.Editor.Tests
{
    public sealed class WaterSystemEditModeTests
    {
        private GameObject _serviceObject;
        private WaterQueryService _service;

        [SetUp]
        public void SetUp()
        {
            _serviceObject = new GameObject("WaterQueryService_Test");
            _service = _serviceObject.AddComponent<WaterQueryService>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_serviceObject);
            foreach (WaterBodyBehaviour body in UnityEngine.Object.FindObjectsByType<WaterBodyBehaviour>())
                UnityEngine.Object.DestroyImmediate(body.gameObject);
        }

        [Test]
        public void EmptyService_ReturnsFalse()
        {
            Assert.That(_service.TrySample(Vector3.zero, out _), Is.False);
            Assert.That(_service.TryGetWaterBody(Vector3.zero, out _), Is.False);
        }

        [Test]
        public void Ocean_ReportsSurfaceAndDoesNotSubmergePointAboveSurface()
        {
            GameObject gameObject = new GameObject("Ocean_Test");
            OceanBody ocean = gameObject.AddComponent<OceanBody>();
            _service.Register(ocean);

            Assert.That(_service.TrySample(new Vector3(10f, 3f, 10f), out WaterSample above), Is.True);
            Assert.That(above.SurfaceHeight, Is.EqualTo(0f).Within(0.001f));
            Assert.That(above.IsSubmerged, Is.False);
            Assert.That(_service.TrySample(new Vector3(10f, -2f, 10f), out WaterSample below), Is.True);
            Assert.That(below.SignedDepth, Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void Ocean_DisablesLegacySurfaceCollider()
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Collider surfaceCollider = gameObject.GetComponent<Collider>();
            Assert.That(surfaceCollider.enabled, Is.True);

            gameObject.AddComponent<OceanBody>();

            Assert.That(surfaceCollider.enabled, Is.False);
        }

        [Test]
        public void RotatedLake_UsesOrientedBox()
        {
            GameObject gameObject = new GameObject("Lake_Test");
            gameObject.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(4f, 4f, 2f);
            LakeWaterBody lake = gameObject.AddComponent<LakeWaterBody>();
            _service.Register(lake);

            Vector3 inside = gameObject.transform.TransformPoint(new Vector3(1.5f, -1f, 0f));
            Vector3 outside = gameObject.transform.TransformPoint(new Vector3(0f, -1f, 1.5f));
            Assert.That(_service.TrySample(inside, out WaterSample sample), Is.True);
            Assert.That(sample.BodyType, Is.EqualTo(WaterBodyType.Lake));
            Assert.That(_service.TrySample(outside, out _), Is.False);
        }

        [Test]
        public void Service_RejectsDuplicatesAndUnregisters()
        {
            TestBody body = new TestBody(0, 1f, WaterBodyType.Custom);
            Assert.That(_service.Register(body), Is.True);
            Assert.That(_service.Register(body), Is.False);
            Assert.That(_service.RegisteredBodyCount, Is.EqualTo(1));
            Assert.That(_service.Unregister(body), Is.True);
            Assert.That(_service.TrySample(Vector3.zero, out _), Is.False);
        }

        [Test]
        public void SpatialIndex_OnlySamplesBodiesNearTheQueryCell()
        {
            for (int i = 0; i < 128; i++)
            {
                Bounds bounds = new Bounds(new Vector3(i * 128f, 0f, 0f), new Vector3(16f, 16f, 16f));
                _service.Register(new TestBody(0, 1f, WaterBodyType.Custom, bounds));
            }

            Assert.That(_service.TrySample(new Vector3(64f * 128f, 0f, 0f), out _), Is.True);
            Assert.That(_service.LastBroadPhaseCandidateCount, Is.EqualTo(1));
            Assert.That(_service.LastSampledBodyCount, Is.EqualTo(1));
        }

        [Test]
        public void Refresh_MovesBodyBetweenSpatialCells()
        {
            TestBody body = new TestBody(0, 1f, WaterBodyType.Custom,
                new Bounds(Vector3.zero, Vector3.one * 10f));
            _service.Register(body);
            Assert.That(_service.TrySample(Vector3.zero, out _), Is.True);

            body.Bounds = new Bounds(new Vector3(1024f, 0f, 0f), Vector3.one * 10f);
            _service.Refresh(body);
            Assert.That(_service.TrySample(Vector3.zero, out _), Is.False);
            Assert.That(_service.TrySample(new Vector3(1024f, 0f, 0f), out _), Is.True);
        }

        [Test]
        public void Overlap_SelectsHigherPriorityThenLocalBody()
        {
            TestBody ocean = new TestBody(10, 0f, WaterBodyType.Ocean);
            TestBody local = new TestBody(10, 1f, WaterBodyType.Lake);
            _service.Register(ocean);
            _service.Register(local);

            Assert.That(_service.TrySample(new Vector3(0f, -1f, 0f), out WaterSample sample), Is.True);
            Assert.That(sample.WaterBody, Is.SameAs(local));

            TestBody higher = new TestBody(20, -0.5f, WaterBodyType.Ocean);
            _service.Register(higher);
            Assert.That(_service.TrySample(new Vector3(0f, -1f, 0f), out sample), Is.True);
            Assert.That(sample.WaterBody, Is.SameAs(higher));
        }

        [Test]
        public void StraightRiver_SamplesWidthDepthAndFlow()
        {
            GameObject gameObject = new GameObject("River_Test");
            SplineContainer container = gameObject.AddComponent<SplineContainer>();
            container.Splines[0].Add(new BezierKnot(new float3(0f, 0f, 0f)), TangentMode.Linear);
            container.Splines[0].Add(new BezierKnot(new float3(0f, 0f, 20f)), TangentMode.Linear);
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();
            SplineRiverWaterBody river = gameObject.AddComponent<SplineRiverWaterBody>();
            river.Rebuild();
            _service.Register(river);

            Assert.That(_service.TrySample(new Vector3(1f, -1f, 10f), out WaterSample sample), Is.True);
            Assert.That(sample.BodyType, Is.EqualTo(WaterBodyType.River));
            Assert.That(Vector3.Dot(sample.FlowDirection, Vector3.forward), Is.GreaterThan(0.99f));
            Assert.That(_service.TrySample(new Vector3(10f, -1f, 10f), out _), Is.False);
            Assert.That(_service.TrySample(new Vector3(0f, -10f, 10f), out _), Is.False);
        }

        [Test]
        public void CurvedRiver_SamplesCurveWithoutLeftRightFlip()
        {
            GameObject gameObject = new GameObject("CurvedRiver_Test");
            SplineContainer container = gameObject.AddComponent<SplineContainer>();
            container.Splines[0].Add(new BezierKnot(new float3(0f, 0f, 0f)), TangentMode.AutoSmooth);
            container.Splines[0].Add(new BezierKnot(new float3(10f, 0f, 10f)), TangentMode.AutoSmooth);
            container.Splines[0].Add(new BezierKnot(new float3(0f, 0f, 20f)), TangentMode.AutoSmooth);
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();
            SplineRiverWaterBody river = gameObject.AddComponent<SplineRiverWaterBody>();
            river.Rebuild();
            _service.Register(river);

            Vector3 midpoint = (Vector3)container.EvaluatePosition(0.5f);
            Assert.That(_service.TrySample(midpoint - Vector3.up, out WaterSample sample), Is.True);
            Assert.That(sample.SurfaceNormal.y, Is.GreaterThan(0f));
            Assert.That(river.GeneratedMesh, Is.Not.Null);
            Assert.That(river.GeneratedMesh.vertexCount, Is.GreaterThan(4));
        }

        private sealed class TestBody : IWaterBody
        {
            private readonly float _surface;
            private readonly WaterBodyType _type;

            public int Priority { get; }
            public Bounds Bounds { get; set; }
            public Bounds WorldBounds => Bounds;

            public TestBody(int priority, float surface, WaterBodyType type, Bounds? bounds = null)
            {
                Priority = priority;
                _surface = surface;
                _type = type;
                Bounds = bounds ?? new Bounds(Vector3.zero, Vector3.one * 100f);
            }

            public bool TrySample(Vector3 position, out WaterSample sample)
            {
                sample = new WaterSample(this, position,
                    new Vector3(position.x, _surface, position.z), Vector3.up, Vector3.zero, _type);
                return true;
            }
        }
    }
}
#endif
