# Better Shuttle Launch 텍스처 제작 가이드

이 문서는 Better Shuttle Launch에서 교체 가능한 UI 텍스처의 파일명, 배치 경로, 권장 크기를 정리한다.

## 기본 규칙

- 실제 배포 폴더 기준 루트는 `Better Shuttle Launch/Textures`이다.
- 코드에서는 확장자 없이 `UI/Commands/BSL_LaunchWhenReady` 같은 경로로 로드한다.
- 파일은 `.png`를 권장한다. 투명 배경이 필요한 아이콘은 알파 채널을 포함한다.
- 지정 파일이 없으면 기존 바닐라 아이콘 또는 코드로 그린 기본 UI를 그대로 사용한다.
- 현재 코드는 9-slice 처리를 하지 않고 텍스처를 사각형에 맞춰 늘려 그린다. 테두리가 있는 배경 이미지는 표시 비율에 맞춰 제작하는 편이 안전하다.

## 폴더 구조

```text
Better Shuttle Launch/
  Textures/
    UI/
      Commands/
      Tracker/
      Status/
      Badges/
```

## 명령 버튼 아이콘

배치 경로: `Better Shuttle Launch/Textures/UI/Commands`

| 파일명 | 권장 크기 | 비율 | 용도 | 현재 사용 |
| --- | ---: | ---: | --- | --- |
| `BSL_LaunchWhenReady.png` | 128 x 128 | 1:1 | 통합 버튼 `준비되면 발사` | 사용 |
| `BSL_CancelLaunch.png` | 128 x 128 | 1:1 | 예약 중일 때 `예약 발사 취소` | 사용 |
| `BSL_OpenTracker.png` | 64 x 64 | 1:1 | 관제 UI 헤더 아이콘 | 사용 |
| `BSL_LaunchToSettlement.png` | 128 x 128 | 1:1 | `정착지로 발사` 전용 아이콘 후보 | 예약 |
| `BSL_Return.png` | 128 x 128 | 1:1 | `귀환` 전용 아이콘 후보 | 예약 |

권장 스타일:
- RimWorld 기즈모에서 작게 표시되므로 중심부 실루엣이 뚜렷해야 한다.
- 바깥쪽 8~12px 정도는 투명 여백을 남긴다.
- `BSL_LaunchWhenReady`와 `BSL_CancelLaunch`는 상태 차이가 즉시 보이도록 색상이나 형태를 분리한다.

## 관제 UI 패널

배치 경로: `Better Shuttle Launch/Textures/UI/Tracker`

| 파일명 | 권장 크기 | 표시 크기 | 비율 | 용도 |
| --- | ---: | ---: | ---: | --- |
| `Panel_Background.png` | 860 x 520 | 약 430 x 260 | 43:26 | 관제 창 전체 배경 |
| `Panel_Header.png` | 860 x 60 | 약 430 x 30 | 43:3 | 관제 창 헤더 |
| `Row_Background.png` | 820 x 120 | 약 410 x 60 | 41:6 | 왕복선 1행 배경 |
| `Progress_Rail.png` | 512 x 16 | 가변 x 8 | 긴 막대 | 이동 경로 바탕선 |
| `Progress_Fill.png` | 512 x 16 | 진행률만큼 가변 | 긴 막대 | 이동 경로 진행 채움 |
| `Shuttle_Marker.png` | 64 x 64 | 약 28 x 28 | 1:1 | 진행바 위 왕복선 마커 프레임 |
| `Filter_Local.png` | 64 x 64 | 26 x 26 | 1:1 | 현재 맵 왕복선만 보기 토글 |
| `Endpoint_Empty.png` | 64 x 64 | 24 x 24 | 1:1 | 출발지/도착지가 빈 월드 타일일 때 쓰는 fallback 아이콘 |
| `Endpoint_Map.png` | 64 x 64 | 24 x 24 | 1:1 | 맵 대상 아이콘이 없을 때 쓰는 fallback 아이콘 |
| `Endpoint_Faction.png` | 64 x 64 | 24 x 24 | 1:1 | 세력 대상 아이콘이 없을 때 쓰는 fallback 아이콘 |
| `Button_Normal.png` | 160 x 48 | 80 x 24 또는 26 x 26 | 10:3 또는 1:1 대응 | 기본 버튼 |
| `Button_Hover.png` | 160 x 48 | 80 x 24 또는 26 x 26 | 10:3 또는 1:1 대응 | 마우스 오버 버튼 |
| `Button_Disabled.png` | 160 x 48 | 80 x 24 또는 26 x 26 | 10:3 또는 1:1 대응 | 비활성 버튼 |

