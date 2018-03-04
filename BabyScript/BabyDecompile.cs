using System;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System.IO;

namespace BabyScript
{
    public class BabyDecompile
    {
        //to break up multiline XML comments
        private static readonly Regex NewlineRegex = new Regex("\r\n|\r|\n");

        private static bool breakpointEncountered = false;
        private static string currentComment = null;

        private static bool TryAssignmentShortcut(XmlReader reader, TextWriter writer)
        {
            if (reader.Name != "set_value")
            {
                return false;
            }
            if (!reader.IsEmptyElement)
            {
                return false;
            }

            string varExact = null;
            string varName = null;

            string comment = null;

            while (reader.MoveToNextAttribute())
            {
                if (reader.Name == "name")
                {
                    varName = reader.Value;
                }
                else if (reader.Name == "exact")
                {
                    varExact = reader.Value;
                }
                else if (reader.Name == "comment")
                {
                    comment = reader.Value;
                }
                else
                {
                    reader.MoveToElement();
                    return false;
                }
            }

            if (varExact == null || varName == null)
            {
                reader.MoveToElement();
                return false;
            }

            writer.Write(varName);
            writer.Write(" = ");
            writer.Write(varExact);
            writer.WriteLine(";");

            if (comment != null)
            {
                WriteComment(comment, writer);
            }
            return true;
        }

        private static void WriteAttributes(XmlReader reader, TextWriter writer, AnonAttributeConfig anonAttrConfig)
        {
            if (!reader.HasAttributes)
            {
                return;
            }

            if (breakpointEncountered) Console.Error.WriteLine("Debug enabled for {0}", reader.Name);

            string[] impliedNames = anonAttrConfig.GetAnonAttributes(reader.Name);

            List<BabyAttribute> allAttributes = new List<BabyAttribute>();
            List<BabyAttribute> namedAttributes = new List<BabyAttribute>();
            List<BabyAttribute> anonAttributes = new List<BabyAttribute>();

            //just get a list of all the attributes, in our own data structure
            while (reader.MoveToNextAttribute())
            {
                if (reader.Name == "comment")
                {
                    currentComment = reader.Value;
                    continue;
                }
                BabyAttribute newAttribute = new BabyAttribute(reader.Name.Replace(":", ""), reader.Value);
                allAttributes.Add(newAttribute);
            }

            if (breakpointEncountered) Console.Error.WriteLine("Has implied name config: {0}", impliedNames != null);

            //if there are some implied attributes, try to match them in order with what we get until we can't
            //since the order matters, we can't match the second implied one without having the first
            if (impliedNames != null)
            {
                foreach (string name in impliedNames)
                {
                    bool found = false;
                    foreach (BabyAttribute attrib in allAttributes)
                    {
                        if (attrib.Name == name)
                        {
                            attrib.IsAnonymous = true;
                            anonAttributes.Add(attrib);
                            found = true;
                        }
                    }
                    if (!found)
                    {
                        break;
                    }
                }
            }

            //and now, a named attribute is just an attribute we didn't find to be anonymous
            foreach (BabyAttribute attribute in allAttributes)
            {
                //if (!anonAttributes.Contains(attribute))
                if (!attribute.IsAnonymous)
                {
                    namedAttributes.Add(attribute);
                }
            }

            if (breakpointEncountered) Console.Error.WriteLine("Anon attributes: {0}", anonAttributes.Count);
            if (breakpointEncountered) Console.Error.WriteLine("Named attributes: {0}", namedAttributes.Count);
            if (breakpointEncountered) Console.Error.WriteLine("Total attributes: {0}", allAttributes.Count);

            //things to write is all the nameless ones followed by the named ones
            IEnumerable<BabyAttribute> thingsToWrite = anonAttributes.Concat<BabyAttribute>(namedAttributes);

            int attrNumber = 0;
            foreach (BabyAttribute attribute in thingsToWrite)
            {
                if (!attribute.IsAnonymous)
                {
                    writer.Write(attribute.Name);
                    writer.Write(":");
                }

                BabyScriptLexer lexer = new BabyScriptLexer(new AntlrInputStream(attribute.Value));
                CommonTokenStream tokens = new CommonTokenStream(lexer);
                BabyScriptParser parser = new BabyScriptParser(tokens);
                parser.exprEof();
                if (parser.NumberOfSyntaxErrors > 0)
                {
                    Console.Error.WriteLine("Line {0}: {1} isn't a valid expression, got to wrap it", ((IXmlLineInfo)reader).LineNumber, attribute.Value);
                    writer.Write("\"" + attribute.Value + "\"");
                }
                else
                {
                    writer.Write(attribute.Value);
                }

                if (allAttributes.Count > 1 && attrNumber < allAttributes.Count - 1)
                {
                    writer.Write(", ");
                }
                attrNumber++;
            }

            reader.MoveToElement();

            breakpointEncountered = false;
        }

        private static void WriteComment(string comment, TextWriter writer)
        {
            writer.Write(" //" + NewlineRegex.Replace(comment, " "));
        }

        public static bool Convert(string fileName, Stream inputStream, Stream outputStream, NameShortcutConfig nameShortcuts, AnonAttributeConfig anonAttrConfig)
        {
            XmlReader reader = XmlReader.Create(inputStream);
            StreamWriter writer = new StreamWriter(outputStream);

            int tabWidth = 4;

            int indentLevel = 0;
            while (reader.Read())
            {
                string indent = new string(' ', tabWidth * indentLevel);
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "breakpoint")
                    {
                        breakpointEncountered = true;
                    }

                    string shortName = nameShortcuts.ToShort(reader.Name);
                    writer.Write(indent);

                    if (TryAssignmentShortcut(reader, writer))
                    {
                        continue;
                    }

                    writer.Write(shortName != null ? shortName : reader.Name);

                    if (reader.HasAttributes)
                    {
                        int attrCount = reader.AttributeCount;
                        writer.Write("(");
                        WriteAttributes(reader, writer, anonAttrConfig);
                        writer.Write(")");
                    }

                    if (reader.IsEmptyElement)
                    {
                        writer.Write(";");
                    }
                    if (currentComment != null)
                    {
                        WriteComment(currentComment, writer);
                        currentComment = null;
                    }

                    writer.WriteLine();

                    if (!reader.IsEmptyElement)
                    {
                        writer.Write(indent);
                        writer.WriteLine("{");
                        indentLevel++;
                    }
                }
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    indentLevel--;
                    indent = new string(' ', tabWidth * indentLevel);
                    writer.Write(indent);
                    writer.WriteLine("}");
                }
                if (reader.NodeType == XmlNodeType.Comment)
                {
                    string[] commentLines = NewlineRegex.Split(reader.Value);
                    foreach (string line in commentLines)
                    {
                        writer.Write(indent);
                        writer.WriteLine("//" + line);
                    }
                }
            }

            writer.Flush();
            return true;
        }
    }
}