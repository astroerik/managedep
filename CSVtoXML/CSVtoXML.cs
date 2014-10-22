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
                WebRequest request = WebRequest.Create("https://gs-aetdfile.ndc.nasa.gov/aetd/sudoers.csv");
                ((HttpWebRequest)request).UserAgent = String.Format("AETD Sudowin ({0})", typeof(CSVtoXML).Assembly.GetName().Version);
                ((HttpWebRequest)request).Timeout = 4500;
                ((HttpWebRequest)request).ReadWriteTimeout = 4500;

                using (var response = request.GetResponse())
                {
                    using (var tempFile = new TempFileCollection())
                    {
                        string file = tempFile.AddExtension("csv");
                        Stream responseStream = response.GetResponseStream();
                        using (FileStream fileStream = File.Create(file))
                        {
                            responseStream.CopyTo(fileStream);
                        }

                        CsvFileDescription sudoersCSV = new CsvFileDescription
                        {
                            SeparatorChar = ',',
                            FirstLineHasColumnNames = true
                        };

                        CsvContext csvContext = new CsvContext();
                        IEnumerable<Sudoer> sudoers = csvContext.Read<Sudoer>(file, sudoersCSV);
                        sudoers = (from s in sudoers where s.System.ToUpper() == System.Environment.MachineName.ToUpper() select s).OrderBy(s => s.Username);
                    
                        XmlDocument xmlDoc = new XmlDocument();
                        XmlSchema xmlSchema = new XmlSchema();
                        xmlSchema.Namespaces.Add("xmlns", "http://sudowin.sourceforge.net/schemas/XmlAuthorizationPlugin/");
                        xmlDoc.Schemas.Add(xmlSchema);

                        XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
                        XmlElement root = xmlDoc.DocumentElement;

                        xmlDoc.InsertBefore(xmlDeclaration, root);

                        XmlElement sudoersXml = xmlDoc.CreateElement(string.Empty, "sudoers", string.Empty);
                        sudoersXml.SetAttribute("privilegesGroup", "Administrators");
                        sudoersXml.SetAttribute("loggingLevel", "Both");
                        sudoersXml.SetAttribute("allowAllCommands", "false");
                        sudoersXml.SetAttribute("startTime", "00:00:00.00000");
                        sudoersXml.SetAttribute("endTime", "23:59:59.99999");
                        xmlDoc.AppendChild(sudoersXml);

                        XmlElement usersXml = xmlDoc.CreateElement(string.Empty, "users", string.Empty);
                        sudoersXml.AppendChild(usersXml);

                        XmlElement usersGroupXml = xmlDoc.CreateElement(string.Empty, "userGroup", string.Empty);
                        usersGroupXml.SetAttribute("name", "sudoers");
                        usersXml.AppendChild(usersGroupXml);

                        XmlElement usersGroupUsersXml = xmlDoc.CreateElement(string.Empty, "users", string.Empty);
                        usersGroupXml.AppendChild(usersGroupUsersXml);

                        string lastUsername = "";
                        XmlElement userXml = null;
                        XmlElement commandsXml = null;
                        foreach (Sudoer sudoer in sudoers)
                        {
                            if (sudoer.Username != lastUsername)
                            {
                                lastUsername = sudoer.Username;
                                userXml = xmlDoc.CreateElement(string.Empty, "user", string.Empty);
                                usersGroupUsersXml.AppendChild(userXml);
                                userXml.SetAttribute("name", sudoer.Username);
                                if (sudoer.Path == "*")
                                {
                                    userXml.SetAttribute("allowAllCommands", "true");
                                    commandsXml = null;
                                    continue;
                                }
                                else
                                {
                                    userXml.SetAttribute("allowAllCommands", "false");
                                    commandsXml = xmlDoc.CreateElement(string.Empty, "commands", string.Empty);
                                    userXml.AppendChild(commandsXml);
                                }
                            }
                            
                            //we have a userXml now and commandsXml so we're appending things
                            XmlElement commandXml = xmlDoc.CreateElement(string.Empty, "command", string.Empty);
                            commandXml.SetAttribute("path", sudoer.Path);

                            if (sudoer.Checksum != null)
                            {
                                commandXml.SetAttribute("md5Checksum", sudoer.Checksum);
                            }

                            if (sudoer.Arguments != null)
                            {
                                commandXml.SetAttribute("argumentString", sudoer.Arguments);
                            }

                            commandsXml.AppendChild(commandXml);
                        }

                        using (var stringWriter = new StringWriter())
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
