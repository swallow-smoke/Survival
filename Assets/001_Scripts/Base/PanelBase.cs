using System;
using System.Collections.Generic;
using System.Linq;
using _001_Scripts.Interface;
using _001_Scripts.UI.Component;
using DG.Tweening;
using UnityEngine;

namespace _001_Scripts.Base
{
    public abstract class PanelBase : MonoBehaviour
    {
        private List<IUIAnimator> _animator;
        private bool isOpen;

        private void Awake()
        {
            _animator = GetComponentsInChildren<IUIAnimator>().ToList();
        }

        public virtual void Open()
        {
            _animator.ForEach(panel =>
            {
                panel.FadeIn();
            });
        }

        public virtual void Close()
        {
            _animator.ForEach(panel =>
            {
                panel.FadeOut();
            });
        }
    }
}