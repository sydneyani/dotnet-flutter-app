# Meridian mobile

Flutter client for the Meridian field-operations API. Sign in, review the shift board, and list, open, edit, and advance work orders. Asset pages record inspections. Lists and the dashboard keep the last successful response on disk and show it when the API cannot be reached.

Run the API first (`dotnet run --project backend/src/Meridian.Api`), then:

```bash
cd mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://localhost:8080
```

Android emulators should use `http://10.0.2.2:8080`. The sign-in screen can fill the demo technician account (`maya.okonkwo@meridian.demo` / `Meridian#demo1`).

```bash
flutter analyze
flutter test
```
