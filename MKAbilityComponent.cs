using System.Collections.Generic;
using UnityEngine;

namespace Minikit.AbilitySystem
{
    public class MKAbilityComponent : MonoBehaviour
    {
        /// <summary> Every tag currently granted by loose grants, active abilities and applied effects, reference counted per source </summary>
        public MKTagCountContainer grantedTags { get; } = new();

        private List<MKAbility> grantedAbilities = new();
        private Dictionary<MKTag, MKEffect> effectsByTag = new();
        private Dictionary<MKTag, MKAggregateAttribute> aggregateAttributesByTag = new();
        private Dictionary<MKTag, MKFixedAttribute> fixedAttributesByTag = new();
        private Stack<List<MKAbility>> abilityBufferPool = new();
        private List<MKEffect> effectBuffer = new();


        protected virtual void Awake()
        {
        }

        protected virtual void Start()
        {
        }

        protected virtual void Update()
        {
            float deltaTime = Time.deltaTime;

            if (grantedAbilities.Count > 0)
            {
                List<MKAbility> buffer = RentAbilityBuffer();
                foreach (MKAbility ability in buffer)
                {
                    ability.Tick(deltaTime);
                }
                ReturnAbilityBuffer(buffer);
            }

            if (effectsByTag.Count > 0)
            {
                effectBuffer.AddRange(effectsByTag.Values);
                foreach (MKEffect effect in effectBuffer)
                {
                    // An earlier tick may have removed it
                    if (effectsByTag.TryGetValue(effect.typeTag, out MKEffect current)
                        && current == effect)
                    {
                        effect.Tick(deltaTime);
                    }
                }
                effectBuffer.Clear();
            }
        }


        public bool AddAttribute(MKAggregateAttribute _aggregateAttribute)
        {
            return aggregateAttributesByTag.TryAdd(_aggregateAttribute.tag, _aggregateAttribute);
        }

        public bool AddAttribute(MKFixedAttribute _fixedAttribute)
        {
            return fixedAttributesByTag.TryAdd(_fixedAttribute.tag, _fixedAttribute);
        }

        public bool RemoveAttribute(MKTag _tag)
        {
            if (aggregateAttributesByTag.Remove(_tag))
            {
                return true;
            }
            
            if (fixedAttributesByTag.Remove(_tag))
            {
                return true;
            }

            return false;
        }

        public MKAggregateAttribute GetAggregateAttribute(MKTag _tag)
        {
            return aggregateAttributesByTag.GetValueOrDefault(_tag);
        }

        public MKFixedAttribute GetFixedAttribute(MKTag _tag)
        {
            return fixedAttributesByTag.GetValueOrDefault(_tag);
        }

        public bool AddEffectStacks(MKTag _effectTag, int _stacks)
        {
            if (_stacks <= 0)
            {
                return false;
            }

            if (effectsByTag.ContainsKey(_effectTag))
            {
                if (effectsByTag[_effectTag].AddStacks(_stacks) > 0)
                {
                    return true; // Successfully added more stacks to an existing effect
                }
            }

            return false;
        }

        public bool AddEffect(MKEffect _effect, int _stacks = 1, MKEffectContext _context = null)
        {
            if (!effectsByTag.TryAdd(_effect.typeTag, _effect))
            {
                return false;
            }

            effectsByTag[_effect.typeTag].Added(this, _context);
            grantedTags.AddTags(_effect.grantedTags);
            effectsByTag[_effect.typeTag].AddStacks(_stacks);

            return true;
        }

        public bool RemoveEffect(MKTag _tag)
        {
            if (effectsByTag.ContainsKey(_tag))
            {
                MKEffect effect = effectsByTag[_tag];
                effectsByTag.Remove(_tag);
                grantedTags.RemoveTags(effect.grantedTags);
                effect.Removed();
                return true;
            }

            return false;
        }

        public MKEffect GetEffect(MKTag _tag)
        {
            return effectsByTag.GetValueOrDefault(_tag);
        }

