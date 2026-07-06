#nullable enable
using System;

namespace RagsToRiches.Core.Timing
{
    /// <summary>
    /// Управляемый вручную источник времени: используется в unit-тестах
    /// и в симуляторе баланса (Стадия 6) для прогона «дней» игрока.
    /// </summary>
    public sealed class ManualTimeProvider : ITimeProvider
    {
        public double Now { get; private set; }

        public long UtcNowUnixSeconds { get; set; }

        /// <summary>Продвигает игровое время вперёд. Отрицательная дельта — ошибка вызывающего.</summary>
        public void Advance(double seconds)
        {
            if (seconds < 0d)
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Игровое время не может идти назад.");

            Now += seconds;
        }
    }
}
