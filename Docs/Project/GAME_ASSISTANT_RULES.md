# 게임 프로젝트 전역 규칙

이 문서는 모든 에이전트가 따라야 하는 전역 규칙과 읽기 제외 기준의 정본이다.

## 전역 규칙

- 기본 응답 언어는 한국어다.
- generated 결과물 본문만 직접 고치지 말고 생성 경로 또는 정본 코드를 먼저 수정한다. 단, `scene-integrated metadata`처럼 씬 직렬화 계약을 성립시키는 Unity 메타데이터는 [SOURCE_OF_TRUTH.md](SOURCE_OF_TRUTH.md) 기준으로 함께 관리한다.
- 씬에 저장된 직렬화 값과 `authored helper object`는 scene serialization contract의 일부로 본다. 런타임 코드는 누락된 참조와 보조 값만 최소 범위로 보강한다.
- UI 변경은 관리 대상 이름, layout binding, editor preview 경로를 같이 확인한다.
- 문서와 코드의 동작 기준이 함께 바뀌면 같은 변경 안에서 문서도 갱신한다.
- PowerShell 텍스트 I/O와 저장 대상 텍스트 파일은 UTF-8을 기준으로 다룬다.
- Unity 실행이나 컴파일을 직접 확인하지 못했다면 최종 결과에 남은 검증 단계를 적는다.
- 읽기 제외 기준을 바꾸면 `.claude/settings.json`과 `.codex/config.toml`을 같은 변경 안에서 맞춘다.

## 읽기 제외 기준

Claude, Codex 등 모든 에이전트는 아래 목록을 기본 읽기 대상에서 제외한다.

### Unity 캐시, 빌드, 로그 디렉터리

- `Library/`, `Temp/`, `obj/`, `Obj/`
- `Build/`, `Builds/`
- `Logs/`, `UserSettings/`, `MemoryCaptures/`, `Recordings/`, `TestResults/`
- `.utmp/`

### 자동 생성 솔루션, 프로젝트 파일

- `*.csproj`, `*.sln`, `*.slnx`, `*.unityproj`
- `*.suo`, `*.user`, `*.userprefs`, `*.DotSettings.user`

### 로컬 개발 환경, 개인 설정, IDE 로컬

- `*.pdb`, `*.mdb`
- `.vs/`, `.idea/`

### 로그, 아카이브, 출력물

- `*.log`
- `*.apk`, `*.aab`, `*.unitypackage`

### 바이너리 에셋 원본

- 이미지: `*.png`, `*.jpg`, `*.jpeg`, `*.gif`, `*.bmp`, `*.tga`, `*.tif`, `*.tiff`, `*.psd`, `*.exr`, `*.hdr`, `*.ico`, `*.webp`
- 3D: `*.fbx`, `*.obj`, `*.blend`, `*.blend1`, `*.dae`, `*.3ds`, `*.ma`, `*.mb`
- 오디오: `*.wav`, `*.mp3`, `*.ogg`, `*.flac`, `*.aiff`
- 비디오: `*.mp4`, `*.mov`, `*.avi`, `*.webm`
- 폰트: `*.ttf`, `*.otf`, `*.woff`, `*.woff2`
- 드라이브, 아카이브: `*.dll`, `*.so`, `*.dylib`, `*.lib`, `*.exe`, `*.zip`, `*.7z`, `*.rar`, `*.tar`, `*.gz`

바이너리 원본은 작업에 필수일 때만 읽고, generated 출력물 본문은 직접 편집하지 않는다. `scene-integrated metadata` 예외는 [SOURCE_OF_TRUTH.md](SOURCE_OF_TRUTH.md)를 따른다.
