using System;
using System.Linq;
using System.Web.UI;
using System.Collections.Generic;
using System.Diagnostics;
using log4net;
using log4net.Config;
using MongoDB.Bson;
using MongoDB.Driver;

namespace It.Uniba.Di.Cdg.SocialTfs.ProxyServer.AdminPanel
{
    public partial class Login : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Session.Clear();
            string username = Request.Form["ctl00$MainContent$UsernameTB"];
            string password = Request.Form["ctl00$MainContent$PasswordTB"];
            if (username != null)
            {
                ConnectorDataContext db = new ConnectorDataContext();
                var collection = db.Settings();
                if (Signup(db, username, password))
                {
                    Session["Username"] = username;
                    Stopwatch w1 = Stopwatch.StartNew();
                    bool settings = collection.AsQueryable().Any(s => s.key == "SmtpServer");
                    w1.Stop();
                    ILog log1 = LogManager.GetLogger("QueryLogger");
                    log1.Info(" Elapsed time: " + w1.Elapsed + ", select any settings with key 'SmtpServer'");
                    if (settings)
                        Response.Redirect("Default.aspx");
                    else
                        Response.Redirect("Settings.aspx");
                }
                else
                {
                    errorLB.Attributes.Add("class", "error");
                    errorLB.InnerText = "The username or the password is not correct";
                }
            }
            else if (Request.QueryString["type"] == "error")
            {
                errorLB.Attributes.Add("class", "error");
                errorLB.InnerHtml = Request.QueryString["message"];
            }
            else if (Request.QueryString["type"] == "confirm")
            {
                errorLB.Attributes.Add("class", "confirm");
                errorLB.InnerText = Request.QueryString["message"];
            }
        }

        private bool Signup(ConnectorDataContext db, string username, string password)
        {
            var collection = db.Users();
            Stopwatch w = Stopwatch.StartNew();
            IEnumerable<User> users = collection.AsQueryable().Where(u => u.isAdmin && u.username == username && u.password == SocialTFSProxy.Encrypt(password));
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", select the admin");
            if (users.Count() >= 1)
                return true;
            else
                return false;
        }

    }

}