# EmberThrone — Sprint Tracker

## Mevcut Sprint: Sprint 1

## Mevcut Faz: Faz 0 — Foundation ✅ TAMAMLANDI

## Tamamlananlar ✅

- [x] Unity 6.3 LTS kuruldu
- [x] GitHub repo oluşturuldu
- [x] .gitignore ayarlandı
- [x] Assets/_Game/ klasör yapısı — TECH-001 §3.1
- [x] ServiceLocator implementasyonu — TECH-001 §3.3
- [x] EventBus implementasyonu — TECH-001 §3.3
- [x] GameEvents tanımları (level/board/economy/meta events)
- [x] GameBootstrapper (boot sırası, DontDestroyOnLoad)
- [x] LevelConfig ScriptableObject — TECH-001 §4.1 (22 alan)
- [x] ChapterConfig ScriptableObject (15 level/chapter, task, area restore)
- [x] EconomyConfig ScriptableObject (coin/gem cap, FTUE, lives, spend trigger)
- [x] SaveManager (ISaveManager + SaveData + SaveManager) — TECH-001 §7
- [x] Object Pooling (ObjectPool<T> + IPoolManager + PoolManager) — TECH-001 §9
- [x] GameBootstrapper güncelleme (SaveManager + PoolManager + Firebase register)
- [x] Grammar Simulator Editor Tool (GrammarRules + GrammarSimulator) — TECH-001 §5
- [x] Firebase stubs (IFirebaseService + IAnalyticsManager + IRemoteConfigManager + FirebaseStubService) — TECH-001 §6

## Devam Eden 🔄

- [ ] Unity Editor'da derleme kontrolü (sıfır hata)
- [ ] Grammar Simulator ile test level'ları üret (EmberThrone > Grammar Simulator)

## Sıradaki ⏳ (Faz 1 — Core Gameplay)

- [ ] Board/Grid sistemi (tile spawn, gravity, match detection)
- [ ] Input sistemi (swipe/tap)
- [ ] Match-3 temel mekanik (eşleşme, patlama, düşme)
- [ ] Sahne yönetimi (boot → menu → game flow)

## Son Sohbet Notu

Sprint 1 Faz 0 TAMAMLANDI. Tüm foundation sistemleri yerinde: ServiceLocator, EventBus, 3 SO şeması, SaveManager, Object Pooling, Grammar Simulator, Firebase stubs. 19 C# dosyası, tam mimari altyapı hazır. Sırada Faz 1 Core Gameplay var.

---

## Her Sohbet Akışı

**Sen:** sadece şunu yazarsın:

```
PROGRESS.md ve GDD/MASTER-GDD-001_Executive_Summary.docx dosyalarını oku.
Kaldığımız yerden devam et.
```

**AI:** PROGRESS.md'yi okur, ne yapılacağını bilir, işe koyulur.

**Sohbet sonunda:** AI PROGRESS.md'yi günceller, tamamlananları işaretler.


