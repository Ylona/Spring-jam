using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using SpringJam2026.Events;
using UnityEngine;

namespace SpringJam2026.Editor
{
    public class EventHubDebugger : EditorWindow
    {
        private class EventInfoData
        {
            public string Name;
            public EventInfo EventInfo;
            public MethodInfo BroadcastMethod;
            public Type[] Parameters;
        }

        private class EventState
        {
            public bool isListening;
            public Delegate handler;
            public List<string> logs = new();
            public object[] inputValues;
        }

        private List<EventInfoData> events = new();
        private Dictionary<string, EventState> states = new();

        private ScrollView eventList;
        private TextField filterField;

        private string filter = "";

        [MenuItem("Spring Jam 2026/Tools/EventHub Debugger")]
        public static void ShowWindow()
        {
            GetWindow<EventHubDebugger>();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_SpringJam/_Scripts/Editor/EventHub/EventHubDebugger.uxml");

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/_SpringJam/_Scripts/Editor/EventHub/EventHubDebugger.uss");

            root.Add(visualTree.CloneTree());
            root.styleSheets.Add(styleSheet);

            eventList = root.Q<ScrollView>("event-list");
            filterField = root.Q<TextField>("filter-field");

            root.Q<Button>("refresh-button").clicked += () =>
            {
                CacheEvents();
                RefreshEventList();
            };

            filterField.RegisterValueChangedCallback(evt =>
            {
                filter = evt.newValue;
                RefreshEventList();
            });

            CacheEvents();
            RefreshEventList();
        }

        private void CacheEvents()
        {
            events.Clear();

            var type = typeof(EventHub);
            var eventFields = type.GetEvents(BindingFlags.Public | BindingFlags.Static);

            foreach (var ev in eventFields)
            {
                if (!ev.Name.StartsWith("Ev_"))
                    continue;

                string baseName = ev.Name.Replace("Ev_", "");
                string methodName = $"Broadcast_{baseName}";

                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                    continue;

                var parameters = method.GetParameters().Select(p => p.ParameterType).ToArray();

                events.Add(new EventInfoData
                {
                    Name = baseName,
                    EventInfo = ev,
                    BroadcastMethod = method,
                    Parameters = parameters
                });
            }
        }

        private void RefreshEventList()
        {
            eventList.Clear();

            foreach (var evt in events)
            {
                if (!string.IsNullOrEmpty(filter) &&
                    !evt.Name.ToLower().Contains(filter.ToLower()))
                    continue;

                eventList.Add(CreateEventCard(evt));
            }
        }

        private VisualElement CreateEventCard(EventInfoData evt)
        {
            if (!states.ContainsKey(evt.Name))
            {
                states[evt.Name] = new EventState
                {
                    inputValues = new object[evt.Parameters.Length]
                };
            }

            var state = states[evt.Name];

            var card = new VisualElement();
            card.AddToClassList("event-card");

            // HEADER
            var header = new VisualElement();
            header.AddToClassList("event-header");

            var name = new Label(evt.Name);
            name.AddToClassList("event-name");

            var status = new VisualElement();
            status.AddToClassList("status-dot");
            status.style.backgroundColor = state.isListening ? Color.green : Color.red;

            var listenBtn = new Button(() =>
            {
                ToggleListening(evt, state);
                RefreshEventList();
            })
            {
                text = state.isListening ? "Stop" : "Listen"
            };

            header.Add(name);
            header.Add(status);
            header.Add(listenBtn);

            card.Add(header);

            // BROADCAST
            bool canBroadcast = AreAllParametersSupported(evt.Parameters);
            
            if (canBroadcast && evt.Parameters.Length > 0)
            {
                var inputContainer = new VisualElement();
                inputContainer.AddToClassList("input-container");

                for (int i = 0; i < evt.Parameters.Length; i++)
                {
                    int index = i;
                    var paramType = evt.Parameters[i];

                    VisualElement field = CreateFieldForType(paramType, state, index);
                    inputContainer.Add(field);
                }

                card.Add(inputContainer);
            }
            
            if (canBroadcast)
            {
                var broadcastBtn = new Button(() =>
                {
                    evt.BroadcastMethod.Invoke(null,
                        evt.Parameters.Length == 0 ? null : state.inputValues);
                })
                {
                    text = "Broadcast"
                };

                card.Add(broadcastBtn);
            }
            else
            {
                var label = new Label("Broadcast unavailable (unsupported parameters)");
                label.style.color = Color.yellow;
                label.style.unityFontStyleAndWeight = FontStyle.Italic;

                card.Add(label);
            }

            // LOGS
            var logScroll = new ScrollView();
            logScroll.AddToClassList("log-container");

            foreach (var log in state.logs)
            {
                logScroll.Add(new Label(log));
            }

            card.Add(logScroll);

            return card;
        }
        
