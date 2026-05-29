using System.Threading;
using _001_Scripts.Base;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.UI.Views
{
    public class Stamina : UIBase
    {
        private CancellationTokenSource _cts;
        [SerializeField] private Image stamina;

        public void StatUpdate(float value)
        {
            stamina.fillAmount = value / 100;
        }
    }
}