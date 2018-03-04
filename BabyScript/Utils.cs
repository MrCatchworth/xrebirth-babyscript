using System;
using System.IO;
using System.Collections.Generic;

namespace BabyScript
{
    public class Utils
    {
        public static void ReadConfigs(string nameFile, string attributeFile, out NameShortcutConfig nameConfig, out AnonAttributeConfig attrConfig)
        {
            nameConfig = null;
            attrConfig = null;
            try
            {
                attrConfig = AnonAttributeConfig.ReadFromFile(attributeFile);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException(string.Format("Failed to process anonymous attribute config: {0}", e.Message), e);
            }
            catch (FileNotFoundException e)
            {
                throw new ArgumentException(string.Format("Failed to open anonymous attribute config: {0}", e.Message), e);
            }
            catch (IOException e)
            {
                throw new ArgumentException(string.Format("Failed to read anonymous attribute config: {1}", e.Message), e);
            }

            try
            {
                nameConfig = NameShortcutConfig.ReadFromFile(nameFile);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException(string.Format("Failed to process name shortcut config: {0}", e.Message), e);
            }
            catch (FileNotFoundException e)
            {
                throw new ArgumentException(string.Format("Failed to open name shortcut config: {0}", e.Message), e);
            }
            catch (IOException e)
            {
                throw new ArgumentException(string.Format("Failed to read name shortcut config: {1}", e.Message), e);
            }
        }

        public static readonly string FileMessageFormat = "{0}: {1}";
        public static readonly string FileLineMessageFormat = "{0}:{1}: {2}";
        public static readonly string FileLineColMessageFormat = "{0}:{1}:{2}: {3}";
    }
}