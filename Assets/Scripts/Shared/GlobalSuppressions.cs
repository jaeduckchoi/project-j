using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Style",
    "IDE0130:Namespace does not match folder structure",
    Justification = "공용 데이터는 Shared 어셈블리 아래에서 Shared.Data 네임스페이스를 사용한다. Unity가 RootNamespace를 비워 두기 때문에 IDE0130을 명시적으로 억제한다.",
    Scope = "namespace",
    Target = "~N:Shared.Data")]
