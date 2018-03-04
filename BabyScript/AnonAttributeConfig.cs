using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

public class AnonAttributeConfig
{
    private Dictionary<string, string[]> NameToAttributes;
    
    static readonly Regex lineRegex = new Regex("^([A-Za-z][A-Za-z0-9_]*):([A-Za-z][A-Za-z0-9_]*(?:,[A-Za-z][A-Za-z0-9_]*)*)$");
    static readonly string errorPrefix = "Anonymous attribute config:";
    public static AnonAttributeConfig ReadFromFile(string inFile)
    {
        Dictionary<string, string[]> nameToAttrib = new Dictionary<string, string[]>();
        
        int lineNum=0;
        foreach (string rawLine in File.ReadLines(inFile))
        {
            lineNum++;
            
            string line = rawLine.Trim();
            
            //allow C-style single line comments and empty lines
            if (line == string.Empty || line.StartsWith("//"))
            {
                continue;
            }
            
            Match m = lineRegex.Match(line);
            if (!m.Success)
            {
                throw new ArgumentException(string.Format(errorPrefix+" invalid formatting on line {0}: {1}", lineNum, line));
            }
            
            string tag = m.Groups[1].Value;
            
            string namesString = m.Groups[2].Value;
            string[] rawNames = namesString.Split(',');
            List<string> names = new List<string>();
            
            //disallow duplicates in name list
            foreach (string name in rawNames)
            {
                if (names.Contains(name))
                {
                    throw new ArgumentException(string.Format(errorPrefix+" line {0}: tag {1} defines duplicate name {2}", lineNum, tag, name));
                }
                names.Add(name);
            }
            
            nameToAttrib[tag] = names.ToArray();
        }
        
        return new AnonAttributeConfig(nameToAttrib);
    }
    
    private AnonAttributeConfig(Dictionary<string, string[]> myMap)
    {
        NameToAttributes = myMap;
    }
    
    public string[] GetAnonAttributes(string name)
    {
        string[] retVal;
        return NameToAttributes.TryGetValue(name, out retVal) ? retVal : null;
    }
}