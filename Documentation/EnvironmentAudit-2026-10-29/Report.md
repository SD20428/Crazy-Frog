# Crazy Frog 환경 점검 및 자동 검증 설계

점검 기준일: 2026-10-29 KST. 이 문서와 같은 폴더의 JSON은 이번 점검 증거다. 이전 `Documentation/FrogEscape` 기록과 구분한다.

## 1. 현재 상태

- 프로젝트: `C:\Users\USER\Desktop\Unity\Crazy Frog`.
- ProjectVersion과 실행 중 에디터 모두 Unity **6000.3.23f1**. Pipeline 포트 7800, main PID 36944, 상태 ready.
- Unity.exe 프로세스 중 나머지 두 개는 AssetImportWorker이며 추가 대화형 에디터가 아니다. 이번 점검에서 에디터를 실행하지 않았다.
- Windows CLI `C:\Users\USER\AppData\Local\Unity\bin\unity.exe`, 버전 **1.0.0-beta.14**. 업데이트 조회도 latestVersion이 동일했다. 설치/업데이트하지 않았다.
- Pipeline **0.9.1-exp.1**, Test Framework **1.6.0**, URP **17.3.0**, Input System **1.20.0**, Timeline **1.8.13**, uGUI **2.0.0**.
- MainMenu 하나가 열려 있고 active/loaded, dirty=false. Play Mode stopped.
- `git rev-parse --show-toplevel`은 저장소 없음으로 실패. 초기화하지 않았다. `Backups/BeforeFrogEscape.zip`은 기존 백업이며 이번 작업에서 복원/신선도 검증하지 않았다.
- 기본 샌드박스에서는 localhost/WSL/CIM 접근이 제한되었다. 허용된 별도 조회에서 연결 성공했으므로 초기 unreachable을 실제 에디터 고장으로 판정하지 않는다.

## 2. 실제 검증 결과

| 항목 | 결과 | 해석 |
|---|---|---|
| 프로젝트/실행 버전 확인 | 성공 | 디스크와 실행 중 에디터 일치 |
| CLI → 기존 에디터 | 성공 | 상태·명령 목록·Console·씬·테스트 검색 응답 수신 |
| Console | 오류 0, 경고 0 | `console-status.json`, compilationFailed=false, compiling=false |
| 컴파일 확인 요청 | 성공, up_to_date | `recompile.json`: failed=false, 오류/경고 배열 비어 있음. 새 전체 재컴파일을 수행했다는 뜻은 아님 |
| EditMode 테스트 검색 | 성공, 0개 | 실행 가능한 테스트가 검색되지 않음. 테스트 실행/통과 아님 |
| PlayMode 테스트 검색 | 성공, 0개 | 위와 동일 |
| 기존 PlaytestDriver 재실행 | 미실행 | 입력 설정·가상 키보드·씬·시간 배율을 바꾸고 기존 JSON/스크린샷을 덮어씀 |
| 빌드/독립 exe/주행·차량 회귀 | 미검증 | 이번 점검에서 실행하지 않음 |
| WSL에서 Editor.log 읽기 | 성공 | stat 및 지정 오류 패턴 검색 수행, 검색 결과 없음. 모든 종류의 경고/오류가 없다는 증명은 아님 |
| cfcheck.sh 전체 실행 | 미실행 | 자동 승인 검토에서 거부 |
| MCP initialize/tools/list | 미검증 | 별도 프로토콜 점검 프로세스가 자동 승인 검토에서 거부됨 |
| 원본 보존 | 성공 | Assets/Packages/ProjectSettings 1,695개 파일 SHA-256 전후 동일. 추가/삭제/내용 차이 0 |

현재 Console 카운트는 현재 세션 상태다. 이전 오류가 지워졌거나 아직 실행하지 않은 경로의 런타임 오류까지 부정하지 않는다. CLI 컴파일 요약의 `compilationFailed`는 null이지만 Console groundTruth의 동명 값은 false였다.

