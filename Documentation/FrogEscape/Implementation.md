# Frog Escape 구현 기록

## 분석 및 보존
- Unity 6000.3.23f1 / URP 17.3.0 / Input System 1.20.0 (신규 입력만 사용).
- Timeline 1.8.13, uGUI 2.0.0 및 Pipeline 0.9.1-exp.1 설치됨.
- 기존 Assets에는 SampleScene, URP 설정, Readme와 기본 Input Actions만 존재. F1 코드/모델/애니메이션 없음.
- 기존 카메라/차량/UI를 재사용할 대상은 없으며 URP, Input System, Timeline 패키지를 재사용.
- Git 저장소 없음. 변경 전 Assets/Packages/ProjectSettings를 Backups/BeforeFrogEscape.zip에 백업.
- 작업 전 Console: 컴파일 실패 없음, `Maximized serialized file backup not found.` 에디터 레이아웃 오류 1건.

## 구현 구조와 완료 기준
- Assets/FrogEscape에 별도 런타임 코드, 에디터 생성기, 5개 씬, 데이터, 프리팹, Timeline, 자체 합성 오디오.
- 경로 좌표 기반 아케이드 이동/충돌, 상태 머신 기반 경찰/탄환/유도탄. 게임 규칙과 Timeline 연출 분리.
- 한국어 uGUI/TMP UI. Windows의 설치된 맑은 고딕을 DynamicOS로 사용해 폰트 파일 재배포 불필요.
- 원본 SampleScene 및 기존 컨트롤러는 보존. BuildSettings에 게임 씬을 등록할 때 원본 씬도 유지.
- 기본 플레이 약 4분, 스테이지별 재도전, Easy/Normal, 타이틀 복귀, 일시정지.
- 각 단계의 컴파일/Console/Play Mode 검증과 Windows 빌드 결과를 별도 보고서에 기록.
- 초기 모델/배경은 코드로 제작한 오리지널 로우폴리 프리미티브. 외부 캐릭터·원곡 사용 없음.

## 범위
PRD의 3단계 추격과 2개 전환, 엔딩까지 완결되는 로컬 PC 싱글 플레이. 온라인 기능과 정교한 차량 물리는 제외.
