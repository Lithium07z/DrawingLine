# DrawingLine

## 로컬 설정과 서명키

Unity의 Library, Logs 등 생성 파일과 개인 서명키는 Git에 커밋하지 않습니다.
프로젝트를 처음 열면 Unity가 필요한 캐시를 다시 생성합니다.

Android 빌드에 사용할 서명키는 저장소 외부의 안전한 위치에 보관하고 Unity의
Publishing Settings에서 지정하세요. 과거에 공개된 키를 새 배포에 재사용하지 마세요.
이미 출시한 앱에 사용한 키라면 앱 서명키와 업로드 키를 구분하여 배포 플랫폼의
키 교체 절차를 먼저 확인해야 합니다. Git에서 지우는 것만으로 기존 키가 폐기되지는 않습니다.


## Photon 로컬 설정

Unity를 열기 전에 아래 명령을 저장소 루트의 PowerShell에서 실행하세요.
이 템플릿에는 App ID가 없으며 기존 RPC 목록과 Unity 참조를 보존합니다.

```powershell
Copy-Item -LiteralPath 'Configuration/PhotonServerSettings.asset.example' -Destination 'Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset'
Copy-Item -LiteralPath 'Configuration/PhotonServerSettings.asset.meta.example' -Destination 'Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset.meta'
```

그런 다음 Unity의 PhotonServerSettings에서 본인의 Realtime 및 Chat App ID를 입력하세요.
실제 설정 파일과 meta 파일은 Git에서 제외됩니다. `.example` 파일에는 실제 App ID를 넣지 마세요.

Photon Dashboard에서 두 앱의 사용량과 인증 구성을 확인하세요. Custom Authentication을
사용하려면 클라이언트 인증을 먼저 구현·검증한 후 익명 접속을 차단해야 합니다.
저장소에서 App ID를 지워도 이미 배포한 클라이언트에서 App ID를 추출할 수 있으므로,
서버 측 인증 설정도 필요합니다.