기존 `Playtest-Easy.json`, `Playtest-Normal.json`은 파일 수정시각 2026-10-28의 기록이며 각각 PASS, checks=34, failures=0이다. 이번 실행 결과로 계산하지 않았다. 기존 보고서의 배포 빌드 성공도 이번 검증과 별개다.

## 3. 명령 및 MCP 조사

실행 중 에디터에서 **37개 그룹, 166개 명령**을 조회했다. 전체 설명·인자·스키마는 `command-inventory.json`에 보존했다.

| 용도 | 실제 발견된 명령 |
|---|---|
| 상태/씬 조회 | `editor status`, `scene list_open`, `scene get_hierarchy` |
| 로그 | `console status`, `console get` (level/tail/since/since_session) |
| 컴파일 | `script recompile`, `script recompile_status`; CLI `unity recompile` |
| 테스트 | `test list`, `test run`, `test status`, `test cancel` |
| 진단 | `diagnostics get_performance`, `diagnostics run_audit`, `diagnostics audit_status` |
| 구현/조작 가능 영역 | script, asset, component, gameobject, prefab, scene, material, animation, settings 등 |
| 실행/배포 가능 영역 | editor.playmode, capture, build, build.target, job 등 |

발견된 명령이 모두 안전한 읽기 전용 명령이라는 뜻은 아니다. 이번에는 생성/저장/설정 변경/빌드/Play Mode 진입 명령을 사용하지 않았다. Project Auditor 실행 가능성 및 패키지 유무도 검증하지 않았다.

로컬 unity-cli 스킬 문서는 `unity mcp`가 CLI와 같은 Pipeline 명령을 stdio MCP 도구로 노출한다고 설명한다. 이번 Codex 도구 카탈로그에서는 직접 호출할 Unity 도구가 발견되지 않았다. 따라서 **CLI/Pipeline 연결은 확인했지만, 현재 Codex 세션의 Unity MCP 직접 호출 성공은 확인하지 못했다.** CLI에서 조사한 166개를 현재 MCP 세션의 실제 tools/list 결과로 표현하면 안 된다. MCP 설정 파일과 연결 등록은 수정하지 않았다.

향후 대상 고정 예시:

```powershell
$projectPath = 'C:\Users\USER\Desktop\Unity\Crazy Frog'
unity status --project-path $projectPath --format json
unity command console status --project-path $projectPath --caller plugin --skill unity-cli --format json
unity command test list --mode editor --project-path $projectPath --caller plugin --skill unity-cli --format json
unity command test list --mode playmode --project-path $projectPath --caller plugin --skill unity-cli --format json
unity recompile --project-path $projectPath --timeout 60 --format json
```

위 코드 중 recompile은 컴파일 검증 요청이며 순수 상태 조회와 구분한다. 테스트 모드 인자는 Pipeline 명령에서 `editor`/`playmode`다. 별도 CLI `unity test`와 혼동하지 않는다. 현재처럼 에디터가 열린 공유 프로젝트에서 `unity test`, `unity run`, `unity build` 등 새 batch 에디터를 시작할 수 있는 경로는 사용하지 않는다.

## 4. 프로젝트 구조와 의존성

