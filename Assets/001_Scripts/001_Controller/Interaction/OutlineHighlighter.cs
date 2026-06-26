using System;
using UnityEngine;

namespace _001_Scripts.Controller.Interaction
{
    public class OutlineHighlighter
    {
        private readonly Material _outlineMat;

        public OutlineHighlighter(Material outlineMat)
        {
            _outlineMat = outlineMat;
        }

        public void SetHighlight(GameObject go, bool on)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            var current = renderer.sharedMaterials;

            if (on)
            {
                if (current.Length > 0 && current[current.Length - 1] == _outlineMat)
                    return; // already highlighted

                var withOutline = new Material[current.Length + 1];
                current.CopyTo(withOutline, 0);
                withOutline[current.Length] = _outlineMat;
                renderer.sharedMaterials = withOutline;
            }
            else
            {
                if (current.Length == 0 || current[current.Length - 1] != _outlineMat)
                    return; // not highlighted

                var withoutOutline = new Material[current.Length - 1];
                Array.Copy(current, withoutOutline, current.Length - 1);
                renderer.sharedMaterials = withoutOutline;
            }
        }
    }
}
