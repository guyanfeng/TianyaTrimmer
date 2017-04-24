using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace TianyaTrimmer
{
    public class LineFormatter
    {
        public string Format(string content)
        {
            /*var phaseStart = false;
            var newLine = false;
            var builder = new StringBuilder();
            foreach (var c in content)
            {
                if (!phaseStart)
                {
                    if (char.IsWhiteSpace(c))
                        continue;
                    phaseStart = true;
                }
                else
                {
                    builder.Append(c);
                    
                }
            }*/
            content = Regex.Replace(content, "^\\s+", "");
            content = Regex.Replace(content, Environment.NewLine + "\\s+", Environment.NewLine);
            content = Regex.Replace(content, "(" + Environment.NewLine + "){1,}", Environment.NewLine + Environment.NewLine);
            return content;
        }

        /*public string NextCharIsNewLine(string content, int index)
        {
            
        }*/
    }
}