- `Assets/FrogEscape`: 게임 자산과 코드. 런타임 Scripts 20개, Editor 생성/시각 개선 스크립트 4개, Tests/PlaytestDriver 1개.
- `Scripts/Core`: GameConfig(난이도, 씬 경로, Timeline, 오디오), GameManager(영속 세션/상태/씬 이동), StageController(씬별 중앙 연결).
- `Scripts/Player`: BikeController(Input System 입력, 거리/좌우 좌표, RoadPath 배치), DodgeController(회피 상태/쿨다운), PlayerHealth(피해/무적/실패).
- `Scripts/AI`, `Combat`: PoliceAI, PoliceAttackController와 탄환 풀, ProjectileController, MissileController의 공격/회피 판정.
- `Scripts/Systems`: RoadPath, ObstacleSpawner, CameraController, RiderPresentation, GameAudio, SewerAtmosphere, CutsceneController 및 Timeline clip/track.
- `Scripts/UI/GameHUD`: uGUI/TMP와 세션/스테이지 상태에 의존.
- `Scenes`: MainMenu → Stage01_Overpass → Stage02_Sewer → Stage03_MissileChase → Ending. 실제 전환은 GameConfig 씬 문자열과 CutsceneController/StageController 경로에 의존.
- `Prefabs`: FrogBike, PoliceCar, CometMissile. ScriptableObjects/GameConfig.asset, Timelines 3개, Models/Materials/Audio가 씬 구성에 사용됨.
- `Assets/Settings`: URP 설정. `Assets/TextMesh Pro`: UI 리소스. `Assets/Scenes/SampleScene.unity`: 기존 기본 씬.
- Build Settings: 게임 씬 5개와 SampleScene 모두 enabled. GameManager는 인덱스 대신 설정의 경로로 로드한다.
- `Documentation/FrogEscape`: 설계/구현/과거 실행 결과. `Builds`, `Backups`: 기존 배포물/백업. Library/Temp/Logs는 생성 데이터다.

의존 흐름:

```text
GameConfig → GameManager ← StageController
                              ├─ BikeController → RoadPath / DodgeController
                              ├─ PlayerHealth → GameManager / Bike / Audio
                              ├─ PoliceAI / PoliceAttack → Projectile pool
                              ├─ MissileController → Dodge / PlayerHealth
                              ├─ ObstacleSpawner → RoadPath / PlayerHealth
                              ├─ CutsceneController → Timeline / Camera / Scene Load
                              └─ HUD / Audio / RiderPresentation / SewerAtmosphere
```

StageController와 GameManager.Instance에 대한 의존이 강해 단독 테스트에는 세션과 관련 구성 요소의 준비가 필요하다. 현재 차량은 RoadPath상의 거리·좌우 좌표를 매 프레임 갱신하는 아케이드 이동이다. 제어 코드 변경은 입력뿐 아니라 회피·충돌·경찰 조준·카메라·Timeline 연출에 영향을 줄 수 있다.

Assets 아래 `.asmdef`/`.asmref`, NUnit `[Test]`/`[UnityTest]` 테스트는 발견되지 않았다. Tests라는 폴더 이름만으로 Test Runner에 등록되지 않는다. PlaytestDriver는 `#if UNITY_EDITOR`로 감싼 MonoBehaviour 통합 검사다. Editor 생성기는 씬/에셋 저장을 수행하므로 검증 명령으로 호출하면 안 된다. 모든 씬/Prefab 직렬화 참조와 런타임 누락 참조를 전수 검증한 것은 아니다.

## 5. Ubuntu WSL2 / cfcheck 연동

Ubuntu 배포판은 running, WSL version 2. `bash -lc`의 `command -v unity`는 찾지 못했다. 기존 `/home/nyod09/cfcheck.sh`와 `.bashrc` 함수는 **Windows powershell.exe를 호출**하는 방식이다. Linux용 Unity CLI가 별도로 설치되어 있다는 증거는 없다.

기존 cfcheck 내용은 Windows CLI status → Assets C# 개수 → Windows Editor.log 오류 패턴 최근 10개 출력이다. 프로젝트 내부의 별도 shell/PowerShell CI 검증 스크립트는 발견하지 못했다.

실제 읽기 가능한 로그:

```bash
LOG='/mnt/c/Users/USER/AppData/Local/Unity/Editor/Editor.log'
tail -n 200 "$LOG"
# 연속 관찰용 제안, 이번에는 실행하지 않음
tail -F "$LOG"
```

