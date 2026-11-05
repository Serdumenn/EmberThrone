# EmberThrone — Yol Haritası

Hedef: türün en iyileri seviyesinde akıcı, sosyal, canlı etkinlikli bir match-3. Kod, seviye ve araçların hepsini yapay zekâ üretir.
Mekanikler türün ortak mekanikleri; isim, karakter, görsel ve seviyeler EmberThrone'a özgü.

## Faz 0 — Temel ✅
- [x] Saf C# oyun motoru (Unity'siz test edilebilir, deterministik)
  - eşleşme: 3'lü, 4'lü → roket, 2x2 → güdümlü, L/T → bomba, 5'li → renk bombası
  - 10 combo (roket+roket, bomba+bomba, renk+renk vb.), zincirleme patlama
  - engeller: kutu (can puanlı), zemin katmanı, kilit/buz katmanı, toplanan obje
  - yerçekimi: düz + çapraz akış, şekilli tahtalar (delikler), spawner'lar
  - hedefler, hamle sayacı, kazanma/kaybetme, "+5 hamle ile devam"
  - booster'lar: çekiç, satır, sütun, karıştır + oyun öncesi başlangıç özel taşları
  - hamle kalmayınca otomatik karıştırma, ipucu
- [x] JSON seviye formatı + 10 örnek seviye
- [x] Bot simülatörü: zorluk ölçümü, otomatik hamle ayarı, kırılma testi
- [x] 69 birim testi + CI (her push'ta)
- [x] Proje ayarları: dikey ekran, geçerli Android paket adı, IL2CPP + ARM64

## Faz 1 — Oynanabilir prototip (sıradaki)
- [x] Yerel oturuma geç (Unity bulutta çalışmıyor); Unity editörünü AI'ın yönetebileceği MCP köprüsünü kur (MCP for Unity v10.3.0, kurulum: CLAUDE.md)
- [x] Motor kodu Unity'de ilk kez derlendi, EditMode testleri geçti
- [x] Unity sahne kurulumu kodla (tek boş bootstrap sahnesi, prefab yok)
- [x] Tahta görünümü, dokunma/kaydırma girişi (yeni Input System), hamle olaylarını animasyonla oynatma
- [x] Geçici görseller (koddan üretilen şekiller), hedef paneli, hamle sayacı, seviye seçimi, kazandın/kaybettin pencereleri
- [x] Dikey ekran, farklı ekran oranları ve çentik uyumu (20:9, 21:9, 16:9, tablet 3:4 denendi)
- [x] Play mode testleri: sanal fareyle giriş, bütün özel taş ikilileri, botun 10 seviyeyi bitirmesi; görünüm her hamlede motorla karşılaştırılıyor
- [x] GitHub Actions: test + APK akışı (PongMobilePrototype örnek alındı)
- [ ] Unity secret'larını bu repoya ekle (sahibi yapacak), ilk bulut APK'sını al
- [ ] Gerçek telefonda dene: his, dokunma, performans (henüz hiç cihazda çalışmadı)
- [ ] Bulutta test edilecek: `Sprites/Default` ve yazı tipi Android build'inde sorunsuz mu

## Faz 2 — Profesyonel görünüm ve his
- [ ] Sanat yönü: EmberThrone teması (ateş/kor krallığı), renk paleti, karakter
- [ ] AI ile sprite üretim hattı + tutarlı stil
- [ ] Parçacık efektleri, ekran sarsıntısı, squash/stretch, combo kutlamaları, ses ve müzik, titreşim
- [ ] Seviye sonu kutlaması (kalan hamleler → roketler → coin)
- [ ] Oyun içi booster çubuğu, ipucu (motorda hazır, ekranda yok), "+5 hamle" teklifi

## Faz 3 — Meta ve ekonomi
- [ ] Harita / ilerleme, bölge inşa-dekor meta oyunu (yıldızlarla görevler)
- [ ] Canlar (5 can, 30 dk yenilenme, sunucu saati), coin, booster envanteri
- [ ] Günlük ödül, seri bonusları, kumbara
- [ ] Seviye üretimi: prosedürel üretici + bot ile ayar → 100, sonra 500+ seviye

## Faz 4 — Backend ve sosyal
- [ ] Hesap + bulut kayıt
- [ ] Takımlar/klanlar: kur, katıl, ara; takım sohbeti; can isteme/gönderme
- [ ] Liderlik tabloları, takım etkinlikleri (ortak hedefe birlikte oynama), turnuvalar
- [ ] Sunucu doğrulama: deterministik motorla hamle tekrarı (hile koruması)
- Öneri: Nakama (açık kaynak; takım, sohbet, turnuva, liderlik tablosu hazır geliyor), kendi sunucunda

## Faz 5 — LiveOps
- [ ] Etkinlik altyapısı (başlangıç/bitiş sunucudan), sezon kartı, sınırlı süreli etkinlikler
- [ ] Teklifler, remote config, A/B test, analitik hunileri, push bildirim

## Faz 6 — Yayın
- [ ] Google Play Billing (IAP), AAB + imzalama, Play Console
- [ ] Soft launch (tek ülke), KPI: D1 ≥ %40, D7 ≥ %15, seviye bazlı kayıp analizi
- [ ] Gerçek oyuncu verisiyle bot kalibrasyonu ve seviye ayarı

## Sahibinin vereceği kararlar
1. GitHub secret'ları (PongMobilePrototype'takilerin aynısı) → APK derlemesi için
2. Tema ve sanat yönü onayı
3. Paket adı / şirket adı (ilk mağaza yüklemesine kadar değişebilir: şu an `com.serdumenn.emberthrone`)
4. GDD/ belgeleri ile bu yol haritası ayrışıyor, hangisi geçerli?
   - Özel taşlar: GDD'de TNT ve 3x3 bomba var, güdümlü yok; motorda güdümlü var, TNT yok, bomba 5x5
   - Booster'lar: GDD çekiç, bomba, renk fırçası; motor çekiç, satır, sütun, karıştır
   - Engeller: GDD 7 tip (ateş, zincir, tel kafes...); motor kutu, zemin, buz, toplanan obje
   - Tahta: GDD 9x9; seviyeler 8x9
   - Sosyal ve sunucu: GDD klan sistemini kapsam dışı sayar ve Firebase der; bu yol haritası klan + Nakama der
