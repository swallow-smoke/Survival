using System.Collections.Generic;
using System.Linq;
using _001_Scripts.Interface;
using UnityEngine;

namespace _001_Scripts.Base
{
    public abstract class PanelBase : MonoBehaviour
    {
        private List<IUIAnimator> _animator;
        public bool isViz;

        protected void Awake()
        {
            _animator = GetComponentsInChildren<IUIAnimator>().ToList();
            // Debug.Log(_animator.Count);
        }

        public virtual void Open()
        {
            _animator.ForEach(panel =>
            {
                panel.FadeIn();
            });
            isViz = true;
        }

        public virtual void Close()
        {
            _animator.ForEach(panel =>
            {
                panel.FadeOut();
            });
            isViz = false;
        }
    }
}