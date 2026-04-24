using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Jonggu.UI")]
[assembly: InternalsVisibleTo("Jonggu.Editor")]
[assembly: InternalsVisibleTo("Jonggu.Gameplay.EditModeTests")]
[assembly: InternalsVisibleTo("Jonggu.Gameplay.PlayModeTests")]

[assembly: SuppressMessage(
    "Style",
    "IDE0130:Namespace does not match folder structure",
    Justification = "Restaurant은 단일 폴더 + 단일 네임스페이스 형태를 의도적으로 유지해 직렬화와 참조 안정성을 보존한다.",
    Scope = "namespace",
    Target = "~N:Restaurant")]