        /**
         * If we plan to support complex types we need to add it here (2)
         */
        private VisualElement CreateFieldForType(Type type, EventState state, int index)
        {
            if (type == typeof(bool))
            {
                var toggle = new Toggle(type.Name);
                toggle.value = state.inputValues[index] as bool? ?? false;

                toggle.RegisterValueChangedCallback(evt =>
                {
                    state.inputValues[index] = evt.newValue;
                });

                return toggle;
            }

            if (type == typeof(int))
            {
                var field = new IntegerField(type.Name);
                field.value = state.inputValues[index] as int? ?? 0;

                field.RegisterValueChangedCallback(evt =>
                {
                    state.inputValues[index] = evt.newValue;
                });

                return field;
            }

            if (type == typeof(float))
            {
                var field = new FloatField(type.Name);
                field.value = state.inputValues[index] as float? ?? 0f;

                field.RegisterValueChangedCallback(evt =>
                {
                    state.inputValues[index] = evt.newValue;
                });

                return field;
            }

            if (type == typeof(string))
            {
                var field = new TextField(type.Name);
                field.value = state.inputValues[index] as string ?? "";

                field.RegisterValueChangedCallback(evt =>
                {
                    state.inputValues[index] = evt.newValue;
                });

                return field;
            }

            return null;
        }

        private void ToggleListening(EventInfoData evt, EventState state)
        {
            if (!state.isListening)
            {
                var handler = CreateSafeHandler(evt, state);
                evt.EventInfo.AddEventHandler(null, handler);

                state.handler = handler;
                state.isListening = true;
            }
            else
            {
                evt.EventInfo.RemoveEventHandler(null, state.handler);
                state.isListening = false;
            }
        }

        private Delegate CreateSafeHandler(EventInfoData evt, EventState state)
        {
            var handlerType = evt.EventInfo.EventHandlerType;
            var invokeMethod = handlerType.GetMethod("Invoke");
            var parameters = invokeMethod.GetParameters();

            var paramExprs = parameters.Select(p => Expression.Parameter(p.ParameterType)).ToArray();

            var objectArray = Expression.NewArrayInit(
                typeof(object),
                paramExprs.Select(p => Expression.Convert(p, typeof(object)))
            );

            var instance = Expression.Constant(this);
            var evtConst = Expression.Constant(evt);
            var stateConst = Expression.Constant(state);

            var logMethod = typeof(EventHubDebugger)
                .GetMethod(nameof(LogEvent), BindingFlags.NonPublic | BindingFlags.Instance);

            var body = Expression.Call(instance, logMethod, evtConst, stateConst, objectArray);

            var lambda = Expression.Lambda(handlerType, body, paramExprs);

            return lambda.Compile();
        }

        private void LogEvent(EventInfoData evt, EventState state, object data)
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff");

            string value;

            if (data == null)
                value = "VOID";
            else if (data is object[] arr)
                value = string.Join(", ", arr.Select(x => x?.ToString() ?? "null"));
            else
                value = data.ToString();

            string caller = GetCallerInfo();

            state.logs.Add($"[{time}] {value} :: {caller}");

            if (state.logs.Count > 50)
                state.logs.RemoveAt(0);

            RefreshEventList();
        }

        private string GetCallerInfo()
        {
            try
            {
                var stack = new System.Diagnostics.StackTrace(true);

                foreach (var frame in stack.GetFrames())
                {
                    var method = frame.GetMethod();
                    var type = method.DeclaringType;

                    if (type == null ||
                        type == typeof(EventHubDebugger) ||
                        type == typeof(EventHub))
                        continue;

                    if (method.Name.Contains("lambda") ||
                        method.Name.Contains("<"))
                        continue;

                    string file = frame.GetFileName();
                    int line = frame.GetFileLineNumber();

                    if (!string.IsNullOrEmpty(file))
                        return $"{System.IO.Path.GetFileName(file)}:{line}";

                    return $"{type.Name}.{method.Name}";
                }
            }
            catch { }

            return "Unknown";
        }
        
        private bool AreAllParametersSupported(Type[] parameters)
        {
            foreach (var type in parameters)
            {
                if (!IsTypeSupported(type))
                    return false;
            }

            return true;
        }

        /**
         * If we plan to support complex types we should add them here (1) and then
         * add it to the CreateFieldForType() function as well.
         */
        private bool IsTypeSupported(Type type)
        {
            return type == typeof(bool)
                   || type == typeof(int)
                   || type == typeof(float)
                   || type == typeof(string);
        }
    }
}