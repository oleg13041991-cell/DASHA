#nullable enable

namespace RagsToRiches.Core.Timing
{
    /// <summary>
    /// Абстракция времени для Core-слоя. Прямые обращения к Time.time
    /// и DateTime в симуляции запрещены (правило CLAUDE.md) — время
    /// всегда инжектируется, чтобы работали unit-тесты и оффлайн-расчёт.
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>Монотонное игровое время в секундах (таймеры, очереди, станции).</summary>
        double Now { get; }

        /// <summary>Unix-время UTC в секундах (оффлайн-доход, защита от перевода часов).</summary>
        long UtcNowUnixSeconds { get; }
    }
}