        public bool AddAbility(MKAbility _ability)
        {
            if (HasAbility(_ability))
            {
                return false;
            }

            grantedAbilities.Add(_ability);
            _ability.Added(this);
            OnAddedAbility(_ability);
            return true;
        }

        protected virtual void OnAddedAbility(MKAbility _ability)
        {
        }

        public bool RemoveAbility(MKTag _tag)
        {
            return GetAbility(_tag) is MKAbility ability
                && RemoveAbility(ability);
        }

        public bool RemoveAbility(MKAbility _ability)
        {
            if (!grantedAbilities.Remove(_ability))
            {
                return false;
            }

            _ability.Removed(this);
            OnRemovedAbility(_ability);

            return true;
        }

        protected virtual void OnRemovedAbility(MKAbility _ability)
        {
        }

        public bool RemoveAbilities(IEnumerable<MKTag> _tagList)
        {
            int numberRemoved = 0;
            foreach (MKTag tagInList in _tagList)
            {
                if (RemoveAbility(tagInList))
                {
                    numberRemoved++;
                }
            }

            return numberRemoved > 0;
        }

        public bool RemoveAbilities(List<MKAbility> _abilities)
        {
            int numberRemoved = 0;
            foreach (MKAbility ability in _abilities)
            {
                if (RemoveAbility(ability))
                {
                    numberRemoved++;
                }
            }

            return numberRemoved > 0;
        }

        public bool ActivateAbility(MKTag _tag, params object[] _params)
        {
            foreach (MKAbility ability in grantedAbilities)
            {
                if (ability.typeTag == _tag)
                {
                    if (ability.CanActivate())
                    {
                        ability.Activate(_params);
                        return true;
                    }
                }
            }

            return false;
        }

        public bool ActivateAbility(MKAbility _ability, params object[] _params)
        {
            if (!grantedAbilities.Contains(_ability)
                || !_ability.CanActivate())
            {
                return false;
            }

            _ability.Activate(_params);
            return true;
        }

        public bool CancelAbility(MKTag _tag)
        {
            foreach (MKAbility ability in grantedAbilities)
            {
                if (ability.typeTag == _tag
                    && ability.active)
                {
                    ability.Cancel();
                    return true;
                }
            }

            return false;
        }

        public bool CancelAbility(MKAbility _ability)
        {
            if (!_ability.active
                || !grantedAbilities.Contains(_ability))
            {
                return false;
            }

            _ability.Cancel();
            return true;
        }

        public bool CancelAbilities(IEnumerable<MKTag> _tagList)
        {
            int numberCancelled = 0;
            foreach (MKTag tagInList in _tagList)
            {
                if (CancelAbility(tagInList))
                {
                    numberCancelled++;
                }
            }

            return numberCancelled > 0;
        }

        public bool CancelAbilities(List<MKAbility> _abilities)
        {
            int numberCancelled = 0;
            foreach (MKAbility ability in _abilities)
            {
                if (CancelAbility(ability))
                {
                    numberCancelled++;
                }
            }

            return numberCancelled > 0;
        }

        public IEnumerable<MKAbility> IterateAbilities()
        {
            return grantedAbilities;
        }

        public bool HasAbility(MKTag _tag)
        {
            return GetAbility(_tag) != null;
        }

        public bool HasAbility(MKAbility _ability)
        {
            return grantedAbilities.Contains(_ability);
        }

        public MKAbility GetAbility(MKTag _tag)
        {
            foreach (MKAbility ability in grantedAbilities)
            {
                if (ability.typeTag == _tag)
                {
                    return ability;
                }
            }

            return null;
        }

        public List<MKTag> GetAllActiveAbilities()
        {
            List<MKTag> tagList = new();
            foreach (MKAbility ability in grantedAbilities)
            {
                if (ability.active)
                {
                    tagList.Add(ability.typeTag);
                }
            }

            return tagList;
        }

