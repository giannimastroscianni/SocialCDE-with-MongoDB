using System;
using System.Collections.Generic;
using System.Web.Security;
using System.Xml.Linq;
using System.Xml;
using System.IO;
using System.Diagnostics;
using log4net;
using log4net.Config;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Linq;

namespace It.Uniba.Di.Cdg.SocialTfs.ProxyServer.AdminPanel
{
    public partial class NewUser : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebUtility.CheckCredentials(this);
            if (Request.RequestType == "POST")
            {
                SaveUsers();
            }
        }

        private void SaveUsers()
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var collection = db.Users();
            XmlDocument requestXml = new XmlDocument();
            requestXml.Load(new XmlTextReader(new StreamReader(Request.InputStream)));
            List<string> mailError = new List<string>();
            foreach (XmlNode item in requestXml.SelectNodes("//users/user"))
            {
                try
                {
                    String passwd = Membership.GeneratePassword(10, 2);
                    int newId = generateId(db);                                    //ho usato un metodo per generare l'id
                    User user = new User()
                    {
                        _id = newId,
                        username = item.InnerText,
                        email = item.InnerText,
                        password = SocialTFSProxy.Encrypt(passwd)
                    };
                    Stopwatch w = Stopwatch.StartNew();
                    collection.InsertOneAsync(user);
                    w.Stop();
                    ILog log = LogManager.GetLogger("QueryLogger");
                    log.Info(" Elapsed time: " + w.Elapsed + ", insert the new user");
                    if (WebUtility.SendEmail(item.InnerText, "SocialTFS invitation", GetBody(item.InnerText, passwd), true))
                    {
                        //    db.SubmitChanges();     insert fatta sopra
                    }
                    else
                        mailError.Add(item.InnerText);
                }
                catch
                {
                    mailError.Add(item.InnerText);
                }
            }
            XElement root = new XElement("Root");
            foreach (string item in mailError)
                root.Add(new XElement("NotSent", item));
            Response.Clear();
            Response.ContentType = "text/xml";
            Response.Write(new XDocument(root));
            Response.End();
        }

        private int generateId(ConnectorDataContext db)
        {
            var collection = db.Users();
            Stopwatch w = Stopwatch.StartNew();
            List<int> users = collection.AsQueryable().Select(u => u._id).ToList();
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", select all users to create a new id");
            int toReturn = (users.Max()) + 1;
            return toReturn;
        }

        private String GetBody(string email, string passwd)
        {
            String body = string.Empty;
            body += "<p>You have been invited to join the SocialCDE community.</p>";
            body += "<p>You can download the latest version of the SocialCDE plugin for Visual Studio and Eclipse from this address: <a href=\"";
            body += Request.Url.AbsoluteUri.Replace("NewUser", "DownloadClient") + "\">";
            body += Request.Url.AbsoluteUri.Replace("NewUser", "DownloadClient") + "</a></p>";
            body += "<p>Sign up using the following information.</p>";
            body += "<table>";
            body += String.Format("<tr><td>Proxy server host:</td><td style=\"font-weight:bold\">http://{0}</td></tr>", Request.Url.Authority);
            body += String.Format("<tr><td>Email:</td><td style=\"font-weight:bold\">{0}</td></tr>", email);
            body += String.Format("<tr><td>Invitation code:</td><td style=\"font-weight:bold\">{0}</td></tr>", passwd);
            body += "<tr><td>Username:</td><td style=\"font-weight:bold\">&lt;choose your username&gt;</td></tr>";
            body += "<tr><td>Password:</td><td style=\"font-weight:bold\">&lt;choose your password&gt;</td></tr>";
            body += "</table>";
            body += "<br/><p>Regards,<br/>";
            body += "The SocialCDE Admin</p>";
            return body;
        }

    }

}