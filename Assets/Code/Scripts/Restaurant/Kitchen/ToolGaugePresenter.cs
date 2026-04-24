using System;
using System.Collections.Generic;
using Code.Scripts.CoreLoop.Core;
using UnityEngine;

namespace Code.Scripts.Restaurant.Kitchen
{
    /// <summary>
    /// 허브 씬에 직렬화된 특정 조리기구 게이지의 진행률 표시를 담당한다.
    /// </summary>
    public sealed class ToolGaugePresenter : MonoBehaviour
    {
        private static readonly Dictionary<string, Vector3> BaseScales = new(StringComparer.Ordinal);

        [SerializeField] private KitchenToolType toolType;
        [SerializeField] private Transform fillRoot;

        /// <summary>
        /// 이 프리젠터가 담당하는 조리기구 타입이다.
        /// </summary>
        public KitchenToolType ToolType => toolType;

        private void Awake()
        {
            if (fillRoot == null)
            {
                fillRoot = transform;
            }
        }

        /// <summary>
        /// 이 프리젠터가 담당하는 게이지를 직접 갱신한다.
        /// </summary>
        public void SetProgress(float normalizedProgress)
        {
            SetTransformProgress(fillRoot, normalizedProgress);
        }

        /// <summary>
        /// 현재 허브 컨텍스트에 직렬화된 게이지 프리젠터를 찾아 진행률을 갱신한다.
        /// </summary>
        public static void SetSceneGaugeProgress(KitchenToolType toolType, float normalizedProgress)
        {
            HubRuntimeContext hubContext = GameRuntimeAccess.HubContext;
            if (hubContext == null
                || !hubContext.TryGetGaugePresenter(toolType, out ToolGaugePresenter presenter))
            {
                return;
            }

            presenter.SetProgress(normalizedProgress);
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
