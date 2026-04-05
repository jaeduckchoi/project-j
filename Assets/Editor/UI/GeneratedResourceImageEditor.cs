#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Image = UnityEngine.UI.Image;

// ProjectEditor.UI 네임스페이스
namespace Editor.UI
{
    /// <summary>
    /// Image 인스펙터 아래에 generated UI 스프라이트 추천과 Resources 미러 전환 도구를 붙인다.
    /// 기본 Unity Image 인스펙터는 유지하고, 이 프로젝트 규칙에 맞는 보조 패널만 추가한다.
    /// </summary>
    [CustomEditor(typeof(Image), true)]
    [CanEditMultipleObjects]
    public class GeneratedResourceImageEditor : UnityEditor.UI.ImageEditor
    {
        private const string GeneratedSpriteRoot = "Assets/Generated/";
        private const string ResourceGeneratedSpriteRoot = "Assets/Resources/Generated/";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawGeneratedSpriteHelper();
        }

        /// <summary>
        /// 현재 Image가 generated UI 규칙과 연결되는 경우에만 보조 패널을 그린다.
        /// </summary>
        private void DrawGeneratedSpriteHelper()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generated Sprite Helper", EditorStyles.boldLabel);

            if (targets == null || targets.Length != 1)
            {
                EditorGUILayout.HelpBox("추천 sprite 확인과 자동 교체는 Image 하나를 선택했을 때만 보여줍니다.", MessageType.Info);
                return;
            }

            Image image = target as Image;
            if (image == null)
            {
                return;
            }

            string currentAssetPath = GetAssetPath(image.sprite);

            bool hasMirroredResourceSprite = TryGetMirroredResourceSprite(
                image.sprite,
                out Sprite mirroredResourceSprite,
                out string mirroredResourceAssetPath);

            if (!hasMirroredResourceSprite)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                "`Assets/Generated` 스프라이트를 직접 참조 중입니다. `Assets/Resources/Generated` 미러 자산으로 교체할 수 있습니다.",
                MessageType.Warning);

            DrawPathField("현재 Source Image", string.IsNullOrWhiteSpace(currentAssetPath) ? "(비어 있음)" : currentAssetPath);
            DrawPathField("미러 Asset", mirroredResourceAssetPath);

            if (GUILayout.Button("Resources 버전으로 교체"))
            {
                ApplySpriteToImage(image, mirroredResourceSprite);
            }
        }

        /// <summary>
        /// 현재 Source Image가 Assets/Generated 쪽을 가리키면 Resources 미러 자산을 찾는다.
        /// </summary>
        private static bool TryGetMirroredResourceSprite(Sprite sprite, out Sprite mirroredSprite, out string mirroredAssetPath)
        {
            mirroredSprite = null;
            mirroredAssetPath = null;

            string currentAssetPath = GetAssetPath(sprite);
            if (!IsProjectGeneratedAssetPath(currentAssetPath))
            {
                return false;
            }

            mirroredAssetPath = ResourceGeneratedSpriteRoot + currentAssetPath.Substring(GeneratedSpriteRoot.Length);
            mirroredSprite = AssetDatabase.LoadAssetAtPath<Sprite>(mirroredAssetPath);
            return mirroredSprite != null;
        }

        /// <summary>
        /// 실제 Image에 sprite를 반영하고 씬 dirty 상태를 갱신한다.
        /// </summary>
        private static void ApplySpriteToImage(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            Undo.RecordObject(image, "Apply Generated UI Sprite");
            image.sprite = sprite;

            if (sprite != null)
            {
                if (sprite.border.sqrMagnitude > 0f)
                {
                    image.type = Image.Type.Sliced;
                }
                else if (image.type == Image.Type.Sliced)
                {
                    image.type = Image.Type.Simple;
                }
            }

            image.DisableSpriteOptimizations();
            image.SetAllDirty();
            EditorUtility.SetDirty(image);

            if (image.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(image.gameObject.scene);
            }
        }

        private static string GetAssetPath(Sprite sprite)
        {
            return sprite == null ? null : NormalizeAssetPath(AssetDatabase.GetAssetPath(sprite));
        }

        private static bool IsProjectGeneratedAssetPath(string assetPath)
        {
            return !string.IsNullOrWhiteSpace(assetPath)
                   && assetPath.StartsWith(GeneratedSpriteRoot, StringComparison.Ordinal);
        }

        private static string NormalizeAssetPath(string assetPath)
        {
            return string.IsNullOrWhiteSpace(assetPath) ? assetPath : assetPath.Replace('\\', '/');
        }

        private static void DrawPathField(string label, string value)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(label, value);
            }
        }
    }
}
#endif
