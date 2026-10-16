using UnityEngine;

namespace Minikit.AbilitySystem
{
    public enum MKModifierOperation
    {
        Add,
        Multiply,
        Override
    }

    /// <summary> Identified by instance, so several sources can apply modifiers with the same tag without colliding.
    /// The tag labels where a modifier came from. </summary>
    public class MKModifier
    {
        public MKTag tag { get; private set; }
        public MKModifierOperation operation { get; private set; }
        public float value { get; internal set; } = 0f;


        public MKModifier(MKTag _tag, MKModifierOperation _operation, float _value)
        {
            tag = _tag;
            operation = _operation;
            value = _value;
        }
    }
} // Minikit.AbilitySystem namespace
