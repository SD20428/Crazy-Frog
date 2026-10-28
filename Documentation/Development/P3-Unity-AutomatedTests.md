# P3 Unity 자동 테스트: 구현 및 실제 검증

2026-10-29 KST. Unity 6000.3.23f1, Test Framework 1.6.0, Pipeline 0.9.1-exp.1.
기준 커밋 e1accce34914031a4962fa7d4bc10b810e94efb8,
작업 브랜치 `feature/p3-unity-tests`. 사용자에게 PlayMode 스모크 실행과
후속 로컬 커밋 승인을 받았다. GitHub Push는 수행하지 않는다.

**EditMode 12개와 PlayMode 스모크 1개 모두 실제 실행·통과.**
이는 테스트 실행 기반과 에셋 구성을 검증한 결과이며, 전체 게임 기능 회귀 검증은 아직 INCOMPLETE다.

## 1. 신규 파일과 어셈블리 범위

```text
Assets/AutomatedTests/                           (Unity 생성 폴더 .meta 포함)
  EditMode/
    CrazyFrog.AutomatedTests.EditMode.asmdef
    TestAssets.cs
    SceneBuildSettingsTests.cs
    GameConfigAssetTests.cs
    EssentialPrefabTests.cs
  PlayMode/
    CrazyFrog.AutomatedTests.PlayMode.asmdef
    PlayerLoopSmokeTests.cs
Documentation/Development/P3-Unity-AutomatedTests.md
```

각 asmdef/스크립트와 폴더의 .meta는 기존 에디터가 생성했고 보존했다.
EditMode는 Editor 플랫폼 전용이며 UnityEngine.TestRunner,
UnityEditor.TestRunner, nunit.framework.dll을 참조한다. PlayMode는 독립적인
테스트 어셈블리이며 NUnit과 UnityEngine.TestRunner만 참조한다.
둘 다 UNITY_INCLUDE_TESTS 조건을 사용하며 autoReferenced=false다.

Assembly-CSharp는 asmdef 테스트 어셈블리에서 직접 참조할 수 없다.
이번 검사는 AssetDatabase와 SerializedObject로 실제 에셋을 읽으며
런타임 타입 이름만 확인한다. runtime asmdef, asmref, 코드 이동은 없다.
Assets/FrogEscape/Tests/PlaytestDriver.cs는 새 asmdef의 하위 경로가 아니므로
기존 Assembly-CSharp 소속을 유지한다.

## 2. 실제 발견·실행된 EditMode 목록과 실패 조건

모든 테스트의 namespace는 `CrazyFrog.AutomatedTests.EditMode`다.

| Fixture / 실제 메서드 | Case 수 | 검증 / 실패 조건 |
|---|---:|---|
| SceneBuildSettingsTests.RequiredSceneExistsAndIsEnabledExactlyOnce | 5 | MainMenu, Stage01_Overpass, Stage02_Sewer, Stage03_MissileChase, Ending 각각 SceneAsset 로드, Build Settings 단일 등록·활성·GUID 일치. 누락/비활성/중복/불일치 시 실패 |
| SceneBuildSettingsTests.ConfigScenePathsMatchTheActualGameSequence | 1 | GameConfig의 menuScene/endingScene/stageScenes 3개 경로·순서가 실제 게임과 일치. 다른 경로/순서/개수 시 실패 |
| GameConfigAssetTests.BothDifficultySettingsHaveUsableMovementAndTimingValues | 1 | easy/normal의 실제 직렬화 필드, 양의 체력·장애물 간격·속도·시간, 유한값, cooldown≥duration, missileDodgeWindow≤warningTime. 누락/잘못된 수치 시 실패 |
| GameConfigAssetTests.StageDurationsAreFinitePositiveAndMatchStageCount | 1 | stageDurations 3개가 스테이지 수와 일치하고 유한 양수. 0/음수/NaN/Infinity/개수 불일치 시 실패 |
| GameConfigAssetTests.RequiredAudioMaterialsAndTimelinesResolve | 1 | AudioClip 8개, Material 2개, 전환 Timeline 2개와 엔딩 Timeline의 실제 참조/타입. 끊어진 참조·잘못된 타입 시 실패 |
| EssentialPrefabTests.VisualPrefabLoadsWithRequiredPartsAndValidRenderReferences | 3 | FrogBike/PoliceCar/CometMissile 로드, Prefab 자산 확인, 실제 필수 시각 부품·Mesh·Material·Missing Script 검사. 누락/끊어진 참조 시 실패 |

