﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NubiloSoft.CoverageExt.Data;
using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;

namespace NubiloSoft.CoverageExt.Native
{
    public class NativeData : ICoverageData
    {
        private Dictionary<string, Tuple<BitVector, ProfileVector>> lookup = new Dictionary<string, Tuple<BitVector, ProfileVector>>();

        public Tuple<BitVector, ProfileVector> GetData(string filename)
        {
            filename = filename.Replace('/', '\\').ToLower();

            Tuple<BitVector, ProfileVector> result = null;
            lookup.TryGetValue(filename, out result);
            return result;
        }

        public DateTime FileDate { get; set; }

        public IEnumerable<Tuple<string, int, int>> Overview()
        {
            foreach (var kv in lookup)
            {
                int covered = 0;
                int uncovered = 0;
                foreach (var item in kv.Value.Item1.Enumerate())
                {
                    if (item.Value)
                    {
                        ++covered;
                    }
                    else
                    {
                        ++uncovered;
                    }
                }

                yield return new Tuple<string, int, int>(kv.Key, covered, uncovered);
            }
        }

        public NativeData(string filename, DTE dte, OutputWindow output)
        {
            // Get file date (for modified checks)
            FileDate = new System.IO.FileInfo(filename).LastWriteTimeUtc;

            // Read file:
            using (var sr = new System.IO.StreamReader(filename))
            {
                string name = sr.ReadLine();
                while (name != null)
                {
                    if (name.StartsWith("FILE: "))
                    {
                        string currentFile = name.Substring("FILE: ".Length);
                        // If relative, we add solutionDir to relative directory.
                        if (!System.IO.Path.IsPathRooted(currentFile))
                        {
                            // Here: we can add a list of ordered registered folders and try to find file inside.
                            string solutionDir = System.IO.Path.GetDirectoryName(dte.Solution.FullName);
                            currentFile = System.IO.Path.Combine(solutionDir, currentFile);

                            if(!System.IO.File.Exists(currentFile))
                                output.WriteLine("Impossible to find into solution: {0}", currentFile);
                        }

                        string cov = sr.ReadLine();

                        if (cov.StartsWith("RES: "))
                        {
                            cov = cov.Substring("RES: ".Length);

                            BitVector currentVector = new Data.BitVector();

                            for (int i = 0; i < cov.Length; ++i)
                            {
                                char c = cov[i];
                                if (c == 'c' || c == 'p')
                                {
                                    currentVector.Set(i + 1, true);
                                }
                                else if (c == 'u')
                                {
                                    currentVector.Set(i + 1, false);
                                }
                                else
                                {
                                    currentVector.Ensure(i + 1);
                                }
                            }

                            ProfileVector currentProfile = new Data.ProfileVector(currentVector.Count);

                            string prof = sr.ReadLine();
                            if (prof.StartsWith("PROF: "))
                            {
                                prof = prof.Substring("PROF: ".Length);
                                int line = 0;

                                for (int i = 0; i < prof.Length;)
                                {
                                    int deep = 0;
                                    while (i < prof.Length && prof[i] != ',')
                                    {
                                        char c = prof[i];
                                        deep = deep * 10 + (c - '0');
                                        ++i;
                                    }
                                    ++i;

                                    int shallow = 0;
                                    while (i < prof.Length && prof[i] != ',')
                                    {
                                        char c = prof[i];
                                        shallow = shallow * 10 + (c - '0');
                                        ++i;
                                    }
                                    ++i;

                                    currentProfile.Set(line, deep, shallow);
                                    ++line;
                                }
                            }
                            else
                            {
                                name = prof;
                                continue;
                            }

                            try
                            {
                                lookup.Add(currentFile.ToLower(), new Tuple<BitVector, ProfileVector>(currentVector, currentProfile));
                            }
                            catch (Exception e)
                            {
                                output.WriteLine("Error loading coverage report: {0} with key {1}", e.Message, currentFile.ToLower());
                            }
                        }
                    }
                    // otherwise: ignore; grab next line

                    name = sr.ReadLine();
                }
            }
        }
    }
}
