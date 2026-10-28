# Crazy Frog: 데스크톱·노트북 GitHub 동기화 준비

기준: Unity **6000.3.23f1 (09d2ecc7fb28)**, Windows 11. 작성일 2026-10-29 KST.
현재 단계는 백업과 Git 준비다. git init, 최초 커밋, GitHub 저장소 생성,
원격 연결, 최초 Push는 각각 승인 전 실행하지 않는다.

## 현재 적용한 파일 정책

| Git에 포함 | Git에서 제외하되 로컬에서 유지 |
|---|---|
| Assets 전체와 파일·폴더의 .meta | Library, Temp, Obj, Logs, Builds/Build |
| Packages/manifest.json, packages-lock.json 및 로컬 패키지 소스 | UserSettings, IDE 캐시, 생성된 루트 csproj/sln |
| ProjectSettings 전체와 ProjectVersion.txt | Backups, ValidationResults |
| Documentation, Tools, .gitignore, .gitattributes, .vsconfig | 기기별 .codex, .mcp.json, 실제 .env |

현재 프로젝트는 Visible Meta Files, Asset Serialization 값 2(Force Text)다.
이 설정을 변경하거나 에셋을 다시 직렬화하지 않았다. 씬·Prefab·.asset·.meta는
한 세트로 커밋한다. 파일을 이동할 때는 Unity에서 이동하고 대응 .meta를 함께 보존한다.

.gitattributes는 기존 파일 바이트를 보존하도록 자동 줄바꿈 변환을 끈다.
Tools 하위 .sh만 LF를 명시한다. 커스텀 Unity merge driver나 LFS 필터는 아직 없다.
이 정책은 GitHub Desktop에서도 사용된다. 두 기기의 전역 Git 설정을 바꾸지 않는다.

## 대용량 파일 판단

분석 시 기존 추적 후보 1,748개는 156,209,608 bytes였고, 최대 파일은
Assets/FrogEscape/Scenes/Stage01_Overpass.unity (9,781,843 bytes)다.
50 MiB 이상 및 100 MiB 이상 파일은 각각 0개다. 새 준비 문서/도구가 최초 커밋에 추가된다.
Assets의 약 120 MB는 여러 .asset에 분산되어 있다. 현재 음원 최대 705,644 bytes,
PNG 최대 283,063 bytes이므로 **현재 Git LFS 도입은 보류**한다.

GitHub는 50 MiB 초과 파일에 경고하고 100 MiB 초과 파일은 일반 Git Push를 차단한다.
향후 큰 PSD/FBX/영상/음원 등 자주 교체되는 바이너리를 추가할 때, 최초 커밋 전에
해당 경로의 LFS 적용을 별도 검토한다. 모든 .asset/.unity/.prefab을 일괄 LFS 처리하지 않는다.
GitHub Desktop은 LFS를 포함하지만 추적 규칙은 별도로 설정해야 한다. 현재 기기의
Git LFS 3.7.1 설치는 확인했으며 `git lfs install/track`은 실행하지 않았다.

