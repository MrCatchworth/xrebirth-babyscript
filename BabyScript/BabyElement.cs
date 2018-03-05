using System;

namespace BabyScript
{
    public class BabyElement
    {
        public bool IsAssignment { get; private set; }
        public bool IsComment { get; private set; }
        public readonly string Name;
        public readonly BabyAttribute[] Attributes;
        public readonly BabyElement[] Children;

        public static BabyElement CreateAssignment(string name, string exact)
        {
            BabyAttribute[] attributes = new BabyAttribute[]
            {
                new BabyAttribute("name", name),
                new BabyAttribute("exact", exact)
            };
            BabyElement[] children = new BabyElement[0];

            BabyElement retVal = new BabyElement("set_value", attributes, children);
            retVal.IsAssignment = true;
            return retVal;
        }

        public static BabyElement CreateComment(string commentText)
        {
            BabyAttribute[] attributes = new BabyAttribute[]
            {
                new BabyAttribute("text", commentText)
            };
            BabyElement[] children = new BabyElement[0];

            BabyElement retVal = new BabyElement("", attributes, children);
            retVal.IsComment = true;
            return retVal;
        }

        public BabyElement(string name, BabyAttribute[] attr, BabyElement[] children)
        {
            Name = name;
            Attributes = attr;
            Children = children;
            IsAssignment = false;
            IsComment = false;
        }
    }
}