# Rags to Riches — Idle Tycoon

Android idle arcade simulator на Unity 6 (URP, low-poly 3D).
Путь от нищего до магната: 6 ступеней-локаций с престижем.
Референс механик: Pizza Ready.

- **План разработки (10 стадий):** [docs/PLAN.md](docs/PLAN.md)
- **Текущая стадия:** Стадия 0 — прототип производительности → [docs/STAGE0.md](docs/STAGE0.md)
- **Правила проекта для Claude Code:** [CLAUDE.md](CLAUDE.md)

## Быстрый старт (Стадия 0)

1. Открыть проект в **Unity 6000.x** (Unity Hub → Add) с модулем Android Build Support.
2. Меню **RagsToRiches → 1. Настроить URP**.
3. Меню **RagsToRiches → 2. Собрать перф-сцену** → Play для проверки.
4. Меню **RagsToRiches → 3. Собрать Debug APK (IL2CPP, ARM64)**.
5. `adb install -r Builds/RagsToRiches-Stage0-il2cpp.apk` → замер FPS по [docs/STAGE0.md](docs/STAGE0.md).

## Структура проекта

```
Assets/
  Scripts/
    Core/      — чистая C# симуляция, без UnityEngine (asmdef: noEngineReferences)
      Timing/  — ITimeProvider, ManualTimeProvider (инжектируемое время)
    View/      — MonoBehaviour-слой: PlayerController, IsometricCameraFollow,
                 NpcWanderer, FpsCounter
    Editor/    — PerfSceneBuilder (URP + генерация перф-сцены),
                 AndroidBuildScript (debug APK, min API 24)
  Tests/
    EditMode/  — unit-тесты Core (Unity Test Runner)
  Scenes/      — PerfTest.unity (генерируется скриптом)
  Settings/    — URP-ассеты и материалы (генерируются скриптом)
docs/          — план и инструкции стадий
Packages/      — manifest.json (URP 17, AI Navigation 2, Test Framework)
```

## Ветки

Одна стадия плана = одна ветка. Merge только при зелёных тестах (Test Runner, edit mode).
