# 🚀 Space Splattoon 🚀
<img width="380" height="206" alt="image" src="https://github.com/user-attachments/assets/de1aee60-11fe-4a77-8d1b-ab02514c4f68" />


**Space Splattoon**은 Unity 엔진으로 제작된 3인칭 아레나 슈팅 게임입니다. 플레이어는 두 팀으로 나뉘어 잉크를 쏘아 맵의 거점을 점령하고, 제한 시간 내에 더 많은 거점을 차지하는 팀이 승리합니다. 이 프로젝트는 실시간 멀티플레이, 페인팅 시스템, 그리고 Unity의 기술 스택을 깊이 있게 활용하는 것을 목표로 합니다.

## 🎮 플레이가능 빌드 (현재 스팩은 20명서버제한이있습니다 - 스팀출시를 위해 개선중입니다.)
https://drive.google.com/file/d/1ScFXoNUl5UiAilYnGFbbJAxScSuqN5pN/view?usp=drive_link


## 🎮 트레일러
https://drive.google.com/file/d/1Rhk1LjSxFAOiUhMAcSyACAufEVabcikV/view?usp=drive_link

## 🎮 시연영상
https://drive.google.com/file/d/1g4Ko9bxFdURumAZlQDzFPFBnMUT5AGGk/view?usp=sharing

<img width="1024" height="576" alt="image" src="https://github.com/user-attachments/assets/02342577-b631-48ba-b04b-30dde7cad8c7" />
<img width="1024" height="576" alt="image" src="https://github.com/user-attachments/assets/77e1011e-12d6-4c89-b09a-802f16eb8515" />
<img width="1024" height="576" alt="image" src="https://github.com/user-attachments/assets/e4d1beb0-5f1e-48a5-a666-8e4aa5082091" />
<img width="1024" height="576" alt="image" src="https://github.com/user-attachments/assets/e972b7d3-9c16-47d1-a5fe-1240ed61d444" />
<img width="1024" height="576" alt="image" src="https://github.com/user-attachments/assets/ed483074-495f-46ca-ab00-08e46aca5885" />
<img width="1024" height="576" alt="image" src="https://github.com/user-attachments/assets/3ba5a2b7-8083-486e-ab1b-5f6d53a34d89" />
<img width="1024" height="576" alt="image" src="https://github.com/user-attachments/assets/970f6cbe-1665-4f36-ba0f-ed7ecca55e48" />


## 🌟 주요 특징

*   **실시간 3v3 멀티플레이**: Photon (PUN 2)을 활용하여 멀티플레이 환경을 제공합니다.
*   **페인팅 시스템**: `CommandBuffer`를 이용한 GPU 렌더링 제어를 통해, 맵에 동적으로 잉크를 칠하는 고성능 페인팅 시스템을 구현했습니다.
*   **현대적인 캐릭터 컨트롤**: Unity의 새로운 Input System을 기반으로 반응성 높고 직관적인 플레이어 조작감을 제공합니다.
*   **전략적인 거점 점령전**: 단순한 킬/데스가 아닌, 팀원과의 협력을 통해 맵의 주요 거점을 전략적으로 점령하고 지키는 것이 승리의 핵심입니다.

## 🛠️ 기술 스택

*   **게임 엔진**: Unity 2022.3.17f1
*   **네트워킹**: Photon Unity Networking (PUN) 2
*   **입력 시스템**: Unity Input System
*   **렌더링**: Universal Render Pipeline (URP) 및 `CommandBuffer`를 이용한 커스텀 렌더링
*   **언어**: C#

## 📂 프로젝트 구조

프로젝트는 Unity의 표준 권장 사항과 기능별 모듈화를 따라 구성되어 있습니다. 각 디렉토리는 특정 유형의 에셋이나 스크립트를 포함하며, 이는 프로젝트의 유지보수성과 확장성을 높입니다.

