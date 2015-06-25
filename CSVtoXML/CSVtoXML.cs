using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.CodeDom.Compiler;
using Microsoft.Win32;

using LINQtoCSV;

namespace Sudowin.Common
{
    public class CSVtoXML
    {
        public string GetXML()
        {
            string xml = "";

            try
            {
                string sudoersLocation = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\SudoWin\\", "Sudoers", "https://gs-aetdfile.ndc.nasa.gov/aetd/sudoers.xml");

                WebRequest request = WebRequest.Create(sudoersLocation);
                ((HttpWebRequest)request).UserAgent = String.Format("AETD Sudowin ({0})", typeof(CSVtoXML).Assembly.GetName().Version);
                ((HttpWebRequest)request).Timeout = 4500;
                ((HttpWebRequest)request).ReadWriteTimeout = 4500;

                using (var response = request.GetResponse())
                {
                    using (var tempFile = new TempFileCollection())
                    {
                        string file = tempFile.AddExtension("xml");
                        Stream responseStream = response.GetResponseStream();
                        using (FileStream fileStream = File.Create(file))
                        {
                            responseStream.CopyTo(fileStream);
                        }

                        file

                        var stringWriter = new StringWriter();
                        using (var xmlTextWriter = XmlWriter.Create(stringWriter))
                        {
                            xmlDoc.WriteTo(xmlTextWriter);
                            xmlTextWriter.Flush();
                            xml = stringWriter.GetStringBuilder().ToString();
                        }
                    }
                }
            }
            catch (Exception)
            {
                xml = "";
            }

            return xml;
        }
    }

    public class Sudoer
    {
        [CsvColumn(Name = "system", FieldIndex = 1, CanBeNull = false)]
        public string System { get; set; }

        [CsvColumn(Name="username", FieldIndex = 2, CanBeNull = false)]
        public string Username { get; set; }

        [CsvColumn(Name="path", FieldIndex = 3, CanBeNull = true)]
        public string Path { get; set; }

        [CsvColumn(Name="checksum", FieldIndex = 4, CanBeNull=true)]
        public string Checksum { get; set; }

        [CsvColumn(Name="arguments", FieldIndex = 5, CanBeNull=true)]
        public string Arguments { get; set; }
    }
}
