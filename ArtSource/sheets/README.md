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
| `tiles_rewards.png` | İki kül zemin; altın, kalp, sandık, kupa (henüz kesilmiyor) | `Resources/Board/ob_floor*.png` |
| `icons.png` | Çekiç, satır ve sütun okları, karıştır, dişli, ev | `Resources/UI/Art/bo_*.png`, `ic_gear.png`, `ic_home.png` |
| `emby.png` | Emby: el sallayan, sevinen, üzgün | `Resources/UI/Art/mascot*.png` |
| `ui_kit.png` | Pencere, kurdele, yeşil ve turuncu buton, halka, hap | `Resources/UI/Chrome/*.png` |
| `bg_game.png` | Oyun arka planı | `Resources/Board/bg_game.jpg`, `Resources/UI/Art/bg_map.jpg` |
| `map_1a/1b`, `map_2a/2b`, `map_3` | Bölge haritaları | `Resources/UI/Art/map_*.jpg` |

## Tarif (`sheets.json`)

- `sheets`: ızgaralı sayfalar. Her hücre için `out` listesi: `path`, `fill` (nesnenin 512 piksellik karedeki payı), isteğe bağlı `align` (`horizontal` ya da `vertical`: nesnenin uzun eksenini hizalar), `hue`, `saturation`, `value`.
  - Hücre seçenekleri: `keepHoles` (gri iç bölgeleri silme), `seeds` (silinecek kapalı boşlukların içinden noktalar), `erase` (silinecek kutular), `loose` (zemin eşiği; ışıltılı kenarlar için yükseltilir), `glass` (ortayı yarı saydam yapar; buz).
- `images`: olduğu gibi küçültülüp JPG yazılan görseller (`width`, `blur`).
- `maps`: yatay parçaları üst üste, kenarlarını eriterek birleştirir (`top` ve `bottom` kırpma, `blend` geçiş yüksekliği).
- `chrome`: arayüz dokuları. `three` yatayda, `nine` iki yönde esner; esneyen bölüm çizimin orta kesitinden üretilir. `flatten` kavisli bandı düzleştirir (kurdele). `fit` olduğu gibi sığdırır.

## Yeni sayfa eklerken

- Zemin düz gri, gölge ve yazı yok; nesneler arasında bol boşluk.
- Tahta karoları tam önden ya da tam üstten; perspektifli karo kullanılmaz.
- Harita parçaları yatay (3:2), yol alt ortadan girip üst ortadan çıkar, üst ve alt kenar düz çayır.
- Yolun düğüm noktaları `Scripts/Game/UI/LevelMap.cs`'te yüzde olarak durur; harita değişince yeniden izlenir.
