using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpringJam.Systems.DayLoop
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class DayLoopRuntime : MonoBehaviour
    {
        private static readonly DayLoopTaskDefinition[] DefaultTaskDefinitions =
        {
            new DayLoopTaskDefinition("bloom-flowers", "Help the flowers bloom", true, true),
            new DayLoopTaskDefinition("guide-bees", "Guide the bees to the right plants", true, true),
            new DayLoopTaskDefinition("learn-routines", "Learn the villagers' routines", true, true),
            new DayLoopTaskDefinition("cook-spring-meal", "Cook a spring meal", false, false),
        };

        [Min(5f)]
        [SerializeField] private float dayDurationSeconds = 120f;
        [Min(0f)]
        [SerializeField] private float startDayDurationSeconds = 3f;
        [SerializeField] private List<DayLoopTaskDefinition> taskDefinitions = new List<DayLoopTaskDefinition>(DefaultTaskDefinitions);

        private DayLoopStateMachine stateMachine;

        public static DayLoopRuntime Instance { get; private set; }

        public event Action<DayLoopSnapshot> LoopStarted;
        public event Action<DayLoopSnapshot> PhaseChanged;
        public event Action<DayLoopEndContext> LoopEnded;
        public event Action<DayLoopTaskSnapshot> TaskChanged;
        public event Action<string> KnowledgeLearned;

        public DayLoopSnapshot CurrentSnapshot => stateMachine != null ? stateMachine.CurrentSnapshot : null;
        public int ActiveLoopIndex => stateMachine != null ? stateMachine.ActiveLoopIndex : 0;
        public float DayDurationSeconds => dayDurationSeconds;
        public float StartDayDurationSeconds => startDayDurationSeconds;
        public float ElapsedSeconds => stateMachine != null ? stateMachine.ElapsedSeconds : 0f;
        public float PhaseElapsedSeconds => stateMachine != null ? stateMachine.PhaseElapsedSeconds : 0f;
        public DayLoopPhase CurrentPhase => stateMachine != null ? stateMachine.CurrentPhase : DayLoopPhase.StartDay;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (taskDefinitions == null || taskDefinitions.Count == 0)
            {
                taskDefinitions = new List<DayLoopTaskDefinition>(DefaultTaskDefinitions);
            }

            try
            {
                stateMachine = new DayLoopStateMachine(dayDurationSeconds, startDayDurationSeconds, taskDefinitions);
                stateMachine.LoopStarted += HandleLoopStarted;
                stateMachine.PhaseChanged += HandlePhaseChanged;
                stateMachine.LoopEnded += HandleLoopEnded;
                stateMachine.TaskChanged += HandleTaskChanged;
                stateMachine.KnowledgeLearned += HandleKnowledgeLearned;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                enabled = false;
            }
        }

        private void Start()
        {
            if (stateMachine != null)
            {
                stateMachine.Begin();
            }
        }

        private void Update()
        {
            if (stateMachine != null)
            {
                stateMachine.Tick(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            if (stateMachine != null)
            {
                stateMachine.LoopStarted -= HandleLoopStarted;
                stateMachine.PhaseChanged -= HandlePhaseChanged;
                stateMachine.LoopEnded -= HandleLoopEnded;
                stateMachine.TaskChanged -= HandleTaskChanged;
                stateMachine.KnowledgeLearned -= HandleKnowledgeLearned;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool StartActiveDay()
        {
            return stateMachine != null && stateMachine.StartActiveDay();
        }

        public bool TryCompleteTask(string taskId)
        {
            return stateMachine != null && stateMachine.TryCompleteTask(taskId);
        }

        public bool TryGetTask(string taskId, out DayLoopTaskSnapshot taskSnapshot)
        {
            if (stateMachine == null)
            {
                taskSnapshot = null;
                return false;
            }

            return stateMachine.TryGetTask(taskId, out taskSnapshot);
        }

        public bool TryLearnKnowledge(string knowledgeId)
        {
            return stateMachine != null && stateMachine.TryLearnKnowledge(knowledgeId);
        }

        public bool HasLearned(string knowledgeId)
        {
            return stateMachine != null && stateMachine.HasLearned(knowledgeId);
        }

        public void FreezeLoop()
        {
            if (stateMachine != null)
            {
                stateMachine.Freeze();
            }
        }

        public void RestartLoop()
        {
            if (stateMachine != null)
            {
                stateMachine.ForceReset();
            }
        }

        private void HandleLoopStarted(DayLoopSnapshot snapshot)
        {
            LoopStarted?.Invoke(snapshot);
        }

        private void HandlePhaseChanged(DayLoopSnapshot snapshot)
        {
            PhaseChanged?.Invoke(snapshot);
        }

        private void HandleLoopEnded(DayLoopEndContext context)
        {
            LoopEnded?.Invoke(context);
        }

        private void HandleTaskChanged(DayLoopTaskSnapshot taskSnapshot)
        {
            TaskChanged?.Invoke(taskSnapshot);
        }

        private void HandleKnowledgeLearned(string knowledgeId)
        {
            KnowledgeLearned?.Invoke(knowledgeId);
        }
    }
}

