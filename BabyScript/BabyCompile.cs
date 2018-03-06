using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Antlr4.Runtime.Misc;

namespace BabyScript
{
    public class BabyCompile
    {
        public class XmlWritingListener : BabyScriptBaseListener
        {
            private readonly XmlWriter Writer;
            private BabyScriptParser.ElementContext CurrentElement;
            public bool Error { get; private set; }

            private readonly BabyCompile Owner;
            private readonly BabyScriptParser Parser;

            Queue<string> AvailableNames = new Queue<string>();
            bool HasImpliedAttributes;

            public XmlWritingListener(BabyCompile owner, BabyScriptParser parser)
            {
                Owner = owner;
                Parser = parser;
                Writer = XmlWriter.Create(Owner.outputStream, new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  "
                });
                Error = false;
            }

            private void SetCurrentElement(BabyScriptParser.ElementContext ctx, string realName)
            {
                string[] anonAttributes = Owner.attrConfig.GetAnonAttributes(realName);
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

            public override void EnterComment(BabyScriptParser.CommentContext ctx)
            {
                Writer.WriteComment(((BabyComment)ctx.treeNode).Text);
            }

            public override void EnterElement(BabyScriptParser.ElementContext ctx)
            {
                BabyElement element = (BabyElement)ctx.treeNode;
                string realName = element.Name;
                string fullName = Owner.nameConfig.ToFull(realName);
                if (fullName != null)
                {
                    realName = fullName;
                }

                if (Error) return;

                Writer.WriteStartElement(realName);
                SetCurrentElement(ctx, realName);

                if (element.IsAssignment)
                {
                    foreach (BabyAttribute a in element.Attributes)
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
                                Owner.path,
                                CurrentElement.Start.Line,
                                CurrentElement.Start.Column,
                                ((BabyElement)CurrentElement.treeNode).Name + "has an anonymous attribute but the config has no rule for it"
                             )
                        );
                        Error = true;
                    }
                    else if (AvailableNames.Count == 0)
                    {
                        Console.Error.WriteLine(
                            string.Format(Utils.FileLineColMessageFormat,
                                Owner.path,
                                CurrentElement.Start.Line,
                                CurrentElement.Start.Column,
                                ((BabyElement)CurrentElement.treeNode).Name + " has more anonymous attributes than the config specifies for it"
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

            public void Flush()
            {
                Writer.Flush();
            }
        }

        private class MyErrorListener: BaseErrorListener
        {
            public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
            {
                Console.Error.WriteLine("The type of this token is: " + recognizer.Vocabulary.GetSymbolicName(offendingSymbol.Type));
                Console.Error.WriteLine("The text of this token is: " + offendingSymbol.Text);
            }
        }

        private readonly string path;
        private readonly Stream inputStream;
        private readonly Stream outputStream;
        private readonly AnonAttributeConfig attrConfig;
        private readonly NameShortcutConfig nameConfig;

        public BabyCompile(string fileName, Stream input, Stream output, NameShortcutConfig nameShortcuts, AnonAttributeConfig anonAttrConfig)
        {
            path = fileName;
            inputStream = input;
            outputStream = output;
            nameConfig = nameShortcuts;
            attrConfig = anonAttrConfig;
        }

        public bool Convert()
        {
            
            BabyScriptLexer lex = new BabyScriptLexer(new AntlrInputStream(inputStream));
            CommonTokenStream tokenStream = new CommonTokenStream(lex);
            BabyScriptParser parser = new BabyScriptParser(tokenStream);
            parser.AddErrorListener(new MyErrorListener());

            BabyScriptParser.DocumentContext doc = parser.document();

            if (parser.NumberOfSyntaxErrors > 0)
            {
                Console.Error.WriteLine(string.Format(Utils.FileMessageFormat, path, "Aborting write due to syntax error(s)"));
                return false;
            }

            XmlWritingListener listener = new XmlWritingListener(this, parser);
            new ParseTreeWalker().Walk(listener, doc);
            listener.Flush();

            if (listener.Error)
            {
                Console.Error.WriteLine(string.Format(Utils.FileMessageFormat, path, "Aborting write due to semantic error(s)"));
                return false;
            }
            return true;
        }
    }
}