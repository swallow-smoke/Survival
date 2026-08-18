# Survival 통합 물 시스템

## 구성과 책임

- `WaterQueryService`는 씬의 `IWaterBody`를 등록하고 위치 질의를 하나로 통합한다. 질의는 Bounds 1차 필터와 `TrySample` 2차 판정을 거친 뒤 우선순위, 로컬 수역, 포함 깊이, 등록 순서로 하나를 고른다.
- `OceanBody`, `LakeWaterBody`, `SplineRiverWaterBody`는 렌더러나 물리 Trigger와 무관하게 `WaterSample`을 계산한다.
- `PlayerWaterSensor`는 발, 가슴, 머리, 카메라를 나누어 검사하고 hysteresis를 적용한다. `MovementController`는 가슴 샘플의 `Swimming`만 이동 모드에 사용한다.
- `UnderwaterVolumeController`는 카메라 샘플만 사용해 URP Volume의 weight를 부드럽게 전환한다.
- `BuoyancyController`는 각 부력점에서 동일한 질의 API를 사용한다. Rigidbody 중력은 유지되며, 부력·수직 감쇠·물 흐름과의 상대 속도 힘을 `AddForceAtPosition`으로 적용한다.
- `SplineRiverWaterBody`는 Splines 2.8.4를 월드 거리로 샘플링해 공간 질의 캐시와 렌더 메시를 함께 만든다. 런타임 질의나 `FixedUpdate`에서 리스트와 배열을 생성하지 않는다.

WorldBuilder의 `WorldBuilder.Runtime.Water`는 베이크된 대규모 월드 데이터를 위한 별도 시스템이다. 씬에서 즉시 편집하는 이 시스템과 이름공간 및 생명주기를 공유하지 않는다. 대규모 스트리밍 월드에서 두 데이터를 함께 쓸 경우 WorldBuilder 결과를 `IWaterBody`로 노출하는 어댑터를 추가하고, 두 서비스를 동시에 게임플레이에 주입하지 않는다.

## 씬 설정

### 공통 서비스

1. 씬에 `WaterQueryService`를 하나 둔다. 기존 `SampleScene/WaterSystem` 오브젝트는 그대로 사용할 수 있다.
2. `GLifeTimeScope`가 서비스를 `IWaterQueryService`, 구 API 호환용 `IWaterQuery`, `IWaterRegistry`로 등록한다.
3. 물 컴포넌트는 VContainer 주입 또는 안전한 씬 레지스트리 연결을 통해 활성화/비활성화와 서비스 재생성에 맞춰 등록된다.

### 바다

`GameObject > Survival > Water > Ocean`을 선택한다. 생성된 `OceanBody`에서 다음을 설정한다.

- `Infinite`: 무한 XZ 바다 또는 `Finite Size` 범위
- `Sea Level`: 기준 수면
- `Use Waves`와 최대 4개의 방향, 진폭, 파장, 속도
- `Current Direction/Speed`: 게임플레이 해류
- `Priority`: 겹치는 로컬 물보다 우선해야 할 때만 높인다

기본 생성 Surface는 Water 레이어와 프로젝트 물 머티리얼을 사용한다. CPU 질의와 `Custom/Water` 셰이더는 동일한 파도 값을 MaterialPropertyBlock으로 공유한다. 수면 위 위치도 유효한 바다 샘플을 받을 수 있으나, 잠수 여부는 반드시 `SignedDepth > 0`으로 판정한다.

### 호수

`GameObject > Survival > Water > Lake`를 선택한다. `BoxCollider`는 질의 전용 Trigger이며 실제 포함 판정은 Collider의 로컬 공간에서 수행되므로 회전된 호수도 정확하다. `Surface` Transform, `Depth`, 약한 흐름과 Priority를 설정한다. Polygon/Mesh 호수는 새 `IWaterBody` 구현으로 확장한다.

### 강

1. `GameObject > Survival > Water > Spline River`를 선택한다.
2. Unity Spline 도구로 Knot를 두 개 이상 배치한다.
3. `RiverProfile` 또는 컴포넌트의 fallback 폭, 깊이, 흐름, 샘플 간격, UV 값을 설정한다.
4. `Generate / Rebuild`를 누른다. Auto Rebuild가 켜져 있으면 Edit Mode의 Spline 변경을 debounce한 뒤 갱신한다.
5. 영구 Mesh 에셋이 필요하면 `Bake Mesh Asset`을 사용한다. 임시 생성 Mesh는 재사용되고 씬 재로드 시 다시 생성된다.
6. `Reverse Flow`, 끝 Fade, 측면·바닥, 선택적 MeshCollider를 설정한다. 물 판정과 부력은 이 Collider에 의존하지 않는다.

가변 구간은 `River Parameter Keys`를 정규화 위치 순서로 추가해 Width, Depth, Flow Speed, Surface Offset을 보간한다. UV의 V축과 tangent는 실제 강 길이 및 흐름 방향을 따른다.

### Terrain carving

강 오브젝트의 `RiverTerrainCarver`에 Terrain을 지정하고 `Apply Carving`을 명시적으로 누른다. 강 폭과 깊이에 따라 높이를 내리고 Bank Falloff로 강변을 섞는다. 자동 또는 런타임 변경은 없으며 Unity Undo로 원본 TerrainData를 복구할 수 있다. 공유 TerrainData를 사용하는 여러 씬에서는 Apply 전에 복제 여부를 확인한다.

