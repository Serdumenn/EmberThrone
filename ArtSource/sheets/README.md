# Çizim sayfaları

Oyundaki bütün görsellerin kaynağı bu klasördür. Her sayfa birkaç nesneyi düz gri (`#808080`) zemin üzerinde, gölgesiz ve yazısız gösterir. Sprite'ları, arka planları, haritaları ve arayüz dokularını `tools/sprites/cut_sheets.py` bu sayfalardan üretir; kesim tarifi `sheets.json`'dadır.

```bash
python tools/sprites/cut_sheets.py
python tools/sprites/cut_sheets.py pieces.png
```

İlki her şeyi, ikincisi yalnız adı verilen sayfayı yeniden üretir. Pillow ve numpy gerekir.

## Sayfalar

| Dosya | İçerik | Çıktı |
|---|---|---|
| `pieces.png` | Altı taş | `Resources/Board/piece_*.png` |
| `specials.png` | Roket, kor bombası, renk küresi, kor kuşu, fener, yıldız | `Resources/Board/sp_*.png`, `ob_lantern.png`, `Resources/UI/Art/ui_star*.png` |
| `obstacles.png` | Üç sandık, buz | `Resources/Board/ob_box*.png`, `ob_ice.png` |
| `tiles_rewards.png` | İki kül zemin; altın, kalp, sandık, kupa | `Resources/Board/ob_floor*.png`, `Resources/UI/Art/ic_coin.png`, `ic_heart.png`, `ic_chest.png`, `ic_trophy.png` |
| `soft_pieces.png`, `soft_specials.png`, `soft_obstacles.png` | Yumuşak 3B görünümde altı taş; roketler, bomba, kor kuşu, küre, fener; üç kutu, buz, iki zemin | `Resources/Board/piece_*.png`, `sp_*.png`, `ob_*.png` |
| `soft_rewards.png`, `soft_tabs.png` | Altın, kalp, elmas, hediye, görev parşömeni, sandık; tezgâh, kupa, ocak evi, kalkan, kartlar, dünya küresi | `Resources/UI/Art/ic_*.png` |
| `soft_loading.png`, `soft_home.png`, `soft_bg_game.png` | Yükleme ekranı (karakter kadrosu), ana ekran köy maketi, oyun arka planı (dikey, 1536x2752) | `Resources/UI/Art/bg_loading.jpg`, `bg_home.jpg`, `Resources/Board/bg_game.jpg`, `Resources/UI/Art/bg_map.jpg` |
| `soft_boosters.png`, `soft_icons.png`, `soft_emby.png` | Çekiç, oklar, karıştır, dişli, artı; yıldız, kilit, saat, takvim, klaket, madalya; Emby'nin üç pozu | `Resources/UI/Art/bo_*.png`, `ic_*.png`, `ui_star*.png`, `mascot*.png` |
| `soft_ui_kit.png` | Yeşil ve turuncu buton, kurdele, halka, lacivert hap (pencere zemini `tools/ui_chrome`'dan) | `Resources/UI/Chrome/btn_*.png`, `ribbon.png`, `ring*.png`, `chip.png`, `tag.png` |
| `cards_sofra.png`, `cards_demirci.png`, `cards_fenerci.png`, `cards_bahce.png`, `cards_ayaz.png`, `cards_kor.png` | Koleksiyonun altı seti; sayfa başına altı kart nesnesi, kart sırasıyla | `Resources/UI/Cards/<set>_<kart>.png` (256 piksel) |
| `soft_district_firin.png`, `soft_district_demirci.png` | Fırın Sokağı ve Demirci Çarşısı bölge görselleri (yatay, 2752x1536) | `Resources/UI/Art/district_2.jpg`, `district_3.jpg` |
| `lobby_a.png` | Elmas, hediye kutusu, görev parşömeni, pazar tezgâhı, kalkan, artı butonu | `Resources/UI/Art/ic_gem.png`, `ic_gift.png`, `ic_quests.png`, `ic_shop.png`, `ic_team.png`, `ic_plus.png` |
| `lobby_b.png` | Mor sandık, çalar saat, asma kilit, takvim, klaket, madalya | `Resources/UI/Art/ic_chest_premium.png`, `ic_clock.png`, `ic_lock.png`, `ic_calendar.png`, `ic_video.png`, `ic_medal.png` |
| `icons.png` | Çekiç, satır ve sütun okları, karıştır, dişli, ev | `Resources/UI/Art/bo_*.png`, `ic_gear.png`, `ic_home.png` |
| `emby.png` | Emby: el sallayan, sevinen, üzgün | `Resources/UI/Art/mascot*.png` |
| `ui_kit.png` | Pencere, kurdele, yeşil ve turuncu buton, halka, hap | `Resources/UI/Chrome/*.png` |
| `bg_game.png` | Oyun arka planı | `Resources/Board/bg_game.jpg`, `Resources/UI/Art/bg_map.jpg` |
| `map_1a/1b`, `map_2a/2b`, `map_3` | Eski yol haritaları | Kullanılmıyor; dünya ekranı 2026-11-12'de bölge kartlarına geçti |

## Tarif (`sheets.json`)

- `sheets`: ızgaralı sayfalar. Sayfa düzeyinde `size` (çıktı karesinin kenarı, varsayılan 512) ve `fill` verilebilir. Her hücre için `out` listesi: `path`, `fill` (nesnenin karedeki payı), isteğe bağlı `align` (`horizontal` ya da `vertical`: nesnenin uzun eksenini hizalar), `hue`, `saturation`, `value`.
  - Hücre seçenekleri: `keepHoles` (gri iç bölgeleri silme), `seeds` (silinecek kapalı boşlukların içinden noktalar), `erase` (silinecek kutular), `loose` (zemin eşiği; ışıltılı kenarlar için yükseltilir), `glass` (ortayı yarı saydam yapar; buz), `soft` (parlama halesini yumuşak saydamlığa çevirir: `to` eşiği ve haleyi kapsayan `boxes`).
- `images`: olduğu gibi küçültülüp JPG yazılan görseller (`width`, `blur`, `crop`: kaynaktan alınacak kutu).
- `maps`: yatay parçaları üst üste, kenarlarını eriterek birleştirir (`top` ve `bottom` kırpma, `blend` geçiş yüksekliği).
- `chrome`: arayüz dokuları. `three` yatayda, `nine` iki yönde esner; esneyen bölüm çizimin orta kesitinden üretilir. `flatten` kavisli bandı düzleştirir (kurdele). `fit` olduğu gibi sığdırır.

## Yeni sayfa eklerken

- Zemin düz gri, gölge ve yazı yok; nesneler arasında bol boşluk.
- Tahta karoları tam önden ya da tam üstten; perspektifli karo kullanılmaz.
- Bölge görselleri yatay (16:9), karakter ve yazı içermez; bölge kartında `crop` ile seçilen yatay şerit görünür.
- Parlayan nesnelerin (alev, ışıltı) halesi gri zemine karışır ve kesimde gri leke kalır. `soft` bunu yumuşak saydamlığa çevirir; kutular yalnız haleyi kapsamalı, yoksa nesnenin açık renkli parçaları da saydamlaşır.
- `loose` düşürülen hücrede komşu hücrenin gri tonu kenarda ince çizgi bırakabilir; dört kenara ince `erase` kutusu eklenir.
