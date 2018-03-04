using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyScript
{
    static class Program
    {
        class ProcessOptionException : Exception
        {
            public ProcessOptionException(string message) : base(message)
            {
            }

            public ProcessOptionException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }

        class OptionProcessor
        {
            public delegate void ProcessMethod(Queue<string> argQueue);

            public readonly string Name;
            public readonly ProcessMethod Process;
            public OptionProcessor(string name, ProcessMethod process)
            {
                Name = name;
                Process = process;
            }
        }
        static void ProcessArgs(Queue<string> argQueue, IEnumerable<OptionProcessor> processors)
        {
            bool hasMoreOptions = true;
            do
            {
                if (argQueue.Count == 0)
                {
                    break;
                }
                string nextArg = argQueue.Peek();
                if (!string.IsNullOrEmpty(nextArg) && nextArg[0] == '-')
                {
                    string optName = argQueue.Dequeue().Substring(1);
                    OptionProcessor processor = processors.FirstOrDefault(p => p.Name == optName);
                    if (processor == null)
                    {
                        throw new ProcessOptionException("Unrecognised option " + optName);
                    }

                    try
                    {
                        processor.Process(argQueue);
                    }
                    catch (InvalidOperationException e)
                    {
                        throw new ProcessOptionException("Not enough arguments for option " + optName, e);
                    }
                    catch (Exception e)
                    {
                        throw new ProcessOptionException("Failed to process option " + optName + ": " + e.Message, e);
                    }
                }
                else
                {
                    hasMoreOptions = false;
                }
            } while (hasMoreOptions);
        }

        enum ProgramMode { Unspecified, Compile, Decompile };
        static bool helpMode = false;
        static ProgramMode mode;
        static string[] inFilePaths = null;
        static string outFilePath = null;
        static string attrConfigPath = null;
        static string nameConfigPath = null;

        static void Run(string[] args)
        {
            OptionProcessor[] processors = new OptionProcessor[]
            {
                new OptionProcessor("nameconfig", delegate(Queue<string> optArgs)
                {
                    string nameConfigPath = optArgs.Dequeue();
                }),
                new OptionProcessor("anonattrconfig", delegate(Queue<string> optArgs)
                {
                    string attrConfigPath = optArgs.Dequeue();
                }),
                new OptionProcessor("o", delegate(Queue<string> optArgs)
                {
                    outFilePath = optArgs.Dequeue();
                }),
                new OptionProcessor("comp", delegate(Queue<string> optArgs)
                {
                    if (mode != ProgramMode.Unspecified)
                        throw new ArgumentException("Can't specify both compile and decompile");
                    mode = ProgramMode.Compile;
                }),
                new OptionProcessor("decomp", delegate(Queue<string> optArgs)
                {
                    if (mode != ProgramMode.Unspecified)
                        throw new ArgumentException("Can't specify both compile and decompile");
                    mode = ProgramMode.Decompile;
                }),
                new OptionProcessor("h", delegate(Queue<string> optArgs)
                {
                    helpMode = true;
                })
            };

            Queue<string> argQueue = new Queue<string>(args);
            Console.Error.WriteLine("I have a queue of size " + argQueue.Count);
            //process command line arguments to set up controls
            try
            {
                ProcessArgs(argQueue, processors);
            }
            catch (ProcessOptionException e)
            {
                Console.Error.WriteLine(e.Message);
                Environment.ExitCode = 1;
                return;
            }
            mode = mode == ProgramMode.Unspecified ? ProgramMode.Compile : mode;

            if (helpMode)
            {
                Console.Error.WriteLine("Help text goes here");
                return;
            }

            //try to get input paths
            inFilePaths = argQueue.ToArray();
            if (inFilePaths.Length == 0)
            {
                Console.Error.WriteLine("At least one input file path must be specified");
                Environment.ExitCode = 1;
                return;
            }
            bool multipleInputFiles = inFilePaths.Length > 1;

            //load in config files
            AnonAttributeConfig attrConfig = null;
            NameShortcutConfig nameConfig = null;
            try
            {
                attrConfigPath = attrConfigPath ?? "anonAttributes.txt";
                nameConfigPath = nameConfigPath ?? "tagNameShortcuts.txt";
                Utils.ReadConfigs(nameConfigPath, attrConfigPath, out nameConfig, out attrConfig);
            }
            catch (ArgumentException e)
            {
                Console.Error.WriteLine(e.Message);
                Environment.ExitCode = 2;
                return;
            }

            //think about the output path based on the specified input path[s]
            if (multipleInputFiles)
            {
                //if more than one input file is specified, the output path must be a directory
                if (outFilePath == null)
                {
                    outFilePath = ".";
                }
                else
                {
                    if (!Directory.Exists(outFilePath))
                    {
                        Console.Error.WriteLine(outFilePath + " does not refer to a valid output directory");
                        Environment.ExitCode = 3;
                        return;
                    }
                }
            }
            else
            {
                //if one input file is specified, the output path can be a directory with or without a filename
                if (outFilePath == null)
                {
                    outFilePath = mode == ProgramMode.Compile ? "out.xml" : "out.txt";
                }
                else
                {
                    if (Directory.Exists(outFilePath))
                    {
                        outFilePath = Path.Combine(outFilePath, mode == ProgramMode.Compile ? "out.xml" : "out.txt");
                    }
                }
            }

            //go through and [de]compile each file
            bool totalSuccess = true;
            foreach (string path in inFilePaths)
            {
                string curOutPath;
                if (multipleInputFiles)
                {
                    string extension = mode == ProgramMode.Compile ? ".xml" : ".txt";
                    string outputFileName = Path.GetFileNameWithoutExtension(path) + extension;
                    curOutPath = Path.Combine(outFilePath, outputFileName);
                }
                else
                {
                    curOutPath = outFilePath;
                }

                FileStream inputStream = null;
                try
                {
                    inputStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(string.Format("Failed to open {0} for reading: {1}", path, e.Message));
                    continue;
                }

                FileStream outputStream = null;
                try
                {
                    outputStream = new FileStream(curOutPath, FileMode.Create, FileAccess.Write);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(string.Format("Failed to open {0} for writing: {1}", curOutPath, e.Message));
                }

                Console.Error.WriteLine(string.Format("Converting {0} to make {1}", path, curOutPath));

                bool success;
                if (mode == ProgramMode.Compile)
                {
                    success = BabyCompile.Convert(path, inputStream, outputStream, nameConfig, attrConfig);
                }
                else
                {
                    success = BabyDecompile.Convert(path, inputStream, outputStream, nameConfig, attrConfig);
                }
                inputStream.Close();

                if (!success)
                {
                    totalSuccess = false;
                    continue;
                }

                //TODO: ask for confirmation when the output file already exists
                try
                {
                    outputStream.Flush();
                    outputStream.Close();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(string.Format("Failed to close output file {0}: {1}", curOutPath, e.Message));
                }
            }

            Environment.ExitCode = totalSuccess ? 0 : 4;
        }

        static void Main(string[] args)
        {
            Run(args);
            Console.Error.WriteLine("Exit code is " + Environment.ExitCode);
            Console.ReadKey();
        }
    }
}
