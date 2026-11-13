# Seviye üretici

31. seviyeden itibaren seviyeler kuralla üretilir ve hamle sayıları bot ile ayarlanır (brief kararı: tek kişiyle sürdürülebilir, elle seviye yok).

```bash
dotnet build tools/LevelSim -c Release
python tools/levelgen/generate_levels.py
python tools/levelgen/generate_levels.py 47 48
python tools/levelgen/generate_levels.py --no-tune
```

İlki hazır işaretli bütün bölgelerin seviyelerini üretip ayarlar, ikincisi yalnız verilen seviyeleri, üçüncüsü ayarsız üretir (hızlı deneme). `--all` hazır olmayan bölgeleri de üretir.

## Plan (`plan.json`)

| Alan | Anlamı |
|---|---|
| `seed` | Ana tohum; seviye tohumu `seed * 1000 + seviye no`. Aynı plan aynı seviyeleri verir |
| `districts[].index` | Bölge sırası (0'dan; 3 → seviye 31–40) |
| `districts[].names` | On seviyenin adı (oyunda "Seviye başı" penceresinde görünür) |
| `rerolls` | Seviye no → sayı. Beğenilmeyen seviye aynı kurallarla başka tohumla üretilir (ör. 90 en çok hamleyle bile hedefe yetişmiyordu) |
| `districts[].ready` | Bölgenin manzarası, görevleri ve dünya kartı hazır mı; değilse seviyeleri üretilmez |

## Kurallar

- Yeni engeller: Fener Limanı (31-40) kor kandilini, Bilge Kütüphanesi (41-50) kül saksısını, Çiçek Bahçeleri (51-60) buzdaki köylüyü tanıtır; bu bölgelerin ilk üç seviyesi öğretici (yalnız yeni engel; yeni engel ve renk; yeni engel ve kutu). Sonraki seviyelere %70 olasılıkla bilinen bir aile eklenir, en çok 4 hedef.
- Dolu tahta: Çiçek Bahçeleri'nden itibaren 1. ve 6. seviye dışında tahtanın %36'sı (her bölge +%2, zor seviyede +%5, en çok %50) tek canlı kutu ve kandille dolar; üst üç satır serbest başlar. Saksılar 1-2 hücrelik aynalı küme.
- Hamle sınırı: ayardan sonra 40 hamleyi geçen seviye bir sonraki tohumla yeniden çizilir (en çok 6 deneme); sonuç yine her çalıştırmada aynıdır.
- Bölge içi sıra: 1 kolay renk hedefi, 2 kutu, 3 zemin, 4 örtü, 5 zor, 6 nefes, 7–9 karışık, 10 süper zor. Her bölgenin 5. seviyesi `hard`, 10. seviyesi `superhard`.
- Hedef kazanma oranı (bot, %35 rastgele hamle): 1. seviye %92'den 9. seviye %77'ye iner, zor seviye %62, süper zor %46; her bölge bu oranları 0,8 puan düşürür.
- Tahta 7–9 sütun, 9–10 satır; delikler hep simetrik. Kutu, zemin ve örtü kalıpları: şerit, çerçeve, bloklar, elmas, sütunlar, dama, satırlar.
- Kutu HP'si, zemin ve örtü kalınlığı bölge ilerledikçe artar; renk hedefi sayısı her iki bölgede 5 artar.
- Toplanan obje varsa üst sıraya iki tane konur; sonradan 1–3 tane daha doğar (zor seviyede bir fazla). Fenerlerin düşeceği sütunlarda delik ve kutu olmaz.
- Ayar: `tools/LevelSim --tune` ikili aramayla hedef orana en yakın en az hamleyi bulur ve dosyaya yazar (300 oyun).