총 **12개**. SampleScene 추가 등록은 허용한다. Prefab은 조사 결과 시각 템플릿이며
게임 컨트롤러는 씬에서 구성하므로, Prefab에 BikeController 등이 없다는 이유로 실패시키지 않는다.
Prefab/씬을 인스턴스화하거나 저장하지 않으며 로드된 설정에도 값을 쓰지 않는다.
이 검사는 게임 실행 중 이동·입력·충돌·연출의 동작을 증명하지 않는다.

## 3. 실행 증거

고유 결과 폴더:

`ValidationResults/P3-20261028T160525780Z-9028df3af0dc41c3983586351089e6de/`

- `baseline.json`: 기준 브랜치·커밋, 신규 소스 시각, 변경 전 외부 백업 위치.
- `import-request.json`: 실제 `compiling` 응답.
- `compile-status.json`: `completed`, failed=false, errors=[], compilationFailed=false.
- `editmode-discovery.json`: 정확한 12개 FullName, 어셈블리, 모드.
- `editmode-run-request.json`: assembly 필터와 mode=editor, async 요청의 실제 응답.
- `editmode-run-status-1.json`: running.
- `editmode-run-status-2.json`: completed, total=12, passed=12, failed=0,
  skipped=0, inconclusive=0, duration=0.47초 및 각 테스트 결과.
- `final-compile-status.json`, `playmode-discovery.json`, `final-editmode-discovery.json`:
  PlayMode 기반 추가 후에도 컴파일 완료, EditMode 12개/PlayMode 1개 발견.
- `final-console-status.json`: 현재 Console 오류·경고 0, 세션과 cursor 포함.
- `p1-link.json`: 변경 없는 P1 실행의 별도 보고서 연결.
- `summary.json`: 최종 집계·원본 보존·어셈블리/씬 증거.

이번에는 단순 `up_to_date`가 아닌 **compiling → completed** 응답과 신규
Library/ScriptAssemblies/CrazyFrog.AutomatedTests.EditMode.dll 생성을 확인했다.
이후 PlayMode 어셈블리도 별도 컴파일 완료했다. P1의 freshCompilation=false는
P1 자체가 컴파일을 수행하지 않았다는 뜻이며 P3의 컴파일 증거와 모순되지 않는다.

현재 MainMenu 씬 한 개가 active/loaded이고 dirty=false다. 저장하지 않았다.
기존 Assets/Packages/ProjectSettings/Tools의 1,701개 파일은 변경 전 새 외부 백업과
SHA-256 비교로 보존을 확인했다. P1 코드·스키마·WSL 래퍼를 수정하지 않았다.
기존 과거 보고서와 백업도 삭제하거나 덮어쓰지 않았다.

별도 승인 후 PlayMode 실행 증거:
`ValidationResults/P3-PlayMode-20261028T162348382Z-03f7ff083e18437eb4ef8a8e91bbf40f/`.
`run-request.json`은 PlayMode 어셈블리만 선택한 비동기 실행 요청이고,
`status-1.json`은 completed, total=1, passed=1, failed/skipped/inconclusive=0,
duration=0.06초를 기록한다. `scenes-after.json`, `memory-after.json` 및
`summary.json`은 기존 MainMenu, 시간/오디오/실행 설정과 임시 자원 정리를 검증한다.
실행 전후 Assets/Packages/ProjectSettings/Tools **1,718개 파일 SHA-256 차이 0**이다.

## 4. 같은 테스트를 다시 실행하는 절차

현재 에디터에 연결하는 CLI만 사용한다. `unity test`로 batch 에디터를 새로 실행하지 않는다.
Unity Test Runner 창에서도 EditMode의 해당 어셈블리를 선택해 실행할 수 있다.

