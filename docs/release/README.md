# Yayın kılavuzu (Google Play, yalnız Android)

Durum: 2026-11-11. İmzalı sürüm akışı ve reklam altyapısı hazır; hesap adımları aşağıda. Düzen PongMobilePrototype ile aynıdır.

## Projede hazır olanlar

| Konu | Durum |
|---|---|
| Paket adı | `com.oceanforge.emberthrone` (Pong'daki `com.oceanforge.pingipongi` düzeni; ilk mağaza yüklemesinden sonra değiştirilemez), hedef API 36, en düşük API 25, IL2CPP, ARMv7 + ARM64 |
| Geliştirme APK'sı | `.github/workflows/build-android.yml`: `main`'e her push'ta testler, sonra APK |
| İmzalı AAB | `.github/workflows/release-android.yml`: `v0.1.0` gibi bir etiket push edilince (ya da Actions'tan elle) testler koşar, imzalı `.aab` ve sembol dosyası üretilir. Sürüm adı etiketten, sürüm kodu 1000 + çalıştırma numarasından gelir |
| Ekran | Yalnız dikey; 16:9, 20:9, 21:9 ve tablet 3:4 ekran turuyla denetlenir. Çentik payı hesaplanır, bölünmüş ekran kapalı |
| Reklam | AdMob (Google Mobile Ads 10.7.0): kaybedince ödüllü reklamla "+5 hamle", 6. seviyeden sonra kazanılan seviyelerin ardından en çok 3 dakikada bir geçiş reklamı. Avrupa'da ilk açılışta Google UMP onay penceresi, gerektiğinde Ayarlar'da "Gizlilik seçenekleri". Kod: `Scripts/Game/Ads/` |
| Reklam kimlikleri | **Şu an Google'ın herkese açık deneme kimlikleri.** Gerçek kimlikler girilmeden mağazaya çıkılmaz (adım 4) |
| Lisans | Tescilli ("All rights reserved"), Ocean Forge |

## Hesap adımları

### 1. Unity secret'ları
[../CI-ANDROID.md](../CI-ANDROID.md) içindeki tablo. Bunlar olmadan ne APK ne AAB üretilir.

### 2. Yükleme anahtarı ve GitHub secret'ları
Anahtar `keytool` ile oluşturulur (Unity'nin OpenJDK'sında vardır) ve repo dışında saklanır. Komut iki şifre ve birkaç kimlik sorusu sorar:

```
"C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe" -genkeypair -v -keystore emberthrone-upload.keystore -alias upload -keyalg RSA -keysize 2048 -validity 10000
```

Base64'e çevirme (PowerShell):

```
[Convert]::ToBase64String([IO.File]::ReadAllBytes("emberthrone-upload.keystore")) | Set-Clipboard
```

GitHub → Settings → Secrets and variables → Actions:

| Ad | Değer |
|---|---|
| `ANDROID_KEYSTORE_BASE64` | keystore'un base64 hâli |
| `ANDROID_KEYSTORE_PASS` | keystore şifresi |
| `ANDROID_KEYALIAS_NAME` | `upload` |
| `ANDROID_KEYALIAS_PASS` | anahtar şifresi |

`emberthrone-upload.keystore` repoya konmaz (`.gitignore` `*.keystore` dosyalarını zaten dışarıda tutar) ve yedeklenir. Kaybolursa Play Console'dan yükleme anahtarı sıfırlanması istenir.

### 3. Play Console (Faz 6)
1. Play Console → Create app → **EmberThrone**, Game, Free. "Play App Signing" açık kalır.
2. Test sürümü: GitHub → Actions → **Release Android (signed AAB)** → **Run workflow** → Version `0.1.0`. Bitince **EmberThrone-0.1.0-…** artifact'ından `.aab` indirilir.
3. Testing → **Internal testing** → yeni sürüm → `.aab` yüklenir, test kullanıcıları eklenir.

Kişisel geliştirici hesaplarında üretimden önce en az 12 test kullanıcısıyla 14 günlük kapalı test zorunludur.

### 4. AdMob
1. AdMob → Apps → **Add app** → Android → EmberThrone. Uygulama kimliği (`ca-app-pub-…~…`) Unity'de **Assets → Google Mobile Ads → Settings** penceresindeki "Android App ID" alanına yazılır (`Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`).
2. İki reklam birimi açılır: **Rewarded** ("+5 hamle") ve **Interstitial** (seviye arası). Kimlikleri `Assets/EmberThrone/Scripts/Game/Ads/AdRules.cs` içindeki `RewardedUnitId` ve `InterstitialUnitId` alanlarına yazılır.
3. Geliştiricinin telefonu AdMob → Settings → **Test devices** listesine eklenir; kendi reklamına tıklamak hesabı riske atar.
4. Privacy & messaging → **GDPR** mesajı oluşturulup yayınlanır (dil: otomatik).
5. Mağaza sayfası yayınlanınca uygulama AdMob'da mağazaya bağlanır; `app-ads.txt` stüdyo sitesinde durur.

### 5. Gizlilik ve mağaza formları
- Stüdyo sitesinde (`serdumenn.github.io`) EmberThrone için oyun sayfası ve gizlilik politikası sayfası açılır; adresi Play Console'a ve Ayarlar'daki bağlantıya girer.
- Play Console → **Data safety**: reklam kimliği ve yaklaşık konum reklam için toplanır (AdMob), hesap ya da bulut kayıt yok.
- **Ads** bölümünde "Contains ads" işaretlenir; içerik derecelendirme anketi doldurulur.

## Sürüm ve commit düzeni
- Tek branch vardır: `main`. Her zaman derlenebilir tutulur; her push'ta testler ve geliştirme APK'sı koşar.
- Commit'ler GitHub Desktop'tan atılır.
- Commit mesajı İngilizce, emir kipinde ve kısa: ilk satır en fazla ~60 karakter (`Add level select screen`, `Fix win save on early exit`). Gerekirse boş bir satırdan sonra açıklama.
- Bir commit tek bir konuyu kapsar; ilgisiz değişiklikler ayrı commit'lere bölünür.
- Yayın: GitHub Desktop → **History** → yayınlanacak commit'e sağ tık → **Create Tag** → `v0.1.0` → **Push origin**. Etiket imzalı AAB'yi üretir; sürüm adı etiketten alınır, sürüm kodu kendiliğinden artar (Player Settings'e dokunmak gerekmez).
- GitHub → Releases → **Draft a new release** → aynı etiket → kısa sürüm notu ("What's new"). Play Console'daki "Release notes" ile aynı metin kullanılır.
