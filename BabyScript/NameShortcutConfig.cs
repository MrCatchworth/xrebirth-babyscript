using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;

public class NameShortcutConfig
{
    private readonly Dictionary<string,string> shortToFull;
    private readonly Dictionary<string,string> fullToShort;
    
    private static Regex lineRegex = new Regex("^([A-Za-z][A-Za-z0-9_]*):([A-Za-z][A-Za-z0-9_]*)$");
    
    public static NameShortcutConfig ReadFromFile(string inFile)
    {
        Dictionary<string,string> stf = new Dictionary<string,string>();
        
        int lineNum = 0;
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
                throw new ArgumentException("Malformed line "+line);
            }
            
            string fullName = m.Groups[1].Value;
            string shortName = m.Groups[2].Value;
            
            stf[shortName] = fullName;
        }
        
        return new NameShortcutConfig(stf);
    }
    
    public NameShortcutConfig(Dictionary<string,string> stf)
    {
        Dictionary<string,string> fts = new Dictionary<string,string>();
        
        foreach (KeyValuePair<string,string> kvp in stf)
        {
            fts[kvp.Value] = kvp.Key;
        }
        
        shortToFull = stf;
        fullToShort = fts;
    }
    
    public string ToFull(string shortName)
    {
        string retVal;
        return shortToFull.TryGetValue(shortName, out retVal) ? retVal : null;
    }
    
    public string ToShort(string fullName)
    {
        string retVal;
        return fullToShort.TryGetValue(fullName, out retVal) ? retVal : null;
    }
}