using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Style",
    "IDE0130:Namespace does not match folder structure",
    Justification = "공용 데이터는 Shared 기능 폴더 아래에 두되 기존 Data 네임스페이스를 유지한다.",
    Scope = "namespace",
    Target = "~N:Data")]
