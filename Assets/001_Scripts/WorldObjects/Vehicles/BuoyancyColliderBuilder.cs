using UnityEngine;

using AstraNope.Localization;
namespace AstraNope.WorldObjects.Vehicles
{
    [System.Obsolete("BuoyancyController no longer requires a trigger proxy.")]
    public static class BuoyancyColliderBuilder
    {
        public static BoxCollider Build(BuoyancyController owner, Vector3 boxSize, Vector3 boxOffset)
        {
            var t = owner.transform;

            var child = new GameObject("BuoyancyCollider");
            child.transform.SetParent(t);
            child.transform.localPosition = boxOffset;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            var boxCollider = child.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;

            if (boxSize != Vector3.zero)
            {
                boxCollider.size = boxSize;
            }
            else
            {
                var renderers = owner.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    var bounds = renderers[0].bounds;
                    foreach (var r in renderers)
                        bounds.Encapsulate(r.bounds);

                    boxCollider.size = new Vector3(
                        bounds.size.x / t.lossyScale.x,
                        bounds.size.y / t.lossyScale.y,
                        bounds.size.z / t.lossyScale.z
                    );
                    boxCollider.center = t.InverseTransformPoint(bounds.center);
                }
                else
                {
                    boxCollider.size = Vector3.one;
                    Debug.LogWarning(L10n.T("k_add769963f"));
                }
            }

            return boxCollider;
        }
    }
}
