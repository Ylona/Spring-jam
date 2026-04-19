using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SpringJam.Systems.DayLoop;
using UnityEngine;

namespace SpringJam.Tests.EditMode
{
    public sealed class MaraHoneyDropperTests
    {
        [Test]
        public void GuideBeesCompletion_WalksMaraToDropPointRevealsHoneyJarAndHeadsToMintPatch()
        {
            if (DayLoopRuntime.Instance != null)
            {
                Object.DestroyImmediate(DayLoopRuntime.Instance.gameObject);
            }

            GameObject runtimeRoot = new GameObject("DayLoopRuntime");
            DayLoopRuntime runtime = runtimeRoot.AddComponent<DayLoopRuntime>();
            InvokePrivateMethod(runtime, "Awake");
            InvokePrivateMethod(runtime, "Start");

            GameObject startRoot = new GameObject("Mara Start");
            startRoot.transform.position = new Vector3(1f, 1f, 1f);

            GameObject dropRoot = new GameObject("Mara Honey Drop Stand");
            dropRoot.transform.position = new Vector3(3f, 1f, 1f);

            GameObject honeyDropRoot = new GameObject("Honey Jar Drop");
            honeyDropRoot.transform.position = new Vector3(3f, 0f, 1f);

            GameObject postDropRoot = new GameObject("Mint Patch Work Point");
            postDropRoot.transform.position = new Vector3(5f, 1f, 1f);

            GameObject honeyRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            honeyRoot.name = "Honey Jar";
            Renderer honeyRenderer = honeyRoot.GetComponent<Renderer>();
            Collider honeyCollider = honeyRoot.GetComponent<Collider>();

            GameObject maraRoot = new GameObject("Mara");
            SpriteRenderer maraSpriteRenderer = maraRoot.AddComponent<SpriteRenderer>();
            MaraHoneyDropper dropper = maraRoot.AddComponent<MaraHoneyDropper>();
            SetPrivateField(dropper, "startAnchor", startRoot.transform);
            SetPrivateField(dropper, "dropAnchor", dropRoot.transform);
            SetPrivateField(dropper, "honeyDropAnchor", honeyDropRoot.transform);
            SetPrivateField(dropper, "postDropAnchor", postDropRoot.transform);
            SetPrivateField(dropper, "honeyJar", honeyRoot.transform);
            SetPrivateField(dropper, "honeyJarRenderers", new[] { honeyRenderer });
            SetPrivateField(dropper, "honeyJarColliders", new[] { honeyCollider });
            SetPrivateField(dropper, "maraSpriteRenderer", maraSpriteRenderer);
            SetPrivateField(dropper, "moveSpeed", 10f);
            InvokePrivateMethod(dropper, "Awake");
            InvokePrivateMethod(dropper, "OnEnable");
            InvokePrivateMethod(dropper, "Start");

            Assert.That(maraRoot.transform.position, Is.EqualTo(startRoot.transform.position));
            Assert.That(maraSpriteRenderer.enabled, Is.False);
            Assert.That(honeyRenderer.enabled, Is.False);
            Assert.That(honeyCollider.enabled, Is.False);

            Assert.That(runtime.StartActiveDay(), Is.True);
            Assert.That(runtime.TryCompleteTask("guide-bees"), Is.True);

            Assert.That(dropper.IsWalkingToDrop, Is.True);
            Assert.That(dropper.IsWalkingAfterDrop, Is.False);
            Assert.That(dropper.HasDroppedHoney, Is.False);
            Assert.That(maraSpriteRenderer.enabled, Is.True);
            Assert.That(honeyRenderer.enabled, Is.False);
            Assert.That(honeyCollider.enabled, Is.False);

            InvokePrivateMethod(dropper, "UpdateDropMovement", 1f);

            Assert.That(dropper.IsWalkingToDrop, Is.False);
            Assert.That(dropper.IsWalkingAfterDrop, Is.True);
            Assert.That(dropper.HasDroppedHoney, Is.True);
            Assert.That(honeyRoot.transform.position, Is.EqualTo(honeyDropRoot.transform.position));
            Assert.That(honeyRenderer.enabled, Is.True);
            Assert.That(honeyCollider.enabled, Is.True);

            InvokePrivateMethod(dropper, "UpdateDropMovement", 1f);

            Assert.That(dropper.IsWalkingAfterDrop, Is.False);
            Assert.That(maraRoot.transform.position, Is.EqualTo(postDropRoot.transform.position));

            runtime.RestartLoop();

            Assert.That(dropper.IsWalkingAfterDrop, Is.False);
            Assert.That(dropper.HasDroppedHoney, Is.False);
            Assert.That(maraRoot.transform.position, Is.EqualTo(startRoot.transform.position));
            Assert.That(maraSpriteRenderer.enabled, Is.False);
            Assert.That(honeyRenderer.enabled, Is.False);
            Assert.That(honeyCollider.enabled, Is.False);

            Object.DestroyImmediate(maraRoot);
            Object.DestroyImmediate(honeyRoot);
            Object.DestroyImmediate(postDropRoot);
            Object.DestroyImmediate(honeyDropRoot);
            Object.DestroyImmediate(dropRoot);
            Object.DestroyImmediate(startRoot);
            Object.DestroyImmediate(runtimeRoot);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static void InvokePrivateMethod(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method '{methodName}' on {target.GetType().Name}.");
            method.Invoke(target, arguments.Length == 0 ? null : arguments);
        }
    }
}