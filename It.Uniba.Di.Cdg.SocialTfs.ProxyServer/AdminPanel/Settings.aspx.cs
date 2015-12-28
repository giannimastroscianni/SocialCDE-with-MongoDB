using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Xml.Linq;
using System.Diagnostics;
using log4net;
using log4net.Config;
using MongoDB.Driver;
using MongoDB.Bson;

namespace It.Uniba.Di.Cdg.SocialTfs.ProxyServer.AdminPanel
{
    public partial class Settings : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebUtility.CheckCredentials(this);
            if (Request.RequestType == "GET")
            {
                if (!string.IsNullOrEmpty(Request.Params["username"]))
                    CheckUsername(Request.Params["username"]);
                if (!string.IsNullOrEmpty(Request.Params["email"]))
                    CheckEmail(Request.Params["email"]);
            }
            else if (Request.RequestType == "POST")
            {
                switch (Request.Params["ctl00$MainContent$SettingSE"])
                {
                    case "Admin settings":
                        ChangeAdminSettings();
                        break;
                    case "Mail settings":
                        ChangeSmtpSettings();
                        break;
                }
            }
            FillAdminSettings();
            FillSmtpSettings();
        }

        private void CheckUsername(string username)
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var collection = db.Users();
            Stopwatch w = Stopwatch.StartNew();
            bool usr = collection.AsQueryable().Any(u => u.username == username && !u.isAdmin);
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", check if the username is already used");
            XDocument xml = new XDocument(
                     new XElement("Root",
                         new XElement("IsAviable", !usr)));
            Response.Clear();
            Response.ContentType = "text/xml";
            Response.Write(xml);
            Response.End();
        }

        private void CheckEmail(string email)
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var collection = db.Users();
            Stopwatch w1 = Stopwatch.StartNew();
            bool mail = collection.AsQueryable().Any(u => u.email == email && !u.isAdmin);
            w1.Stop();
            ILog log1 = LogManager.GetLogger("QueryLogger");
            log1.Info(" Elapsed time: " + w1.Elapsed + ", check if the email is already used");
            XDocument xml = new XDocument(
                     new XElement("Root",
                         new XElement("IsAviable", !mail)));
            Response.Clear();
            Response.ContentType = "text/xml";
            Response.Write(xml);
            Response.End();
        }

        private void ChangeSmtpSettings()
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var collection = db.Settings();
            try
            {
                int sett = collection.AsQueryable().Where(s => s.key == "SmtpServer").Count();
                if (sett < 1) addSettings();
                bool changePassword = true;
                if (ChangeMailPasswordCB.Checked)
                {
                    if (Request.Params["ctl00$MainContent$MailPasswordTB"].Equals(Request.Params["ctl00$MainContent$MailConfirmTB"]))
                    {
                        Stopwatch w2 = Stopwatch.StartNew();
                        var builders = Builders<Setting>.Filter;
                        var filter = builders.Eq("key", "MailPassword");
                        var update = Builders<Setting>.Update
                        .Set("value", SocialTFSProxy.EncDecRc4("key", Request.Params["ctl00$MainContent$MailPasswordTB"]));
                        var result = collection.UpdateOneAsync(filter, update).Result;
                        w2.Stop();
                        ILog log2 = LogManager.GetLogger("QueryLogger");
                        log2.Info(" Elapsed time: " + w2.Elapsed + ", select and set the value of 'MailPassword' key from settings");
                    }
                    else
                    {
                        ErrorPA.Attributes.Add("class", "error");
                        ErrorPA.InnerText = "Passwords do not match.";
                        changePassword = false;
                    }
                }
                if (changePassword)
                {
                    var builders = Builders<Setting>.Filter;
                    var filter = builders.Eq("key", "SmtpServer");
                    var update = Builders<Setting>.Update
                    .Set("value", Request.Params["ctl00$MainContent$SmtpServerTB"]);
                    Stopwatch w3 = Stopwatch.StartNew();
                    var result = collection.UpdateOneAsync(filter, update).Result;
                    w3.Stop();
                    ILog log3 = LogManager.GetLogger("QueryLogger");
                    log3.Info(" Elapsed time: " + w3.Elapsed + ", set the value of 'Smtp Server' key");
                    var filter1 = builders.Eq("key", "SmtpPort");
                    var update1 = Builders<Setting>.Update
                    .Set("value", Request.Params["ctl00$MainContent$SmtpPortTB"]);
                    Stopwatch w4 = Stopwatch.StartNew();
                    var result1 = collection.UpdateOneAsync(filter1, update1).Result;
                    w4.Stop();
                    ILog log4 = LogManager.GetLogger("QueryLogger");
                    log4.Info(" Elapsed time: " + w4.Elapsed + ", set the value of 'Smtp Port' key");
                    var filter2 = builders.Eq("key", "SmtpSecurity");
                    var update2 = Builders<Setting>.Update
                    .Set("value", Request.Params["ctl00$MainContent$SmtpSecuritySE"]);
                    Stopwatch w5 = Stopwatch.StartNew();
                    var result2 = collection.UpdateOneAsync(filter2, update2).Result;
                    w5.Stop();
                    ILog log5 = LogManager.GetLogger("QueryLogger");
                    log5.Info(" Elapsed time: " + w5.Elapsed + ", set the value of 'Smtp Security' key");
                    var filter3 = builders.Eq("key", "MailAddress");
                    var update3 = Builders<Setting>.Update
                    .Set("value", Request.Params["ctl00$MainContent$MailAddressTB"]);
                    Stopwatch w6 = Stopwatch.StartNew();
                    var result3 = collection.UpdateOneAsync(filter3, update3).Result;
                    w6.Stop();
                    ILog log6 = LogManager.GetLogger("QueryLogger");
                    log6.Info(" Elapsed time: " + w6.Elapsed + ", set the value of 'MailAddress' key");
                    ErrorPA.Attributes.Add("class", "confirm");
                    ErrorPA.InnerText = "Data stored.";
                }
            }
            catch
            { }
        }

        private void addSettings()
        {
            ConnectorDataContext db = new ConnectorDataContext();
            try
            {
                List<Setting> settings = new List<Setting>()
                     {
                         new Setting(){
                             key="SmtpServer",
                             value = Request.Params["ctl00$MainContent$SmtpServerTB"]
                         },
                         new Setting () {
                           key = "SmtpPort",
                          value = Request.Params["ctl00$MainContent$SmtpPortTB"]
                         },
                         new Setting () {
                          key= "SmtpSecurity",
                         value = Request.Params["ctl00$MainContent$SmtpSecuritySE"]
                         },
                         new Setting () {
                           key = "MailAddress",
                          value = Request.Params["ctl00$MainContent$MailAddressTB"]
                         },
                         new Setting () {
                            key = "MailPassword",
                            value=SocialTFSProxy.EncDecRc4("key",Request.Params["ctl00$MainContent$MailPasswordTB"])
                         }
                     };
                db.Settings().InsertManyAsync(settings);
                ErrorPA.Attributes.Add("class", "confirm");
                ErrorPA.InnerText = "Data stored.";
            }
            catch
            {
                ErrorPA.Attributes.Add("class", "error");
                ErrorPA.InnerText = "Something was wrong. Please try again later.";
            }
        }

        private void FillSmtpSettings()
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var collection = db.Settings();
            try
            {
                Stopwatch w9 = Stopwatch.StartNew();
                SmtpServerTB.Value = collection.AsQueryable().Where(s => s.key == "SmtpServer").Single().value;
                w9.Stop();
                ILog log9 = LogManager.GetLogger("QueryLogger");
                log9.Info(" Elapsed time: " + w9.Elapsed + ", select the value of 'SmtpServer' key from settings");
                Stopwatch w10 = Stopwatch.StartNew();
                SmtpPortTB.Value = collection.AsQueryable().Where(s => s.key == "SmtpPort").Single().value;
                w10.Stop();
                ILog log10 = LogManager.GetLogger("QueryLogger");
                log10.Info(" Elapsed time: " + w10.Elapsed + ", select the value of 'SmtpPort' key from settings");
                Stopwatch w11 = Stopwatch.StartNew();
                SmtpSecuritySE.Value = collection.AsQueryable().Where(s => s.key == "SmtpSecurity").Single().value;
                w11.Stop();
                ILog log11 = LogManager.GetLogger("QueryLogger");
                log11.Info(" Elapsed time: " + w11.Elapsed + ", select the value of 'SmtpSecurity' key from settings");
                Stopwatch w12 = Stopwatch.StartNew();
                MailAddressTB.Value = collection.AsQueryable().Where(s => s.key == "MailAddress").Single().value;
                w12.Stop();
                ILog log12 = LogManager.GetLogger("QueryLogger");
                log12.Info(" Elapsed time: " + w12.Elapsed + ", select the value of 'MailAddress' key from settings");
            }
            catch { }
        }

        private void ChangeAdminSettings()
        {
            string username = Request.Params["ctl00$MainContent$AdminUsernameTB"];
            string email = Request.Params["ctl00$MainContent$AdminEmailTB"];
            string password = Request.Params["ctl00$MainContent$PasswordTB"];
            string confirm = Request.Params["ctl00$MainContent$ConfirmTB"];
            ConnectorDataContext db = new ConnectorDataContext();
            var collection = db.Users();
            Stopwatch w = Stopwatch.StartNew();
            User admin = collection.AsQueryable().Where(u => u.isAdmin).Single();
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", select the admin to change his settings");
            bool changePassword = true;
            if (ChangePasswordCB.Checked)
                if (password.Equals(confirm))
                {
                    var builders = Builders<User>.Filter;
                    var filter = builders.Eq("isAdmin", true);
                    var update = Builders<User>.Update
                    .Set("password", SocialTFSProxy.Encrypt(password));
                    var result = collection.UpdateOneAsync(filter, update).Result;
                }
                else
                {
                    ErrorPA.Attributes.Add("class", "error");
                    ErrorPA.InnerText = "Passwords do not match.";
                    changePassword = false;
                }
            if (changePassword)
            {
                Stopwatch w2 = Stopwatch.StartNew();
                bool usr = collection.AsQueryable().Any(u => (u.username == username || u.email == email) && !u.isAdmin);
                w2.Stop();
                ILog log2 = LogManager.GetLogger("QueryLogger");
                log2.Info(" Elapsed time: " + w2.Elapsed + ", check if there is an user with admin's username or email");
                if (!usr)
                {
                    //    aggiorno
                    var builders = Builders<User>.Filter;
                    var filter = builders.Eq("isAdmin", true);
                    var update = Builders<User>.Update
                    .Set("username", username);
                    var result = collection.UpdateOneAsync(filter, update).Result;
                    var update1 = Builders<User>.Update
                    .Set("email", email);
                    var result1 = collection.UpdateOneAsync(filter, update1).Result;
                    ErrorPA.Attributes.Add("class", "confirm");
                    ErrorPA.InnerText = "Data stored";
                }
                else
                {
                    ErrorPA.Attributes.Add("class", "error");
                    ErrorPA.InnerText = "Username or email already exist.";
                }
            }
        }

        private void FillAdminSettings()
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var collection = db.Users();
            Stopwatch w = Stopwatch.StartNew();
            User admin = collection.AsQueryable().Where(u => u.isAdmin).Single();
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", select the admin to fill his settings");
            AdminUsernameTB.Value = admin.username;
            AdminEmailTB.Value = admin.email;
        }

    }

}