1. `git status`, 기존 에디터 ready/stopped, compiling=false, 씬 dirty=false를 확인한다.
   dirty 또는 사용자의 Play Mode 작업이 있으면 실행하지 않는다.
2. 기존 test status가 running이면 새 실행으로 덮지 말고 먼저 해당 실행을 확인한다.
3. 고유 UTC 시각+GUID의 ValidationResults 하위 폴더를 생성한다. 이전 결과 경로를 재사용하지 않는다.
4. 테스트 변경을 임포트한 뒤 script recompile_status의 완료와 Console을 확인한다.
   `up_to_date`만으로 새 컴파일이 발생했다고 기록하지 않는다.
5. test list에서 예상 12개와 정확한 assembly를 비교한 뒤 아래 EditMode 명령을 실행한다.
6. 요청 응답의 running은 통과가 아니다. test status가 completed일 때까지 한정된 시간 내 확인한다.
   total/passed/failed/skipped/inconclusive 및 실제 FullName을 모두 보존한다.
7. 통신 timeout은 내부 테스트 종료를 의미하지 않는다. 상태 확인 전 재실행하지 않는다.
8. Console, 씬 구성/dirty, Git 원본 변경 여부, P1을 확인한 뒤 종료한다.

설치된 Pipeline 스키마로 확인하고 실제 사용한 명령:

```powershell
$projectPath = 'C:\Users\USER\Desktop\Unity\Crazy Frog'
unity command test list --mode editor --project-path $projectPath --caller plugin --skill unity-cli --format json
unity command test run --mode editor --filter CrazyFrog.AutomatedTests.EditMode --filter_type assembly --async_tests true --project-path $projectPath --caller plugin --skill unity-cli --format json
unity command test status --project-path $projectPath --caller plugin --skill unity-cli --format json
```

stdout의 외부 success와 실제 data.result의 실행 상태/요약을 모두 확인한다.
CLI exit=0만으로 테스트 통과라고 판정하지 않는다. 원시 응답을 실행별 폴더에 저장한다.
Pipeline 내부 Temp/pipeline_test_status.json은 현재 상태 캐시이므로 영구 보고서로 사용하지 않는다.

## 5. PlayMode 기반: 별도 승인 후 실제 통과

발견된 테스트:
`CrazyFrog.AutomatedTests.PlayMode.PlayerLoopSmokeTests.PlayerLoopAdvancesInAnIsolatedTemporaryScene`

컴파일·발견 후 사용자 승인을 받아 **1개 실행·통과**했다.
테스트는 게임 씬이 로드되어 있으면 실패하고, 별도 미저장 임시 Scene과 GameObject에서
두 프레임 진행을 검사한다. UnityTearDown에서 소유한 오브젝트/씬만 정리한다.
입력 장치·timeScale·AudioListener·게임 singleton을 직접 변경하지 않는다.
프레임/씬 해제에는 5초의 실제 시간 제한이 있고, 에디터 자체가 멈추면 코루틴 제한도
동작하지 않으므로 향후 실행 담당자의 별도 timeout/status/cancel 처리가 필요하다.
정상 정리 경로는 실제 검증했다. 시간 초과·강제 중단 경로는 아직 검증하지 않았다.

설치된 Test Framework의 TaskList/CreateBootstrapSceneTask를 확인했다.
PlayMode 실행은 빈 bootstrap scene을 만들고 Assets/InitTestScene<GUID>.unity를
임시 저장한 뒤 종료 시 삭제·기존 씬 구성을 복원한다. 이 과정에서 SaveAssets도 호출한다.
따라서 기존 씬/에셋의 미저장 상태를 확인하고, 종료 후 임시 파일 및 원본 해시·씬 복원을
재검증해야 한다. 이번 정상 실행에서는 임시 파일/객체/씬 잔여 0개와 원본 해시 일치를 확인했다.
실패/중단 시 복원 여부까지 일반화하지 않는다.
Play 진입 자체의 기존 RuntimeInitializeOnLoadMethod(GameManager.ResetStatics)도 호출된다.