        /// <summary> Active abilities whose type tag matches (hierarchically) any of <paramref name="_tags"/>. Empty when <paramref name="_tags"/> is null. </summary>
        public List<MKAbility> GetAllActiveAbilitiesWithTags(IEnumerable<MKTag> _tags)
        {
            List<MKAbility> abilities = new();
            if (_tags == null)
            {
                return abilities;
            }

            foreach (MKAbility ability in grantedAbilities)
            {
                if (ability.active
                    && ability.typeTag.MatchesAny(_tags))
                {
                    abilities.Add(ability);
                }
            }

            return abilities;
        }

        public void CancelActiveAbilitiesWithTags(MKTagContainer _tags)
        {
            if (_tags == null
                || _tags.isEmpty)
            {
                return;
            }

            List<MKAbility> buffer = RentAbilityBuffer();
            foreach (MKAbility ability in buffer)
            {
                if (ability.active
                    && MatchesAny(ability.typeTag, _tags))
                {
                    ability.Cancel();
                }
            }
            ReturnAbilityBuffer(buffer);
        }

        public void AddGrantedLooseTags(IEnumerable<MKTag> _tags)
        {
            foreach (MKTag tag in _tags)
            {
                AddGrantedLooseTag(tag);
            }
        }

        /// <summary> Loose grants are counted: each call needs a matching <see cref="RemoveGrantedLooseTag"/> </summary>
        public void AddGrantedLooseTag(MKTag _tag)
        {
            grantedTags.AddTag(_tag);

            List<MKAbility> buffer = RentAbilityBuffer();
            foreach (MKAbility ability in buffer)
            {
                if (ability.active
                    && MatchesAny(_tag, ability.cancelledByGrantedLooseTags))
                {
                    ability.Cancel();
                }
            }
            ReturnAbilityBuffer(buffer);
        }

        public void RemoveGrantedLooseTags(IEnumerable<MKTag> _tags)
        {
            foreach (MKTag tag in _tags)
            {
                RemoveGrantedLooseTag(tag);
            }
        }

        public void RemoveGrantedLooseTag(MKTag _tag)
        {
            grantedTags.RemoveTag(_tag);
        }

        public IMKTagCollection GetGrantedTags()
        {
            return grantedTags;
        }

        public bool HasGrantedTag(MKTag _tag)
        {
            return grantedTags.HasTag(_tag);
        }

        public bool HasAnyGrantedTags(IEnumerable<MKTag> _tags)
        {
            return grantedTags.HasAny(_tags);
        }

        public bool HasAllGrantedTags(IEnumerable<MKTag> _tags)
        {
            return grantedTags.HasAll(_tags);
        }

        // Indexed so the per-activation checks don't box an enumerator
        public bool HasAnyGrantedTags(MKTagContainer _tags)
        {
            IReadOnlyList<MKTag> tags = _tags.GetTags();
            for (int i = 0; i < tags.Count; i++)
            {
                if (grantedTags.HasTag(tags[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAllGrantedTags(MKTagContainer _tags)
        {
            IReadOnlyList<MKTag> tags = _tags.GetTags();
            for (int i = 0; i < tags.Count; i++)
            {
                if (!grantedTags.HasTag(tags[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchesAny(MKTag _tag, MKTagContainer _tags)
        {
            IReadOnlyList<MKTag> tags = _tags.GetTags();
            for (int i = 0; i < tags.Count; i++)
            {
                if (_tag.MatchesTag(tags[i]))
                {
                    return true;
                }
            }

            return false;
        }

        // Pooled because cancels can re-enter (a cancelled ability granting a loose tag)
        private List<MKAbility> RentAbilityBuffer()
        {
            List<MKAbility> buffer = abilityBufferPool.Count > 0 ? abilityBufferPool.Pop() : new();
            buffer.AddRange(grantedAbilities);
            return buffer;
        }

        private void ReturnAbilityBuffer(List<MKAbility> _buffer)
        {
            _buffer.Clear();
            abilityBufferPool.Push(_buffer);
        }
    }
} // Minikit.AbilitySystem namespace
