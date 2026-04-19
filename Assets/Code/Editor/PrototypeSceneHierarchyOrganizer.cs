#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Code.Scripts.Exploration.World;
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
            return OrganizeSceneHierarchy(scene, sceneNameOverride, saveScene, SceneHierarchyContractSettings.GetCurrent());
        }

        internal static bool OrganizeSceneHierarchy(
            Scene scene,
            string sceneNameOverride,
            bool saveScene,
            SceneHierarchyContractSettings hierarchyContracts)
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
                EnsureGroupTransform(scene, managedSceneName, entry, hierarchyContracts, transformMap, new HashSet<string>(StringComparer.Ordinal));
            }

            transformMap.Clear();
            CollectNamedTransforms(scene, transformMap);

            foreach (PrototypeSceneHierarchyEntry entry in PrototypeSceneHierarchyCatalog.EnumerateLeafEntries(managedSceneName))
            {
                if (!transformMap.TryGetValue(entry.ObjectName, out Transform target) || target == null)
                {
                    continue;
                }

                ApplyEntry(scene, managedSceneName, entry, hierarchyContracts, target, transformMap, treatAsGroup: false);
            }

            ApplyHelperContracts(scene, managedSceneName, hierarchyContracts);

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
            SceneHierarchyContractSettings hierarchyContracts,
            IDictionary<string, Transform> transformMap,
            ISet<string> visiting)
        {
            if (transformMap.TryGetValue(entry.ObjectName, out Transform existing) && existing != null)
            {
                ApplyEntry(scene, managedSceneName, entry, hierarchyContracts, existing, transformMap, treatAsGroup: true);
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

            ApplyEntry(scene, managedSceneName, entry, hierarchyContracts, groupTransform, transformMap, treatAsGroup: true);
            visiting.Remove(entry.ObjectName);
            return groupTransform;
        }

        private static void ApplyEntry(
            Scene scene,
            string managedSceneName,
            PrototypeSceneHierarchyEntry entry,
            SceneHierarchyContractSettings hierarchyContracts,
            Transform target,
            IDictionary<string, Transform> transformMap,
            bool treatAsGroup)
        {
            if (target == null)
            {
                return;
            }

            if (TryGetContractOverride(scene, managedSceneName, entry.ObjectName, target, hierarchyContracts, out SceneHierarchyContractEntry contract, out Transform contractParent))
            {
                if (contractParent == null)
                {
                    target.SetParent(null, !treatAsGroup);
                }
                else if (target.parent != contractParent)
                {
                    target.SetParent(contractParent, !treatAsGroup);
                }

                if (treatAsGroup)
                {
                    target.localPosition = Vector3.zero;
                    target.localRotation = Quaternion.identity;
                    target.localScale = Vector3.one;
                }

                target.SetSiblingIndex(ClampSiblingIndex(target.parent, contract.SiblingIndex));
                target.gameObject.SetActive(contract.InitialActiveSelf);
                transformMap[entry.ObjectName] = target;
                return;
            }

            Transform targetParent = ResolveParent(scene, managedSceneName, entry, hierarchyContracts, transformMap);
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
            SceneHierarchyContractSettings hierarchyContracts,
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
                return EnsureGroupTransform(scene, managedSceneName, parentEntry, hierarchyContracts, transformMap, new HashSet<string>(StringComparer.Ordinal));
            }

            return FindNamedTransform(scene, entry.ParentName);
        }

        private static void ApplyHelperContracts(
            Scene scene,
            string managedSceneName,
            SceneHierarchyContractSettings hierarchyContracts)
        {
            if (hierarchyContracts == null)
            {
                return;
            }

            HashSet<string> managedObjectNames = PrototypeSceneHierarchyCatalog.GetManagedObjectNames(managedSceneName);
            foreach (SceneHierarchyContractEntry contract in hierarchyContracts.EnumerateSceneEntries(managedSceneName))
            {
                if (contract == null
                    || string.IsNullOrWhiteSpace(contract.SceneObjectPath)
                    || managedObjectNames.Contains(contract.ObjectName))
                {
                    continue;
                }

                Transform target = PrototypeSceneHierarchyContractSyncUtility.ResolveSceneTransform(scene, contract.SceneObjectPath);
                if (target == null || target.GetComponent<SceneAuthoredHelperContractMarker>() == null)
                {
                    continue;
                }

                Transform targetParent = string.IsNullOrWhiteSpace(contract.ParentScenePath)
                    ? null
                    : PrototypeSceneHierarchyContractSyncUtility.ResolveSceneTransform(scene, contract.ParentScenePath);
                if (!string.IsNullOrWhiteSpace(contract.ParentScenePath) && targetParent == null)
                {
                    continue;
                }

                if (targetParent == null)
                {
                    target.SetParent(null, true);
                }
                else if (target.parent != targetParent)
                {
                    target.SetParent(targetParent, true);
                }

                target.SetSiblingIndex(ClampSiblingIndex(target.parent, contract.SiblingIndex));
                target.gameObject.SetActive(contract.InitialActiveSelf);
            }
        }

        private static bool TryGetContractOverride(
            Scene scene,
            string managedSceneName,
            string objectName,
            Transform target,
            SceneHierarchyContractSettings hierarchyContracts,
            out SceneHierarchyContractEntry contract,
            out Transform contractParent)
        {
            contract = null;
            contractParent = null;
            if (hierarchyContracts == null || string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            string sceneObjectPath = PrototypeSceneHierarchyContractSyncUtility.BuildSceneObjectPath(target);
            if (!string.IsNullOrWhiteSpace(sceneObjectPath)
                && hierarchyContracts.TryGetEntry(managedSceneName, objectName, sceneObjectPath, out contract))
            {
                return TryResolveContractParent(scene, contract, out contractParent);
            }

            if (hierarchyContracts.TryGetEntry(managedSceneName, objectName, out contract))
            {
                return TryResolveContractParent(scene, contract, out contractParent);
            }

            return false;
        }

        private static bool TryResolveContractParent(Scene scene, SceneHierarchyContractEntry contract, out Transform contractParent)
        {
            if (contract == null)
            {
                contractParent = null;
                return false;
            }

            if (string.IsNullOrWhiteSpace(contract.ParentScenePath))
            {
                contractParent = null;
                return true;
            }

            contractParent = PrototypeSceneHierarchyContractSyncUtility.ResolveSceneTransform(scene, contract.ParentScenePath);
            return contractParent != null;
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
