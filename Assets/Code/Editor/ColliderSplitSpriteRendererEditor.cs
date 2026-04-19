using Exploration.World;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// Collider 기반 스프라이트 분할 프리뷰를 수동으로 재생성하거나 정리하는 인스펙터입니다.
    /// </summary>
    [CustomEditor(typeof(ColliderSplitSpriteRenderer))]
    [CanEditMultipleObjects]
    public sealed class ColliderSplitSpriteRendererEditor : UnityEditor.Editor
    {
        /// <summary>
        /// 기본 직렬화 필드와 현재 프리뷰 상태, 수동 실행 버튼을 함께 그립니다.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Split Preview", EditorStyles.boldLabel);

            ColliderSplitSpriteRenderer splitRenderer = (ColliderSplitSpriteRenderer)target;
            EditorGUILayout.LabelField("현재 조각 수", splitRenderer.LastGeneratedPartCount.ToString());
            EditorGUILayout.LabelField("마지막 상태 해시", splitRenderer.LastSplitStateHash.ToString());
            EditorGUILayout.LabelField("프리뷰 활성", splitRenderer.HasActivePreview ? "Yes" : "No");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("프리뷰 재생성"))
                {
                    RunForTargets(item => item.RebuildSplitPreview());
                }

                if (GUILayout.Button("프리뷰 정리"))
                {
                    RunForTargets(item => item.ClearSplitPreview());
                }
            }
        }

        private void RunForTargets(System.Action<ColliderSplitSpriteRenderer> action)
        {
            foreach (Object item in targets)
            {
                if (item is ColliderSplitSpriteRenderer splitRenderer)
                {
                    action(splitRenderer);
                }
            }

            SceneView.RepaintAll();
            Repaint();
        }
    }
}
