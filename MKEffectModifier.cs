using UnityEngine;

namespace Minikit.AbilitySystem
{
    public class MKEffectModifier
    {
        public MKTag attributeTag { get; private set; }
        public MKModifierOperation operation { get; private set; }
        public float magnitude { get; private set; }
        /// <summary> Add scales by stack count, Multiply compounds per stack, Override ignores stacks </summary>
        public bool scaleWithStacks { get; private set; }

        internal MKAggregateAttribute attribute;
        internal MKModifier modifier;


        public MKEffectModifier(MKTag _attributeTag, MKModifierOperation _operation, float _magnitude, bool _scaleWithStacks = false)
        {
            attributeTag = _attributeTag;
            operation = _operation;
            magnitude = _magnitude;
            scaleWithStacks = _scaleWithStacks;
        }


        public float GetValue(int _stacks)
        {
            if (!scaleWithStacks)
            {
                return magnitude;
            }

            switch (operation)
            {
                case MKModifierOperation.Add:
                    return magnitude * _stacks;
                case MKModifierOperation.Multiply:
                    return Mathf.Pow(magnitude, _stacks);
                default:
                    return magnitude;
            }
        }
    }
} // Minikit.AbilitySystem namespace
