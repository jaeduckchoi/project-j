# 커밋 메시지 템플릿 규칙
# - 모든 커밋 메시지는 한글로 작성한다.
# - 영문 diff 요약이나 자동 생성 초안이 있어도 최종 커밋 메시지는 한글로 다시 작성한다.
# - 첫 줄 형식은 type : subject 로 작성한다.
# - 제목은 50자 이하로 작성하고 마지막에 마침표를 붙이지 않는다.
# - 제목만으로 충분하면 body 와 footer 는 생략할 수 있다.
# - 본문은 제목 아래 한 줄을 비우고 작성한다.
# - 파일 경로, 코드 식별자, 브랜치명처럼 고유 명칭을 제외하면 영문 문장을 그대로 쓰지 않는다.
#
# 사용 가능한 type
# feat    : 새로운 기능 추가
# update  : 기존 기능 수정
# fix     : 버그 수정
# docs    : 문서 및 주석 수정
# design  : CSS 등 UI 디자인 변경
# style   : 오타, 포맷팅, 세미콜론, 띄어쓰기 등 동작 변화 없음
# rename  : 파일명 수정
# delete  : 필요 없는 파일 삭제
# refactor: 코드 리팩토링
# test    : 테스트 코드 추가
# chore   : 빌드 설정, 프로젝트 설정, import, 함수명 수정 등
#
# squash merge
# [squash] hotfix/blabla

type : subject

# body

# footer