출처: [GitHub 파일 크기 제한](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github),
[GitHub Desktop과 LFS](https://docs.github.com/en/desktop/configuring-and-customizing-github-desktop/about-git-large-file-storage-and-github-desktop).

## 승인 후 데스크톱에서 수행할 순서

1. 외부 백업의 verification.json이 VERIFIED인지 확인한다. Unity 작업/자동화를 멈춘 뒤 시작한다.
2. Git 작성자 이름/이메일 및 GitHub Desktop 로그인 계정을 확인한다. 전역 설정을 임의 변경하지 않는다.
3. 별도 승인 후 기존 프로젝트 루트에서 `git init -b main`을 실행한다.
4. 최초 add는 Assets, Packages, ProjectSettings, Documentation, Tools, .gitignore,
   .gitattributes, .vsconfig로 명시한다. 실제 `git status`, `git diff --cached --stat`,
   `git ls-files`, `git check-ignore`로 범위와 제외 규칙을 확인한다.
5. Assets의 .meta 누락, Library/Temp/Backups/ValidationResults 포함, 기기별 인증/설정
   혼입이 없음을 확인한 뒤 최초 커밋 승인을 받아 커밋한다. 기존 과거 문서·스크린샷도 포함된다.
6. GitHub Desktop의 File → Add local repository로 이 폴더를 등록한다.
   이미 초기화된 폴더를 등록하며 별도 새 프로젝트 폴더를 만들지 않는다.
7. GitHub 저장소 생성 승인 후, 사용자 소유자/저장소 이름을 확정하고 **Private**로 만든다.
   기존 로컬 이력을 올릴 빈 원격에는 README/license/gitignore를 따로 생성하지 않는다.
8. 별도 원격 연결 승인 후 origin URL을 연결한다. GitHub Desktop의 Publish repository를
   사용한다면 생성·연결·Push를 함께 수행할 수 있으므로 **세 작업 모두 승인된 뒤** 진행한다.
   Keep this code private 선택과 실제 원격 Private 상태를 확인한다.
9. 최초 Push 승인 후 main을 Push하고 GitHub 파일 목록과 최신 커밋을 확인한다.
   토큰/개인 키를 프로젝트 파일이나 명령 인자로 저장하지 않는다.

승인 범위가 정해지기 전 위 단계의 실행은 하지 않는다. 초기화하지 않은 상태에서는
Git의 실제 인덱스·check-ignore 결과를 검증할 수 없으므로, 현재 후보 목록은 사전 분석이다.

## 노트북에서 처음 Clone하고 Unity 실행하기

원격 Private 저장소 생성 및 첫 Push가 완료된 뒤 사용하는 절차다. 이번 단계에서
노트북 Clone이나 Unity 시작을 실행하지 않았다.

1. Unity Hub에서 **6000.3.23f1**과 필요한 Windows 빌드 모듈을 설치한다.
   프로젝트가 요구하는 버전과 정확히 일치시킨다. 자동 버전 업그레이드를 승인하지 않는다.
2. GitHub Desktop에서 Private 저장소에 접근 가능한 계정으로 로그인한다.
3. File → Clone repository → GitHub.com 또는 URL에서 저장소를 선택한다.
   OneDrive/Dropbox 동기화 폴더와 WSL Linux 파일시스템을 피해 로컬 Windows 경로에 Clone한다.
4. Clone 완료 후 Assets, Packages, ProjectSettings, ProjectVersion.txt, .meta가 있는지 확인한다.
   GitHub Desktop Changes가 깨끗한지 확인한다. Git LFS가 향후 도입되면 포인터가 아닌
   실제 콘텐츠까지 내려받았는지도 확인한다.
5. Unity Hub의 Add/Add project from disk로 **Assets의 부모 프로젝트 폴더**를 등록한다.
   새 프로젝트를 만들지 않는다. 6000.3.23f1로 열고 첫 패키지 복원·Library 생성을 기다린다.
   이 단계는 초기 에셋 임포트/컴파일을 수행하므로 충분한 공간·네트워크가 필요하다.
6. Console과 Package Manager 오류를 확인한다. 패키지 버전을 임의 업데이트하지 않는다.
   기존 MainMenu는 Assets/FrogEscape/Scenes/MainMenu.unity다. 실제 플레이 검증은 별도 수행한다.
7. 첫 실행 후 GitHub Desktop Changes를 확인한다. 생성 폴더는 제외되어야 한다.
   ProjectSettings/에셋 변경이 생기면 자동 커밋하지 말고 원인을 확인한다.
8. MCP/Unity CLI 인증과 연결은 기기별로 설정한다. 데스크톱의 .codex/MCP 설정을 복사하거나
   Git으로 공유하지 않는다. 이미 있는 연결도 이 작업에서 변경하지 않았다.

GitHub Desktop 공식 절차:
[로컬 저장소 등록](https://docs.github.com/en/desktop/adding-and-cloning-repositories/adding-a-repository-from-your-local-computer-to-github-desktop),
[Clone](https://docs.github.com/en/desktop/adding-and-cloning-repositories/cloning-and-forking-repositories-from-github-desktop).

### 현재 P1의 기기별 경로 제한

게임 프로젝트 자체는 다른 Windows 경로에 Clone할 수 있지만, 현재 P1은 데스크톱
`C:\Users\USER\Desktop\Unity\Crazy Frog`와 기본 Unity CLI 설치 경로가 고정되어 있다.
WSL 래퍼도 같은 경로를 사용한다. 노트북 경로가 다르면 그대로 실행해서는 안 된다.
노트북에서 P1까지 쓰려면 같은 경로/CLI 환경을 구성하거나, 후속 승인 작업으로
프로젝트 경로의 명시적 설정·대상 검증을 추가해야 한다. 이번 P2는 P1을 수정하지 않는다.
외부 복구 사본에서 P1을 실행해도 원래 데스크톱 프로젝트를 대상으로 한다는 점도 같다.

## 기기를 바꾸는 일상 순서

1. 현재 기기에서 Unity 저장을 마치고 프로젝트를 닫는다.
2. GitHub Desktop에서 변경 목록을 검토하고 커밋·Push한다. 원격 반영을 확인한다.
3. 다른 기기에서 Unity를 열기 전에 Fetch origin → Pull origin을 수행한다.
4. 최신 커밋과 깨끗한 작업 트리를 확인한 뒤 Unity를 연다.

동시에 양쪽에서 같은 씬/Prefab을 편집하지 않는다. 충돌 시 양쪽 변경을 보존하고
어느 버전을 사용할지 검토한다. .meta 삭제/재생성이나 무조건 덮어쓰기로 해결하지 않는다.
Git은 디스크 폴더 실시간 동기화가 아니며 커밋·Push하지 않은 변경은 다른 기기에 전달되지 않는다.

## 백업과 복구

외부 백업 기본 위치: `C:\Users\USER\Desktop\Unity\CrazyFrog-RecoveryBackups`.
Tools/Backup/New-CFRecoveryBackup.ps1은 고유 폴더를 만들고, 압축 전 여유 공간 확인,
manifest SHA-256 작성, 새 폴더 실제 압축 해제, 모든 복원 파일 해시 비교와 원본 안정성
검사를 수행한다. 기존 ZIP/보고서는 덮어쓰거나 삭제하지 않는다.

백업에는 소스·문서·도구뿐 아니라 기존 Backups와 당시 ValidationResults도 포함한다.
Git에서는 이 둘을 제외한다. 생성된 Library/Temp/Logs/Builds와 로컬 MCP 설정은 백업에서 제외한다.
프로젝트별 P1 잠금을 사용해 수집기와 백업이 겹치지 않게 하지만 수동 편집까지 막지는 않는다.

복구는 백업의 RESTORE.md와 manifest.json을 따라 **새 빈 경로**에서 먼저 검증한다.
현재 프로젝트 위에 바로 풀지 않는다. Unity 임포트·실행까지 검증한 백업은 아니며,
동일 C: 디스크의 백업은 디스크 고장에 대비한 별도 매체 백업을 대체하지 않는다.

## 현재 미실행 항목

git init, 최초 커밋, GitHub Private 저장소 생성, origin 연결, 최초 Push,
노트북 Clone/Unity 시작, GitHub Desktop UI 동기화 및 원격 복구는 아직 수행하지 않았다.
Unity Version Control 패키지는 설치되어 있지만, 로컬 .plastic 워크스페이스 메타데이터와
cm 명령은 발견되지 않았다. 패키지 설치만으로 UVCS 사용 중이라고 판단하지 않는다.
클라우드 UVCS 저장소 존재/계정 상태는 이번에 조회하지 않았고 기존 설정은 보존했다.
