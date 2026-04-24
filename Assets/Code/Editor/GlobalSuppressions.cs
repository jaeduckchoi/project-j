#if UNITY_EDITOR
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Jonggu.Gameplay.EditModeTests")]

[assembly: SuppressMessage(
    "Style",
    "IDE0130:Namespace does not match folder structure",
    Justification = "Editor 폴더는 UnityEditor 타입과 충돌을 피하기 위해 ProjectEditor 네임스페이스를 사용한다.",
    Scope = "namespace",
    Target = "~N:ProjectEditor")]

[assembly: SuppressMessage(
    "Style",
    "IDE0130:Namespace does not match folder structure",
    Justification = "Editor/UI 폴더는 프로젝트 규칙에 맞춰 ProjectEditor.UI 네임스페이스를 사용한다.",
    Scope = "namespace",
    Target = "~N:ProjectEditor.UI")]
#endif
