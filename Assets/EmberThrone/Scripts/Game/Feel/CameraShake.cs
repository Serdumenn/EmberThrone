using UnityEngine;

namespace EmberThrone.Game
{
    public sealed class CameraShake : MonoBehaviour
    {
        public const string EnabledKey = "et.shake";
        const float Decay = 1.7f;
        const float MaxOffset = 0.22f;
        const float MaxRoll = 1.2f;
        const float Frequency = 28f;

        Vector3 _rest;
        float _trauma;
        float _time;

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(EnabledKey, 1) == 1;
            set => PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
        }

        public float Trauma => _trauma;
        public Vector3 Rest => _rest;

        public void Add(float amount)
        {
            if (amount <= 0f || !Enabled) return;
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        public void SetRest(Vector3 position)
        {
            _rest = position;
            if (_trauma <= 0f) transform.position = position;
        }

        public void Stop()
        {
            _trauma = 0f;
            transform.position = _rest;
            transform.localRotation = Quaternion.identity;
        }

        void LateUpdate()
        {
            if (_trauma <= 0f) return;
            _trauma = Mathf.Max(0f, _trauma - Decay * Time.deltaTime);
            if (_trauma <= 0f)
            {
                Stop();
                return;
            }
            float shake = _trauma * _trauma;
            _time += Time.deltaTime * Frequency;
            var offset = new Vector3(Mathf.PerlinNoise(_time, 0.3f) * 2f - 1f, Mathf.PerlinNoise(0.7f, _time) * 2f - 1f, 0f);
            transform.position = _rest + offset * (MaxOffset * shake);
            transform.localRotation = Quaternion.Euler(0f, 0f, (Mathf.PerlinNoise(_time, 5.1f) * 2f - 1f) * MaxRoll * shake);
        }
    }
}
