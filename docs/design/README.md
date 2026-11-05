# EmberThrone — Tasarım Süreci

Bu klasör oyunun tasarım ve teknik kararlarının kaydıdır. Plan ve durum [ROADMAP.md](../../ROADMAP.md)'dedir. Her faz bir **onay kapısı** ile biter; kapıdan geçmeyen iş bir sonraki faza taşınmaz (PongMobilePrototype'taki düzen).

## Kaynaklar

| Kaynak | İçerik |
|---|---|
| `GDD/` (yalnız yerelde, git'e girmiyor) | 16 Word belgesi: MASTER-GDD-001 özeti, PTH-001 vizyon, SYS-CORE-001 çekirdek döngü, UX-001 ekran ve animasyon, TECH-001 teknik şartname, LVL-DESIGN-001, META-001, MONETIZATION-001, LIVEOPS-001, DATA-001 ve yamaları |
| [ROADMAP.md](../../ROADMAP.md) | Fazlar, yapılanlar, sahibinin vereceği kararlar (GDD ile motor arasındaki farklar dahil) |
| [../qa/](../qa/) | Kalite denetimleri |
| [../release/README.md](../release/README.md) | Yayın kılavuzu, sürüm ve commit düzeni |

## Roller

| Rol | Kim | Sorumluluk |
|---|---|---|
| Creative Director, ürün sahibi, kapı onayları, commit, cihaz testi | Fatih | Vizyon, kararlar, GitHub Desktop'tan commit ve push, telefonda deneme |
| Geliştirme, test, belgeler | Yapay zekâ ajanları | Kod, araçlar, otomatik testler, ekran görüntüsü turu, denetimler |

## Fazlar

| Faz | Çıktı | Kapı | Durum |
|---|---|---|---|
| 0 · Temel | Deterministik C# motoru, seviye formatı, bot simülatörü, CI | — | Tamam |
| 1 · Oynanabilir prototip | Tek sahne, koddan arayüz, giriş, animasyonlar, 10 seviye, test + APK + imzalı AAB akışları | Kapı 1: telefonda deneme | Kodda tamam; bulut derlemesi Unity secret'larını, cihaz testi APK'yı bekliyor |
| 2 · Görünüm ve his | Sanat yönü (style frame seçenekleri), sprite hattı, efektler, ses, titreşim | Kapı 2: görsel yön seçimi | — |
| 3 · Meta ve ekonomi | Harita, canlar, coin, booster envanteri, 100+ seviye | Kapı 3 | — |
| 4 · Backend ve sosyal | Hesap, bulut kayıt, sunucu doğrulaması | Kapı 4 | GDD ile çelişki açık (ROADMAP karar 4) |
| 5 · LiveOps | Etkinlikler, remote config, analitik | Kapı 5 | — |
| 6 · Yayın | Mağaza materyalleri, Play Console, kapalı test | Kapı 6: yayın | Akış hazır, hesap adımları sahibinde |

## Karar kaydı

| Tarih | Karar | Gerekçe |
|---|---|---|
| 2026-11-05 | Oyun kuralları Unity'den bağımsız, deterministik C# motorunda | Unity'siz test, bot ile seviye ayarı, ileride tekrar ve sunucu doğrulaması |
| 2026-11-05 | Tek boş sahne; sahne ve arayüz koddan kurulur | Prefab ve sahne referansı kırılmaz, ajanlar metin üzerinden çalışır |
| 2026-11-05 | Geçici görseller kodla üretilir; renk + şekil birlikte (UX-001 ACC-004) | Faz 2'deki gerçek sanata kadar görsel dosya yok |
| 2026-11-05 | Arayüz: uGUI, CanvasScaler Expand (1080×1920) | 1080×1920 alan her ekrana sığar; Pong UI Toolkit'e geçti, Faz 2'de yeniden değerlendirilir |
| 2026-11-05 | Seviyede en fazla 3 hedef | SYS-CORE-001 DL-002; hedef paneli bu sayıya göre |
| 2026-11-05 | APK yalnızca GitHub Actions'ta | Ürün sahibinin kararı; Pong ile aynı |
| 2026-11-05 | Unity editörünü ajanlar MCP for Unity köprüsüyle yönetir | Ücretsiz, MIT, yalnız bu bilgisayardan erişilir |
| 2026-11-05 | Unity AI paketleri projede kalır | Ürün sahibinin kararı |
| 2026-11-05 | Eski Faz 0 kodu (`Assets/_Game`) main'e birleştirildi, kendiliğinden başlamaz | Ürün sahibinin kararı; Faz 3'te kullanılabilir |
| 2026-11-05 | Tek branch: `main`; commit'leri ürün sahibi atar | Ürün sahibinin kararı |
| 2026-11-05 | Unity 6000.3.25f1 | Pong ile aynı sürüm; testler bu sürümde geçti |
| 2026-11-06 | Pong'daki git ve yayın düzeni: imzalı AAB akışı, Git LFS, tescilli lisans, `docs/` yapısı, commit kuralları | Ürün sahibinin kararı; profesyonel ve tutarlı süreç |
| 2026-11-06 | Git LFS yalnızca bundan sonraki binary'ler için | Geçmiş yeniden yazılmaz, force push gerekmez |
| 2026-11-06 | Android: ARMv7 + ARM64, şirket adı SERDUMEN | Pong ile aynı; eski 32 bit telefonlar da desteklenir |
| 2026-11-06 | Paket adı `com.oceanforge.emberthrone` | Ürün sahibinin kararı; Pong ile aynı stüdyo düzeni. İlk mağaza yüklemesinden sonra değişmez |
