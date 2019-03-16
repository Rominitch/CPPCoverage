using NubiloSoft.CoverageExt.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NubiloSoft.CoverageExt.Cobertura
{
    /// <summary>
    /// This class post-processes the coverage results. We basically scan each file for "#pragma EnableCodeCoverage" 
    /// and "#pragma DisableCodeCoverage" and if we encounter this, we'll update the bit vector.
    /// WARNING: If you use high level of warning (level 4), you need to disable the following code 4068 or the "#pragma warning (disable:4068)".
    ///
    /// To avoid it, this system works using following SINGLE comment too: "// EnableCodeCoverage" and "// DisableCodeCoverage".
    /// </summary>
    public class HandlePragmas
    {
        private const string pragma  = "#pragma ";
        private const string comment = "//";

        public static void Update(string filename, BitVector data)
        {
            bool enabled = true;

            try
            {
                string[] lines = File.ReadAllLines(filename);
                for (int i = 0; i < lines.Length; ++i)
                {
                    string line = lines[i];
                    int idx = line.IndexOf(pragma);
                    if (idx >= 0)
                    {
                        idx += pragma.Length;
                        string t = line.Substring(idx).TrimStart();
                        if (t.StartsWith("EnableCodeCoverage"))
                        {
                            data.Remove(i + 1);
                            enabled = true;
                        }
                        else if (t.StartsWith("DisableCodeCoverage"))
                        {
                            enabled = false;
                        }
                    }
                    else // Support comment
                    {
                        idx = line.IndexOf(comment);
                        if (idx >= 0)
                        {
                            idx += comment.Length;
                            string t = line.Substring(idx).TrimStart();
                            if (t.StartsWith("EnableCodeCoverage"))
                            {
                                data.Remove(i + 1);
                                enabled = true;
                            }
                            else if (t.StartsWith("DisableCodeCoverage"))
                            {
                                enabled = false;
                            }
                        }
                    }

                    // Update data accordingly:
                    if (!enabled)
                    {
                        data.Remove(i + 1);
                    }
                }

                if (!enabled)
                {
                    var count = data.Count;
                    for (int i = lines.Length; i < count; ++i)
                    {
                        data.Remove(i + 1);
                    }
                }
            }
            catch { } // we don't want this to crash our program.
        }
    }
}