## 플레이어와 카메라

`MovementController`는 기존 프리팹 호환을 위해 런타임에 누락된 `PlayerWaterSensor`와 `UnderwaterVolumeController`를 한 번 추가한다. 정식 프리팹에는 두 컴포넌트를 미리 추가해 다음 Transform을 명시하는 것이 좋다.

- Feet: 기존 `footTrs`
- Chest: 캐릭터 몸 중심
- Head: 머리 또는 시점 높이
- Camera Point: 실제 카메라 Transform

센서는 `TouchingWater`, `Wading`, `Swimming`, `HeadUnderwater`, `CameraUnderwater`를 분리한다. 기존 `PlayerMovementMessage.isSwimming`은 유지되고, 상세 상태는 `PlayerWaterStateMessage`로 발행된다.

`UnderwaterProfile`은 tint, saturation, vignette, 전환 시간과 향후 fog/distortion/caustics 데이터를 가진다. 기본 컨트롤러는 Unity 6 URP 호환 Volume의 Color Adjustments와 Vignette를 사용하고 전역 `RenderSettings.fog`를 수정하지 않는다.

## Rigidbody와 잠수함

일반 오브젝트에 `BuoyancyController`를 추가하고 Rigidbody, 부피 또는 Box 크기를 설정한다. 명시적인 `BuoyancyPoint`가 없으면 Box의 네 모서리와 중심을 사용한다. 큰 선박은 Transform 기반 점을 직접 배치하고 Weight/Radius를 조정한다.

- `NaturalBuoyancy`: 밀도와 잠긴 부피에 따른 일반 부력
- `NeutralBuoyancy`: 질량과 중력에 맞춘 잠수정용 중성 부력
- `ControlledDepth`: `Target Surface Depth`를 유지하는 힘 추가
- `EmergencyAscent`: 제한된 추가 상승력

`SmallSubVehicle`은 중성 부력을 사용하고 조종 수직 입력을 목표 속도에 합친다. `LargeSubVehicle`은 기존 이동/비상 부력 메시지 흐름을 유지하며, 수직 조종 중에는 자동 상승력이 입력과 싸우지 않도록 `OverrideVertical`을 사용한다. 컴포넌트 비활성화, 파괴, 물 이탈 시 원래 damping과 gravity 설정이 복구된다.

## URP 렌더링

`Assets/003_Resources/Shader/River.shader`의 `Custom/Water`는 깊이 색, 두 Normal Map, 굴절, Fresnel, foam, smoothness, 투명도, 강 길이 방향 UV와 CPU 동기 파도를 제공한다.

URP Asset에서 Depth Texture와 Opaque Texture가 필요하다. `Tools > Survival > Water > Validate URP Rendering`으로 확인한다. 검증기는 사용자 Renderer Asset을 변경하지 않는다. Full Screen 왜곡/caustics가 필요하면 Unity 6 Render Graph 호환 Renderer Feature를 별도 추가하고 `UnderwaterProfile` 값을 사용한다.

## 기존 코드 마이그레이션

- `OceanBody`의 스크립트 GUID와 `seaLevel` 직렬화 이름은 유지된다.
- `LocalWaterVol`과 `WaterVolume`은 `LakeWaterBody` 호환 컴포넌트로 남아 기존 씬/프리팹을 읽는다.
- `IWaterbody`, `IWaterQuery`, 구 Registry 메서드는 `[Obsolete]` 어댑터이며 내부적으로 새 API를 호출한다. 새 코드에서 사용하지 않는다.
- `BuoyancyTriggerProxy`와 `BuoyancyColliderBuilder`는 직렬화/소스 호환용 no-op 또는 obsolete 타입이다. 새 부력은 Trigger를 만들지 않는다.
- 기존 `SampleScene`의 Ocean과 `WaterQueryService` 참조는 유지되며, 플레이어 및 차량 보조 컴포넌트는 누락 시 런타임에 생성된다.

## 검증

EditMode 테스트는 빈 위치 false, Ocean 높이/잠수, 회전 Lake, 중복 등록/해제, 겹침 우선순위, 직선·곡선 River, 폭·깊이·흐름과 Mesh 생성을 검사한다.

수동 Play Mode 절차:

1. `Tools > Survival > Water > Create Validation Setup`으로 검증 오브젝트를 만들거나 SampleScene을 연다.
2. Console 오류가 없는지 확인한다.
3. 플레이어의 발, 가슴, 머리, 카메라가 차례로 잠길 때 상태가 순서대로 전환되고 수면 경계에서 흔들리지 않는지 확인한다.
4. 바다, 호수, 강에서 같은 수영 입력이 동작하는지 확인한다.
5. 카메라만 수면 아래일 때 Volume weight가 부드럽게 올라가는지 확인한다.
6. Buoyant Test Cube가 중력을 유지하면서 수면에 안정되고, 강에서는 흐름 방향으로 점진적으로 이동하는지 확인한다.
7. 물 밖으로 옮기거나 BuoyancyController를 끄면 damping이 복구되는지 확인한다.
8. Small/Large Sub를 탑승해 이동, 수직 입력, 이탈, Emergency Ascent가 유지되는지 확인한다.
9. Profiler의 GC Alloc 열에서 반복 물 질의와 Buoyancy FixedUpdate가 0 B인지 확인한다.

에디터/개발 빌드의 `WaterDebugProbe`는 현재 WaterBody, 타입, 수면, SignedDepth, FlowVelocity를 표시한다.
