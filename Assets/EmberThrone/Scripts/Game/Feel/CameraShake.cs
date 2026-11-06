using UnityEngine;

namespace EmberThrone.Game
{
    /// <summary>
    /// Screen shake from a decaying "trauma" value: hits add trauma, the shake is its square (small hits barely move,
    /// big ones punch) and it settles by itself. It moves only the camera, around a rest position the layout sets,
    /// never the board. Smooth noise, not a random jump every frame.
    /// </summary>
    public sealed class CameraShake : MonoBehaviour
    {
        /// <summary>Trauma lost per second: a full shake settles in about 0.6 s.</summary>
        const float Decay = 1.7f;
        /// <summary>Largest offset, in board cells.</summary>
        const float MaxOffset = 0.22f;
        /// <summary>Largest roll, in degrees.</summary>
        const float MaxRoll = 1.2f;
        const float Frequency = 28f;

        Vector3 _rest;
        float _trauma;
        float _time;

        public float Trauma => _trauma;
        public Vector3 Rest => _rest;

        /// <summary>Hits add up; they never reset a shake that is already running.</summary>
        public void Add(float amount)
        {
            if (amount <= 0f) return;
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        /// <summary>Where the camera sits when it is not shaking (the layout's camera position).</summary>
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
