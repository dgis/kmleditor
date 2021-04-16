using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
