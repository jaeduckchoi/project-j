using System.Collections.Generic;
using Editor.UI;
using NUnit.Framework;
using TMPro;
using UI.Layout;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor.Tests
{
    public sealed class PrototypeUILayoutBindingSyncUtilityEditModeTests
    {
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
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void SyncManagedBindingsFromScene_CapturesUniqueNamedRectTransform_WhenBindingIsMissing()
        {
            Scene scene = CreateIsolatedScene();
            PrototypeUILayoutBindingSettings settings = CreateSettings();
            RectTransform guideText = CreateRectObject(scene, "GuideText");
            guideText.anchorMin = new Vector2(0.25f, 0f);
            guideText.anchorMax = new Vector2(0.75f, 0f);
            guideText.pivot = new Vector2(0.5f, 0f);
            guideText.anchoredPosition = new Vector2(42f, 18f);
            guideText.sizeDelta = new Vector2(320f, 64f);

            TextMeshProUGUI text = guideText.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 19f;
            text.color = new Color(0.2f, 0.3f, 0.4f, 1f);

            int capturedCount = PrototypeUILayoutBindingSyncUtility.SyncManagedBindingsFromScene(scene, settings);

            Assert.That(capturedCount, Is.GreaterThan(0));
            Assert.That(settings.TryGetEntry("GuideText", out PrototypeUILayoutBindingEntry entry), Is.True);
            Assert.That(entry.SceneObjectPath, Is.EqualTo("GuideText"));
            Assert.That(entry.TryGetLayout(out PrototypeUIRect layout), Is.True);
            Assert.That(layout.AnchorMin, Is.EqualTo(new Vector2(0.25f, 0f)));
            Assert.That(layout.AnchorMax, Is.EqualTo(new Vector2(0.75f, 0f)));
            Assert.That(layout.AnchoredPosition, Is.EqualTo(new Vector2(42f, 18f)));
            Assert.That(layout.SizeDelta, Is.EqualTo(new Vector2(320f, 64f)));
            Assert.That(entry.ApplyText, Is.True);
        }

        [Test]
        public void ResolveLayoutPreviewObject_PrefersExplicitBinding_WhenSameNamedObjectsAreAmbiguous()
        {
            Scene scene = CreateIsolatedScene();
            PrototypeUILayoutBindingSettings settings = CreateSettings();
            GameObject firstRoot = CreateRootObject(scene, "FirstGroup");
            GameObject secondRoot = CreateRootObject(scene, "SecondGroup");
            RectTransform firstGuideText = CreateRectObject(scene, "GuideText", firstRoot.transform);
            RectTransform secondGuideText = CreateRectObject(scene, "GuideText", secondRoot.transform);

            GameObject unresolved = PrototypeUILayoutBindingSyncUtility.ResolveLayoutPreviewObject(scene, settings, "GuideText");
            Assert.That(unresolved, Is.Null);

            settings.CaptureFromSource("GuideText", secondGuideText, PrototypeUILayoutBindingSyncUtility.BuildSceneObjectPath(secondGuideText));
            GameObject resolved = PrototypeUILayoutBindingSyncUtility.ResolveLayoutPreviewObject(scene, settings, "GuideText");
            Assert.That(resolved, Is.SameAs(secondGuideText.gameObject));
            Assert.That(resolved, Is.Not.SameAs(firstGuideText.gameObject));
        }

        private PrototypeUILayoutBindingSettings CreateSettings()
        {
            PrototypeUILayoutBindingSettings settings = ScriptableObject.CreateInstance<PrototypeUILayoutBindingSettings>();
            cleanupTargets.Add(settings);
            return settings;
        }

        private Scene CreateIsolatedScene()
        {
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private GameObject CreateRootObject(Scene scene, string name)
        {
            GameObject gameObject = new(name);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            cleanupTargets.Add(gameObject);
            return gameObject;
        }

        private RectTransform CreateRectObject(Scene scene, string name, Transform parent = null)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            if (gameObject.scene != scene)
            {
                SceneManager.MoveGameObjectToScene(gameObject, scene);
            }

            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }
            cleanupTargets.Add(gameObject);
            return gameObject.GetComponent<RectTransform>();
        }
    }
}
