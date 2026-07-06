# Стадия 0 — Прототип производительности

Цель: убедиться, что low-poly 3D на Unity 6 + URP держит стабильные 60 FPS
на реальном устройстве и слабом эмуляторе (2GB RAM), до того как писать геймплей.

## DoD (Definition of Done)

- [ ] APK ставится на реальное Android-устройство (min API 24).
- [ ] 60 FPS стабильно в перф-сцене (15 машин, 10 NPC с NavMeshAgent, мягкие тени).
- [ ] На эмуляторе 2GB RAM игра запускается и не вылетает.
- [ ] Решение зафиксировано: остаёмся на 3D или переходим на fallback 2.5D.

## Шаги (на локальной машине)

### 1. Открыть проект

1. Unity Hub → Add → папка репозитория. Версия: **Unity 6000.x** (проект создан
   под 6000.0.32f1; при другом патче Hub предложит апгрейд — соглашайтесь).
2. Обязателен модуль **Android Build Support** (+ SDK & NDK Tools, OpenJDK).
3. Первое открытие: Unity сгенерирует `Library/`, `ProjectSettings/` и `.meta`-файлы.
   **Закоммитьте сгенерированные `.meta` и ProjectSettings** после проверки.

### 2. Собрать сцену и APK (меню «RagsToRiches» в шапке редактора)

1. **«1. Настроить URP»** — создаёт URP-ассет и назначает его в Graphics/Quality.
2. **«2. Собрать перф-сцену»** — генерирует `Assets/Scenes/PerfTest.unity`:
   земля 56×56 м, 15 машин (кубы-препятствия, запекаются в NavMesh), 10 NPC-капсул
   с NavMeshAgent, капсула-игрок (CharacterController + floating-джойстик),
   изометрическая камера со слежением, FPS-оверлей. Скрипт идемпотентен.
3. Нажать **Play** — проверить: игрок бегает (WASD), NPC ходят, камера следит.
4. **«3. Собрать Debug APK (IL2CPP, ARM64)»** — репрезентативный замер
   (первая сборка IL2CPP долгая). Если NDK не установлен — пункт
   **«4. …(Mono, быстрая сборка)»**, но Mono/ARMv7 не встанет на
   64-бит-only устройства (Pixel 7+) и FPS будет ниже, чем в релизе.
5. APK появится в `Builds/`.

### 3. Замер FPS на устройстве

```bash
adb install -r Builds/RagsToRiches-Stage0-il2cpp.apk
adb logcat -s Unity | grep PerfTest      # Windows: findstr PerfTest
```

- На экране жёлтый оверлей: мгновенный FPS + окно 5 секунд (avg / min).
- В logcat каждые 5 секунд строка `[PerfTest] avg=… worst frame=…`.
- Замер: 2–3 минуты активно бегать по сцене среди NPC и машин.
- Критерий: **avg ≥ 58, worst frame ≥ 30** (единичные просадки при загрузке не считаются).

### 4. Слабый эмулятор (2GB)

Android Studio → Device Manager → Create Device → любой телефон →
образ API 24+ → Show Advanced Settings → RAM **2048 MB**.
Запустить APK, убедиться: ставится, работает, не вылетает по памяти.
FPS эмулятора не показателен (другой GPU) — там проверяем только стабильность.

### 5. Тесты

Window → General → Test Runner → EditMode → Run All.
Должны пройти тесты `RagsToRiches.Tests.EditMode` (ManualTimeProviderTests).

## Если 60 FPS нет

1. Отключить тени у Directional Light (главный кандидат).
2. В URP-ассете: выключить HDR, MSAA → Disabled, Render Scale 0.9.
3. Если и это не помогло на устройстве 2019 года — фиксируем fallback: изометрия 2.5D.
