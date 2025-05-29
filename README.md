# Project S

## 프로젝트 개요
Project S는 Unity를 사용하여 개발된 2D 액션 게임입니다. 플레이어는 다양한 적들과 전투하며, 복장 시스템을 통해 새로운 능력을 획득하고 성장할 수 있습니다.

## 주요 기능

### 1. 복장 시스템
- 4개의 파츠로 구성된 복장 세트
- 파츠 수집을 통한 세트 해금
- 세트별 특수 능력 부여
- UI를 통한 복장 관리

### 2. 전투 시스템
- 다양한 무기 속성
- 차징 공격 시스템
- 넉백 및 피격 효과
- 적 AI 패턴

### 3. 적 AI 시스템
- 상태 머신 기반 AI
- 다양한 적 타입
  - 일반 적
  - 엘리트 적
  - 보스
- 패턴 기반 공격 시스템

### 4. 아이템 시스템
- 복장 파츠
- 무기 속성
- 소비 아이템
- 인벤토리 관리

## 기술 스택
- Unity 2022.3 LTS
- C#
- 디자인 패턴
  - 상태 패턴 (State Pattern)
  - 옵저버 패턴 (Observer Pattern)
  - 전략 패턴 (Strategy Pattern)
  - 컴포지트 패턴 (Composite Pattern)

## 시스템 구조

### 1. 매니저 시스템
- GameManager: 게임 전체 상태 관리
- CostumeManager: 복장 시스템 관리
- InventoryManager: 인벤토리 관리
- WeaponManager: 무기 시스템 관리
- ObjectManager: 상호작용 오브젝트 관리

### 2. 상태 머신
- PlayerStateMachine: 플레이어 상태 관리
- EnemyStateMachine: 적 AI 상태 관리
- BossStateMachine: 보스 패턴 관리

### 3. 이벤트 시스템
- 이벤트 기반 데이터 전달
- UI 업데이트 자동화
- 시스템 간 느슨한 결합

## 설치 및 실행
1. Unity 2022.3 LTS 설치
2. 프로젝트 클론
3. Unity Hub에서 프로젝트 열기
4. Play 모드로 실행

## 개발 환경 설정
1. Unity 2022.3 LTS
2. Visual Studio 2022
3. Git

## 프로젝트 구조
```
Assets/
├── 03_Scripts/
│   ├── Boss/
│   ├── Enemy/
│   ├── Manager/
│   ├── Player/
│   ├── UI/
│   └── Utility/
├── Prefabs/
├── Scenes/
└── ScriptableObjects/
```

## 주요 클래스 설명

### 1. 플레이어 관련
- PlayerStateManager: 플레이어 상태 관리
- PlayerInputHandler: 입력 처리
- PlayerMovement: 이동 시스템

### 2. 적 관련
- BaseEnemy: 적 기본 클래스
- EnemyStateMachine: 적 AI 상태 관리
- BossStateMachine: 보스 패턴 관리

### 3. 시스템 관련
- CostumeManager: 복장 시스템
- InventoryManager: 인벤토리
- WeaponManager: 무기 시스템

## 라이선스
이 프로젝트는 MIT 라이선스를 따릅니다.

## 기여 방법
1. Fork the Project
2. Create your Feature Branch
3. Commit your Changes
4. Push to the Branch
5. Open a Pull Request

## 연락처
프로젝트 관련 문의사항은 이슈를 통해 남겨주세요. 