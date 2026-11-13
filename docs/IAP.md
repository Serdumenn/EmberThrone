# Gerçek parayla satış (uygulama içi satın alma)

Oyun, Google Play üzerinden gerçek parayla satış için Unity IAP 5.4.3 kullanır (Pong ile aynı sürüm). Kod hazırdır; ürünlerin Google Play Console'da tanımlanması proje sahibinin işidir.

## Ürünler

Ürün kimlikleri koddaki kimliklerle birebir aynı olmalıdır (`Scripts/Core/Meta/Store.cs`, `StoreRules.Products`). Hepsi "uygulama içi ürün" (in-app product) olarak açılır; oyun her satın alımı kendisi tüketir, bu yüzden aynı ürün tekrar alınabilir. Başlangıç paketinin tek seferlik olmasını oyun kendisi sağlar.

| Kimlik | İçerik | Önerilen fiyat |
|---|---|---|
| `starter_pack` | 500 altın, 50 elmas, 5 çekiç, 3 satır, 3 sütun (bir kez) | 1,99 $ |
| `ember_pass` | Kor Yolu: bu ayın ücretli ödül şeridi ve en çok 8 can | 4,99 $ |
| `piggy_bank` | Kumbaradaki altın (kazandıkça dolar, 1000 altında kırılabilir, en çok 3000) | 2,99 $ |
| `gems_080` | 80 elmas | 0,99 $ |
| `gems_200` | 200 elmas | 1,99 $ |
| `gems_500` | 500 elmas | 4,99 $ |
| `gems_1200` | 1200 elmas | 9,99 $ |
| `coins_1000` | 1000 altın | 1,99 $ |
| `coins_3000` | 3000 altın | 4,99 $ |
| `booster_pack` | 5 çekiç, 3 satır, 3 sütun, 2 fırça | 2,99 $ |

Oyundaki fiyat etiketi mağazadan gelir (oyuncunun para biriminde). Tablodaki dolar fiyatları yalnız editörde ve mağazaya ulaşılamadığında görünür.

## Proje sahibinin adımları

1. Play Console > Ayarlar > Ödeme profili: satıcı hesabı (ödeme profili) yoksa açın.
2. Play Console > Para kazanma > Ürünler > Uygulama içi ürünler: tablodaki her kimliği aynı yazımla oluşturun, fiyatı girin, etkinleştirin.
3. Play Console > Ayarlar > Lisans testi: kendi Gmail hesabınızı (ve test edecek kişileri) ekleyin. Bu hesaplarla yapılan satın almalar ücretsizdir.
4. Bir APK/AAB'yi en az dahili test kanalına yükleyin; ürünler ancak Play'e yüklenmiş bir uygulamada görünür.
5. Telefonda mağazayı açın: fiyatlar yerel para biriminde görünmeli, bir test alımı yapılınca ödül penceresi çıkmalı.

## Nasıl çalışır

- `PurchaseService` (`Scripts/Game/Store/`) mağazaya bağlanır, ürünleri ve yarım kalmış alımları çeker. Her alım önce profile yazılır (`Economy.GrantPurchase`), ancak kayıt başarılıysa mağazaya onaylanır; böylece uygulama kapanırsa alım bir sonraki açılışta yeniden teslim edilir. Aynı işlem kimliği iki kez ödül vermez (profil son 64 işlemi hatırlar).
- Satın alınan altın ve elmas birikim sınırına takılmaz.
- Editörde ve testlerde gerçek mağaza açılmaz; satın alma hemen verilir (`PurchaseService.SimulateInEditor`).
- Sunucu tarafı fiş doğrulaması henüz yok; çevrimiçi altyapı (Faz 4) ile eklenecek.