Windows 로그 원본은 `C:\Users\USER\AppData\Local\Unity\Editor\Editor.log`. sandbox의 LOCALAPPDATA는 다른 경로였으므로 무조건 환경변수를 신뢰하지 말고 실제 에디터 로그 경로를 확인한다. 프로젝트 `Logs/AssetImportWorker*.log`는 보조 자료다.

cfcheck 개선 설계:

1. 사람이 읽는 요약과 기계 판정 JSON을 분리한다. CLI 종료 코드와 JSON `success`를 모두 검사하고 명령별 오류를 유지한다.
2. 현재 cfcheck는 마지막 echo 때문에 앞 단계 실패를 정상 종료로 가릴 수 있다. grep 무매치는 정상 상황으로 처리하고, 상태/로그 접근 실패는 명시적으로 수집하여 최종 종료 코드로 전달한다.
3. 누적 로그 전체 grep은 과거 오류와 현재 오류를 섞는다. 검증 시작 때 파일 크기/식별 정보와 Console session/cursor를 기록하고, 종료 때 새 구간만 수집한다. 로그 축소/교체 및 Console session 변경 시 reset/incomplete로 표시한다.
4. Console `groundTruth`와 로그 증분을 함께 보관한다. `since_session`과 `since`로 중복 수집을 방지하고 dropped/reset을 검증 누락으로 처리한다. 에러 문자열만으로 테스트 통과를 판정하지 않는다.
5. 오류 수집에는 C# 컴파일 오류 외 Exception/Assert/셰이더 실패/에디터 종료도 포함하되, CLI 구조화 결과를 우선한다. 로그 휴리스틱은 보조 증거로 구분한다.
6. 결과는 프로젝트 Assets 밖 `Documentation/Validation/<run-id>/`에 기록한다. 기존 Playtest/BuildResult JSON을 덮어쓰지 않는다. 자동화 런타임에는 임시 파일 작성 후 atomic rename으로 최종 JSON을 공개한다.

권장 집계 형식(설계 예시, 이번 테스트 결과가 아님):

```json
{
  "schemaVersion": 1,
  "runId": "unique-id",
  "projectPath": "C:\\Users\\USER\\Desktop\\Unity\\Crazy Frog",
  "editorVersion": "6000.3.23f1",
  "overall": "incomplete",
  "compile": {"status": "up_to_date", "freshCompilation": false},
  "editMode": {"status": "not_run", "discovered": 0, "reason": "no_tests"},
  "playMode": {"status": "not_run", "discovered": 0, "reason": "no_tests"},
  "acceptance": {"status": "not_run", "reason": "requires_isolated_output_and_cleanup"},
  "console": {"errors": 0, "warnings": 0},
  "artifacts": []
}
```

종료 코드 제안: 0=요청한 필수 검증 모두 통과, 1=컴파일/테스트 실패, 2=연결/도구/로그 접근 실패, 3=필수 테스트 없음·미실행·타임아웃으로 판정 불완전. `up_to_date`는 컴파일 게이트에 허용할 수 있지만 `freshCompilation=false`를 유지한다. 테스트 0개는 테스트 성공으로 집계하지 않는다. 연결 점검 모드의 성공과 전체 회귀 검증의 성공도 구분한다.

## 6. 충돌 방지와 자동화 순서

**Windows의 단일 coordinator만 에디터 명령과 파일 변경을 수행하고 WSL은 요청·결과 수집을 담당한다.** 동일 프로젝트의 Library/Temp를 Windows·Linux Unity가 동시에 사용하지 않는다.

