using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace BabyScript
{
    public class BabyCompile
    {
        public class XmlWritingListener : BabyScriptBaseListener
        {
            private NameShortcutConfig NameShortcuts;
            private AnonAttributeConfig ImpliedAttributes;
            private XmlWriter Writer;
            private BabyScriptParser.ElementContext CurrentElement;
            public bool Error { get; private set; }
            private readonly string fileName;

            Queue<string> AvailableNames = new Queue<string>();
            bool HasImpliedAttributes;

            public XmlWritingListener(XmlWriter w, string fname, NameShortcutConfig names, AnonAttributeConfig attributes)
            {
                Writer = w;
                NameShortcuts = names;
                ImpliedAttributes = attributes;
                Error = false;
                fileName = fname;
            }

            private void SetCurrentElement(BabyScriptParser.ElementContext ctx, string realName)
            {
                string[] anonAttributes = ImpliedAttributes.GetAnonAttributes(realName);
                HasImpliedAttributes = anonAttributes != null;

                AvailableNames.Clear();
                if (HasImpliedAttributes)
                {
                    foreach (string name in anonAttributes)
                    {
                        AvailableNames.Enqueue(name);
                    }
                }

                CurrentElement = ctx;
            }

            public override void EnterElement(BabyScriptParser.ElementContext ctx)
            {
                string realName = ctx.ele.Name;
                string fullName = NameShortcuts.ToFull(realName);
                if (fullName != null)
                {
                    realName = fullName;
                }

                if (Error) return;

                Writer.WriteStartElement(realName);
                SetCurrentElement(ctx, realName);

                if (ctx.ele.IsAssignment)
                {
                    foreach (BabyAttribute a in ctx.ele.Attributes)
                    {
                        Writer.WriteAttributeString(a.Name, a.Value);
                    }
                }
            }

            public override void ExitElement(BabyScriptParser.ElementContext ctx)
            {
                if (Error) return;
                Writer.WriteEndElement();
            }

            public override void EnterAttribute(BabyScriptParser.AttributeContext ctx)
            {
                string name = ctx.attr.Name;
                if (name == null)
                {
                    if (!HasImpliedAttributes)
                    {
                        Console.Error.WriteLine(
                            string.Format(Utils.FileLineColMessageFormat,
                                fileName,
                                CurrentElement.Start.Line,
                                CurrentElement.Start.Column,
                                CurrentElement.ele.Name + "has an anonymous attribute but the config has no rule for it"
                             )
                        );
                        Error = true;
                    }
                    else if (AvailableNames.Count == 0)
                    {
                        Console.Error.WriteLine(
                            string.Format(Utils.FileLineColMessageFormat,
                                fileName,
                                CurrentElement.Start.Line,
                                CurrentElement.Start.Column,
                                CurrentElement.ele.Name + " has more anonymous attributes than the config specifies for it"
                            )
                        );
                        Error = true;
                    }
                    else
                    {
                        name = AvailableNames.Dequeue();
                    }
                }

                if (Error) return;

                string realValue = ctx.attr.Value;
                //if the attribute had to packed in double quotes, time to unpack it again
                if (realValue.Length > 1 && realValue[0] == '"')
                {
                    realValue = realValue.Substring(1, realValue.Length - 2);
                }

                Writer.WriteAttributeString(name, realValue);
            }
        }

        public static bool Convert(string fileName, Stream inputStream, Stream outputStream, NameShortcutConfig nameShortcuts, AnonAttributeConfig anonAttrConfig)
        {
            StreamWriter output = new StreamWriter(outputStream);
            BabyScriptLexer lex = new BabyScriptLexer(new AntlrInputStream(inputStream));
            CommonTokenStream stream = new CommonTokenStream(lex);
            BabyScriptParser parser = new BabyScriptParser(stream);

            BabyScriptParser.DocumentContext doc = parser.document();

            if (parser.NumberOfSyntaxErrors > 0)
            {
                Console.Error.WriteLine(string.Format(Utils.FileMessageFormat, fileName, "Aborting write due to syntax error(s)"));
                return false;
            }
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    "
            };
            XmlWriter writer = XmlWriter.Create(output, settings);
            XmlWritingListener listener = new XmlWritingListener(writer, fileName, nameShortcuts, anonAttrConfig);

            new ParseTreeWalker().Walk(listener, doc);

            if (listener.Error)
            {
                Console.Error.WriteLine(string.Format(Utils.FileMessageFormat, fileName, "Aborting write due to semantic error(s)"));
                return false;
            }
            return true;
        }
    }
}