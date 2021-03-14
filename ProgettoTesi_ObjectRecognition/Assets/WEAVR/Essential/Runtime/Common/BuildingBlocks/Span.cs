namespace TXT.WEAVR.Common
{
    using System;

    [Serializable]
    public struct Span
    {
        public static readonly Span UnitSpan = new Span(0, 1);

        public float min;
        public float max;
        public float? step;
        public float center => (min + max) * 0.5f;
        public float distance => UnityEngine.Mathf.Abs(max - min);
        public float signedDistance => max - min;

        public float random => UnityEngine.Random.Range(min, max);

        public Span(float min, float max) {
            if(min < max) {
                this.min = min;
                this.max = max;
            }
            else {
                this.min = max;
                this.max = min;
            }
            step = null;
        }

        public bool IsValid(float value) {
            return (min <= value && value <= max) || UnityEngine.Mathf.Approximately(value, min) || UnityEngine.Mathf.Approximately(value, max);
        }

        public bool IsOverlap(Span other) {
            return IsValid(other.min) || IsValid(other.max);
        }

        public void FitInside(Span other) {
            min = min < other.min ? other.min : min;
            max = max > other.max ? other.max : max;
        }

        public void Limit(float min, float max)
        {
            if(max < min)
            {
                float t = min;
                min = max;
                max = t;
            }

            this.min = UnityEngine.Mathf.Max(min, this.min);
            this.max = UnityEngine.Mathf.Min(max, this.max);
        }

        public void Resize(float min, float max) {
            if (min < max) {
                this.min = min;
                this.max = max;
            }
            else {
                this.min = max;
                this.max = min;
            }
        }

        public void Resize(Span other)
        {
            min = other.min;
            max = other.max;
        }

        public float Clamp(float value)
        {
            return UnityEngine.Mathf.Clamp(value, min, max);
        }

        public float Normalize(float value)
        {
            return (value - min) / (max - min);
        }

        public float Denormalize(float value)
        {
            return value * (max - min) + min;
        }

        public void Invert()
        {
            float temp = max;
            max = min;
            min = temp;
        }

        public override string ToString()
        {
            return $"[{min}, {max}]";
        }

        public override bool Equals(object obj)
        {
            return obj is Span s && s.min == min && s.max == max;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(Span a, Span b)
        {
            return a.min == b.min && a.max == b.max;
        }

        public static bool operator !=(Span a, Span b)
        {
            return a.min != b.min || a.max != b.max;
        }
    }

    [Serializable]
    public class OptionalSpan : Optional<Span>
    {
        private OptionalSpan() { }

        public OptionalSpan(Span span, bool enabled)
        {
            this.enabled = enabled;
            value = span;
        }

        public static implicit operator OptionalSpan(Span value)
        {
            return new OptionalSpan()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Span(OptionalSpan optional)
        {
            return optional.value;
        }
    }
}