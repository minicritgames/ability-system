using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Minikit.AbilitySystem.Internal
{
    public static class MKAbilityReflector
    {
        private static Dictionary<MKTag, Type> registeredAbilities = new();
        private static Dictionary<MKTag, Type> registeredEffects = new();


        static MKAbilityReflector()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(MKAbility))
                        && !type.IsAbstract) // Ignore abstract ability classes since we don't want to register them
                    {
                        FieldInfo tagFieldInfo = type.GetField(MKAbility.__typeTagFieldName);
                        MKTag abilityTypeTag = (MKTag)tagFieldInfo.GetValue(null);
                        if (abilityTypeTag != null)
                        {
                            registeredAbilities.Add(abilityTypeTag, type);
                        }
                        else
                        {
                            Debug.LogError($"Failed to register {nameof(MKAbility)} because field {MKAbility.__typeTagFieldName} wasn't overridden");
                        }
                        
                        continue;
                    }

                    if (type.IsSubclassOf(typeof(MKEffect))
                        && !type.IsAbstract)
                    {
                        FieldInfo tagFieldInfo = type.GetField(MKEffect.__typeTagFieldName);
                        MKTag abilityTypeTag = (MKTag)tagFieldInfo.GetValue(null);
                        if (abilityTypeTag != null)
                        {
                            registeredEffects.Add(abilityTypeTag, type);
                        }
                        else
                        {
                            Debug.LogError($"Failed to register {nameof(MKEffect)} because field {MKEffect.__typeTagFieldName} wasn't overridden");
                        }
                        
                        continue;
                    }
                }
            }
        }


        public static Type GetRegisteredAbilityType(MKTag _tag)
        {
            if (registeredAbilities.TryGetValue(_tag, out Type abilityType))
            {
                return abilityType;
            }

            Debug.LogError($"Failed to get registered {nameof(MKAbility)} type from tag {_tag.key}");
            return null;
        }

        public static Type GetRegisteredEffectType(MKTag _tag)
        {
            if (registeredEffects.TryGetValue(_tag, out Type effectType))
            {
                return effectType;
            }

            Debug.LogError($"Failed to get registered {nameof(MKEffect)} type from tag {_tag.key}");
            return null;
        }
    }
} // Minikit.AbilitySystem namespace
