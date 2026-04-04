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
            attachedRigidbody = GetComponent<Rigidbody>();

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

            if (resetRigidbodies && attachedRigidbody != null)
            {
                attachedRigidbody.position = capturedPosition;
                attachedRigidbody.rotation = capturedRotation;
                attachedRigidbody.linearVelocity = Vector3.zero;
                attachedRigidbody.angularVelocity = Vector3.zero;
                attachedRigidbody.Sleep();
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
    }
}