1. 프로젝트 절대 경로·에디터 버전·main PID 고정. ready, stopped, compiling=false, 열린 씬 dirty=false인지 확인한다. 사용자가 작업 중이거나 씬이 dirty면 저장/전환하지 않고 blocked로 반환한다.
2. Windows coordinator가 프로젝트별 exclusive file handle(FileShare.None) 또는 이름 있는 mutex를 획득한다. WSL도 반드시 coordinator를 거치게 하여 하나의 잠금을 공유한다. WSL flock만으로 Windows 프로세스를 보호한다고 가정하지 않는다. 협조하지 않는 수동 편집까지 막을 수는 없으므로 실행 도중 원본 해시 변경도 검사한다.
3. 변경 전 백업/해시와 열린 씬, 활성 씬, 입력 설정, 시간 배율, pause, Console session/cursor, 로그 offset을 기록한다. Git이 없으므로 초기화 없이 백업을 사용한다.
4. 승인된 범위의 기능 구현 → 기존 에디터에 컴파일 요청 → 완료/명시적 실패까지 한정된 시간 대기. 통신 timeout은 컴파일 실패와 구분하고 새 에디터 시작으로 복구하지 않는다.
5. compile 실패면 오류 코드·경로·라인·스택을 수집하고 해당 범위만 수정한다. 성공이면 테스트 검색 → EditMode → PlayMode/통합 검사 순으로 진행한다. 아직 테스트가 없으므로 현 단계의 전체 회귀 결과는 incomplete다.
6. 실행 중 비동기 test run은 job/status의 terminal 결과까지 추적한다. CLI timeout만으로 내부 작업이 끝났다고 판단하지 않는다. 이전 작업 종료 확인 전 수정/재실행하지 않는다.
7. 실패 분석 → 최소 수정 → 컴파일 및 영향 범위 재검증. 자동 수정 반복 횟수/시간 상한을 두고 같은 오류 반복, 범위 밖 변경, 에디터 비정상 상태는 중단·보고한다.
8. finally에서 Play Mode 종료, 입력/시간/오디오 등 변경 상태 복원, 원래 씬 구성 확인. 결과 파일 검증과 보존 해시 비교 후 잠금 해제한다. Console clear, save_all, 씬 자동 저장, 에셋 재생성은 실행하지 않는다.

## 7. 필요한 추가 작업

우선순위 1: 기존 MCP 연결의 도구가 이 Codex 세션에 노출되는지 확인하고 상태 읽기 한 번으로 직접 경로를 검증한다. 설정 재작성/중복 서버 등록은 하지 않는다. 현재 CLI 경로는 이미 사용 가능하다.

우선순위 2: Windows coordinator와 cfcheck의 JSON/종료 코드 계약을 구현한다. 이번에는 설계만 작성했으며 cfcheck.sh와 MCP 설정은 수정하지 않았다.

우선순위 3: 테스트 구조 설계. 런타임 asmdef 도입은 Assembly-CSharp 및 기존 Editor/테스트 드라이버 참조에 영향을 주므로 별도 변경으로 검토한다. 좁은 EditMode 후보는 난이도 설정, RoadPath 계산, 경계/쿨다운 규칙이며, 실제 시간/입력/씬 연동은 PlayMode 통합 검사로 둔다. 테스트를 위해 현재 차량 동작을 임의 리팩터링하지 않는다.

우선순위 4: PlaytestDriver의 출력 경로를 run-id별로 분리하고 타임아웃/예외에서도 입력 장치·입력 설정·timeScale·pause·씬 복원을 보장하도록 개선한 뒤 Easy/Normal 통합 검사를 재실행한다. 기존 34개 성공 기록은 새 버전의 회귀 증명이 아니다.

우선순위 5: 강제 전체 재컴파일, 새 Windows 빌드, 독립 exe와 실측 성능 검증은 별도 단계로 남긴다. 현재 프로젝트의 코드/씬/Prefab/에셋/차량 제어는 변경하지 않았다.

## 승인 제한 기록

자동 승인 검토가 (1) cfcheck.sh 전체 실행을 부작용 판별 불가로, (2) CLI MCP 프로세스 점검을 추가 Unity 인스턴스 실행 가능성으로 거부했다. 두 작업은 실행하지 않았고 우회하지 않았다. 개별 읽기 전용 조회로 가능한 분석은 완료했다. 해당 두 직접 검증을 추가로 진행하려면 별도 실행 승인이 필요하다.
