using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nucs.JsonSettings;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace ConcurSyncLib
{
    public class Utils
    {

        static ConcurSyncSettings settings;

        public static string GetLogDir(int hrid)
        {
            //settings = JsonSettings.Load<ConcurSyncSettings>();
            string dir = settings.LogDir;
            int mod = hrid % 100;
            dir += mod.ToString() + "\\" + hrid + "\\";
            return dir;
        }

        public static void LogJson(string hrId, string fileName, string json, bool append)
        {
            settings = JsonSettings.Load<ConcurSyncSettings>();
            StringReader sr = new StringReader(json);
            StringWriter sw = new StringWriter();
            JsonTextReader jr = new JsonTextReader(sr);
            JsonTextWriter jw = new JsonTextWriter(sw);
            string dir = GetLogDir(Convert.ToInt32(Regex.Replace(hrId, "[^0-9]", "")));
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            while (File.Exists(dir + fileName)) {
                string pattern = "\\w*user_\\w*_?\\d*(_v(\\d+)\\.json)";
                //.?\_v(\d+)\..?
                pattern = ".?(_v(\\d+))";
                Match m = Regex.Match(fileName, pattern);
                int rev = Convert.ToInt32(m.Groups[2].Value) + 1;
                fileName = fileName.Replace(m.Groups[1].Value, "_v" + rev.ToString());

            }
            StreamWriter stw = new StreamWriter(dir + fileName, append);
            jw.Formatting = Formatting.Indented;
            jw.WriteToken(jr);
            stw.WriteLine(sw.ToString());
            stw.Close();
        }

        public static void LogJson(string fileName , string json, bool append)
        {
            settings = JsonSettings.Load<ConcurSyncSettings>();
            StringReader sr = new StringReader(json);
            StringWriter sw = new StringWriter();
            JsonTextReader jr = new JsonTextReader(sr);
            JsonTextWriter jw = new JsonTextWriter(sw);
            StreamWriter stw = new StreamWriter(settings.LogDir + fileName, append);
            jw.Formatting = Formatting.Indented;
            jw.WriteToken(jr);
            stw.WriteLine(sw.ToString());
            stw.Close();
        }

        public static void LogText(string hrId, string fileName, string text, bool append)
        {
            string dir = GetLogDir(Convert.ToInt32(hrId));
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            while (File.Exists(dir + fileName))
            {
                string pattern = "\\w*user_\\w*_?\\d*(_v(\\d+)\\.json)";
                //.?\_v(\d+)\..?
                pattern = ".?(_v(\\d+))";
                Match m = Regex.Match(fileName, pattern);
                int rev = Convert.ToInt32(m.Groups[2].Value) + 1;
                fileName = fileName.Replace(m.Groups[1].Value, "_v" + rev.ToString());

            }

            settings = JsonSettings.Load<ConcurSyncSettings>();
            //StringWriter sw = new StringWriter();
            StreamWriter stw = new StreamWriter(dir + fileName, append);
            stw.WriteLine(text);
            stw.Close();

        }


        public static void LogText(string fileName, string text, bool append)
        {
            settings = JsonSettings.Load<ConcurSyncSettings>();
            //StringWriter sw = new StringWriter();
            StreamWriter stw = new StreamWriter(settings.LogDir + fileName, append);
            stw.WriteLine(text);
            stw.Close();

        }


        public static void LogXml(string fileName, string xml, bool append)
        {
            settings = JsonSettings.Load<ConcurSyncSettings>();
            //StringWriter sw = new StringWriter();
            StreamWriter stw = new StreamWriter(settings.LogDir + fileName, append);
            stw.WriteLine(PrintXml(xml));
            stw.Close();
            

        }

        private static string PrintXml(string xml)
        {
            string result = "";

            MemoryStream mStream = new MemoryStream();
            System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(mStream, Encoding.Unicode);
            System.Xml.XmlDocument document = new System.Xml.XmlDocument();

            try
            {
                // Load the XmlDocument with the XML.
                document.LoadXml(xml);

                writer.Formatting = System.Xml.Formatting.Indented;

                // Write the XML into a formatting XmlTextWriter
                document.WriteContentTo(writer);
                writer.Flush();
                mStream.Flush();

                // Have to rewind the MemoryStream in order to read
                // its contents.
                mStream.Position = 0;

                // Read MemoryStream contents into a StreamReader.
                StreamReader sReader = new StreamReader(mStream);

                // Extract the text from the StreamReader.
                string formattedXml = sReader.ReadToEnd();

                result = formattedXml;
            }
            catch (System.Xml.XmlException)
            {
                // Handle the exception
            }

            mStream.Close();
            writer.Close();

            return result;
        }
    }
}
