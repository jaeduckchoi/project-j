# 게임 프로젝트 작업 규칙

## 역할

- 이 문서는 모든 에이전트가 따르는 전역 규칙의 정본이다.
- 루트 엔트리(`AGENTS.md`, `CLAUDE.md`)는 짧은 온보딩 맵으로만 유지한다.
- 작업별 세부 진입점은 `GAME_DOCS_INDEX.md`, 실행 절차는 `AGENT_WORKFLOW.md`, 정본 관계는 `SOURCE_OF_TRUTH.md`를 따른다.

## 항상 지킬 것

- 기본 응답 언어는 한국어다.
- generated 결과물만 직접 고치지 말고 생성 경로 또는 정본 코드부터 수정한다.
- 씬에 저장된 월드 직렬화 값은 정본이며, 런타임 보강 코드는 누락분만 보충한다.
- UI 변경은 관리 대상 이름, layout override, editor preview 흐름을 함께 확인한다.
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 결과에 그 사실과 남은 검증 단계를 적는다.

## 구현 원칙

- Windows PowerShell 명령과 텍스트 파일 I/O는 UTF-8 인코딩을 명시한다.
- 수동 편집은 `apply_patch`를 우선 사용한다.
- 문서와 코드가 어긋나면 같은 변경 안에서 문서도 갱신한다.
- 새 지침이 필요하면 먼저 정본 문서 하나를 정하고, 엔트리와 인덱스에는 링크와 짧은 설명만 둔다.
- 코드 스타일과 포맷팅은 자동 수정 가능한 린터, 훅, 검증 명령을 우선한다.
- 폴더/partial 리팩토링은 public 타입명과 네임스페이스를 우선 유지하고, asmdef 범위와 `.meta`를 함께 정리한다.
- 주석은 책임 경계, 정본 관계, partial 역할처럼 탐색 비용을 줄이는 곳에만 추가한다.

## 검증 원칙

- 구조, UI, generated 경로 변경은 정적 검색과 관련 런타임 코드 기준으로 검증한다.
- 구조 리팩토링 뒤에는 옛 경로 문자열이 문서와 코드에 남지 않았는지 확인한다.
- Unity csproj 빌드가 가능한 범위라면 관련 어셈블리를 빌드해 경로 이동 회귀를 먼저 잡는다.
- Unity에서 직접 실행하지 못했으면 미검증 사실을 명시한다.

## 읽기 범위와 제외 기준

Claude, Codex 등 모든 에이전트는 아래 목록을 기본 읽기 대상에서 제외한다. 이 목록을 바꾸면 `.claude/settings.json`과 `.codex/config.toml`도 같은 변경에서 맞춘다.

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

바이너리 원본은 작업에 필수일 때만 읽고, generated 출력물은 결과 자체를 편집하지 않는다.
