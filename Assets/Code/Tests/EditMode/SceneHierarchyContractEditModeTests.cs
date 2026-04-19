using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Code.Scripts.Exploration.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor.Tests
{
    public sealed class SceneHierarchyContractEditModeTests
    {
        private readonly List<UnityEngine.Object> cleanupTargets = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = cleanupTargets.Count - 1; index >= 0; index--)
            {
                UnityEngine.Object target = cleanupTargets[index];
                if (target != null)
                {
                    UnityEngine.Object.DestroyImmediate(target);
                }
            }

            cleanupTargets.Clear();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void SyncSceneHierarchyContractsFromScene_CapturesCatalogAndMarkedHelperHierarchy()
        {
            Scene scene = CreateIsolatedScene();
            SceneHierarchyContractSettings settings = CreateSettings();

            GameObject sceneSystemRoot = CreateRootObject(scene, PrototypeSceneHierarchyCatalog.SceneSystemRootName);
            GameObject helperParent = CreateRootObject(scene, "HelperParent");

            GameObject storageStation = CreateRootObject(scene, "StorageStation");
            storageStation.transform.SetParent(sceneSystemRoot.transform, false);
            storageStation.SetActive(false);

            GameObject helper = CreateRootObject(scene, "AuthoredHelper");
            helper.transform.SetParent(helperParent.transform, false);
            helper.SetActive(false);
            helper.AddComponent<SceneAuthoredHelperContractMarker>();

            int capturedCount = global::Editor.PrototypeSceneHierarchyContractSyncUtility.SyncSceneHierarchyContractsFromScene(scene, settings, "Hub");

            Assert.That(capturedCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(settings.TryGetEntry("Hub", "StorageStation", out SceneHierarchyContractEntry storageEntry), Is.True);
            Assert.That(storageEntry.SceneObjectPath, Is.EqualTo("SceneSystemRoot/StorageStation"));
            Assert.That(storageEntry.ParentScenePath, Is.EqualTo("SceneSystemRoot"));
            Assert.That(storageEntry.SiblingIndex, Is.EqualTo(0));
            Assert.That(storageEntry.InitialActiveSelf, Is.False);

            Assert.That(settings.TryGetEntryBySceneObjectPath("Hub", "HelperParent/AuthoredHelper", out SceneHierarchyContractEntry helperEntry), Is.True);
            Assert.That(helperEntry.ObjectName, Is.EqualTo("AuthoredHelper"));
            Assert.That(helperEntry.ParentScenePath, Is.EqualTo("HelperParent"));
            Assert.That(helperEntry.SiblingIndex, Is.EqualTo(0));
            Assert.That(helperEntry.InitialActiveSelf, Is.False);
        }

        [Test]
        public void OrganizeSceneHierarchy_UsesContractOverride_ForCatalogAndMarkedHelper()
        {
            Scene scene = CreateIsolatedScene();
            SceneHierarchyContractSettings settings = CreateSettings();

            GameObject sceneGameplayRoot = CreateRootObject(scene, PrototypeSceneHierarchyCatalog.SceneGameplayRootName);
            GameObject sceneSystemRoot = CreateRootObject(scene, PrototypeSceneHierarchyCatalog.SceneSystemRootName);
            GameObject interactionRoot = CreateRootObject(scene, PrototypeSceneHierarchyCatalog.InteractionRootName);
            interactionRoot.transform.SetParent(sceneGameplayRoot.transform, false);

            GameObject storageStation = CreateRootObject(scene, "StorageStation");
            storageStation.transform.SetParent(sceneSystemRoot.transform, false);
            storageStation.SetActive(false);

            GameObject helper = CreateRootObject(scene, "AuthoredHelper");
            helper.transform.SetParent(sceneSystemRoot.transform, false);
            helper.SetActive(false);
            helper.AddComponent<SceneAuthoredHelperContractMarker>();

            settings.CaptureFromSceneObject("Hub", "StorageStation", storageStation.transform, global::Editor.PrototypeSceneHierarchyContractSyncUtility.BuildSceneObjectPath(storageStation.transform));
            settings.CaptureFromSceneObject("Hub", "AuthoredHelper", helper.transform, global::Editor.PrototypeSceneHierarchyContractSyncUtility.BuildSceneObjectPath(helper.transform));

            storageStation.transform.SetParent(interactionRoot.transform, false);
            storageStation.SetActive(true);
            helper.transform.SetParent(interactionRoot.transform, false);
            helper.SetActive(true);

            bool organized = InvokeOrganizer(scene, "Hub", settings);

            Assert.That(organized, Is.True);
            Assert.That(storageStation.transform.parent, Is.SameAs(sceneSystemRoot.transform));
            Assert.That(storageStation.activeSelf, Is.False);
            Assert.That(helper.transform.parent, Is.SameAs(sceneSystemRoot.transform));
            Assert.That(helper.activeSelf, Is.False);
        }

        private static bool InvokeOrganizer(Scene scene, string sceneNameOverride, SceneHierarchyContractSettings settings)
        {
            Type organizerType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException exception)
                    {
                        return exception.Types.Where(type => type != null);
                    }
                })
                .First(type => string.Equals(type?.FullName, "Editor.PrototypeSceneHierarchyOrganizer", StringComparison.Ordinal));

            MethodInfo organizeMethod = organizerType.GetMethod(
                "OrganizeSceneHierarchy",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(Scene), typeof(string), typeof(bool), typeof(SceneHierarchyContractSettings) },
                null);
            Assert.That(organizeMethod, Is.Not.Null);

            return (bool)organizeMethod.Invoke(null, new object[] { scene, sceneNameOverride, false, settings });
        }

        private SceneHierarchyContractSettings CreateSettings()
        {
            SceneHierarchyContractSettings settings = ScriptableObject.CreateInstance<SceneHierarchyContractSettings>();
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
            if (gameObject.scene != scene)
            {
                SceneManager.MoveGameObjectToScene(gameObject, scene);
            }

            cleanupTargets.Add(gameObject);
            return gameObject;
        }
    }
}
