#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Exploration.World;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor
{
    /// <summary>
    /// 지원 씬의 월드 Hierarchy를 공통 그룹 규칙에 맞게 정렬합니다.
    /// 빌더 보강과 내부 유지보수 경로가 이미 저장된 씬과 같은 구조를 유지하도록 에디터에서만 사용합니다.
    /// </summary>
    internal static class PrototypeSceneHierarchyOrganizer
    {
        internal static bool OrganizeSceneHierarchy(Scene scene, bool saveScene)
        {
            return OrganizeSceneHierarchy(scene, null, saveScene);
        }

        internal static bool OrganizeSceneHierarchy(Scene scene, string sceneNameOverride, bool saveScene)
        {
            string managedSceneName = ResolveManagedSceneName(scene, sceneNameOverride);
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(managedSceneName))
            {
                return false;
            }

            Dictionary<string, Transform> transformMap = new(StringComparer.Ordinal);
            CollectNamedTransforms(scene, transformMap);

            foreach (PrototypeSceneHierarchyEntry entry in PrototypeSceneHierarchyCatalog.EnumerateGroupEntries(managedSceneName))
            {
                EnsureGroupTransform(scene, managedSceneName, entry, transformMap, new HashSet<string>(StringComparer.Ordinal));
            }

            transformMap.Clear();
            CollectNamedTransforms(scene, transformMap);

            foreach (PrototypeSceneHierarchyEntry entry in PrototypeSceneHierarchyCatalog.EnumerateLeafEntries(managedSceneName))
            {
                if (!transformMap.TryGetValue(entry.ObjectName, out Transform target) || target == null)
                {
                    continue;
                }

                ApplyEntry(scene, managedSceneName, entry, target, transformMap, treatAsGroup: false);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (saveScene && !string.IsNullOrWhiteSpace(scene.path))
            {
                EditorSceneManager.SaveScene(scene);
            }

            return true;
        }

        private static Transform EnsureGroupTransform(
            Scene scene,
            string managedSceneName,
            PrototypeSceneHierarchyEntry entry,
            IDictionary<string, Transform> transformMap,
            ISet<string> visiting)
        {
            if (transformMap.TryGetValue(entry.ObjectName, out Transform existing) && existing != null)
            {
                ApplyEntry(scene, managedSceneName, entry, existing, transformMap, treatAsGroup: true);
                return existing;
            }

            if (visiting.Contains(entry.ObjectName))
            {
                return null;
            }

            visiting.Add(entry.ObjectName);

            GameObject groupObject = new(entry.ObjectName);
            SceneManager.MoveGameObjectToScene(groupObject, scene);
            Transform groupTransform = groupObject.transform;
            transformMap[entry.ObjectName] = groupTransform;

            ApplyEntry(scene, managedSceneName, entry, groupTransform, transformMap, treatAsGroup: true);
            visiting.Remove(entry.ObjectName);
            return groupTransform;
        }

        private static void ApplyEntry(
            Scene scene,
            string managedSceneName,
            PrototypeSceneHierarchyEntry entry,
            Transform target,
            IDictionary<string, Transform> transformMap,
            bool treatAsGroup)
        {
            if (target == null)
            {
                return;
            }

            Transform targetParent = ResolveParent(scene, managedSceneName, entry, transformMap);
            if (targetParent == null)
            {
                target.SetParent(null, !treatAsGroup);
            }
            else if (target.parent != targetParent)
            {
                target.SetParent(targetParent, !treatAsGroup);
            }

            if (treatAsGroup)
            {
                target.localPosition = Vector3.zero;
                target.localRotation = Quaternion.identity;
                target.localScale = Vector3.one;
            }

            target.SetSiblingIndex(ClampSiblingIndex(target.parent, entry.SiblingIndex));
            transformMap[entry.ObjectName] = target;
        }

        private static Transform ResolveParent(
            Scene scene,
            string managedSceneName,
            PrototypeSceneHierarchyEntry entry,
            IDictionary<string, Transform> transformMap)
        {
            if (string.IsNullOrWhiteSpace(entry.ParentName))
            {
                return null;
            }

            if (transformMap.TryGetValue(entry.ParentName, out Transform parent) && parent != null)
            {
                return parent;
            }

            if (PrototypeSceneHierarchyCatalog.IsGroupObject(managedSceneName, entry.ParentName)
                && PrototypeSceneHierarchyCatalog.TryGetEntry(managedSceneName, entry.ParentName, out PrototypeSceneHierarchyEntry parentEntry))
            {
                return EnsureGroupTransform(scene, managedSceneName, parentEntry, transformMap, new HashSet<string>(StringComparer.Ordinal));
            }

            return FindNamedTransform(scene, entry.ParentName);
        }

        private static string ResolveManagedSceneName(Scene scene, string sceneNameOverride)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(sceneNameOverride)
                && PrototypeSceneHierarchyCatalog.IsSupportedScene(sceneNameOverride))
            {
                return sceneNameOverride;
            }

            return PrototypeSceneHierarchyCatalog.IsSupportedScene(scene.name)
                ? scene.name
                : null;
        }

        private static void CollectNamedTransforms(Scene scene, IDictionary<string, Transform> transformMap)
        {
            if (transformMap == null)
            {
                return;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                CollectNamedTransforms(root.transform, transformMap);
            }
        }

        private static void CollectNamedTransforms(Transform current, IDictionary<string, Transform> transformMap)
        {
            if (current == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(current.name) && !transformMap.ContainsKey(current.name))
            {
                transformMap[current.name] = current;
            }

            for (int index = 0; index < current.childCount; index++)
            {
                CollectNamedTransforms(current.GetChild(index), transformMap);
            }
        }

        private static Transform FindNamedTransform(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                Transform found = FindNamedTransform(root.transform, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindNamedTransform(Transform current, string objectName)
        {
            if (current == null || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            if (string.Equals(current.name, objectName, StringComparison.Ordinal))
            {
                return current;
            }

            for (int index = 0; index < current.childCount; index++)
            {
                Transform found = FindNamedTransform(current.GetChild(index), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static int ClampSiblingIndex(Transform parent, int siblingIndex)
        {
            if (parent == null)
            {
                return Mathf.Max(0, siblingIndex);
            }

            return Mathf.Clamp(siblingIndex, 0, Mathf.Max(0, parent.childCount - 1));
        }
    }
}
#endif
