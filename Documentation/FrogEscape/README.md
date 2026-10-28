# Frog Escape — 실행 및 개발 안내

UI와 그래픽은 밝은 아케이드 스타일로 개선했습니다. 변경 내용은 `VisualUpgrade.md`를 참고하세요.

다음 개선에서는 주행·회피·피격 피드백을 추가하고 두 번째 스테이지를 천장이 있는 하수도 내부로 바꿨습니다. `FeelAndSewer.md`를 참고하세요.

## 실행
- Windows 빌드: `Builds/FrogEscape/FrogEscape.exe`. 배포 시 exe만 복사하지 말고 **FrogEscape 폴더 전체**를 복사하세요.
- Unity: `Assets/FrogEscape/Scenes/MainMenu.unity`를 열고 Play.
- 행사 기본값은 EASY. 타이틀의 난이도 버튼으로 NORMAL 전환.
- A/D 또는 좌우 방향키: 이동. Space: 짧은 회피. ESC: 일시정지/재개.
- 메뉴는 마우스 또는 위/아래 방향키 + Enter로 선택.
- 실패하면 Enter로 현재 스테이지를 재시작. 일시정지 및 결과 화면에서 타이틀 복귀 가능.

## 플레이 흐름
1. 고가도로 60초: 자동 주행, 좌우 이동, 안전한 통로가 남는 장애물과 경찰 추격.
2. 벽타기 Timeline 10초: 측면 카메라, 상승·낙하와 수로 진입.
3. 수로 65초: 단발/시간차/예측 조준. 노란 선에서 벗어나거나 Space로 회피.
4. 탈출 Timeline 10초: 경사로 상승, 추적 유도탄 등장.
5. 외부 도로 약 62초: 유도탄 3회. 노란 예고를 기다린 뒤 **초록 SPACE 신호**에 회피.
6. 마지막 회피 성공 시 17초 엔딩: 유도탄에 올라타기, 균형 잡기, 되돌려 보내기, 코믹한 파티클과 CLEAR.

정상 클리어 약 3분 45초. 재도전/일시정지/마지막 공격 재시도로 길어질 수 있습니다.

## 튜닝
`Assets/FrogEscape/ScriptableObjects/GameConfig.asset`에서 Easy/Normal 각각 조정합니다.

| 항목 | Easy | Normal |
|---|---:|---:|
| 체력 | 4 | 3 |
| 자동 전진 속도 | 24 | 28 |
| 좌우 속도 | 8 | 8 |
| 피격 무적 | 2초 | 1.5초 |
| 회피 지속 시간 | 0.85초 | 0.85초 |
| 회피 쿨다운 | 2.2초 | 2.2초 |
| 탄환 경고 | 2.5초 | 1.8초 |
| 유도탄 회피 신호 구간 | 1.05초 | 0.65초 |
| 장애물 행 간격 | 2행마다 | 매 행 |

유도탄 첫 번째 공격은 추가 경고 시간을 제공합니다. Easy는 마지막 공격 직전 체력을 최소 2로 보완합니다.
각 Scene의 `Route`에서 경로 길이/너비, `Chase Camera`에서 거리/높이/보간을 조정할 수 있습니다.

## 주요 코드
- `GameManager`: 난이도·체력·플레이 기록 보존, 비동기 씬 로딩, 일시정지, 재시작.
- `StageController`: 스테이지 시간, 종료 조건, 플레이 상태, 연출 시작.
- `RoadPath`: 도로 중심/방향/좌우 축을 계산하는 곡선 경로.
- `BikeController`, `DodgeController`, `PlayerHealth`: 실제 Input System 키 입력, 보간 이동, 경계, 회피/무적/피격.
- `ObstacleSpawner`: 미리 제작한 유한 장애물 행 활성화 및 경로 좌표 기반 충돌. 매 행 하나 이상의 안전 통로 확보.
- `PoliceAI`: 거리 보정 속도, 경로 추종, 장애물 감지와 빈 차선 선택, 공격/회복 상태.
- `PoliceAttackController`: Targeting → Warning → Fire → DodgeCheck → Recovery. 탄환 6개를 미리 만들어 재사용.
- `ProjectileController`: 상대 전진 좌표의 교차로 충돌을 판정하고, 회수/재사용 시 상태 초기화.
- `MissileController`: Spawn → Pursue → LockOn → Warning → Dash → Hit/Miss → Despawn. 회전 각속도와 속도 제한, 예측 추적과 경고 후 확정 돌진.
- `CutsceneController`, `FrogCinematicTrack/Clip`: 실제 Timeline/PlayableDirector로 게임 입력을 잠그고 플레이어·경찰·카메라·유도탄을 애니메이션. 컷신 종료 이벤트가 다음 씬을 로드.
- `GameHUD`: 한국어 TMP/uGUI, 입력 안내, 공격 예고, 체력/진행/쿨다운, 모든 메뉴 버튼.
- `GameAudio`: 음악·엔진·사이렌·효과음 분리, 경고 중 음악 음량 감소.
- `FrogEscapeBuilder`: 새 에셋/씬 생성 및 Windows 빌드. 이미 존재하는 게임 씬은 자동 재생성하지 않음.
- `Tests/PlaytestDriver`: **에디터 전용** 실제 Input System 가상 키보드 인수 검사. 빌드에서 제외됨.

## 자산과 원본 보호
- 캐릭터, 바이크, 경찰차, 유도탄, 도시/수로/야외 배경은 프리미티브로 새로 제작한 오리지널 로우폴리 에셋입니다.
- 음악과 효과음은 코드로 합성한 오리지널 오디오입니다. Crazy Frog 캐릭터 모델이나 Axel F 음원은 포함하지 않습니다.
- 텍스트 렌더링은 TextMesh Pro, 한국어 글꼴은 행사 PC의 설치된 **Malgun Gothic (맑은 고딕)**을 DynamicOS로 사용합니다. 폰트 파일은 배포하지 않습니다. 맑은 고딕이 설치된 Windows PC가 실행 환경입니다.
- 기존 `Assets/Scenes/SampleScene.unity`와 기존 에셋은 그대로 유지했습니다.
- 작업 시작 시 `Backups/BeforeFrogEscape.zip`에 Assets/Packages/ProjectSettings를 백업했습니다.
- Build Settings에 게임 씬 5개를 앞쪽에 추가하고 원래 SampleScene 항목도 유지했습니다.
- TMP 필수 리소스는 Unity 패키지의 공식 비대화형 가져오기로 `Assets/TextMesh Pro`에 추가했습니다.

## 빌드 및 검증 재현
- Unity 메뉴 `Frog Escape > Build Windows Player`로 재빌드.
- 에디터에서 MainMenu Play 후 `FrogEscape.Tests.PlaytestDriver.Run(false)` (Easy) 또는 `Run(true)` (Normal) 호출.
- 테스트는 기본 조작을 실제 시간으로 검사한 뒤 전체 경로를 3배 시간으로 실행합니다. 게임의 속도/경고/공격 데이터는 그대로 사용합니다.
- 결과: `Documentation/FrogEscape/Playtest-Easy.json`, `Playtest-Normal.json`.
- 테스트 결과와 미확인 품질 목표는 `TestReport.md` 참고.

행사 참가자에 대한 조작 이해 20초/완료율 목표는 실제 아동 사용자 테스트가 필요합니다. 자동 인수 검사로 사용자 완료율을 증명하지 않습니다.
