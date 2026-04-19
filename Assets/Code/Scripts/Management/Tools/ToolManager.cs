using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Code.Scripts.Management.Tools
{
    /// <summary>
    /// 인벤토리와 별개로 상시 해금된 도구 목록을 관리한다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "Tools", sourceAssembly: "Assembly-CSharp", sourceClassName: "ToolManager")]
    public class ToolManager : MonoBehaviour
    {
        [SerializeField] private List<ToolType> startingUnlockedTools = new()
        {
            ToolType.FishingRod,
            ToolType.Rake,
            ToolType.Sickle
        };

        [SerializeField] private List<ToolType> runtimeUnlockedTools = new();

        private readonly HashSet<ToolType> unlockedTools = new();
        private bool initialized;

        public event Action ToolsChanged;

        public IReadOnlyList<ToolType> RuntimeUnlockedTools => runtimeUnlockedTools;

        /// <summary>
        /// 시작 도구를 한 번만 해시셋에 적재하고 런타임 목록을 갱신한다.
        /// </summary>
        public void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            unlockedTools.Clear();

            foreach (ToolType toolType in startingUnlockedTools)
            {
                if (toolType == ToolType.None)
                {
                    continue;
                }

                unlockedTools.Add(toolType);
            }

            RefreshRuntimeTools();
            ToolsChanged?.Invoke();
        }

        /// <summary>
        /// 해당 도구가 이미 해금되었는지 확인한다.
        /// </summary>
        public bool HasTool(ToolType toolType)
        {
            InitializeIfNeeded();
            return toolType == ToolType.None || unlockedTools.Contains(toolType);
        }

        /// <summary>
        /// 새 도구를 해금하고 변경 이벤트를 보낸다.
        /// </summary>
        public bool UnlockTool(ToolType toolType)
        {
            if (toolType == ToolType.None)
            {
                return false;
            }

            InitializeIfNeeded();
            if (!unlockedTools.Add(toolType))
            {
                return false;
            }

            RefreshRuntimeTools();
            ToolsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// UI 표시용 직렬화 목록을 정렬된 상태로 다시 만든다.
        /// </summary>
        private void RefreshRuntimeTools()
        {
            runtimeUnlockedTools.Clear();

            foreach (ToolType toolType in unlockedTools)
            {
                runtimeUnlockedTools.Add(toolType);
            }

            runtimeUnlockedTools.Sort((left, right) => left.CompareTo(right));
        }
    }
}