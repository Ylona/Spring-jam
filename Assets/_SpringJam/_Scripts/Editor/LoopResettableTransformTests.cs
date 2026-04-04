using NUnit.Framework;
using SpringJam.Systems.DayLoop;
using UnityEngine;

namespace SpringJam.Tests.EditMode
{
    public sealed class LoopResettableTransformTests
    {
        [Test]
        public void ResetToCapturedState_RestoresPoseAndStopsRigidbody()
        {
            GameObject gameObject = new GameObject("Resettable");
            Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
            LoopResettableTransform resettable = gameObject.AddComponent<LoopResettableTransform>();

            gameObject.transform.position = new Vector3(2f, 1f, -3f);
            gameObject.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            gameObject.transform.localScale = new Vector3(1.5f, 2f, 0.5f);
            resettable.CaptureCurrentState();

            rigidbody.position = new Vector3(7f, 4f, 9f);
            rigidbody.rotation = Quaternion.Euler(5f, 10f, 15f);
            rigidbody.linearVelocity = Vector3.one * 4f;
            rigidbody.angularVelocity = Vector3.one * 2f;
            gameObject.transform.localScale = Vector3.one * 3f;

            resettable.ResetToCapturedState();

            AssertVector3(gameObject.transform.position, new Vector3(2f, 1f, -3f));
            Assert.That(gameObject.transform.rotation.eulerAngles.y, Is.EqualTo(45f).Within(0.01f));
            AssertVector3(gameObject.transform.localScale, new Vector3(1.5f, 2f, 0.5f));
            AssertVector3(rigidbody.linearVelocity, Vector3.zero);
            AssertVector3(rigidbody.angularVelocity, Vector3.zero);

            Object.DestroyImmediate(gameObject);
        }

        private static void AssertVector3(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.001f));
        }
    }
}
