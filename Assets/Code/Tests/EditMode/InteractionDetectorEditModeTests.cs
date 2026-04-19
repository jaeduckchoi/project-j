using System.Collections.Generic;
using System.Reflection;
using Code.Scripts.Exploration.Interaction;
using Code.Scripts.Exploration.Player;
using NUnit.Framework;
using UnityEngine;

namespace Editor.Tests
{
    public sealed class InteractionDetectorEditModeTests
    {
        private const BindingFlags PrivateInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly List<Object> cleanupTargets = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = cleanupTargets.Count - 1; index >= 0; index--)
            {
                Object target = cleanupTargets[index];
                if (target != null)
                {
                    Object.DestroyImmediate(target);
                }
            }

            cleanupTargets.Clear();
        }

        [Test]
        public void CurrentInteractable_UsesPlayerFacingDirection_WhenCandidatesOverlapRange()
        {
            InteractionDetector detector = CreateDetectorWithPlayer();
            GuideOnlyInteractable frontTarget = CreateTarget("FrontTarget", new Vector3(0f, -1f, 0f));
            GuideOnlyInteractable backTarget = CreateTarget("BackTarget", new Vector3(0f, 0.4f, 0f));

            RefreshDetector(detector);

            Assert.That(detector.CurrentInteractable, Is.SameAs(frontTarget));
            Assert.That(detector.CurrentInteractable, Is.Not.SameAs(backTarget));
        }

        [Test]
        public void CurrentInteractable_RejectsTargetsBehindDefaultFacingDirection()
        {
            InteractionDetector detector = CreateDetectorWithPlayer();
            CreateTarget("BackTarget", new Vector3(0f, 0.4f, 0f));

            RefreshDetector(detector);

            Assert.That(detector.CurrentInteractable, Is.Null);
        }

        [Test]
        public void CurrentInteractable_UsesClosestColliderPoint_ForWideTargets()
        {
            InteractionDetector detector = CreateDetectorWithPlayer();
            GuideOnlyInteractable wideFrontTarget = CreateTarget(
                "WideFrontTarget",
                new Vector3(2f, -0.6f, 0f),
                new Vector2(4f, 0.4f));

            RefreshDetector(detector);

            Assert.That(detector.CurrentInteractable, Is.SameAs(wideFrontTarget));
        }

        [Test]
        public void CurrentInteractable_FallsBackToDistanceSelection_WhenPlayerControllerIsMissing()
        {
            InteractionDetector detector = CreateDetector(null);
            GuideOnlyInteractable target = CreateTarget("FallbackTarget", new Vector3(0f, 0.4f, 0f));

            RefreshDetector(detector);

            Assert.That(detector.CurrentInteractable, Is.SameAs(target));
        }

        private InteractionDetector CreateDetectorWithPlayer()
        {
            GameObject playerObject = CreateGameObject("Player");
            playerObject.AddComponent<Rigidbody2D>();
            playerObject.AddComponent<PlayerController>();

            return CreateDetector(playerObject.transform);
        }

        private InteractionDetector CreateDetector(Transform parent)
        {
            GameObject detectorObject = CreateGameObject("InteractionRange");
            if (parent != null)
            {
                detectorObject.transform.SetParent(parent, false);
            }

            CircleCollider2D collider = detectorObject.AddComponent<CircleCollider2D>();
            collider.radius = 2f;

            InteractionDetector detector = detectorObject.AddComponent<InteractionDetector>();
            InvokePrivateMethod(detector, "Awake");
            return detector;
        }

        private GuideOnlyInteractable CreateTarget(string objectName, Vector3 position)
        {
            return CreateTarget(objectName, position, new Vector2(0.4f, 0.4f));
        }

        private GuideOnlyInteractable CreateTarget(string objectName, Vector3 position, Vector2 colliderSize)
        {
            GameObject targetObject = CreateGameObject(objectName);
            targetObject.transform.position = position;

            BoxCollider2D collider = targetObject.AddComponent<BoxCollider2D>();
            collider.size = colliderSize;

            GuideOnlyInteractable interactable = targetObject.AddComponent<GuideOnlyInteractable>();
            interactable.Configure(objectName, "테스트 안내");
            return interactable;
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new(name);
            cleanupTargets.Add(gameObject);
            return gameObject;
        }

        private static void RefreshDetector(InteractionDetector detector)
        {
            Physics2D.SyncTransforms();
            InvokePrivateMethod(detector, "Update");
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, PrivateInstanceFlags);
            Assert.That(method, Is.Not.Null, $"메서드 {methodName}를 찾을 수 없습니다.");
            method.Invoke(target, null);
        }
    }
}
