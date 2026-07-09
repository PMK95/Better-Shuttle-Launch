# Better Shuttle Launch 텍스처 가이드

이 문서는 배포 폴더에 넣을 수 있는 명령 버튼 텍스처만 정리합니다.

## 기본 규칙

- 배치 기준 폴더는 `Better Shuttle Launch/Textures`입니다.
- 코드에서는 확장자 없이 `UI/Commands/BSL_LaunchWhenReady` 같은 경로로 로드합니다.
- 파일 형식은 `.png`를 권장합니다.
- 파일이 없으면 RimWorld 바닐라 발사 아이콘이나 기본 UI가 fallback으로 표시됩니다.

## 명령 버튼 아이콘

배치 경로:

```text
Better Shuttle Launch/Textures/UI/Commands
```

| 파일명 | 권장 크기 | 비율 | 용도 |
| --- | ---: | ---: | --- |
| `BSL_LaunchWhenReady.png` | 128 x 128 | 1:1 | 통합 버튼 `준비되면 발사` |
| `BSL_CancelLaunch.png` | 128 x 128 | 1:1 | 예약 취소 아이콘 후보 |

현재 기본 커맨드는 `BSL_LaunchWhenReady.png`를 우선 사용하고, 파일이 없으면 바닐라 발사 아이콘을 사용합니다.
