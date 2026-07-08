# Better Shuttle Launch XML 설정 가이드

이 모드에서 XML로 조정할 수 있는 값은 배포 폴더의 다음 파일에 있습니다.

```text
Better Shuttle Launch/Defs/BetterShuttleLaunch/BetterShuttleLaunchUiConfig.xml
```

## 관제 UI 크기

| XML 필드 | 용도 |
| --- | --- |
| `trackerDefaultWidth` | 관제창 기본 너비 |
| `trackerDefaultHeight` | 관제창 기본 높이 |
| `trackerMinWidth` / `trackerMinHeight` | 리사이즈 최소 크기 |
| `trackerMaxWidth` / `trackerMaxHeight` | 리사이즈 최대 크기 |
| `trackerMinimizedHeight` | 최소화 상태 높이 |
| `trackerRowStrideHeight` | 행 사이 간격 |
| `trackerRowDrawHeight` | 실제 행 배경 높이 |
| `estimatedShuttleTravelTicksPerTile` | 실제 상단 참조가 없을 때 이동 진행률을 추정하는 틱/타일 값 |

## 텍스처 경로

`texturePaths` 아래 값은 `Better Shuttle Launch/Textures` 기준의 확장자 없는 경로입니다.

예를 들어:

```xml
<commandOpenTracker>UI/Commands/BSL_OpenTracker</commandOpenTracker>
```

실제 파일 경로는 다음처럼 배치합니다.

```text
Better Shuttle Launch/Textures/UI/Commands/BSL_OpenTracker.png
```

파일이 없거나 XML 값이 비어 있으면 코드는 기존 기본 경로와 바닐라 fallback을 사용합니다.

## XML로 옮기지 않은 기능

Harmony 패치, 예약 발사 큐, 월드 타겟팅, 도착 감지, 관제 UI 렌더링 같은 동작은 런타임 상태를 읽고 바닐라 API를 호출해야 하므로 C#에 남겨둡니다.
