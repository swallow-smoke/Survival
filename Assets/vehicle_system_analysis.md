# Vehicle System (탑승물) 코드 분석

> 작성 기준: 현재 소스코드 전체 분석  
> 대상 파일: SmallSubVehicle, LargeSubVehicle, VehicleBody, SeatComponent, HatchComponent, BuoyancyController, SurfaceTrigger, PlayerContext, VehicleInjector, VehicleSpawner + Controller 수정분

---

## 목차

1. [전체 구조 개요](#1-전체-구조-개요)
2. [타입 / 인터페이스 정의](#2-타입--인터페이스-정의)
3. [컴포넌트별 상세 분석](#3-컴포넌트별-상세-분석)
4. [메시지 흐름 (이벤트 플로우)](#4-메시지-흐름-이벤트-플로우)
5. [소형 vs 대형 동작 차이](#5-소형-vs-대형-동작-차이)
6. [DI 등록 구조 (GLifeTimeScope)](#6-di-등록-구조-glifetimescope)
7. [Unity Editor 설정 체크리스트](#7-unity-editor-설정-체크리스트)
8. [알려진 이슈 / 주의사항](#8-알려진-이슈--주의사항)

---

## 1. 전체 구조 개요

```
GLifeTimeScope (VContainer 루트)
  └─ PlayerContext          → IPlayerContext 제공 (플레이어 루트 Transform 참조)
  └─ VehicleInjector        → 씬 배치 VehicleBody 인스턴스에 DI 주입
  └─ VehicleSpawner         → 런타임 스폰 + InjectGameObject

[소형 잠수함 프리팹]
  SmallSubVehicle (VehicleBody, IVehicleControllable)
    SeatComponent (ISeat)   → 착석 관리
    HatchComponent          → 인터랙션 진입점 (HatchType.SmallSeat)

[대형 잠수함 프리팹]
  LargeSubVehicle (VehicleBody, IVehicleControllable, ISurfaceDetectable)
    BuoyancyController      → 부력 / 비상 부력
    SurfaceTrigger          → 수면 도달 감지 (Trigger Collider)
  ── 내부 구조 ──
    HatchComponent (LargeEntrance)  → 외부→내부 진입
    HatchComponent (LargeExit)      → 내부→외부 탈출
    SeatComponent  (LargeSeat)      → 운전석 착석
    HatchComponent (LargeSeat)      → 운전석 인터랙션

[플레이어 오브젝트]
  MovementController        → 이동/점프/잠수 + 탑승물 입력 위임
  CameraController          → 시점 + 탑승물 전환 시 앵커 변경
  PlayerContext             → 씬에 배치, playerTrs = 플레이어 루트 지정
```

---

## 2. 타입 / 인터페이스 정의

### `PlayerVehicleState` (enum)
```
파일: Assets/001_Scripts/Type/States/PlayerVehicleState.cs
```

| 값 | 의미 |
|---|---|
| `None` | 도보 (육지 / 수영) |
| `InsideLarge` | 대형 잠수함 내부 보행 (미착석) |
| `Seated` | 탑승물 조종 중 (소형/대형 공통) |

### `VehicleConditionState` (enum)
```
파일: Assets/001_Scripts/Type/States/VehicleConditionState.cs
```
`Normal` / `Damaged` / `Destroyed` — 현재 VehicleBody에 저장만 됨, 실제 로직은 미구현(TODO)

### `IVehicle`
```
파일: Assets/001_Scripts/Data/Structure/Interface/IVehicle.cs
```
| 멤버 | 설명 |
|---|---|
| `float Fuel` | 현재 연료 |
| `float MaxFuel` | 최대 연료 |
| `VehicleConditionState Condition` | 내구 상태 |
| `bool ConsumeFuel(float)` | 연료 소비. 연료 0이면 false 반환 |
| `void Repair(float)` | 수리 (TODO) |

### `IVehicleControllable`
```
파일: Assets/001_Scripts/Data/Structure/Interface/IVehicleControllable.cs
```
| 멤버 | 설명 |
|---|---|
| `Transform CameraAnchor` | 착석 시 카메라가 따라갈 앵커 |
| `EnterControl()` | 착석 시 호출 — 입력 초기화 |
| `ExitControl()` | 이탈 시 호출 |
| `HandleMove(Vector2)` | WASD 입력 전달 |
| `HandleLook(Vector2)` | 마우스 델타 전달 (소형: 기체 회전 / 대형: 빈 메서드) |
| `HandleVertical(float)` | 상승(+1)/하강(-1)/중립(0) |

### `ISeat`
```
파일: Assets/001_Scripts/Data/Structure/Interface/ISeat.cs
```
| 멤버 | 설명 |
|---|---|
| `bool IsOccupied` | 착석 여부 |
| `Transform CameraAnchor` | 좌석 앵커 |
| `IVehicleControllable Controller` | 연결된 조종 컴포넌트 |
| `Sit(Transform player)` | 착석 처리 |
| `Stand(Transform player, Transform spawnPoint, Transform reparentTo)` | 이탈 처리 |

### `IPlayerContext`
```
파일: Assets/001_Scripts/Interface/IPlayerContext.cs
```
`Transform PlayerTrs` — 플레이어 루트 Transform (Rigidbody 소유자). **절대 Head가 아닌 Root**

### `ISurfaceDetectable`
```
파일: Assets/001_Scripts/Interface/ISurfaceDetectable.cs
```
`void OnReachedSurface()` — SurfaceTrigger가 수면 충돌 시 호출

### 메시지 구조체

| 구조체 | 위치 | 필드 |
|---|---|---|
| `PlayerVehicleStateMsg` | `Data/Message/Player/` | `PlayerVehicleState state` |
| `VehicleControlAssignedMsg` | `Data/Message/Player/` | `IVehicleControllable Controller` (null = 이탈) |

> `Data/Message/Vehicle/VehicleControlAssignedMsg.cs`는 주석만 남은 빈 파일 — 건드리지 말 것

---

## 3. 컴포넌트별 상세 분석

---

### `VehicleBody` (abstract)
```
파일: Assets/001_Scripts/Structure/VehicleBody.cs
상속: MonoBehaviour, IVehicle
```

**역할**: SmallSubVehicle / LargeSubVehicle 공통 연료·내구 데이터 베이스 클래스

**Inspector 필드**

| 필드 | 타입 | 설명 |
|---|---|---|
| `fuel` | float | 현재 연료 (protected — 자식 직접 접근 가능) |
| `maxFuel` | float | 최대 연료 |
| `condition` | VehicleConditionState | 내구 상태 |

**주의**: `VehicleInjector.Start()`가 `FindObjectsByType<VehicleBody>()` 로 모든 인스턴스를 찾아 DI 주입. VehicleBody 상속 클래스는 자동으로 대상이 됨.

---

### `SmallSubVehicle`
```
파일: Assets/001_Scripts/Structure/SmallSubVehicle.cs
상속: VehicleBody, IVehicleControllable
요구: Rigidbody
```

**역할**: 소형 잠수정 물리 조종. 마우스로 기체 자체를 회전시킴 (1인칭 느낌).

**Inspector 필드**

| 필드 | 기본값 | 설명 |
|---|---|---|
| `moveSpeed` | 10 | 목표 속도 (m/s) |
| `verticalSpeed` | 5 | 상승/하강 속도 |
| `turnSensitivity` | 2 | 마우스 회전 감도 |
| `pitchMin` | -80 | 피치 하한 (°) |
| `pitchMax` | 80 | 피치 상한 (°) |
| `accelForce` | 50 | 속도 차이에 곱해지는 힘 배율 |
| `_rb` | — | **필수 연결** — 자신의 Rigidbody |
| `_cameraAnchor` | — | **필수 연결** — 카메라가 따라갈 Transform |

**물리 방식**: `AddForce((targetVelocity - currentVelocity) * accelForce, Force)` — 직접 속도 대입 아님

**HandleLook**: 기체 전체 `transform.rotation`을 pitch/yaw로 회전. 카메라는 기체를 따라감.

**ExitControl**: `isControlled = false`만. 속도 초기화 없음 (관성 유지).

---

### `LargeSubVehicle`
```
파일: Assets/001_Scripts/Structure/LargeSubVehicle.cs
상속: VehicleBody, IVehicleControllable, ISurfaceDetectable
요구: Rigidbody
```

**역할**: 대형 잠수함 조종 + 내부 탑승자 관리 + 비상 부력 처리

**Inspector 필드**

| 필드 | 기본값 | 설명 |
|---|---|---|
| `moveSpeed` | 6 | 수평 목표 속도 |
| `verticalSpeed` | 3 | 수직 목표 속도 |
| `accelForce` | 30 | 힘 배율 |
| `_rb` | — | **필수** — 자신의 Rigidbody |
| `_cameraAnchor` | — | **필수** — 함교(브릿지) 내부 카메라 앵커 |
| `_interiorAnchor` | — | **필수** — 내부 탑승자들의 SetParent 대상 |
| `_buoyancy` | — | **필수** — 동일 GO의 BuoyancyController |
| `hasEmergencyBuoyancyModule` | false | 비상 부력 모듈 장착 여부 |

**HandleLook**: 빈 메서드 — 대형은 카메라만 회전, 기체는 회전하지 않음

**연료 소진 시**: `hasEmergencyBuoyancyModule == true`면 `BuoyancyController.ActivateEmergencyBuoyancy()` 호출 후 이동 로직 skip

**OnDestroy**: `_interiorAnchor`의 모든 자식을 `SetParent(null)` — 잠수함 파괴 시 내부 플레이어 살리기

**OnReachedSurface**: `BuoyancyController.DeactivateEmergencyBuoyancy()` 호출

---

### `BuoyancyController`
```
파일: Assets/001_Scripts/Structure/BuoyancyController.cs
요구: Rigidbody
```

**역할**: 잠수함 중력 비활성화 + 부력 가속도 관리

**Inspector 필드**

| 필드 | 기본값 | 설명 |
|---|---|---|
| `_rb` | — | **필수** — LargeSubVehicle과 같은 Rigidbody |
| `buoyancyAccel` | 0 | 평상시 부력 가속도 (0 = 중성부력) |
| `emergencyBuoyancyAccel` | 9.81 | 비상 부력 가속도 |

**Awake**: `_rb.useGravity = false` + `interpolation = Interpolate` 자동 설정

**FixedUpdate**: `AddForce(Vector3.up * accel * mass, Force)` — accel=0이면 skip

**공개 API**

```csharp
ActivateEmergencyBuoyancy()    // 비상 부력 ON
DeactivateEmergencyBuoyancy()  // 비상 부력 OFF
SetBuoyancyAccel(float)        // 평상시 부력 값 변경
bool IsEmergencyActive         // 상태 확인
```

---

### `SurfaceTrigger`
```
파일: Assets/001_Scripts/Structure/SurfaceTrigger.cs
요구: Collider (isTrigger = true 필수)
```

**역할**: 잠수함이 수면에 도달했을 때 `ISurfaceDetectable.OnReachedSurface()` 호출

**Inspector 필드**

| 필드 | 설명 |
|---|---|
| `surfaceLayer` | 수면 레이어 (LayerMask) |

**배치 위치**: 잠수함 상단 (가장 먼저 수면에 닿는 지점)

**Awake**: `GetComponentInParent<ISurfaceDetectable>()` — LargeSubVehicle이 부모 계층에 있어야 함

---

### `SeatComponent`
```
파일: Assets/001_Scripts/Structure/SeatComponent.cs
구현: ISeat
DI: IPublisher<PlayerVehicleStateMsg>, IPublisher<VehicleControlAssignedMsg>, IPlayerContext
```

**역할**: 착석/이탈 처리의 핵심. Rigidbody 물리 전환 + 메시지 발행 담당.

**Inspector 필드**

| 필드 | 설명 |
|---|---|
| `seatAnchor` | **필수** — 플레이어가 SetParent될 Transform |
| `standSpawnPoint` | **필수** — 이탈 시 플레이어 배치 위치 |
| `controllerBehaviour` | **필수** — SmallSubVehicle 또는 LargeSubVehicle MonoBehaviour |
| `standState` | 이탈 후 PlayerVehicleState (소형=None, 대형 운전석=InsideLarge) |
| `standReparentTarget` | 이탈 시 SetParent 대상 (소형=null, 대형=InteriorAnchor) |

**Awake**: `controllerBehaviour as IVehicleControllable` 캐스팅 — `controllerBehaviour`가 IVehicleControllable 구현 안 하면 LogError

**Sit() 처리 순서** (중요):
1. `playerRb.linearVelocity = zero` + `angularVelocity = zero`
2. `playerRb.isKinematic = true` ← **SetParent 이전에 반드시**
3. `player.SetParent(seatAnchor)` + `localPosition/Rotation = 0`
4. `_controller.EnterControl()`
5. Publish `PlayerVehicleStateMsg(Seated)` + `VehicleControlAssignedMsg(_controller)`

**Stand() 처리 순서**:
1. `_controller.ExitControl()`
2. `player.SetParent(reparentTo)`
3. `player.position/rotation = spawnPoint`
4. Publish `PlayerVehicleStateMsg(standState)` + `VehicleControlAssignedMsg(null)`

> `Stand()`는 Rigidbody.isKinematic을 건드리지 않음 — `MovementController.OnVehicleStateChanged`가 `None` 수신 시 `isKinematic = false`로 복원

**StandWithDefaults()**: 내부 저장된 `_playerTrs`, `standSpawnPoint`, `standReparentTarget` 사용 — 외부 키 입력(E키 이탈 등)에서 호출

---

### `HatchComponent`
```
파일: Assets/001_Scripts/Structure/HatchComponent.cs
구현: IInteractable, IInteractableInfo
DI: IPublisher<PlayerVehicleStateMsg>, IPlayerContext
```

**역할**: 인터랙션 진입점. HatchType에 따라 분기.

**HatchType 분기**

| 타입 | 동작 |
|---|---|
| `SmallSeat` | `linkedSeat.Sit(player)` |
| `LargeEntrance` | 내부 앵커로 SetParent + 위치 이동 + `InsideLarge` 발행 |
| `LargeSeat` | `linkedSeat.Sit(player)` |
| `LargeExit` | SetParent(null) + 외부 좌표 이동 + `None` 발행 |

**Inspector 필드**

| 필드 | 사용 HatchType | 설명 |
|---|---|---|
| `type` | 전체 | HatchType 선택 |
| `linkedSeat` | SmallSeat, LargeSeat | 연결된 SeatComponent |
| `interiorSpawnPoint` | LargeEntrance | 내부 진입 위치 |
| `exteriorSpawnPoint` | LargeExit | 외부 하차 위치 |
| `parentVehicle` | LargeEntrance, LargeExit | LargeSubVehicle 참조 (InteriorAnchor 접근용) |
| `displayLabel` | 전체 | 인터랙션 UI 표시 텍스트 |

**중요**: `_playerContext.PlayerTrs`는 플레이어 ROOT Transform이어야 함 (Head면 Rigidbody를 못 찾음)

---

### `PlayerContext`
```
파일: Assets/001_Scripts/Structure/PlayerContext.cs
구현: IPlayerContext
DI 등록: RegisterComponentInHierarchy<PlayerContext>().As<IPlayerContext>()
```

**Inspector 필드**

| 필드 | 설명 |
|---|---|
| `playerTrs` | **필수** — 플레이어 루트 오브젝트 Transform |

> VCam이 Head를 따라가더라도, 이 필드는 반드시 Rigidbody가 붙은 **Root**를 가리켜야 함

---

### `VehicleInjector`
```
파일: Assets/001_Scripts/Structure/VehicleInjector.cs
DI 등록: RegisterComponentInHierarchy<VehicleInjector>()
```

**역할**: 씬에 배치된 VehicleBody 파생 클래스 전부에 DI 주입.

**Start()**: `FindObjectsByType<VehicleBody>(FindObjectsSortMode.None)` → 각각 `_resolver.InjectGameObject(vehicle.gameObject)`

> 씬에 잠수함이 2개 이상 있어도 동작. 단, Start() 이후에 씬에 추가된 오브젝트는 대상 아님 (런타임 스폰은 VehicleSpawner 사용)

---

### `VehicleSpawner`
```
파일: Assets/001_Scripts/Managers/VehicleSpawner.cs
DI 등록: RegisterComponentInHierarchy<VehicleSpawner>()
```

**역할**: 런타임 잠수함 스폰 + DI 주입

**Inspector 필드**

| 필드 | 설명 |
|---|---|
| `smallSubPrefab` | 소형 잠수정 프리팹 |
| `largeSubPrefab` | 대형 잠수함 프리팹 |
| `_trs` | 기본 스폰 위치용 Transform |
| `_rot` | 기본 스폰 회전 (Quaternion — 에디터에서 직접 편집 불편) |

**API**
```csharp
SpawnSmallSub(Vector3 position, Quaternion rotation)  // 지정 위치
SpawnSmallSub()                                        // _trs, _rot 사용
SpawnLargeSub(Vector3 position, Quaternion rotation)
```

---

### `MovementController` (수정 부분)
```
파일: Assets/001_Scripts/Controller/MovementController.cs
```

**추가 필드**

| 필드 | 설명 |
|---|---|
| `_activeVehicle` | 현재 조종 중인 IVehicleControllable (null = 없음) |
| `_vehicleState` | 현재 PlayerVehicleState |

**Awake**: `_rb.interpolation = RigidbodyInterpolation.Interpolate` 자동 설정

**FixedUpdate 분기**

```
Seated    → return (탑승체가 물리 담당)
InsideLarge → MovePosition (kinematic 이동)
Swimming  → linearVelocity 직접 대입
else      → linearVelocity 직접 대입
```

**입력 위임 로직**

| 메서드 | 탑승체 있을 때 |
|---|---|
| `OnMove` | `_activeVehicle.HandleMove(inputValue)` + return |
| `OnLook` | `_activeVehicle is SmallSubVehicle` → `HandleLook` |
| `OnJump` | `HandleVertical(1f / 0f)` + return |
| `OnShift` | `HandleVertical(-1f / 0f)` + return |

**OnVehicleStateChanged**:
- `_rb.isKinematic = (state != None)`
- `state == None` → `_rb.useGravity = !isSwimming`

**구독 목록**: `PlayerStatMessage`, `PlayerUIStateMsg`, `PlayerVehicleStateMsg`, `VehicleControlAssignedMsg`

---

### `CameraController` (수정 부분)
```
파일: Assets/001_Scripts/Controller/CameraController.cs
```

**추가 필드**

| 필드 | 설명 |
|---|---|
| `_playerTrs` | Start()에서 초기 `_trs` 저장 (private, not serialized) |
| `_activeVehicle` | 현재 VehicleControlAssignedMsg로 수신된 컨트롤러 |

**OnLook 로직**:
- `_activeVehicle is SmallSubVehicle` → 카메라 pitch/yaw 계산 skip (기체가 직접 회전하므로)
- 그 외(null 또는 LargeSubVehicle) → 기존 pitch/yaw 계산 계속

**LateUpdate 로직**: 동일하게 SmallSub 체크 후 early return

**OnVehicleControlAssigned**:
- 탑승 시: `_trs = _activeVehicle.CameraAnchor` (잠수함 앵커로 전환)
- 이탈 시: `_trs = _playerTrs` (플레이어 원래 앵커 복원)
- 전환 시 pitch/yaw를 새 `_trs.eulerAngles`로 초기화

**구독 목록**: `PlayerUIStateMsg`, `VehicleControlAssignedMsg`

---

## 4. 메시지 흐름 (이벤트 플로우)

### 소형 잠수정 탑승

```
플레이어 → HatchComponent.Interact()
  └─ HatchType.SmallSeat
  └─ SeatComponent.Sit(player)
       ├─ playerRb.isKinematic = true  [물리 충돌 방지]
       ├─ player.SetParent(seatAnchor)
       ├─ SmallSubVehicle.EnterControl()
       ├─ Publish PlayerVehicleStateMsg(Seated)
       │    └─ MovementController.OnVehicleStateChanged
       │         └─ _rb.isKinematic = true
       └─ Publish VehicleControlAssignedMsg(SmallSubVehicle)
            ├─ MovementController.OnVehicleControlAssigned
            │    └─ _activeVehicle = SmallSubVehicle
            └─ CameraController.OnVehicleControlAssigned
                 └─ _trs = SmallSubVehicle.CameraAnchor
```

### 소형 잠수정 조종

```
InputSystem → MovementController.OnMove(ctx)
  └─ _activeVehicle.HandleMove(inputValue)   → SmallSubVehicle.moveInput 업데이트

InputSystem → MovementController.OnLook(ctx)
  └─ _activeVehicle is SmallSubVehicle
  └─ small.HandleLook(delta)   → transform.rotation 변경 (기체 회전)

CameraController.OnLook(ctx)
  └─ _activeVehicle is SmallSubVehicle → return (skip pitch/yaw)

CameraController.LateUpdate
  └─ _activeVehicle is SmallSubVehicle → return
  └─ _trs = CameraAnchor → 기체 Transform 따라다님
```

### 소형 잠수정 이탈

```
[SeatComponent.StandWithDefaults() 호출 시]
  └─ SmallSubVehicle.ExitControl()
  └─ player.SetParent(null)
  └─ player.position = standSpawnPoint.position
  └─ Publish PlayerVehicleStateMsg(None)
       └─ MovementController: _rb.isKinematic=false, useGravity=!isSwimming
  └─ Publish VehicleControlAssignedMsg(null)
       ├─ MovementController: _activeVehicle = null
       └─ CameraController: _trs = _playerTrs (원래 앵커 복원)
```

### 대형 잠수함 탑승 (2단계)

**1단계: 외부 → 내부 진입**
```
HatchComponent.Interact() [LargeEntrance]
  └─ EnterLarge(player)
       ├─ player.SetParent(InteriorAnchor)
       ├─ player.position = interiorSpawnPoint.position
       └─ Publish PlayerVehicleStateMsg(InsideLarge)
            └─ MovementController: _rb.isKinematic=true, InsideLarge 분기 (MovePosition)
```

**2단계: 내부 보행 → 운전석 착석**
```
HatchComponent.Interact() [LargeSeat]
  └─ SeatComponent.Sit(player)
       ├─ playerRb.isKinematic = true  (이미 true일 가능성 있음, 무해)
       ├─ player.SetParent(seatAnchor)  ← seatAnchor는 LargeSub 내 Transform
       ├─ LargeSubVehicle.EnterControl()
       ├─ Publish PlayerVehicleStateMsg(Seated)
       └─ Publish VehicleControlAssignedMsg(LargeSubVehicle)
            └─ CameraController: _trs = LargeSub.CameraAnchor (함교 앵커)
```

**대형 운전석 이탈 → 내부 보행**
```
SeatComponent.StandWithDefaults()
  standState = InsideLarge, standReparentTarget = InteriorAnchor 설정 기준
  └─ LargeSubVehicle.ExitControl()
  └─ player.SetParent(InteriorAnchor)
  └─ player.position = standSpawnPoint.position
  └─ Publish PlayerVehicleStateMsg(InsideLarge)
  └─ Publish VehicleControlAssignedMsg(null)
       └─ CameraController: _trs = _playerTrs
```

**내부 보행 → 외부 탈출**
```
HatchComponent.Interact() [LargeExit]
  └─ ExitToOutside(player)
       ├─ player.SetParent(null)
       ├─ player.position = exteriorSpawnPoint.position
       └─ Publish PlayerVehicleStateMsg(None)
            └─ MovementController: _rb.isKinematic=false, useGravity=!isSwimming
```

---

## 5. 소형 vs 대형 동작 차이

| 항목 | SmallSubVehicle | LargeSubVehicle |
|---|---|---|
| 카메라 | 기체 회전 따라감 (`_trs = CameraAnchor`) | 함교 내 앵커에서 독립 회전 |
| HandleLook | 기체 자체 회전 (pitch+yaw) | 빈 메서드 (no-op) |
| CameraController.OnLook | SmallSub 감지 시 skip | 정상 pitch/yaw 계산 |
| 내부 탑승자 | 없음 (1인승) | `_interiorAnchor`에 자식 관리 |
| 부력 | 없음 (자체 이동) | BuoyancyController 필수 |
| 연료 소진 | 이동만 안 됨 | 비상 부력 발동 (모듈 있을 때) |
| 착석 진입 | HatchType.SmallSeat 1단계 | LargeEntrance → LargeSeat 2단계 |
| 착석 이탈 후 상태 | None (도보) | InsideLarge (내부 보행) |
| standReparentTarget | null (SetParent(null)) | InteriorAnchor |

---

## 6. DI 등록 구조 (GLifeTimeScope)

```csharp
// Player 메시지 브로커
builder.RegisterMessageBroker<PlayerVehicleStateMsg>(options);
builder.RegisterMessageBroker<VehicleControlAssignedMsg>(options);

// Services
builder.RegisterComponentInHierarchy<PlayerContext>().As<IPlayerContext>();
builder.RegisterComponentInHierarchy<VehicleInjector>();
builder.RegisterComponentInHierarchy<VehicleSpawner>();
```

> `VehicleControlAssignedMsg`는 Player 리전에 1회만 등록됨. Vehicle 리전에 중복 등록하면 컴파일 오류.

**VehicleBody 파생 클래스 주입 경로**:
- 씬 배치: `VehicleInjector.Start()` → `InjectGameObject`
- 런타임 스폰: `VehicleSpawner.SpawnXxx()` → `InjectGameObject`

**SeatComponent, HatchComponent 주입 경로**:
- `VehicleBody` 하위 자식 오브젝트이므로 `InjectGameObject`가 계층 전체에 주입

---

## 7. Unity Editor 설정 체크리스트

### 공통

- [ ] **PlayerContext.playerTrs** → 플레이어 루트 오브젝트 (Rigidbody 있는 것) 연결
  - VCam이 Head를 따라가도 이 필드는 Root 지정
- [ ] **GLifeTimeScope 씬 배치 오브젝트에** PlayerContext, VehicleInjector, VehicleSpawner 자식으로 존재해야 함
- [ ] **Input Action** — `OnLook` 바인딩을 MovementController의 PlayerInput에도 연결 (`Look` 액션)

### 소형 잠수정 프리팹

- [ ] `SmallSubVehicle._rb` → 자신의 Rigidbody 연결
- [ ] `SmallSubVehicle._cameraAnchor` → 카메라 위치용 빈 Transform 연결
- [ ] `SmallSubVehicle.fuel` / `maxFuel` 설정
- [ ] `SeatComponent.seatAnchor` → 플레이어가 앉을 위치 Transform
- [ ] `SeatComponent.standSpawnPoint` → 이탈 후 스폰 위치
- [ ] `SeatComponent.controllerBehaviour` → SmallSubVehicle 컴포넌트
- [ ] `SeatComponent.standState` → `None`
- [ ] `SeatComponent.standReparentTarget` → 비워둠 (null)
- [ ] `HatchComponent.type` → `SmallSeat`
- [ ] `HatchComponent.linkedSeat` → SeatComponent 연결
- [ ] `HatchComponent.displayLabel` → "탑승" 등 원하는 텍스트
- [ ] SmallSubVehicle Rigidbody → `useGravity = false` (수중 전용이면)

### 대형 잠수함 프리팹

- [ ] `LargeSubVehicle._rb` → 자신의 Rigidbody
- [ ] `LargeSubVehicle._cameraAnchor` → 함교 내 카메라 앵커
- [ ] `LargeSubVehicle._interiorAnchor` → 내부 탑승자 부모용 Transform
- [ ] `LargeSubVehicle._buoyancy` → BuoyancyController 연결
- [ ] `LargeSubVehicle.fuel` / `maxFuel` 설정
- [ ] `BuoyancyController._rb` → LargeSubVehicle Rigidbody (같은 GO)
- [ ] `BuoyancyController.buoyancyAccel` → 중성부력 원하면 0
- [ ] `SurfaceTrigger.surfaceLayer` → 수면 레이어 선택
- [ ] SurfaceTrigger 오브젝트 → 잠수함 상단에 배치, Collider `isTrigger = true`
- [ ] **입구 HatchComponent** (`LargeEntrance`)
  - `type = LargeEntrance`
  - `interiorSpawnPoint` → 내부 진입 위치
  - `parentVehicle` → LargeSubVehicle
- [ ] **탈출구 HatchComponent** (`LargeExit`)
  - `type = LargeExit`
  - `exteriorSpawnPoint` → 외부 탈출 위치
  - `parentVehicle` → LargeSubVehicle
- [ ] **운전석 HatchComponent** (`LargeSeat`)
  - `type = LargeSeat`
  - `linkedSeat` → 운전석 SeatComponent
- [ ] **운전석 SeatComponent**
  - `seatAnchor` → 운전석 위치 Transform
  - `standSpawnPoint` → 운전석 이탈 후 위치 (내부)
  - `controllerBehaviour` → LargeSubVehicle
  - `standState` → `InsideLarge`
  - `standReparentTarget` → InteriorAnchor Transform

---

## 8. 알려진 이슈 / 주의사항

### 물리 충돌 (설계 결정 사항)
- `SeatComponent.Sit()`에서 `isKinematic = true`를 **SetParent 이전에** 설정해야 함
- 순서가 바뀌면 플레이어 Rigidbody가 잠수함 Rigidbody와 물리 충돌해 튕겨나감
- 현재 코드는 올바른 순서로 되어 있음

### VCam 타겟 vs PlayerContext.playerTrs
- Cinemachine VCam이 Head Transform을 Follow해도 문제없음
- 단, `PlayerContext.playerTrs`는 반드시 Root (Rigidbody) 지정
- `HatchComponent`와 `SeatComponent`가 이 Transform으로 `SetParent` 및 Rigidbody 조작을 수행

### LargeSubVehicle 이탈 시 isKinematic 복원
- `SeatComponent.Stand()`는 isKinematic을 직접 건드리지 않음
- `MovementController.OnVehicleStateChanged`가 `InsideLarge` 수신 시 `isKinematic = true`로 세팅
- `None` 수신 시 `isKinematic = false`로 복원
- 즉, isKinematic 상태는 항상 `PlayerVehicleState`와 연동됨

### OnLook 입력 연결
- `MovementController.OnLook`은 소형 잠수정 조종에만 사용
- PlayerInput 컴포넌트에서 `Look` 액션을 MovementController에도 바인딩해야 함
- CameraController의 `OnLook`은 대형/일반 상태에서만 pitch/yaw 처리

### VehicleSpawner._rot 필드
- `Quaternion` 타입은 Inspector에서 직접 편집이 불편함
- 스폰 방향 커스텀이 필요하면 `Vector3 _eulerRot`으로 바꾸고 `Quaternion.Euler(_eulerRot)` 변환 권장

### Data/Message/Vehicle/VehicleControlAssignedMsg.cs
- 주석만 남은 빈 파일 — 삭제해도 무방하나, 삭제 시 .meta 파일도 함께 제거 필요
- 현재는 `Data/Message/Player/VehicleControlAssignedMsg.cs`가 정본

### Repair() 미구현
- `VehicleBody.Repair()`는 TODO 상태
- 내구도 시스템 연동 전까지 호출해도 아무 동작 없음

### LargeSub OnDestroy 재배치 미완성
- 파괴 시 내부 탑승자를 `SetParent(null)`로 분리는 하지만 월드 좌표 재배치 없음
- 잠수함 파괴 위치가 깊은 수중이면 플레이어가 그 자리에 떠 있게 됨
- 가까운 수면 좌표 계산 로직 별도 필요 (TODO 주석 있음)