실행 전 TMP 셰이더 3개와 LiberationSans.ttf 경로에서 dirty 객체가 발견돼 한 번 중단했다.
추가 조사 결과 셰이더는 NotEditable 임포트 객체, 폰트 경로 객체는 원본 Font가 아닌
생성된 Texture2D 하위 객체였다. 임포터 dirty=false, 원본/메타 해시는 기준과 일치했다.
dirty 표시만으로 사용자 원본 편집이라고 판단할 수 없었다. dirty 플래그를 지우거나
저장·재임포트하지 않은 채 승인된 테스트를 실행했고 전후 파일 해시 일치로 원본 보존을 확인했다.

## 6. 게임 기능 테스트 확장 분석

공통 원칙: 읽기 전용 원본 GameConfig를 직접 수정하지 않고 복제본을 사용한다.
임시 미저장 씬에 필요한 객체만 구성하고 원본 게임 씬을 열거나 저장하지 않는다.
씬 전환 통합 검사는 별도 승인·상태 복원 검토 후 진행한다.

| 시스템 | 입력 → 기대 결과 | 필요한 구성·독립성 / PlayMode | 코드 수정과 전역 상태·실패 복원 |
|---|---|---|---|
| BikeController | Step(0,dt) → 전진; ±입력 반복 → 좌우 이동, lateral 범위 ±(halfWidth-.7), distance≤road.length-100 | GameManager/복제 GameConfig, StageController, RoadPath, visual/rider, DodgeController. Step은 명시 dt지만 Speed/Dodge가 세션·시간에 의존. 좌표 검사는 fixture로 가능, 실제 Keyboard 입력은 PlayMode 필요 | 로직 변경 불필요. 현재 타입 직접 참조는 불가하므로 reflection adapter 또는 승인된 runtime asmdef 선택 필요. GameManager.Awake의 targetFrameRate/runInBackground와 singleton을 기록·복원하고 임시 객체 제거 |
| DodgeController | Playing에서 1회 TryDodge 성공, 재입력 거부, 지속시간 후 비활성, cooldown 후 성공; 비Playing 거부 | 위 세션/Stage + GameAudio/sfx, stageIndex=2이면 MissileController. Time.time에 의존하여 실제 시간 경계는 PlayMode 필요 | 복제 config로 유한한 검사 시간 설정 가능. readyAt만 조작하면 실제 시간 검증과 구분. singleton·시간·오디오 상태 복원, 임시 자원 파괴. 입력 설정은 바꾸지 않거나 별도 승인 |
| PlayerHealth | 연속 2회 피해 → 체력 1회 감소; 무적/회피 중 무시; health=1 피해 → 0/Failed, 음수 방지 | Session, Stage, Bike/Dodge, GameAudio/sfx. TryDamage 직접 호출 가능하나 Time.time과 회피 상태 fixture 필요; 실제 교차 충돌/무적 만료는 PlayMode | 원본 수정 불필요. fixture 세션 health/hits/mode를 분리하고 audio/timeScale/pause 등 기록 상태 복원. 테스트 피해 주입은 자연 플레이와 구분 |
| PoliceAI / 공격 | 간격 과대/과소 → 추격·감속; distance≤player.distance-7; 막힌 차선 회피; 공격 Targeting→Warning→Fire와 풀 재사용 | RoadPath, Bike, ObstacleSpawner 목록·시각 객체, 설정, stageIndex=1의 PoliceAttackController/탄환 풀. Update/Time.deltaTime 기반으로 PlayMode 권장 | 게임 로직 변경 없이 fixture 구성 가능하나 많은 종속 참조 필요. 전역 singleton·난수 사용 시 state·오디오 복원; 만든 탄환/장애물만 정리. Rigidbody 충돌 테스트로 대체하지 않음 |
| StageController | gameplay Start → elapsed=0, Playing, 초기 거리45, 필요한 체력 복구·장애물 설정; title/ending 분기 확인 | GameConfig, player/health/police/attack/missile/camera/cinematic/HUD/audio/obstacles. Awake가 세션 생성하고 Start가 여러 시스템을 호출해 독립성 낮음. lifecycle은 PlayMode 필요 | 코드 수정 없이 완전한 임시 fixture 또는 승인된 씬 통합 실행. 부분 구성으로 Start를 호출하면 NullReference 위험. 세션·씬 구성·카메라·오디오 상태 복원 |
| GameManager | StartGame/Retry → 체력·기록 초기화/현재 stage 재시작; TogglePause → 정지·복귀; Load → Loading→목표 scene | singleton, 복제 config, 로드 가능한 씬과 입력/오디오/카메라 등 실제 연결 필요. 상태 메서드는 fixture 가능, LoadSceneAsync는 PlayMode 통합 검사 | Awake가 DontDestroyOnLoad, targetFrameRate=60, runInBackground=true; Load/OnDestroy가 timeScale/AudioListener.pause 변경. 성공·예외 모두 임시 세션 제거, 기존 값과 원래 씬 구성 복원. 저장·원본 씬 변경은 별도 승인 |

