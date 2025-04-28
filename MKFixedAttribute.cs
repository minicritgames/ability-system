using UnityEngine;
using UnityEngine.Events;

namespace Minikit.AbilitySystem
{
    /// <summary> Fixed attributes cannot undo any modifiers that are applied. Best use cases for fixed attributes
    /// are values that have their base changed often. </summary>
    public class MKFixedAttribute
    {
        public UnityEvent<float, float> OnValueChanged = new();

        public MKTag tag { get; private set; }
        public float value { get; private set; }


        public MKFixedAttribute(MKTag _tag, float _value)
        {
            tag = _tag;
            value = _value;
        }


        /// <returns> The value after being altered </returns>
        public float AlterValue(float _delta, float _clampMin = float.MinValue, float _clampMax = float.MaxValue)
        {
            float oldValue = value;
            value = Mathf.Clamp(value + _delta, _clampMin, _clampMax);
            OnValueChanged.Invoke(oldValue, value);

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
    }
} // Minikit.AbilitySystem namespace
