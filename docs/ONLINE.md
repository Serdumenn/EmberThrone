# Çevrimiçi hizmetler

Sıralama ve Takım sekmelerinin bugünkü hali ve gerçek oyunculara açılması için gerekenler.

## Bugünkü durum

| Özellik | Nasıl çalışıyor |
|---|---|
| Köy Ligi (Sıralama) | Cihazda. 29 rakip köy halkıdır: oyunun kendi karakterleri, meslek adlarıyla. Puanları hafta boyunca saat saat artar. Hafta pazartesi 00:00'da (cihaz saati) biter, ödül sıraya göre verilir |
| Takımlar | Cihazda. Altı hazır takım vardır, üyeleri köy halkıdır. Can istenir (4 saatte bir, 5 can birkaç dakika içinde gelir); can isteyen üyeye can gönderilir (20 altın) |
| Etkinlikler | Cihazda. Aylık takvim koddaki veridir (`EventRules.Calendar`); başlangıç ve bitiş cihaz saatine göredir |
| Hesap ve bulut kaydı | Yok. İlerleme yalnız cihazda durur (`PlayerPrefs`) |

Gerçek oyuncular, gerçek takımlar ve sohbet için proje Unity Cloud'a bağlanmalıdır. Sunucu tarafı Unity Gaming Services'tir (2026-11-06 kararı, Pingi Pongi'deki gibi).

## Projeyi Unity Cloud'a bağlama

Bir kez yapılır ve Unity hesabı gerektirdiği için proje sahibi yapar. Düğme adları sürüme göre küçük farklar gösterebilir.

1. Unity Hub'dan EmberThrone'u aç (6000.3.25f1).
2. `Edit > Project Settings > Services` sayfasına gir.
3. Unity hesabıyla giriş yap ve kuruluşu seç (Pingi Pongi ile aynı kuruluş).
4. Yeni bir proje kimliği oluştur (`Create project ID`), adını EmberThrone koy ve bağla.
5. Editörü kapat. `ProjectSettings/ProjectSettings.asset` dosyasında `cloudProjectId` dolmuş olmalıdır; dosyayı commit et.

## Bağlandıktan sonra

| Adım | İçerik |
|---|---|
| Paketler | Authentication, Leaderboards, Cloud Code, Cloud Save (Pingi Pongi'deki sürümler) |
| Giriş | Anonim hesap; ardından Google Play Games bağlantısı |
| Köy Ligi | Unity Leaderboards üzerinde haftalık sıfırlanan, 30 kişilik gruplara bölünen tablo. Bağlantı yokken bugünkü köy ligi görünür |
| Takımlar | Cloud Code betikleri ve Cloud Save: takım kurma, katılma, arama; can isteme ve gönderme; takım sohbeti |
| Bulut kaydı | Profilin yedeği; telefon değişince ilerleme geri gelir |
| Etkinlik takvimi | Sunucudan okunur (uzaktan yapılandırma); yeni etkinlik için yeni sürüm gerekmez |
| Canlı test | Editörde iki ayrı oyuncuyla denenir; tablolar yayından önce sıfırlanır |

Kurallar ve ekranlar hazır olduğu için geçiş, veri kaynağını değiştirmekten ibarettir: `Race` ve `Team` sınıflarının verdiği satırlar sunucudan gelen satırlarla değişir.

Unity Gaming Services'in ücretsiz kotası ve fiyatları kendi sayfasında yazar; yayından önce kontrol edilir.
