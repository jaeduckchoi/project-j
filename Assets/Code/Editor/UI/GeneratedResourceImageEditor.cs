#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UI;

// ProjectEditor.UI 네임스페이스
namespace Editor.UI
{
    /// <summary>
    /// Unity 기본 Image 인스펙터를 그대로 사용한다.
    /// 레거시 generated 스프라이트 교체 보조 패널은 더 이상 사용하지 않는다.
    /// </summary>
    [CustomEditor(typeof(Image), true)]
    [CanEditMultipleObjects]
    public sealed class GeneratedResourceImageEditor : UnityEditor.UI.ImageEditor
    {
    }
}
#endif
