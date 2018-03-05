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
        private int indentLevel;
        private string curElementComment;
        private readonly AnonAttributeConfig attrConfig;
        private readonly NameShortcutConfig nameConfig;
        private readonly TextWriter writer;
        private readonly XmlReader reader;
        private static readonly int tabWidth = 4;

        public BabyDecompile(string path, Stream inputStream, Stream outputStream, NameShortcutConfig names, AnonAttributeConfig attrs)
        {
            indentLevel = 0;
            curElementComment = null;
            nameConfig = names;
            attrConfig = attrs;
            reader = XmlReader.Create(inputStream);
            writer = new StreamWriter(outputStream);
        }

        private bool TryAssignmentShortcut()
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
                    curElementComment = reader.Value;
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
            writer.Write(";");
            return true;
        }

        private void WriteAttributes()
        {
            if (!reader.HasAttributes)
            {
                return;
            }

            string[] impliedNames = attrConfig.GetAnonAttributes(reader.Name);

            List<BabyAttribute> allAttributes = new List<BabyAttribute>();
            List<BabyAttribute> namedAttributes = new List<BabyAttribute>();
            List<BabyAttribute> anonAttributes = new List<BabyAttribute>();

            //just get a list of all the attributes, in our own data structure
            while (reader.MoveToNextAttribute())
            {
                if (reader.Name == "comment")
                {
                    curElementComment = reader.Value;
                    continue;
                }
                BabyAttribute newAttribute = new BabyAttribute(reader.Name.Replace(":", ""), reader.Value);
                allAttributes.Add(newAttribute);
            }

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
                if (!attribute.IsAnonymous)
                {
                    namedAttributes.Add(attribute);
                }
            }

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
                    Console.Error.WriteLine("Line {0}: \"{1}\" isn't a valid expression and will be wrapped in doublequotes", ((IXmlLineInfo)reader).LineNumber, attribute.Value);
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
        }

        private void WriteIndent()
        {
            writer.Write(new string(' ', tabWidth * indentLevel));
        }

        private void WriteComment(string comment)
        {
            if (comment.IndexOf('\n') != -1)
            {
                writer.Write("/*");
                writer.Write(comment);
                writer.Write("*/");
            }
            else
            {
                writer.Write("//" + NewlineRegex.Replace(comment, " "));
            }
        }

        public bool Convert()
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {

                    string shortName = nameConfig.ToShort(reader.Name);
                    WriteIndent();

                    //write a shorthand assign statement if possible
                    bool assignWritten = TryAssignmentShortcut();

                    //otherwise, write a normal element
                    if (!assignWritten)
                    {
                        writer.Write(shortName ?? reader.Name);

                        if (reader.HasAttributes)
                        {
                            int attrCount = reader.AttributeCount;
                            writer.Write("(");
                            WriteAttributes();
                            writer.Write(")");
                        }

                        if (reader.IsEmptyElement)
                        {
                            writer.Write(";");
                        }
                    }

                    //either way write any comment as necessary
                    if (curElementComment != null)
                    {
                        writer.Write(' ');
                        WriteComment(curElementComment);
                        curElementComment = null;
                    }

                    writer.WriteLine();

                    if (!assignWritten && !reader.IsEmptyElement)
                    {
                        WriteIndent();
                        writer.WriteLine("{");
                        indentLevel++;
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement)
                {
                    indentLevel--;
                    WriteIndent();
                    writer.WriteLine("}");
                }
                else if (reader.NodeType == XmlNodeType.Comment)
                {
                    WriteIndent();
                    WriteComment(reader.Value);
                    writer.WriteLine();
                }
                else if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.Whitespace)
                {
                    int numNewlines = reader.Value.Count(c => c == '\n');
                    for (int i=0; i<numNewlines-1; i++)
                    {
                        writer.WriteLine();
                    }
                }
            }

            writer.Flush();
            return true;
        }
    }
}