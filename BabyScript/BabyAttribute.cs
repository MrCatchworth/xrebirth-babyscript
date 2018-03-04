using System;

namespace BabyScript
{
    public class BabyAttribute
    {
        public readonly string _name;
        public string Name
        {
            get
            {
                return IsAnonymous ? null : _name;
            }
        }
        public readonly string Value;
        public bool IsAnonymous;

        public BabyAttribute(String name, String value)
        {
            _name = name;
            Value = value;
            IsAnonymous = name == null;
        }
    }
}