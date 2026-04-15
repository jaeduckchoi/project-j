# 게임 프로젝트 작업 규칙

## 기본 규칙

- 기본 응답 언어는 한국어다.
- 루트 엔트리 파일(`AGENTS.md`, `CLAUDE.md`)은 맵이고, 세부 규칙 허브는 `.aiassistant/rules/README.md`, 실제 정본 문서는 `Docs/*`다.
- generated 결과물만 직접 고치지 말고 생성 경로 또는 정본 코드부터 수정한다.
- 씬에 저장된 월드 직렬화 값은 정본이며 런타임 보강 코드는 누락분만 보충한다.
- UI 변경은 `UIManager`, `PrototypeUISceneLayoutCatalog`, `PrototypeUISceneLayoutSettings`, `ui-layout-overrides.asset`를 함께 확인한다.
- UI는 런타임 생성에만 의존하지 말고 `PrototypeUIDesignController`와 `UIManager.EditorPreview`를 통해 에디터에서 보이고 조정 가능해야 한다.
- 정적 generated 에셋 생성이나 복구를 빌더에 기대하지 않는다.
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 결과에 그 사실과 남은 검증 단계를 적는다.

## 구현 규칙

- Windows에서는 새 파일을 길게 한 번에 붙이지 말고 안전하게 작성한다.
- 수동 편집은 `apply_patch`를 우선 사용한다.
- 문서와 코드가 어긋나면 같은 변경 안에서 문서도 갱신한다.
- 과거 빌더/감사 흐름을 현재 검증 기준처럼 문서화하지 않는다.
- 폴더/partial 리팩토링은 public 타입명과 네임스페이스를 우선 유지하고, asmdef 범위와 `.meta`를 함께 정리한다.
- UI처럼 family 단위로 나뉜 영역은 엔트리 파일과 역할별 하위 폴더를 구분해 유지한다.
- 관리 대상 Canvas UI 이름 목록은 `PrototypeUISceneLayoutCatalog` 한 곳을 정본으로 두고, `UIManager`나 에디터 도구에 문자열 목록을 다시 복제하지 않는다.
- 주석은 책임 경계, 정본 관계, partial 역할을 설명하는 수준으로만 추가하고, 자명한 구현 설명은 피한다.

## 검증 규칙

- 구조, UI, generated 경로 변경은 정적 검색과 관련 런타임 코드 기준으로 검증한다.
- 구조 리팩토링 뒤에는 옛 경로 문자열이 문서와 코드에 남지 않았는지 정적 검색으로 확인한다.
- Unity csproj 빌드가 가능한 범위라면 관련 어셈블리를 한 번 빌드해 경로 이동 회귀를 먼저 잡는다.
- Unity에서 직접 실행하지 못했으면 미검증 사실을 명시한다.

## 읽기 범위와 제외 기준

Claude, Codex 등 모든 에이전트는 아래 목록을 기본 읽기 대상에서 제외한다. 이 목록은 단일 정본이며, Claude는 `.claude/settings.json`의 `permissions.deny`로 차단하고, Codex는 `.codex/config.toml`의 `project-j-read-scope` permission profile과 `AGENTS.md` 지침으로 동일 범위를 지킨다. Codex의 `.codex/rules/unity-safety.rules`는 샌드박스 밖 명령 승인 정책만 담당한다. Codex 파일시스템 permission profile은 경로 단위 설정이므로 Claude `permissions.deny`의 확장자 glob 전체를 1:1로 대체하지 못하는 범위는 이 문서와 `AGENTS.md` 지침으로 유지한다.

### Unity 캐시/빌드/로그 디렉터리

- `Library/`, `Temp/`, `obj/`, `Obj/`
- `Build/`, `Builds/`
- `Logs/`, `UserSettings/`, `MemoryCaptures/`, `Recordings/`, `TestResults/`
- `.utmp/`

### 자동 생성 솔루션·프로젝트 파일

- `*.csproj`, `*.sln`, `*.slnx`, `*.unityproj`
- `*.suo`, `*.user`, `*.userprefs`, `*.DotSettings.user`

### 디버그 심볼·개인 설정·IDE 로컬

- `*.pdb`, `*.mdb`
- `.git/`, `.vs/`, `.idea/`

### 로그·크래시·배포 산출물

- `*.log`
- `*.apk`, `*.aab`, `*.unitypackage`

### 바이너리 에셋 원본 (authored/imported)

- 이미지: `*.png`, `*.jpg`, `*.jpeg`, `*.gif`, `*.bmp`, `*.tga`, `*.tif`, `*.tiff`, `*.psd`, `*.exr`, `*.hdr`, `*.ico`, `*.webp`
- 3D: `*.fbx`, `*.obj`, `*.blend`, `*.blend1`, `*.dae`, `*.3ds`, `*.ma`, `*.mb`
- 오디오: `*.wav`, `*.mp3`, `*.ogg`, `*.flac`, `*.aiff`
- 비디오: `*.mp4`, `*.mov`, `*.avi`, `*.webm`
- 폰트: `*.ttf`, `*.otf`, `*.woff`, `*.woff2`
- 네이티브/아카이브: `*.dll`, `*.so`, `*.dylib`, `*.lib`, `*.exe`, `*.zip`, `*.7z`, `*.rar`, `*.tar`, `*.gz`

위 바이너리 원본은 작업이 필수로 요구할 때만 읽고, generated 결과물은 결과 자체를 편집하지 말고 생성 경로와 정본 코드부터 수정한다. 이 목록을 바꾸면 `.claude/settings.json`과 `.codex/config.toml`도 같은 변경에서 맞춘다.
