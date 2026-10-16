using UnityEngine;
using UnityEngine.Events;

namespace Minikit.AbilitySystem
{
    public enum MKMaxChangePolicy
    {
        /// <summary> Only clamps the value down when the max drops below it </summary>
        Clamp,
        /// <summary> A raised max adds the increase to the value </summary>
        FillIncrease,
        /// <summary> Like FillIncrease, but an empty (zero) value stays empty </summary>
        FillIncreaseUnlessEmpty,
        /// <summary> The value keeps its fraction of the max </summary>
        KeepFraction
    }

    /// <summary> Fixed attributes cannot undo any modifiers that are applied. Best use cases for fixed attributes
    /// are values that have their base changed often. </summary>
    public class MKFixedAttribute
    {
        public UnityEvent<float, float> OnValueChanged = new();

        public MKTag tag { get; private set; }
        public float value { get; private set; }
        public MKAggregateAttribute maxAttribute { get; private set; }
        public MKMaxChangePolicy maxChangePolicy { get; private set; }


        public MKFixedAttribute(MKTag _tag, float _value)
        {
            tag = _tag;
            value = _value;
        }


        /// <summary> Bounds this value by a max attribute and decides what happens to it when the max changes </summary>
        public void SetMax(MKAggregateAttribute _maxAttribute, MKMaxChangePolicy _policy = MKMaxChangePolicy.Clamp)
        {
            maxAttribute?.OnCurrentValueChanged.RemoveListener(AggregateAttribute_OnCurrentValueChanged);

            maxAttribute = _maxAttribute;
            maxChangePolicy = _policy;

            if (maxAttribute != null)
            {
                maxAttribute.OnCurrentValueChanged.AddListener(AggregateAttribute_OnCurrentValueChanged);

                if (value > maxAttribute.currentValue)
                {
                    SetValue(maxAttribute.currentValue);
                }
            }
        }

        /// <returns> The value after being altered </returns>
        public float AlterValue(float _delta, float _clampMin = float.MinValue, float _clampMax = float.MaxValue)
        {
            if (maxAttribute != null)
            {
                _clampMax = Mathf.Min(_clampMax, maxAttribute.currentValue);
            }

            float oldValue = value;
            value = Mathf.Clamp(value + _delta, _clampMin, _clampMax);
            if (value != oldValue)
            {
                OnValueChanged.Invoke(oldValue, value);
            }

            return value;
        }

        public void SetValue(float _value)
        {
            float oldValue = value;
            value = _value;
            OnValueChanged.Invoke(oldValue, value);
        }

        /// <summary> Applies a modifier a single time. This cannot be undone or removed, it permanently alters the value of this attribute. </summary>
        public bool BakeModifier(MKModifier _modifier)
        {
            float oldValue = value;

            switch (_modifier.operation)
            {
                case MKModifierOperation.Add:
                    value += _modifier.value;
                    break;
                case MKModifierOperation.Multiply:
                    value *= _modifier.value;
                    break;
                case MKModifierOperation.Override:
                    value = _modifier.value;
                    break;
                default:
                    // Invalid operation
                    return false;
            }

            OnValueChanged.Invoke(oldValue, value);
            return true;
        }

        private void AggregateAttribute_OnCurrentValueChanged(float _oldMax, float _newMax)
        {
            float target = value;
            switch (maxChangePolicy)
            {
                case MKMaxChangePolicy.FillIncrease:
                case MKMaxChangePolicy.FillIncreaseUnlessEmpty:
                    bool fills = maxChangePolicy == MKMaxChangePolicy.FillIncrease
                        || value > 0f;
                    if (_newMax > _oldMax
                        && fills)
                    {
                        target = value + (_newMax - _oldMax);
                    }
                    break;
                case MKMaxChangePolicy.KeepFraction:
                    if (_oldMax > 0f)
                    {
                        target = value * (_newMax / _oldMax);
                    }
                    break;
            }

            target = Mathf.Min(target, _newMax);
            if (target != value)
            {
                SetValue(target);
            }
        }
    }
} // Minikit.AbilitySystem namespace
