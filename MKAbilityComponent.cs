using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Minikit.AbilitySystem
{
    public class MKAbilityComponent : MonoBehaviour 
    {
        private List<MKTag> looseGrantedTags = new();
        private List<MKAbility> grantedAbilities = new();
        private Dictionary<MKTag, MKEffect> effectsByTag = new();
        private Dictionary<MKTag, MKAggregateAttribute> attributesByTag = new();


        protected virtual void Awake()
        {
        }

        protected virtual void Start()
        {
        }

        protected virtual void Update()
        {
            foreach (MKAbility ability in IterateAbilities().ToArray())
            {
                ability.Tick(Time.deltaTime);
            }

            foreach (MKEffect effect in effectsByTag.Values.ToArray())
            {
                effect.Tick(Time.deltaTime);
            }
        }


        public bool AddAttribute(MKAggregateAttribute _attribute)
        {
            return attributesByTag.TryAdd(_attribute.tag, _attribute);
        }

        public bool RemoveAttribute(MKTag _tag)
        {
            if (attributesByTag.ContainsKey(_tag))
            {
                attributesByTag.Remove(_tag);
                return true;
            }

            return false;
        }

        public MKAggregateAttribute GetAttribute(MKTag _tag)
        {
            return attributesByTag.GetValueOrDefault(_tag);
        }

        public bool AddEffectStacks(MKTag _effectTag, int _stacks = 1)
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

        public bool AddEffect(MKEffect _effect, int _stacks = 1)
        {
            effectsByTag.Add(_effect.typeTag, _effect);
            effectsByTag[_effect.typeTag].Added(this);
            effectsByTag[_effect.typeTag].AddStacks(_stacks);

            return true;
        }

        public bool RemoveEffect(MKTag _tag)
        {
            if (effectsByTag.ContainsKey(_tag))
            {
                MKEffect effect = effectsByTag[_tag];
                effectsByTag.Remove(_tag);
                effect.Removed();
                return true;
            }

            return false;
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
            foreach (MKAbility ability in IterateAbilities().ToArray())
            {
                if (ability.typeTag == _tag)
                {
                    if (ability.active)
                    {
                        ability.Cancel();
                    }
                    grantedAbilities.Remove(ability);
                    OnRemovedAbility(ability);

                    return true;
                }
            }

            return false;
        }

        public bool RemoveAbility(MKAbility _ability)
        {
            if (!IterateAbilities().Contains(_ability))
            {
                return false;
            }

            foreach (MKAbility ability in IterateAbilities().ToArray())
            {
                if (ability == _ability)
                {
                    if (ability.active)
                    {
                        ability.Cancel();
                    }
                    grantedAbilities.Remove(ability);
                    OnRemovedAbility(ability);

                    return true;
                }
            }

            return false;
        }

        protected virtual void OnRemovedAbility(MKAbility _ability)
        {
        }

        public bool RemoveAbilities(List<MKTag> _tagList)
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
            foreach (MKAbility ability in IterateAbilities())
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
            if (!IterateAbilities().Contains(_ability))
            {
                return false;
            }

            foreach (MKAbility ability in IterateAbilities())
            {
                if (ability == _ability)
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

        public bool CancelAbility(MKTag _tag)
        {
            foreach (MKAbility ability in IterateAbilities())
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
            if (!IterateAbilities().Contains(_ability))
            {
                return false;
            }

            foreach (MKAbility ability in IterateAbilities())
            {
                if (ability == _ability
                    && ability.active)
                {
                    ability.Cancel();
                    return true;
                }
            }

            return false;
        }

        public bool CancelAbilities(List<MKTag> _tagList)
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
            return grantedAbilities.FirstOrDefault(a => a.typeTag == _tag) != null;
        }

        public bool HasAbility(MKAbility _ability)
        {
            return grantedAbilities.Contains(_ability);
        }

        public List<MKTag> GetAllActiveAbilities()
        {
            List<MKTag> tagList = new();
            foreach (MKAbility ability in IterateAbilities())
            {
                if (ability.active)
                {
                    tagList.Add(ability.typeTag);
                }
            }

            return tagList;
        }

        public List<MKTag> GetAllActiveAbilitiesWithTags(List<MKTag> _tagList = null)
        {
            List<MKTag> tagList = new();
            foreach (MKAbility ability in IterateAbilities())
            {
                if (ability.active
                    && _tagList != null // If we don't supply a valid tag list, fail the check
                    && _tagList.Contains(ability.typeTag))
                {
                    tagList.Add(ability.typeTag);
                }
            }

            return tagList;
        }

        public void AddGrantedLooseTag(MKTag _tag)
        {
            looseGrantedTags.Add(_tag);

            foreach (MKAbility ability in IterateAbilities())
            {
                if (ability.active
                    && ability.cancelledByGrantedLooseTags.Contains(_tag))
                {
                    ability.Cancel();
                }
            }
        }

        public void RemoveGrantedLooseTag(MKTag _tag)
        {
            looseGrantedTags.Remove(_tag);
        }

        public List<MKTag> GetGrantedTags()
        {
            List<MKTag> tagList = new();
            tagList.AddRange(looseGrantedTags);
            foreach (MKAbility ability in IterateAbilities())
            {
                if (ability.active)
                {
                    tagList.AddRange(ability.grantedTags);
                }
            }
            foreach (MKEffect effect in effectsByTag.Values)
            {
                tagList.AddRange(effect.grantedTags);
            }

            return tagList;
        }

        public bool HasGrantedTag(MKTag _tag)
        {
            foreach (MKTag grantedTag in GetGrantedTags())
            {
                if (grantedTag == _tag)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAnyGrantedTags(List<MKTag> _tagList)
        {
            foreach (MKTag grantedTag in GetGrantedTags())
            {
                if (_tagList.Contains(grantedTag))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAllGrantedTags(List<MKTag> _tagList)
        {
            foreach (MKTag grantedTag in GetGrantedTags())
            {
                if (!_tagList.Contains(grantedTag))
                {
                    return false;
                }
            }

            return true;
        }
    }
} // Minikit.AbilitySystem namespace