주의할 점:
- `Button_*`은 텍스트가 중앙에 올라간다. 텍스트 대비가 충분해야 한다.
- 최소화 버튼도 같은 버튼 텍스처를 사용하지만 26 x 26 정사각형으로 그려진다. 버튼 이미지를 가로형으로 만들 경우 좌우 끝 장식이 뭉개질 수 있다.
- `Endpoint_*`는 관제 진행바 양끝에 표시된다. 실제 정착지/세력 아이콘이 있으면 그 아이콘을 우선 사용하고, 대상 아이콘이 없을 때만 fallback으로 사용된다.
- `Panel_Background`, `Panel_Header`, `Row_Background`는 stretch 방식이라 정교한 모서리 장식보다 단순한 질감과 얇은 경계선이 안정적이다.

## 상태 아이콘

배치 경로: `Better Shuttle Launch/Textures/UI/Status`

| 파일명 | 권장 크기 | 표시 크기 | 비율 | 상태 |
| --- | ---: | ---: | ---: | --- |
| `Idle.png` | 64 x 64 | 18 x 18 | 1:1 | 예약 없음 |
| `Loading.png` | 64 x 64 | 18 x 18 | 1:1 | 적재 중 |
| `Waiting.png` | 64 x 64 | 18 x 18 | 1:1 | 조건 대기 |
| `Ready.png` | 64 x 64 | 18 x 18 | 1:1 | 발사 가능 |
| `InFlight.png` | 64 x 64 | 18 x 18 | 1:1 | 이동 중 |
| `Arrived.png` | 64 x 64 | 18 x 18 | 1:1 | 도착 |
| `Failed.png` | 64 x 64 | 18 x 18 | 1:1 | 실패 또는 취소 |

권장 스타일:
- 색상만으로 구분하지 말고 형태도 다르게 만든다.
- `Arrived`는 코드에서 점멸 표시가 들어간다. 너무 밝은 전체 면적보다 명확한 체크/도착 심볼이 좋다.
- 작은 크기로 축소되므로 선 두께는 64px 기준 5~8px 정도가 안정적이다.

## 상태 배지

배치 경로: `Better Shuttle Launch/Textures/UI/Badges`

| 파일명 | 권장 크기 | 표시 크기 | 비율 | 용도 |
| --- | ---: | ---: | ---: | --- |
| `Fuel.png` | 32 x 32 | 14 x 14 | 1:1 | 연료 |
| `Health.png` | 32 x 32 | 14 x 14 | 1:1 | 내구도 |
| `Mass.png` | 32 x 32 | 14 x 14 | 1:1 | 무게 |
| `Passengers.png` | 32 x 32 | 14 x 14 | 1:1 | 탑승 인원 |

권장 스타일:
- 14 x 14로 표시되므로 복잡한 디테일은 피한다.
- 배지 오른쪽에 숫자 텍스트가 붙는다. 아이콘 자체에 숫자나 긴 문자를 넣지 않는다.

## 제작 우선순위

1. `BSL_LaunchWhenReady.png`, `BSL_CancelLaunch.png`
2. `Panel_Background.png`, `Panel_Header.png`, `Row_Background.png`
3. `Progress_Rail.png`, `Progress_Fill.png`, `Shuttle_Marker.png`, `Endpoint_Empty.png`
4. `Status/*.png`
5. `Badges/*.png`
6. `Button_Normal.png`, `Button_Hover.png`, `Button_Disabled.png`

## 빠른 확인 방법

이미지를 넣은 뒤 게임을 켜면 별도 XML 등록 없이 바로 로드된다. 파일이 없거나 이름이 맞지 않으면 오류 없이 기존 기본 UI가 표시된다.
