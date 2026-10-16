using System.Collections.Generic;
using UnityEngine.Events;

namespace Minikit.AbilitySystem
{
    /// <summary> Aggregate attributes can add and remove modifiers as desired. Best use cases for aggregate attributes
    /// are values that don't change their base, but will be modified and unmodified often. </summary>
    public class MKAggregateAttribute
    {
        public UnityEvent<float, float> OnBaseValueChanged = new();
        public UnityEvent<float, float> OnCurrentValueChanged = new();

        public MKTag tag { get; private set; }
        public float baseValue { get; private set; }
        public float currentValue { get; private set; }

        private List<MKModifier> modifiers = new();


        public MKAggregateAttribute(MKTag _tag, float _value)
        {
            tag = _tag;
            baseValue = _value;
            currentValue = baseValue;
        }


        public void SetBaseValue(float _value)
        {
            if (baseValue == _value)
            {
                return;
            }

            float oldBaseValue = baseValue;
            baseValue = _value;
            OnBaseValueChanged.Invoke(oldBaseValue, baseValue);
            UpdateCurrentValue();
        }

        public bool ApplyModifier(MKModifier _modifier)
        {
            if (_modifier == null
                || modifiers.Contains(_modifier))
            {
                return false;
            }

            modifiers.Add(_modifier);
            UpdateCurrentValue();

            return true;
        }

        public bool RemoveModifier(MKModifier _modifier)
        {
            if (!modifiers.Remove(_modifier))
            {
                return false;
            }

            UpdateCurrentValue();

            return true;
        }

        /// <summary> Removes every modifier carrying this tag </summary>
        public bool RemoveModifierByTag(MKTag _tag)
        {
            bool removed = false;
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                if (modifiers[i].tag == _tag)
                {
                    modifiers.RemoveAt(i);
                    removed = true;
                }
            }

            if (!removed)
            {
                return false;
            }

            UpdateCurrentValue();

            return true;
        }

        public bool HasModifier(MKModifier _modifier)
        {
            return modifiers.Contains(_modifier);
        }

        public void SetModifierValue(MKModifier _modifier, float _value)
        {
            if (_modifier.value == _value)
            {
                return;
            }

            _modifier.value = _value;
            if (modifiers.Contains(_modifier))
            {
                UpdateCurrentValue();
            }
        }

        private void UpdateCurrentValue()
        {
            float oldCurrentValue = currentValue;
            float newCurrentValueBase = baseValue;
            float newCurrentValueMultiplier = 1f;
            bool hasOverride = false;
            float overrideValue = 0f;

            // Overrides are applied in order, so the most recently applied one wins
            foreach (MKModifier modifier in modifiers)
            {
                switch (modifier.operation)
                {
                    case MKModifierOperation.Add:
                        newCurrentValueBase += modifier.value;
                        break;
                    case MKModifierOperation.Multiply:
                        newCurrentValueMultiplier *= modifier.value;
                        break;
                    case MKModifierOperation.Override:
                        hasOverride = true;
                        overrideValue = modifier.value;
                        break;
                }
            }

            if (hasOverride)
            {
                currentValue = overrideValue;
            }
            else
            {
                currentValue = newCurrentValueBase * newCurrentValueMultiplier;
            }

            if (currentValue != oldCurrentValue)
            {
                OnCurrentValueChanged.Invoke(oldCurrentValue, currentValue);
            }
        }
    }
} // Minikit.AbilitySystem namespace
