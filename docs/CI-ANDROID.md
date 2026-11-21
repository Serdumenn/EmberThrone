# GitHub Actions ile Android derlemesi

APK ve AAB yalnızca GitHub Actions'ta üretilir, bilgisayarda derlenmez. Yapı PongMobilePrototype ile aynıdır.

| Akış | Ne zaman çalışır | Ne üretir |
|---|---|---|
| `build-android.yml` · Tests and Android Build (APK) | `main`'e her push, ya da Actions'tan elle | Testler, sonra geliştirme APK'sı (Unity'nin hata ayıklama anahtarıyla imzalı, telefona kurmak için) |
| `release-android.yml` · Release Android (signed AAB) | `v0.1.0` gibi bir etiket, ya da Actions'tan elle | Testler, sonra Google Play için imzalı `.aab` ve sembol dosyası |
| `core-tests.yml` · Core Tests | Her push ve PR | Motor testleri ve seviye simülasyonu (Unity lisansı gerekmez) |
| `motion-capture.yml` · Motion capture | Yalnız Actions'tan elle | Tahta animasyonlarını 60 fps kare kare kaydeder; kareler taslak bir release'e zip olarak eklenir, bkz. [reference/README.md](reference/README.md) |

## Secret'lar

GitHub → repo → **Settings → Secrets and variables → Actions → New repository secret**.

| Ad | Değer | Gerektiği akış |
|---|---|---|
| `UNITY_EMAIL` | Unity hesabının e-postası | APK, AAB, hareket kaydı |
| `UNITY_PASSWORD` | Unity hesabının şifresi | APK, AAB, hareket kaydı |
| `UNITY_SERIAL` | Unity seri numarası (PongMobilePrototype'takiyle aynı) | APK, AAB, hareket kaydı |
| `UNITY_LICENSE` | Seri numarası yoksa: `C:\ProgramData\Unity\Unity_lic.ulf` dosyasının tüm içeriği | APK, AAB, hareket kaydı |
| `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME`, `ANDROID_KEYALIAS_PASS` | Yükleme anahtarı, bkz. [release/README.md](release/README.md) | AAB |

GitHub kayıtlı secret'ların değerini bir daha göstermez; Pong'daki değerler bu repoya yeniden girilir. Unity secret'ları yokken APK akışı hata vermez, bir uyarıyla atlanır.

## Çalıştırma ve APK'yı indirme

1. Otomatik: `main`'e push. Elle: **Actions → Tests and Android Build (APK) → Run workflow**.
2. Çalıştırma bitince sayfasındaki **Artifacts** bölümünden `EmberThrone.apk` indirilir (14 gün saklanır).
3. Telefonda dosyaya dokunulur, "Bilinmeyen uygulamaları yükle" izni verilir, **Yükle**.

## Süre ve hatalar

- İlk çalıştırma uzundur: Docker imajı ve Unity `Library` klasörü sıfırdan hazırlanır. Sonrakilerde önbellek kullanılır (logda "Cache restored").
- "Library folder does not exist" bir uyarıdır, ilk çalıştırmada normaldir.
- Testler düşerse derleme başlamaz; sonuçlar çalıştırmanın `test-results` artifact'ında ve **Test Results** kontrolündedir.
- Unity sürümü akışlardaki `UNITY_VERSION` ile `ProjectSettings/ProjectVersion.txt` aynı olmalıdır (şu an 6000.3.25f1).
