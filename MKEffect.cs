using System;
using System.Collections.Generic;
using UnityEngine;
using Minikit.AbilitySystem.Internal;

namespace Minikit.AbilitySystem
{
    public enum MKEffectDurationPolicy
    {
        Duration,
        Infinite
    }

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
        /// <summary> Tags that are granted to the owning MKAbilityComponent while this effect is applied. </summary>
        public MKTagContainer grantedTags { get; } = new();
        public int maxStacks { get; protected set; } = 1; // -1 for infinite
        public MKEffectDurationPolicy durationPolicy { get; protected set; } = MKEffectDurationPolicy.Duration;
        protected float duration = 0f;
        // ----- END SETTINGS -----

        // ----- INSTANCE -----
        public int stacks { get; private set; } = 0;
        public MKEffectContext context { get; private set; } = null;

        protected MKAbilityComponent abilityComponent;
        protected float timeOfApplied = 0f;
        private List<MKEffectModifier> modifiers = new();
        // ----- END INSTANCE -----


        public MKEffect(MKTag _typeTag)
        {
            typeTag = _typeTag;
        }
        
        
        public void PostConstruct()
        {
        }

        public void Added(MKAbilityComponent _abilityComponent, MKEffectContext _context = null)
        {
            abilityComponent = _abilityComponent;
            context = _context;
            timeOfApplied = Time.time;

            foreach (MKEffectModifier modifier in modifiers)
            {
                ApplyModifier(modifier);
            }

            OnAdded();
        }

        protected virtual void OnAdded()
        {
        }

        public void Tick(float _deltaTime)
        {
            OnActiveTick(_deltaTime);

            if (durationPolicy == MKEffectDurationPolicy.Duration
                && GetDurationRemaining() < 0f)
            {
                abilityComponent.RemoveEffect(typeTag);
            }
        }

        protected virtual void OnActiveTick(float _deltaTime)
        {
        }

        public void Removed()
        {
            foreach (MKEffectModifier modifier in modifiers)
            {
                modifier.attribute?.RemoveModifier(modifier.modifier);
                modifier.attribute = null;
                modifier.modifier = null;
            }

            OnRemoved();
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
            RefreshStackedModifiers(oldStacks);

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
            RefreshStackedModifiers(oldStacks);

            return oldStacks - stacks;
        }

        public float GetDuration()
        {
            return duration;
        }

        public float GetDurationRemaining()
        {
            if (durationPolicy == MKEffectDurationPolicy.Infinite)
            {
                return float.PositiveInfinity;
            }

            return GetDuration() - (Time.time - timeOfApplied);
        }

        public void SetDurationRemaining(float _remaining)
        {
            timeOfApplied = Time.time - (GetDuration() - _remaining);
        }

        /// <summary> Modifies an attribute on the owning component while this effect is applied. Attributes the owner doesn't have are skipped. </summary>
        public MKEffectModifier AddModifier(MKTag _attributeTag, MKModifierOperation _operation, float _magnitude, bool _scaleWithStacks = false)
        {
            MKEffectModifier modifier = new(_attributeTag, _operation, _magnitude, _scaleWithStacks);
            modifiers.Add(modifier);

            if (abilityComponent)
            {
                ApplyModifier(modifier);
            }

            return modifier;
        }

        public IReadOnlyList<MKEffectModifier> GetModifiers()
        {
            return modifiers;
        }

        private void ApplyModifier(MKEffectModifier _modifier)
        {
            if (_modifier.modifier != null
                || abilityComponent.GetAggregateAttribute(_modifier.attributeTag) is not MKAggregateAttribute attribute)
            {
                return;
            }

            _modifier.attribute = attribute;
            _modifier.modifier = new MKModifier(typeTag, _modifier.operation, _modifier.GetValue(stacks));
            attribute.ApplyModifier(_modifier.modifier);
        }

        private void RefreshStackedModifiers(int _oldStacks)
        {
            if (stacks == _oldStacks)
            {
                return;
            }

            foreach (MKEffectModifier modifier in modifiers)
            {
                if (modifier.scaleWithStacks
                    && modifier.attribute != null)
                {
                    modifier.attribute.SetModifierValue(modifier.modifier, modifier.GetValue(stacks));
                }
            }
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
