using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Minikit.AbilitySystem.Internal;

namespace Minikit.AbilitySystem
{
    public abstract class MKAbility
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
        /// <summary> Tags that are granted to the owning MKAbilityComponent while this ability is active. </summary>
        public List<MKTag> grantedTags { get; } = new();
        /// <summary> This ability cannot be activated if the owning MKAbilityComponent has any of these tags. </summary>
        public List<MKTag> blockedByTags { get; } = new();
        /// <summary> When this ability is activated successfully, any active abilities on the owning MKAbilityComponent that matches one of these tags will be cancelled. </summary>
        public List<MKTag> cancelAbilityTags { get; } = new();
        /// <summary> Tags that, when granted to the owning MKAbilityComponent, will cancel this ability (only includes grantedLooseTags). </summary>
        public List<MKTag> cancelledByGrantedLooseTags { get; } = new();
        /// <summary> The tag for the effect used to track this ability's cooldown. </summary>
        public MKTag cooldownEffectTag { get; protected set; } = null;
        public int maxCharges { get; protected set; } = 1;
        public MKTag rechargeEffectTag { get; protected set; } = null;
        // ----- END SETTINGS -----

        // ----- INSTANCE -----
        public MKAbilityComponent abilityComponent { get; private set; } = null;
        public bool active { get; private set; } = false;
        public int currentCharges { get; private set; } = 1;
        public UnityEvent<int, int> OnChargesChanged = new();
        protected object[] activationParams;
        // ----- END INSTANCE -----


        public MKAbility(MKTag _typeTag)
        {
            typeTag = _typeTag;
        }
        
        
        public void OnPostConstruct()
        {
            currentCharges = maxCharges;

            if (cooldownEffectTag != null
                && maxCharges <= 1)
            {
                blockedByTags.Add(cooldownEffectTag);
            }
        }

        public virtual void Tick(float _deltaTime)
        {
            if (active)
            {
                OnActiveTick(_deltaTime);
            }

            if (maxCharges > 1
                && currentCharges < maxCharges
                && rechargeEffectTag != null
                && abilityComponent
                && abilityComponent.GetEffect(rechargeEffectTag) == null)
            {
                bool wasDepleted = currentCharges == 0;
                SetCurrentCharges(currentCharges + 1);

                if (wasDepleted
                    && cooldownEffectTag != null)
                {
                    abilityComponent.RemoveGrantedLooseTag(cooldownEffectTag);
                }

                if (currentCharges < maxCharges)
                {
                    StartRecharge();
                }
            }
        }

        protected virtual void OnActiveTick(float _deltaTime)
        {
        }

        public virtual bool CanActivate()
        {
            if (!abilityComponent)
            {
                return false;
            }

            if (abilityComponent.HasAnyGrantedTags(blockedByTags))
            {
                return false;
            }

            if (currentCharges <= 0)
            {
                return false;
            }

            return true;
        }

        public void Activate(params object[] _params)
        {
            activationParams = _params;

            active = true;

            if (maxCharges > 1)
            {
                SetCurrentCharges(currentCharges - 1);

                if (currentCharges == 0
                    && cooldownEffectTag != null)
                {
                    abilityComponent.AddGrantedLooseTag(cooldownEffectTag);
                }

                if (currentCharges < maxCharges
                    && rechargeEffectTag != null
                    && abilityComponent.GetEffect(rechargeEffectTag) == null)
                {
                    StartRecharge();
                }
            }

            List<MKTag> cancelledAbilities = abilityComponent.GetAllActiveAbilitiesWithTags(cancelAbilityTags);
            if (cancelledAbilities.Count > 0)
            {
                abilityComponent.CancelAbilities(cancelledAbilities);
            }

            OnActivate();
        }

        protected virtual void OnActivate()
        {
        }

        public void End()
        {
            if (active)
            {
                active = false;

                OnEnd(false);
            }
        }

        public void Cancel()
        {
            if (active)
            {
                active = false;

                OnEnd(true);
            }
        }

        protected virtual void OnEnd(bool _cancelled)
        {
        }

        protected virtual void AddTrackedEffect(MKTag _effectTag)
        {
            abilityComponent.AddEffect(MKEffect.Create(_effectTag));
        }

        protected virtual void StartCooldown()
        {
            if (cooldownEffectTag != null)
            {
                AddTrackedEffect(cooldownEffectTag);
            }
        }

        protected virtual void StartRecharge()
        {
            if (rechargeEffectTag != null)
            {
                AddTrackedEffect(rechargeEffectTag);
            }
        }

        private void SetCurrentCharges(int _charges)
        {
            int clamped = Mathf.Clamp(_charges, 0, maxCharges);
            if (clamped == currentCharges)
            {
                return;
            }

            int oldCharges = currentCharges;
            currentCharges = clamped;
            OnChargesChanged.Invoke(oldCharges, currentCharges);
        }

        public void Added(MKAbilityComponent _abilityComponent)
        {
            abilityComponent = _abilityComponent;

            OnAdded();
        }

        protected virtual void OnAdded()
        {
        }

        public void Removed(MKAbilityComponent _abilityComponent)
        {
            abilityComponent = null;

            Cancel();
            OnRemoved();
        }

        protected virtual void OnRemoved()
        {
        }

        public static MKAbility Create(MKTag _typeTag)
        {
            return Create<MKAbility>(_typeTag);
        }

        public static T Create<T>(MKTag _typeTag) where T : MKAbility
        {
            Type abilityType = MKAbilityReflector.GetRegisteredAbilityType(_typeTag);
            if (abilityType != null)
            {
                if (Activator.CreateInstance(abilityType, _typeTag) is T abilityInstance)
                {
                    abilityInstance.OnPostConstruct();
                    return abilityInstance;
                }
                else
                {
                    Debug.LogError($"Failed to create instance of {nameof(MKAbility)} because created instance was null");
                    return null;
                }
            }
            else
            {
                Debug.LogError($"Failed to create instance of {nameof(MKAbility)} because type was null");
                return null;
            }
        }
    }
} // Minikit.AbilitySystem namespace
