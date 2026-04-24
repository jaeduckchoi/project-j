using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Jonggu.Editor")]
[assembly: InternalsVisibleTo("Jonggu.Gameplay.EditModeTests")]

[assembly: SuppressMessage(
    "Style",
    "IDE0130:Namespace does not match folder structure",
    Justification = "UI는 상위 구조가 바뀌어도 기존 네임스페이스를 유지해 직렬화와 참조를 안정적으로 보존한다.",
    Scope = "namespace",
    Target = "~N:UI")]

[assembly: SuppressMessage(
    "Style",
    "IDE0130:Namespace does not match folder structure",
    Justification = "UI는 상위 구조가 바뀌어도 기존 네임스페이스를 유지해 직렬화와 참조를 안정적으로 보존한다.",
    Scope = "namespace",
    Target = "~N:UI.Content")]

[assembly: SuppressMessage(
    "Style",
    "IDE0130:Namespace does not match folder structure",
    Justification = "UI는 상위 구조가 바뀌어도 기존 네임스페이스를 유지해 직렬화와 참조를 안정적으로 보존한다.",
    Scope = "namespace",
    Target = "~N:UI.Controllers")]

[assembly: SuppressMessage(
    "Style",
    "IDE0130:Namespace does not match folder structure",
    Justification = "UI는 상위 구조가 바뀌어도 기존 네임스페이스를 유지해 직렬화와 참조를 안정적으로 보존한다.",
    Scope = "namespace",
    Target = "~N:UI.Layout")]

[assembly: SuppressMessage(
    "Style",
    "IDE0130:Namespace does not match folder structure",
    Justification = "UI는 상위 구조가 바뀌어도 기존 네임스페이스를 유지해 직렬화와 참조를 안정적으로 보존한다.",
    Scope = "namespace",
    Target = "~N:UI.Style")]
