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

        private Dictionary<MKTag, MKModifier> modifiers = new();


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
            if (!modifiers.TryAdd(_modifier.tag, _modifier))
            {
                // This modifier has already been applied
                return false;
            }

            UpdateCurrentValue();

            return true;
        }

        public bool RemoveModifier(MKModifier _modifier)
        {
            return RemoveModifierByTag(_modifier.tag);
        }

        public bool RemoveModifierByTag(MKTag _tag)
        {
            if (!modifiers.ContainsKey(_tag))
            {
                // There is no modifier with this tag
                return false;
            }

            modifiers.Remove(_tag);
            UpdateCurrentValue();

            return true;
        }

        private void UpdateCurrentValue()
        {
            float oldCurrentValue = currentValue;
            float newCurrentValueBase = baseValue;
            float newCurrentValueMultiplier = 1f;
            Stack<float> newCurrentValueOverride = new();

            foreach (MKModifier modifier in modifiers.Values)
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
                        newCurrentValueOverride.Push(modifier.value);
                        break;
                }
            }

            if (newCurrentValueOverride.Count > 0)
            {
                currentValue = newCurrentValueOverride.Pop();
            }
            else
            {
                currentValue = newCurrentValueBase * newCurrentValueMultiplier;
            }

            OnCurrentValueChanged.Invoke(oldCurrentValue, currentValue);
        }
    }
} // Minikit.AbilitySystem namespace
