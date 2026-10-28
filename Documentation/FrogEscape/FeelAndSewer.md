# 주행 연출 및 하수도 내부 개선

2026-10-28. 초기 버전 다음 단계로 주행 피드백을 개선하고, 사용자 요청에 따라 두 번째 스테이지를 위가 열린 수로에서 천장이 있는 하수도 내부로 변경했다.

## 플레이 피드백

- 실제 속도에 비례하는 바퀴 회전, 주행 중 몸과 스카프의 작은 움직임.
- Space 회피 중 두 줄의 민트색 잔상과 최대 3도 시야각 변화.
- 피격 시 최대 0.25초의 작은 카메라 반응, 무적 중 캐릭터 밝기 변화.
- 일시정지에서는 애니메이션 정지. 컷신·메뉴·실패 상태에서는 잔상 제거. Timeline의 라이더 움직임은 덮어쓰지 않음.
- 공격 경고를 배경 패널에 표시하고, 유도탄 경고에는 노랑→초록 신호에 맞춘 타이밍 막대 추가. 공격 시점이나 판정은 바꾸지 않음.

## 하수도

- 경로를 따라 이어지는 115개 벽돌 아치 구간으로 천장과 측면을 둘러쌈.
- 자체 생성한 벽돌 패턴, 녹슨 배관과 플랜지, 녹색 오수, 어두운 안개와 따뜻한 천장 조명.
- 72개 조명 중 플레이어 가까이 있는 조명만 활성화. 그림자 없는 점광원으로 제한.
- 기존 밝은 수로의 보·기둥·표지 일부를 비활성화하고, 하수도 전용 재질을 사용하여 다른 스테이지 색상은 유지.
- 난이도별 탈출 지점에 맞춰 앞쪽 천장과 천장등을 정리하고 외부 조명으로 전환. 탈출 컷신이 천장을 관통하지 않도록 연결.

## 파일과 재현

`RiderPresentation.cs`, `SewerAtmosphere.cs`가 표현을 담당한다. `CameraController.cs`, `GameHUD.cs`에는 카메라 반응과 경고 패널만 추가했다. `FeelPassSetup.cs`, `SewerInteriorBuilder.cs`는 Unity Editor 안에서 실행하는 씬 설정 코드다. 새 에디터 파일이 실행 중 Editor의 컴파일 목록에 반영되지 않는 현상이 있어, Unity CLI `script run --file ... --entry ...Apply`로 소스를 직접 컴파일·실행해 적용했다.

변경 전 스크립트와 씬은 `Backups/BeforeFeelPass.zip`에 보관했다. 주행 경로, AI, 속도, 공격 타이밍, 체력과 난이도 설정은 유지했다.

최초 프로젝트 백업과의 최종 비교에는 Build Settings 외에 URP 설정 3개와 PlayerSettings도 차이가 있다(`OriginalFilesChanged.txt`). 셰이더 프리필터·렌더링 설정 직렬화 및 Standalone 배칭 항목 등을 확인했다. 이번 연출 스크립트가 직접 수정한 설정은 아니며 기존 변경을 임의로 되돌리지 않았다. 이전 보고서의 “기존 파일은 Build Settings만 변경”은 당시 비교 시점의 기록이다.

## 검사 주의사항

첫 자동 검사는 비활성 Game View의 키보드 입력 차단 때문에 타이틀에서 시간 초과했다. `PlaytestDriver` 실행 중에만 에디터 입력을 허용하고 종료 시 원래 설정으로 복구하도록 수정했다. 배포 빌드에는 검사 코드가 포함되지 않는다. 최신 검사 결과는 `Playtest-*.json`, 빌드 결과는 `BuildResult.json`이다.

실제 회피 캡처에서 두 TrailRenderer가 각각 emitting=true, 정점 7개를 생성했고 FOV 65.61을 기록했다(`FeelCapture.txt`). 스크린샷은 `Screenshots/Feel-Dodge.png`, `Screenshots/Sewer-Interior.png`, `Normal-scene-11.png`에서 확인할 수 있다. 독립 실행 파일의 전체 플레이 및 현장 성능은 별도 검증 대상이다.

최종 하수도 포함 전체 경로: Easy 34/34 PASS, Normal 34/34 PASS. 별도 상태 검사에서 일시정지 중 바퀴 회전·주행 정지와 재개 후 바퀴 회전 모두 True(`FeelPauseCheck.txt`). 검사 드라이버 제거 후 에디터 입력 설정이 `PointersAndKeyboardsRespectGameViewFocus`로 복원됨을 확인했다. 탈출 컷신에서 천장등 제거와 외부 채광 전환을 캡처로 점검했다.