현재 상기 동작은 실행 검증하지 않았다. 이번 설정 검사를 통과했다고 위 게임 기능이
정상이라고 일반화하지 않는다. 기존 PlaytestDriver는 virtual keyboard/전역 입력 설정,
Time.timeScale=3, 체력 주입, 씬 이동, 고정 보고서·스크린샷 덮어쓰기가 있으므로 실행하지 않았다.

## 7. runtime asmdef 도입 영향과 승인 조건

이번 단계에 runtime asmdef는 **필요하지 않아 추가하지 않았다**.
후속 타입 안전 테스트를 원하면 Scripts 폴더 전용 asmdef를 검토할 수 있다.
현재 Scripts는 Input System, Timeline, uGUI/EventSystems, TMPro에 의존하며,
Unity 6 uGUI의 실제 어셈블리 경계까지 확인해 명시 참조를 구성해야 한다.
기존 Editor 생성기와 PlaytestDriver가 해당 런타임 타입을 참조하므로
autoReferenced 설정과 양쪽 컴파일을 함께 검증해야 한다.

이동 시 타입의 어셈블리 정체성이 Assembly-CSharp에서 바뀐다. MonoScript GUID뿐 아니라
실제 에셋의 m_EditorClassIdentifier(현재 Assembly-CSharp::FrogEscape.GameConfig 등),
Timeline clip/track, 기존 prefab/scene 바인딩, 문자열 reflection·저장 데이터 영향도
확인해야 한다. PlaytestDriver까지 감싸는 Assets/FrogEscape 루트 asmdef는 사용하지 않는다.
도입한다면 별도 백업·승인·참조 검증·기존 게임 통합 검사·복원 절차가 필요하다.
대안은 테스트에 한정한 reflection adapter이며, 이것도 수명주기 fixture를 먼저 검토한다.

## 8. P1 연동 계약

P1 스크립트/스키마/종료 코드를 수정하지 않았다. 실제 P1 호출 결과:
EditMode discovered=12, PlayMode discovered=1, 둘 다 execution=not_run,
freshCompilation=false, overall=INCOMPLETE, exit=3. JSON Schema 검증 통과.
P1은 **수집기 자신의 실행 범위**를 기록하므로 외부 P3 결과를 PASS로 합치지 않는다.
P3 summary에는 discovered/executed/passed/failed/skipped를 따로 저장하고,
P1 보고서 경로를 연결해 결과 혼동을 막는다. Console session/cursor 원시 응답도 보존했다.

## 9. 승인 범위와 남은 검증

1. 승인된 PlayMode 스모크 1개 실행과 정상 종료·원상복구 검증은 완료했다.
2. 필요한 경우에만 runtime asmdef/타입 참조 방식 변경: 위 영향 분석 후 별도 승인.
3. 기존 입력 설정·PlaytestDriver·씬 통합 검증 범위 확대: 각각 별도 승인.
4. P3 테스트·문서의 Git add/로컬 커밋은 사용자 승인을 받았다. GitHub Push는 별도 승인 필요.

에디터 추가 실행, 패키지/Build Settings/MCP 변경, Library 삭제는 하지 않았다.