```
SpaceSplattoon-develop/
├── Assets/
│   ├── 01_Scenes/          # 📜 게임의 주요 씬 파일 (예: Title, Lobby, WaitingRoom, GameScene)
│   ├── 02_Scripts/         # 🧠 게임의 핵심 로직을 담고 있는 C# 스크립트
│   │   ├── Manager/        # GameManager, NetworkManager, UIManager 등 핵심 관리자 스크립트
│   │   ├── Player/         # 플레이어 캐릭터의 동작, 상태, 상호작용 관련 스크립트
│   │   ├── Weapon/         # 무기 시스템 및 발사체 관련 스크립트
│   │   ├── UI/             # 사용자 인터페이스 (UI) 관련 스크립트
│   │   ├── NetworkService/ # Photon PUN2를 활용한 네트워크 통신 및 동기화 스크립트
│   │   ├── Level/          # 맵 구조, 거점, 스폰 포인트 등 레벨 관련 스크립트
│   │   ├── GamePlay/       # 게임 플레이 규칙, 이벤트 처리 등 게임 로직 스크립트
│   │   ├── Item/           # 게임 내 아이템의 기능 및 관리 스크립트
│   │   ├── Effect/         # 시각 및 청각 효과 (파티클, 사운드 등) 관련 스크립트
│   │   ├── Camera/         # 게임 내 카메라 제어 및 시네머신 관련 스크립트
│   │   ├── Animations/     # 캐릭터 및 오브젝트 애니메이션 제어 스크립트
│   │   └── Util/           # 공통적으로 사용되는 유틸리티 함수 및 열거형 정의
│   ├── 03_Prefabs/         # 🧱 재사용 가능한 게임 오브젝트 (플레이어, 무기, UI 요소, 이펙트 등)
│   ├── 04_Images/          # 🖼️ UI, 스프라이트, 텍스처 등 이미지 에셋
│   ├── 05_Models/          # 📦 3D 모델 에셋 (캐릭터, 환경 오브젝트, 무기 등)
│   ├── 06_Sounds/          # 🔊 배경 음악, 효과음 등 오디오 에셋
│   ├── 07_Animations/      # 🎬 애니메이션 클립 및 애니메이터 컨트롤러 에셋
│   ├── 08_Externals/       # 📦 외부에서 임포트된 일반 에셋 (예: Sci-Fi Arena, Stylized Sci-Fi Weapon Pack 등)
│   ├── 09_InputSystems/    # 🎮 Unity Input System 관련 설정 및 PlayerAction.inputactions 파일
│   ├── JMO Assets/         # 📦 JMO Cartoon FX Remaster 등 특정 외부 에셋
│   ├── Material/           # 🎨 게임 오브젝트에 적용되는 머티리얼 에셋
│   ├── Photon/             # 📦 Photon PUN2 관련 에셋 및 스크립트
│   ├── Plugins/            # 🧩 외부 라이브러리 및 플러그인 (DLL 등)
│   ├── Resources/          # 📂 Resources.Load()를 통해 동적으로 로드되는 에셋
│   ├── Settings/           # ⚙️ Unity 프로젝트의 특정 설정 파일 (예: URP 설정)
│   ├── Shader/             # 🖌️ 커스텀 셰이더 그래프 및 HLSL 파일
│   ├── TextMesh Pro/       # 📝 TextMesh Pro 관련 에셋 및 설정
│   ├── TutorialInfo/       # 튜토리얼 정보 및 관련 에셋
│   └── ...                 # 기타 에셋 폴더
├── Packages/               # 📦 Unity Package Manager를 통해 설치된 패키지 종속성
├── ProjectSettings/        # ⚙️ Unity 프로젝트의 전반적인 설정 파일
└── .gitattributes          # Git LFS 설정 등 Git 관련 속성
```

## 🚀 시작하기

### 요구 사항

*   Unity 2022.3.17f1 또는 그 이상
*   Photon Unity Networking (PUN) 2 에셋

### 설치 및 실행

1.  **저장소 클론**:
    ```bash
    git clone https://github.com/your-username/SpaceSplattoon-develop.git
    ```
2.  **Unity 프로젝트 열기**: Unity Hub를 통해 프로젝트를 엽니다.
3.  **Photon 설정**:
    *   Unity Asset Store에서 **PUN 2** 에셋을 다운로드 및 임포트합니다.
    *   Photon Server Settings (`Window > Photon Unity Networking > Pun Wizard`)에서 Photon App ID를 입력합니다.
4.  **실행**:
    *   `Assets/01_Scenes/Title.unity` 씬을 엽니다.
    *   Unity 에디터의 플레이 버튼을 눌러 실행합니다.

## 🕹️ 조작법

| 키 | 액션 | 설명 |
| :--- | :--- | :--- |
| **W, A, S, D** | **이동** | 전후좌우로 자유롭게 움직입니다. |
| **마우스 이동** | **조준** | 카메라 방향을 조절합니다. |
| **마우스 좌클릭** | **잉크 발사** | 조준점을 향해 잉크를 발사합니다. (꾹 누르면 연사) |
| **Ctrl** | **하강** | 이동하는 방향으로 빠르게 돌진합니다. |
| **Space** | **대쉬** | 위로 상승합니다. |
| **Shift** | **상승** | 아래로 내려옵니다. |
| **Tab** | **경기현황판** | 킬뎃과 구조물 색칠 점유율을 확인가능합니다.|
| **마우스 우클릭** | **그래플링 훅** | 훅 발사로 빠르게 이동하며, 자신의 잉크에 맞추면 잉크 회복 속도가 크게 증가합니다. |

## ✨ 게임 플레이 팁

*   **그래플링 훅은 생명줄입니다**: 그래플링 훅은 단순한 이동기가 아닌, 생존과 공격의 흐름을 바꾸는 핵심 기술입니다. 위험할 때 탈출하거나, 고지대를 점령하고, 적의 허를 찌르는 용도 외에도, **자신이 칠한 잉크에 훅을 맞춰 잉크를 빠르게 회복**하는 것이 매우 중요합니다. 이를 통해 끊임없이 공격의 흐름을 이어갈 수 있습니다.
*   **거점을 중심으로 움직이세요**: 맵을 넓게 칠하는 것보다 거점을 확실하게 점령하고 지키는 것이 승리의 열쇠입니다. 팀원들과 함께 주요 길목을 차단하고 거점을 방어하세요.
*   **잉크는 생명입니다**: 잉크는 공격, 이동(대시) 등 모든 행동에 필요한 핵심 자원입니다. 항상 잉크 잔량을 확인하고, 부족할 때는 잠시 전투를 피하며 잉크를 회복하는 것이 중요합니다.

## 🛠️ 유저테스트를 통한 보완 사항
<img width="817" height="1190" alt="image" src="https://github.com/user-attachments/assets/57990a7c-88ac-4da7-88fd-8ff29d28ee62" />


---

피드백이나 질문이 있다면 언제든지 GitHub 이슈를 통해 알려주세요.
