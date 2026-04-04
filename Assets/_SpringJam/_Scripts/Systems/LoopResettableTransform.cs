using UnityEngine;

namespace SpringJam.Systems.DayLoop
{
    [DisallowMultipleComponent]
    public sealed class LoopResettableTransform : MonoBehaviour
    {
        [SerializeField] private bool captureStateOnAwake = true;
        [SerializeField] private bool resetRigidbodies = true;

        private DayLoopRuntime subscribedRuntime;
        private Rigidbody attachedRigidbody;
        private bool hasCapturedState;
        private Vector3 capturedPosition;
        private Quaternion capturedRotation;
        private Vector3 capturedLocalScale;

        private void Awake()
        {
            CacheComponents();

            if (captureStateOnAwake)
            {
                CaptureCurrentState();
            }
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Start()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        [ContextMenu("Capture Current State")]
        public void CaptureCurrentState()
        {
            capturedPosition = transform.position;
            capturedRotation = transform.rotation;
            capturedLocalScale = transform.localScale;
            hasCapturedState = true;
        }

        public void ResetToCapturedState()
        {
            if (!hasCapturedState)
            {
                CaptureCurrentState();
            }

            transform.position = capturedPosition;
            transform.rotation = capturedRotation;
            transform.localScale = capturedLocalScale;

            if (resetRigidbodies)
            {
                Rigidbody rigidbody = CacheComponents();
                if (rigidbody != null)
                {
                    rigidbody.position = capturedPosition;
                    rigidbody.rotation = capturedRotation;
                    rigidbody.linearVelocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                    rigidbody.Sleep();
                }
            }

            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is ILoopResetListener listener)
                {
                    listener.OnLoopReset();
                }
            }
        }

        private void HandleLoopStarted(DayLoopSnapshot _)
        {
            ResetToCapturedState();
        }

        private void TrySubscribe()
        {
            DayLoopRuntime runtime = DayLoopRuntime.Instance;
            if (runtime == null || subscribedRuntime == runtime)
            {
                return;
            }

            Unsubscribe();
            subscribedRuntime = runtime;
            subscribedRuntime.LoopStarted += HandleLoopStarted;
        }

        private void Unsubscribe()
        {
            if (subscribedRuntime == null)
            {
                return;
            }

            subscribedRuntime.LoopStarted -= HandleLoopStarted;
            subscribedRuntime = null;
        }

        private Rigidbody CacheComponents()
        {
            if (attachedRigidbody == null)
            {
                attachedRigidbody = GetComponent<Rigidbody>();
            }

            return attachedRigidbody;
        }
    }
}
