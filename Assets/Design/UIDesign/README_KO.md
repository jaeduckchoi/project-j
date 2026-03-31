# UI 원본 작업 폴더

`Assets/Design/UIDesign`은 UI를 제작할 때 쓰는 원본 작업 폴더다.
여기는 런타임에 직접 연결되는 자산보다, 편집 가능한 소스와 검토 산출물을 보관하는 용도로 유지한다.

## 하위 폴더

- `Vector`
  SVG 같은 벡터 원본을 둔다.
- `Mockups`
  화면 시안, 배치 비교안, 검토용 이미지와 문서를 둔다.
- `Exports`
  검토용 PNG, PDF 같은 산출물을 둔다.

## 기준

- generated 결과물과 직접 대응되는 디자인 소스는 `Assets/Design/GeneratedSources`로 분리한다.
- `UIDesign`은 편집 가능한 원본과 시안 관리에 집중한다.
- 런타임에서 직접 참조하는 파일은 이 폴더에 두지 않는다.
