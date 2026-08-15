using System;
using _001_Scripts.Data.Message;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Controller
{
    public class PlayerAnimationController : MonoBehaviour
    {
        private IDisposable bag;
        [SerializeField] private Animator animator;
        [SerializeField] private float dampTime = 0.1f;
        
        private void OnAnimation(PlayerMovementMessage msg)
        {
            animator.SetFloat("SpeedX", msg.rawVector3.x, dampTime, Time.deltaTime);
            animator.SetFloat("SpeedY", Mathf.Abs(msg.rawVector3.y), dampTime, Time.deltaTime);
            animator.SetFloat("SpeedZ", msg.rawVector3.z, dampTime, Time.deltaTime);
            animator.SetBool("isGround", msg.isGround);
        }
        

        [Inject]
        public void Construct(ISubscriber<PlayerMovementMessage> playerMovementSubscriber)
        {
            var builder = DisposableBag.CreateBuilder();
            
            builder.Add(playerMovementSubscriber.Subscribe(OnAnimation));

            bag = builder.Build();
        }

        private void OnDestroy() => bag?.Dispose();
    }
}