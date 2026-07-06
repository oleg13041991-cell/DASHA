#nullable enable
using UnityEngine;

namespace RagsToRiches.View
{
    /// <summary>
    /// Экранный счётчик FPS для замера на реальном устройстве.
    /// Показывает мгновенный FPS и статистику 5-секундного окна (avg / худший кадр),
    /// а также пишет сводку окна в logcat с тегом [PerfTest].
    /// </summary>
    public sealed class FpsCounter : MonoBehaviour
    {
        private const float WindowSeconds = 5f;

        private float _smoothedDelta;
        private float _windowTimer;
        private int _windowFrames;
        private float _windowWorstDelta;
        private float _lastWindowAvgFps;
        private float _lastWindowMinFps;
        private GUIStyle? _style;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _smoothedDelta = Mathf.Lerp(_smoothedDelta, dt, 0.1f);

            _windowTimer += dt;
            _windowFrames++;
            if (dt > _windowWorstDelta)
                _windowWorstDelta = dt;

            if (_windowTimer < WindowSeconds)
                return;

            _lastWindowAvgFps = _windowFrames / _windowTimer;
            _lastWindowMinFps = _windowWorstDelta > 0f ? 1f / _windowWorstDelta : 0f;
            Debug.Log($"[PerfTest] avg={_lastWindowAvgFps:F1} FPS, worst frame={_lastWindowMinFps:F1} FPS (window {WindowSeconds:F0}s)");

            _windowTimer = 0f;
            _windowFrames = 0;
            _windowWorstDelta = 0f;
        }

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(24, Screen.height / 28),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.yellow }
            };

            float fps = _smoothedDelta > 0f ? 1f / _smoothedDelta : 0f;
            GUI.Label(
                new Rect(20f, 20f, 700f, 220f),
                $"FPS: {fps:F0}\n5s avg: {_lastWindowAvgFps:F0}  min: {_lastWindowMinFps:F0}",
                _style);
        }
    }
}
