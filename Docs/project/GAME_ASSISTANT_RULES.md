# 게임 프로젝트 작업 규칙

## 역할

- 이 문서는 모든 에이전트가 따라야 하는 전역 규칙의 정본이다.
- 루트 힌트 파일(`AGENTS.md`, `CLAUDE.md`)은 진입 온보딩 맵으로만 유지한다.
- 작업 하네스는 `Docs/README.md`, 작업별 진입점은 `GAME_DOCS_INDEX.md`, 실행 원칙은 `AGENT_WORKFLOW.md`, 정본 관계는 `SOURCE_OF_TRUTH.md`를 따른다.

## 항상 지킬 것

- 기본 응답 언어는 한국어다.
- generated 결과물만 직접 고치지 말고 생성 경로 또는 정본 코드부터 수정한다. 다만 `scene-integrated metadata`처럼 scene/prefab 직렬화 계약을 성립시키는 Unity 메타데이터는 `SOURCE_OF_TRUTH.md` 기준으로 함께 관리한다.
- 씬에 저장된 월드 직렬화 값이 정본이고, 코드로 보강할 때는 최소 범위만 보완한다.
- 씬에 저장된 `authored helper object`가 있으면 그것도 scene serialization contract의 일부로 본다.
- UI 변경은 관리 대상 이름, layout override, editor preview 이름을 함께 확인한다.
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 결과에 남은 검증 단계를 적는다.

## 구현 지침

- Windows PowerShell 명령과 텍스트 파일 I/O는 UTF-8 인코딩을 명시한다.
- 수동 편집은 `apply_patch`를 우선 사용한다.
- 문서와 코드가 함께 바뀌면 같은 변경 안에서 문서도 갱신한다.
- 새 진입점이 필요하면 먼저 정본 문서 하나를 정하고, 관련 하네스나 힌트 파일에서는 링크와 짧은 설명만 둔다.
- 코드 수정과 병행한 자동 수정 가능 리포트, 정적 검증 명령을 우선한다.
- 씬용 에디터 프리뷰/동기화 컴포넌트는 `editor preview dirty contract`를 지켜야 한다. 즉, 씬 로드, 도메인 리로드, `OnValidate`에서 자동으로 scene dirty를 만들지 않고 저장 상태 변경은 명시적 rebuild 같은 사용자 의도가 드러난 경로에서만 일어나야 한다.
- 폴더/partial 리팩토링은 public 타입명과 네임스페이스를 우선 유지하고, asmdef 범위와 `.meta`를 함께 정리한다.
- 주석은 책임 경계, 정본 관계, partial 역할처럼 탐색 비용을 줄이는 곳에 우선 추가한다.
- 새로 만들거나 시그니처를 바꾸는 `class`, `struct`, `interface`, `enum`, `public/internal` 메서드에는 XML 문서 주석을 작성한다.
- `private` 메서드도 partial 경계, 상태 전이, 생성/복구처럼 의도를 바로 드러내야 하는 곳에는 XML 문서 주석을 작성한다.
- 복잡 로직 옆의 인라인 주석은 "무엇을 하는지"보다 "왜 이 순서와 조건이 필요한지"를 설명한다.
- 단순 대입, null 체크, 이벤트 호출의 반복 설명은 주석으로 쓰지 않는다.
- generated 결과물이 아닌 직렬화 값을 직접 고쳐야 한다면, 어떤 정본 관계를 보호하는지 주석 또는 XML 요약을 남긴다.
- `scene-integrated metadata`, scene reference, `authored helper object`를 함께 조정할 때는 세 값의 결합 이유가 문서나 코드에서 추적 가능해야 한다.

## 텍스트 인코딩 규칙

- 저장 대상 텍스트 파일은 UTF-8을 사용한다.
- PowerShell에서 파일을 읽고 쓸 때는 `-Encoding utf8` 또는 .NET UTF-8 API를 명시한다.
- `.editorconfig`와 `.gitattributes`를 통해 기본 인코딩과 줄바꿈을 고정한다.
- 커밋 전 `.githooks/pre-commit`이 UTF-8이 아닌 텍스트 파일과 깨진 문자열 패턴을 검사한다.
- 신규 환경에서는 `powershell -ExecutionPolicy Bypass -File Tools/Encoding/Install-GitHooks.ps1`를 한 번 실행해 hook path를 연결한다.

## 검증 지침

- 구조, UI, generated 경로 변경은 정적 검색과 관련 경로 기준으로 검증한다.
- 구조 리팩토링 뒤에는 옛 경로 문자열이 문서와 코드에 남지 않았는지 확인한다.
- 씬/에디터 프리뷰 동기화 변경은 씬 재오픈 시 dirty 없음, 수동 rebuild 전후 결과 일치, 숨김 여부와 저장 여부가 의도와 맞는지 확인한다.
- Unity csproj build가 가능한 범위면 관련 어셈블리를 먼저 빌드해 경로 이동 누락을 잡는다.
- Unity에서 직접 실행하지 못했다면 미검증 사실을 명시한다.

## 읽기 범위와 제외 기준

Claude, Codex 등 모든 에이전트는 아래 목록을 기본 읽기 대상에서 제외한다. 이 목록을 바꾸면 `.claude/settings.json`과 `.codex/config.toml`을 같은 변경 안에서 맞춘다.

### Unity 캐시/빌드/로그 디렉터리

- `Library/`, `Temp/`, `obj/`, `Obj/`
- `Build/`, `Builds/`
- `Logs/`, `UserSettings/`, `MemoryCaptures/`, `Recordings/`, `TestResults/`
- `.utmp/`

### 자동 생성 솔루션/프로젝트 파일

- `*.csproj`, `*.sln`, `*.slnx`, `*.unityproj`
- `*.suo`, `*.user`, `*.userprefs`, `*.DotSettings.user`

### 로컬 개발 환경/개인 설정/IDE 로컬

- `*.pdb`, `*.mdb`
- `.vs/`, `.idea/`

### 로그/아카이브/출력물

- `*.log`
- `*.apk`, `*.aab`, `*.unitypackage`

### 바이너리 에셋 원본 (authored/imported)

- 이미지: `*.png`, `*.jpg`, `*.jpeg`, `*.gif`, `*.bmp`, `*.tga`, `*.tif`, `*.tiff`, `*.psd`, `*.exr`, `*.hdr`, `*.ico`, `*.webp`
- 3D: `*.fbx`, `*.obj`, `*.blend`, `*.blend1`, `*.dae`, `*.3ds`, `*.ma`, `*.mb`
- 오디오: `*.wav`, `*.mp3`, `*.ogg`, `*.flac`, `*.aiff`
- 비디오: `*.mp4`, `*.mov`, `*.avi`, `*.webm`
- 폰트: `*.ttf`, `*.otf`, `*.woff`, `*.woff2`
- 드라이브/아카이브: `*.dll`, `*.so`, `*.dylib`, `*.lib`, `*.exe`, `*.zip`, `*.7z`, `*.rar`, `*.tar`, `*.gz`

바이너리 원본은 작업에 필수일 때만 읽고, generated 출력물 본문은 직접 편집하지 않는다. `scene-integrated metadata` 예외는 `SOURCE_OF_TRUTH.md`를 따른다.
