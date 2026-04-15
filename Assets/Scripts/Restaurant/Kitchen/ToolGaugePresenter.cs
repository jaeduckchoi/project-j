using System;
using System.Collections.Generic;
using UnityEngine;

namespace Restaurant.Kitchen
{
    public sealed class ToolGaugePresenter : MonoBehaviour
    {
        private static readonly Dictionary<string, Vector3> BaseScales = new(StringComparer.Ordinal);

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

            GameObject gaugeObject = GameObject.Find(gaugeName);
            if (gaugeObject == null)
            {
                return;
            }

            SetTransformProgress(gaugeObject.transform, normalizedProgress);
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
