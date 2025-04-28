using System;
using System.Collections.Generic;
using UnityEngine;
using Minikit.AbilitySystem.Internal;

namespace Minikit.AbilitySystem
{
    public abstract class MKEffect
    {
        // ----- INTERNAL -----
        /// <summary> NOTE: Internal, do not edit. </summary>
        public static string __typeTagFieldName = "__typeTag";
        /// <summary> Override in child classes with the new keyword. </summary>
        public static MKTag __typeTag = null;
        // ----- END INTERNAL -----

        // ----- SETTINGS -----
        /// <summary> A unique tag for this ability's class type. </summary>
        public MKTag typeTag { get; private set; } = null;
        /// <summary> Tags that are granted to the owning MASComponent while this effect is applied. </summary>
        public List<MKTag> grantedTags { get; } = new();
        public int maxStacks { get; protected set; } = 1; // -1 for infinite
        protected float duration = 0f;
        // ----- END SETTINGS -----

        // ----- INSTANCE -----
        public int stacks { get; private set; } = 0;

        protected MKAbilityComponent abilityComponent;
        protected float timeOfApplied = 0f;
        // ----- END INSTANCE -----


        public MKEffect(MKTag _typeTag)
        {
            typeTag = _typeTag;
        }
        
        
        public void PostConstruct()
        {
        }

        public void Added(MKAbilityComponent _abilityComponent)
        {
            abilityComponent = _abilityComponent;
            timeOfApplied = Time.time;

            OnAdded();
        }

        protected virtual void OnAdded()
        {
        }

        public void Tick(float _deltaTime)
        {
            OnActiveTick(_deltaTime);

            if (GetDurationRemaining() < 0f)
            {
                abilityComponent.RemoveEffect(typeTag);
            }
        }

        protected virtual void OnActiveTick(float _deltaTime)
        {
        }

        public void Removed()
        {
        }

        protected virtual void OnRemoved()
        {
        }

        public virtual int AddStacks(int _stacks)
        {
            if (_stacks <= 0)
            {
                return 0;
            }

            int oldStacks = stacks;
            stacks = Mathf.Clamp(stacks + _stacks, 0, maxStacks == -1 ? int.MaxValue : maxStacks);

            return stacks - oldStacks;
        }

        public virtual int RemoveStacks(int _stacks)
        {
            if (_stacks <= 0)
            {
                return 0;
            }

            int oldStacks = stacks;
            stacks = Mathf.Clamp(stacks - _stacks, 0, maxStacks == -1 ? int.MaxValue : maxStacks);

            return oldStacks - stacks;
        }

        public float GetDuration()
        {
            return duration;
        }

        public float GetDurationRemaining()
        {
            return GetDuration() - (Time.time - timeOfApplied);
        }


        public static MKEffect Create(MKTag _typeTag)
        {
            Type effectType = MKAbilityReflector.GetRegisteredEffectType(_typeTag);
            if (effectType != null)
            {
                if (Activator.CreateInstance(effectType, _typeTag) is MKEffect effectInstance)
                {
                    effectInstance.PostConstruct();
                    return effectInstance;
                }
                else
                {
                    Debug.LogError($"Failed to create instance of {nameof(MKEffect)} because created instance was null");
                    return null;
                }
            }
            else
            {
                Debug.LogError($"Failed to create instance of {nameof(MKEffect)} because type was null");
                return null;
            }
        }
    }
} // Minikit.AbilitySystem namespace
