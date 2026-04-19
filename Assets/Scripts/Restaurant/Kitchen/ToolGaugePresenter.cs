using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Restaurant.Kitchen
{
    public sealed class ToolGaugePresenter : MonoBehaviour
    {
        private static readonly Dictionary<string, Vector3> BaseScales = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Transform> SceneGaugeTransforms = new(StringComparer.Ordinal);

        [SerializeField] private KitchenToolType toolType;
        [SerializeField] private Transform fillRoot;

        private void Awake()
        {
            if (fillRoot == null)
            {
                fillRoot = transform;
            }
        }

        public void SetProgress(float normalizedProgress)
        {
            SetTransformProgress(fillRoot, normalizedProgress);
        }

        public static void SetSceneGaugeProgress(KitchenToolType toolType, float normalizedProgress)
        {
            string gaugeName = toolType switch
            {
                KitchenToolType.CuttingBoard => "CuttingBoardGauge",
                KitchenToolType.Pot => "PotGauge",
                KitchenToolType.FryingPan => "FryingPanGauge",
                KitchenToolType.Fryer => "FryerGauge",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(gaugeName))
            {
                return;
            }

            Transform gaugeTransform = FindSceneGaugeTransform(gaugeName);
            if (gaugeTransform == null)
            {
                return;
            }

            SetTransformProgress(gaugeTransform, normalizedProgress);
        }

        /// <summary>
        /// 매 프레임 이름 검색을 반복하지 않도록 로드된 씬의 게이지 Transform을 캐시합니다.
        /// </summary>
        private static Transform FindSceneGaugeTransform(string gaugeName)
        {
            if (SceneGaugeTransforms.TryGetValue(gaugeName, out Transform cachedTransform)
                && cachedTransform != null
                && cachedTransform.gameObject.scene.IsValid()
                && cachedTransform.gameObject.scene.isLoaded)
            {
                return cachedTransform;
            }

            GameObject gaugeObject = FindSceneObjectByName(gaugeName);
            if (gaugeObject == null)
            {
                SceneGaugeTransforms.Remove(gaugeName);
                return null;
            }

            SceneGaugeTransforms[gaugeName] = gaugeObject.transform;
            return gaugeObject.transform;
        }

        /// <summary>
        /// 활성 상태에 의존하지 않고 로드된 씬 안에서 이름이 일치하는 게이지 오브젝트를 찾습니다.
        /// </summary>
        private static GameObject FindSceneObjectByName(string objectName)
        {
            GameObject[] sceneObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (GameObject candidate in sceneObjects)
            {
                if (candidate == null
                    || !candidate.scene.IsValid()
                    || !candidate.scene.isLoaded
                    || !string.Equals(candidate.name, objectName, StringComparison.Ordinal))
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        private static void SetTransformProgress(Transform target, float normalizedProgress)
        {
            if (target == null)
            {
                return;
            }

            string key = target.GetInstanceID().ToString();
            if (!BaseScales.ContainsKey(key))
            {
                BaseScales[key] = target.localScale;
            }

            Vector3 baseScale = BaseScales[key];
            float clamped = Mathf.Clamp01(normalizedProgress);
            target.localScale = new Vector3(Mathf.Max(0.001f, baseScale.x * clamped), baseScale.y, baseScale.z);
        }
    }
}
