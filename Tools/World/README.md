# 월드·플레이어 작성 자료

현재 저장된 Hub·Beach 충돌, 발밑 정렬, 기존 플레이어 걷기의 작성 원본입니다. `Saved/CollisionWork`에서 코드·데이터·필수 검증 기준만 승격했습니다. 원래 로컬 작업 폴더는 이번 승격에서 변경하지 않았습니다.

- `Data/collision_rules.json`: 사람이 작성한 충돌·정렬 규칙
- `Data/render_manifest.json`: 원본 Unity 배치·이미지 ID·변환·해시의 고정 입력
- `configure_collision.py`, `collision_geometry.py`: 충돌 작성과 순수 기하 계산
- `configure_depth_sorting.py`, `depth_sorting.py`: 발밑 기준 화면 정렬
- `build_player_blueprint.py`, `configure_player.py`, `configure_player_walk.py`: 기존 Pawn·프레임·걷기 작성
- `QA/`: 실제 Pawn 이동, 앞뒤 가림, 정적 렌더 검증과 픽셀 분석
- `Fixtures/import_report.json`, `Fixtures/collision_authoring_report.json`: 저장 에셋 읽기 검사의 이전 임포트·충돌 작성 기준 입력
- `Fixtures/source_hashes.json`: 원본 파일 보존 기준; 최신 실행 결과가 아님

경로는 `world_paths.py`와 `world_config.json`에서 정합니다. 프로젝트는 상위 `projectJ.uproject`에서 찾으며 결과는 `Saved/WorldQA`에만 기록합니다. 작성 폴더에 보고서·로그·캡처를 생성하지 않습니다. 원본 manifest의 과거 절대 경로는 출처 기록으로 보존하며, 전체 재임포트 시에는 `relative_path`를 환경 변수 `JONGGU_UNITY_SOURCE` 또는 `unity_source_root`에 연결합니다.

저장된 에셋으로 Play하거나 충돌·정렬 기하 검사를 실행할 때 외부 Unity 프로젝트는 필요하지 않습니다. 전체 원본 이미지 재임포트에만 해당 경로가 필요합니다. 기존 비참조 텍스처도 manifest와 UI 복제의 원본이므로 이 승격을 근거로 삭제하지 않습니다.

프로젝트 루트에서 호스트 기하 검증:

```powershell
python Tools/World/test_collision_geometry.py --manifest Tools/World/Data/render_manifest.json --rules-root Tools/World/Data
python Tools/World/test_depth_sorting.py --manifest Tools/World/Data/render_manifest.json --rules-root Tools/World/Data
```

기존 Beach 모서리 사례는 공통 `Tools/Unreal` 런처의 `Baseline` 작업으로 실행합니다. 그 외 `QA/verify_*`는 소스 상단에 지정한 새 에디터·QA 마커를 사용하며 원본 에셋과 맵을 저장하지 않습니다. `QA/analyze_occlusion_captures.py`, `QA/analyze_static_depth_render.py`의 호스트 픽셀 분석에는 NumPy와 Pillow가 필요합니다.

**전체 마이그레이션은 식당 Build에 포함하지 않습니다.** `import_unreal.py`는 명시적인 `-JongguFullWorldImport`가 있어야 실행됩니다. 이 경로는 두 맵과 원본 소유 액터를 재작성하므로 식당 오버레이와 별도의 검토가 필요합니다. `configure_player.py`의 직접 실행도 이 전체 임포트 경로를 사용합니다. 일반적인 식당 변경은 `Content/Python/jonggu/build.py`를 사용합니다.

`validate_unreal.py`는 이전 원본 임포트 계약을 확인하는 도구입니다. 최신 `Saved/WorldQA/import_report.json`이 없으면 보존한 Fixture를 기준으로 삼으므로 보고서의 기준 해시와 검사 범위를 확인합니다. 현재 식당 기능·저장·맵 연결의 완료 여부는 `jonggu.validation` 검증 결과를 따릅니다.

이전 백업·삭제 도구·마이그레이션 실행기·프로브·로그·이미지는 여기에 복제하지 않았습니다. 역사 자료와 실제 최신 실행 결과를 작성 원본과 구분합니다.
