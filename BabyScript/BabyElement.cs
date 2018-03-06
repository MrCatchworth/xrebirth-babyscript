using System;

namespace BabyScript
{
    public class BabyElement : BabyNode
    {
        public bool IsAssignment { get; private set; }
        public bool IsComment { get; private set; }
        public readonly string Name;
        public readonly BabyAttribute[] Attributes;
        

        public static BabyElement CreateAssignment(string name, string exact)
        {
            BabyAttribute[] attributes = new BabyAttribute[]
            {
                new BabyAttribute("name", name),
                new BabyAttribute("exact", exact)
            };

            BabyElement retVal = new BabyElement("set_value", attributes, null);
            retVal.IsAssignment = true;
            return retVal;
        }

        public BabyElement(string name, BabyAttribute[] attr, BabyNode[] children) : base(children)
        {
            Name = name;
            Attributes = attr;
            IsAssignment = false;
            IsComment = false;
        }
    }
}