using System.Diagnostics;
using IceMilkTea.Core;
using UnityEngine;
using UnityEngine.Scripting;

namespace Samples
{
    internal enum EventId
    {
        OutsidePlayerScanning,
        InsidePlayerScanning,
        PlayerInScanning,
        PlayerOutsideScanning,
    }

    public class EnemyController : MonoBehaviour
    {
        private ImtStateMachine<EnemyController, EventId> fsm;
        public float playerScanningRange = 4f;
        public float ownScanningRange = 6f;

        internal float DistanceToPlayer()
        {
            // This implementation is an example and may differ for your scene setup
            var player = PlayerController.Instance.transform.position;
            return Vector3.Distance(transform.position, player);
        }

        internal void MoveTowardsPlayer(float speed)
        {
            // This implementation is an example and may differ for your scene setup
            var player = PlayerController.Instance.transform.position;
            transform.position = Vector3.MoveTowards(transform.position, player, speed * Time.deltaTime);
        }

        internal void RotateAtSpeed(float speed)
        {
            transform.eulerAngles += new Vector3(0, 0, speed * Time.deltaTime);
        }

        private void Awake()
        {
            fsm = new ImtStateMachine<EnemyController, EventId>(this);
            fsm.AddTransition<ExtractIntel, FollowPlayer>(EventId.PlayerInScanning);
            fsm.AddTransition<FollowPlayer, ExtractIntel>(EventId.PlayerOutsideScanning);
            fsm.AddTransition<ExtractIntel, FleeFromPlayer>(EventId.InsidePlayerScanning);
            fsm.AddTransition<FleeFromPlayer, ExtractIntel>(EventId.OutsidePlayerScanning);

            fsm.SetStartState<FollowPlayer>();
        }

        private void Start()
        {
            fsm.Update();
        }

        private void Update()
        {
            fsm.Update();
        }
    }

    internal class FollowPlayer : ImtStateMachine<EnemyController, EventId>.State
    {
        protected override void Update()
        {
            Context.MoveTowardsPlayer(1);
            if (Context.DistanceToPlayer() < Context.ownScanningRange)
            {
                StateMachine.SendEvent(EventId.PlayerOutsideScanning);
            }
        }
    }

    internal class FleeFromPlayer : ImtStateMachine<EnemyController, EventId>.State
    {
        protected override void Update()
        {
            Context.MoveTowardsPlayer(-1);
            if (Context.DistanceToPlayer() > Context.playerScanningRange)
            {
                StateMachine.SendEvent(EventId.OutsidePlayerScanning);
            }
        }
    }

    internal partial class ExtractIntel : ImtStateMachine<EnemyController, EventId>.State
    {
        internal enum InnerEventId
        {
            Reset,
            CollectData,
            SendData,
        }

        private readonly ImtStateMachine<ExtractIntel, InnerEventId> fsm;

        [Preserve]
        public ExtractIntel()
        {
            fsm = new ImtStateMachine<ExtractIntel, InnerEventId>(this);
            RegisterAnyState();
            fsm.AddAnyTransition<CollectData>(InnerEventId.Reset);
            fsm.AddTransition<CollectData, SendData>(InnerEventId.SendData);
            fsm.AddTransition<SendData, CollectData>(InnerEventId.CollectData);
            fsm.SetStartState<CollectData>();
        }

        [Conditional("ENABLE_IL2CPP")]
        private void RegisterAnyState()
        {
            fsm.RegisterStateFactory(t =>
                t == typeof(ImtStateMachine<,>.AnyState)
                    ? new ImtStateMachine<ExtractIntel, InnerEventId>.AnyState()
                    : null);
        }

        protected override void Enter()
        {
            fsm.Update();
            fsm.SendEvent(InnerEventId.Reset);
        }

        protected override void Update()
        {
            if (Context.DistanceToPlayer() > Context.ownScanningRange)
            {
                StateMachine.SendEvent(EventId.PlayerInScanning);
            }

            if (Context.DistanceToPlayer() < Context.playerScanningRange)
            {
                StateMachine.SendEvent(EventId.InsidePlayerScanning);
            }

            fsm.Update();
        }
    }

    internal partial class ExtractIntel
    {
        private class SendData : ImtStateMachine<ExtractIntel, InnerEventId>.State
        {
            private float startTime;
            private float elapsed => Time.time - startTime;

            protected override void Enter()
            {
                startTime = Time.time;
            }

            protected override void Update()
            {
                if (elapsed > 5f)
                {
                    StateMachine.SendEvent(InnerEventId.CollectData);
                }

                Context.Context.RotateAtSpeed(100f);
            }
        }
    }

    internal partial class ExtractIntel
    {
        private class CollectData : ImtStateMachine<ExtractIntel, InnerEventId>.State
        {
            private float startTime;
            private float elapsed => Time.time - startTime;

            protected override void Enter()
            {
                startTime = Time.time;
            }

            protected override void Update()
            {
                if (elapsed > 5f)
                {
                    StateMachine.SendEvent(InnerEventId.SendData);
                }
            }
        }
    }
}