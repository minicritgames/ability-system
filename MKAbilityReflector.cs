using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Minikit.AbilitySystem.Internal
{
    public static class MKAbilityReflector
    {
        private static Dictionary<MKTag, Type> registeredAbilities = new();
        private static Dictionary<MKTag, Type> registeredEffects = new();


        static MKAbilityReflector()
        {
            foreach (Type type in MKReflection.GetConcreteSubclassesOf<MKAbility>())
            {
                if (type.GetField(MKAbility.__typeTagFieldName)?.GetValue(null) is MKTag abilityTypeTag)
                {
                    registeredAbilities.Add(abilityTypeTag, type);
                }
                else
                {
                    Debug.LogError($"Failed to register {nameof(MKAbility)} because field {MKAbility.__typeTagFieldName} wasn't overridden");
                }
            }

            foreach (Type type in MKReflection.GetConcreteSubclassesOf<MKEffect>())
            {
                if (type.GetField(MKEffect.__typeTagFieldName)?.GetValue(null) is MKTag effectTypeTag)
                {
                    registeredEffects.Add(effectTypeTag, type);
                }
                else
                {
                    Debug.LogError($"Failed to register {nameof(MKEffect)} because field {MKEffect.__typeTagFieldName} wasn't overridden");
                }
            }
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            RuntimeHelpers.RunClassConstructor(typeof(MKAbilityReflector).TypeHandle);
        }

        public static Type GetRegisteredAbilityType(MKTag _tag)
        {
            if (registeredAbilities.TryGetValue(_tag, out Type abilityType))
            {
                return abilityType;
            }

            Debug.LogError($"Failed to get registered {nameof(MKAbility)} type from tag {_tag.name}");
            return null;
        }

        public static Type GetRegisteredEffectType(MKTag _tag)
        {
            if (registeredEffects.TryGetValue(_tag, out Type effectType))
            {
                return effectType;
            }

            Debug.LogError($"Failed to get registered {nameof(MKEffect)} type from tag {_tag.name}");
            return null;
        }
    }
} // Minikit.AbilitySystem namespace
