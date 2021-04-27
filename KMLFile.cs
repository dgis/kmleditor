//    KMLEditor - A small KML files editor to edit the skins for the emulators like Emu48.
//    Copyright (C) 2021 Regis COSNIER
//
//    This program is free software; you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation; either version 2 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program; if not, write to the Free Software
//    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.Collections.Generic;
using System.IO;

namespace KMLEditor
{
    public class KMLFile
    {
        private string filename;
        private bool isRootKMLFile = false;
        private List<string> lines = new List<string>();
        //string sourceCode = "";
        public bool isDirty;

        public KMLFile(bool isRootKMLFile)
        {
            this.isRootKMLFile = isRootKMLFile;
        }

        public string GetFilename()
        {
            return filename;
        }
        public string GetFilenameOnly()
        {
            return Path.GetFileName(filename);
        }
        public IList<string> GetLines()
        {
            return lines;
        }
        public string GetSourceCode()
        {
            return string.Join("\r\n", lines);// sourceCode;
        }

        public bool ReadFile(string filename)
        {
            this.filename = filename;

            //var sourceCodeText = new StringBuilder();
            string line;
            try
            {
                StreamReader sr = new StreamReader(filename);
                line = sr.ReadLine();
                while (line != null)
                {
                    lines.Add(line);
                    //sourceCodeText.AppendLine(line);
                    line = sr.ReadLine();
                }
                sr.Close();
                //sourceCode = sourceCodeText.ToString();
                return true;
            }
            catch (Exception /* ex */)
            {
                //Console.WriteLine("Exception: " + e.Message);
            }
            finally
            {
                //Console.WriteLine("Executing finally block.");
            }
            return false;
        }

        public bool WriteFile(string saveWithFilename)
        {
            string filename = saveWithFilename != null ? saveWithFilename : this.filename;

            try
            {
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    foreach (var line in lines)
                    {
                        sw.WriteLine(line);
                    }
                }
                isDirty = false;
                return true;
            }
            catch (Exception /* ex */)
            {
                //Console.WriteLine("Exception: " + e.Message);
            }
            finally
            {
                //Console.WriteLine("Executing finally block.");
            }
            return false;
        }
    }
}
