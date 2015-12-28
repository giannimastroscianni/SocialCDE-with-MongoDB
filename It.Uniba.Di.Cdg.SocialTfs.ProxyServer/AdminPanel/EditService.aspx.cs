using System;
using System.Linq;
using It.Uniba.Di.Cdg.SocialTfs.ServiceLibrary;
using System.Diagnostics;
using log4net;
using log4net.Config;
using MongoDB.Bson;
using MongoDB.Driver;

namespace It.Uniba.Di.Cdg.SocialTfs.ProxyServer.AdminPanel
{
    public partial class EditService : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebUtility.CheckCredentials(this);
            if (Request.RequestType == "GET")
                if (String.IsNullOrEmpty(Request.QueryString["id"]))
                    Response.Redirect("Services.aspx");
                else
                    PopulateService();
            else if (Request.RequestType == "POST")
                SaveService();
            else
                Response.Redirect("Services.aspx");
        }

        private void PopulateService()
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var serviceInstanceCollection = db.ServiceInstances();
            Stopwatch w = Stopwatch.StartNew();
            ServiceInstance service = serviceInstanceCollection.AsQueryable().Where(serin => serin._id == Int32.Parse(Request.QueryString["id"])).Single();
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", select the service instances to edit(ES)");
            IService iService = ServiceFactory.getService(service.service.name);
            if (!iService.GetPrivateFeatures().Contains(FeaturesType.MoreInstance) && !iService.GetPrivateFeatures().Contains(FeaturesType.Labels))
                Response.Redirect("Services.aspx");
            Id.Value = service._id.ToString();
            ServiceTB.Value = service.service.name;
            NameTB.Value = service.name;
            HostTB.Value = service.host;
            if (iService.GetPrivateFeatures().Contains(FeaturesType.OAuth1))
            {
                ConsumerKeyTB.Value = service.consumerKey;
                ConsumerSecretTB.Value = service.consumerSecret;
            }
            else
            {
                ConsumerKeyTB.Attributes["required"] = String.Empty;
                ConsumerSecretTB.Attributes["required"] = String.Empty;
                ConsumerKeyRW.Visible = false;
                ConsumerSecretRW.Visible = false;
            }
            if (!iService.GetPrivateFeatures().Contains(FeaturesType.Labels))
            {
                GitHubLabelRW.Visible = false;
                ErrGitHubLabelRW.Visible = false;
            }
            else
            {
                ServiceTB.Disabled = true;
                NameTB.Disabled = true;
                HostTB.Disabled = true;
                GitHubLabelTB.Value = ServiceFactory.GitHubLabels;
                ErrGitHubLabelRW.Visible = true;
            }
        }

        private void SaveService()
        {
            if (!String.IsNullOrEmpty(NameTB.Attributes["required"]) && String.IsNullOrEmpty(Request.Params["ctl00$MainContent$NameTB"]))
                Response.Redirect("Services.aspx");
            if (!String.IsNullOrEmpty(HostTB.Attributes["required"]) && String.IsNullOrEmpty(Request.Params["ctl00$MainContent$HostTB"]))
                Response.Redirect("Services.aspx");
            if (!String.IsNullOrEmpty(ConsumerKeyTB.Attributes["required"]) && String.IsNullOrEmpty(Request.Params["ctl00$MainContent$ConsumerKeyTB"]))
                Response.Redirect("Services.aspx");
            if (!String.IsNullOrEmpty(ConsumerSecretTB.Attributes["required"]) && String.IsNullOrEmpty(Request.Params["ctl00$MainContent$ConsumerSecretTB"]))
                Response.Redirect("Services.aspx");
            ConnectorDataContext db = new ConnectorDataContext();
            var serviceInstanceCollection = db.ServiceInstances();
            Stopwatch w = Stopwatch.StartNew();
            ServiceInstance service = (from serin in serviceInstanceCollection.AsQueryable() where serin._id == Int32.Parse(Request.QueryString["id"]) select serin).Single();
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", select the service instances to save(ES)");
            IService iService = ServiceFactory.getService(service.service.name);
            var filter = Builders<ServiceInstance>.Filter.Eq("_id", service._id);
            if (iService.GetPrivateFeatures().Contains(FeaturesType.Labels))
            {
                if (String.IsNullOrEmpty(Request.Params["ctl00$MainContent$GitHubLabelTB"]))
                {
                    System.Diagnostics.Debug.WriteLine("label nulla");
                    ServiceFactory.GitHubLabels = String.Empty;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("labels associate " + Request.Params["ctl00$MainContent$GitHubLabelTB"]);
                    ServiceFactory.GitHubLabels = Request.Params["ctl00$MainContent$GitHubLabelTB"];
                }
            }
            else
            {
                //   faccio gli update
                var update = Builders<ServiceInstance>.Update
        .Set("name", Request.Params["ctl00$MainContent$NameTB"]);
                Stopwatch w1 = Stopwatch.StartNew();
                var result = serviceInstanceCollection.UpdateOneAsync(filter, update).Result;
                w1.Stop();
                ILog log1 = LogManager.GetLogger("QueryLogger");
                log1.Info(" Elapsed time: " + w1.Elapsed + ", service's name update(ES)");
                var update1 = Builders<ServiceInstance>.Update
        .Set("host", Request.Params["ctl00$MainContent$HostTB"]);
                Stopwatch w2 = Stopwatch.StartNew();
                var result1 = serviceInstanceCollection.UpdateOneAsync(filter, update1).Result;
                w2.Stop();
                ILog log2 = LogManager.GetLogger("QueryLogger");
                log2.Info(" Elapsed time: " + w2.Elapsed + ", service's host update(ES)");
                if (iService.GetPrivateFeatures().Contains(FeaturesType.OAuth1))
                {
                    var update2 = Builders<ServiceInstance>.Update
        .Set("consumerKey", Request.Params["ctl00$MainContent$ConsumerKeyTB"]);
                    Stopwatch w3 = Stopwatch.StartNew();
                    var result2 = serviceInstanceCollection.UpdateOneAsync(filter, update2).Result;
                    w3.Stop();
                    ILog log3 = LogManager.GetLogger("QueryLogger");
                    log3.Info(" Elapsed time: " + w3.Elapsed + ", service's consumerKey update(ES)");
                    var update3 = Builders<ServiceInstance>.Update
        .Set("consumerSecret", Request.Params["ctl00$MainContent$ConsumerSecretTB"]);
                    Stopwatch w4 = Stopwatch.StartNew();
                    var result3 = serviceInstanceCollection.UpdateOneAsync(filter, update3).Result;
                    w4.Stop();
                    ILog log4 = LogManager.GetLogger("QueryLogger");
                    log4.Info(" Elapsed time: " + w4.Elapsed + ", service's consumerSecret update(ES)");
                }
                else if (iService.GetPrivateFeatures().Contains(FeaturesType.TFSAuthenticationWithDomain))
                {
                    var update4 = Builders<ServiceInstance>.Update
       .Set("consumerKey", Request.Params["ctl00$MainContent$UsernameTB"]);
                    Stopwatch w5 = Stopwatch.StartNew();
                    var result4 = serviceInstanceCollection.UpdateOneAsync(filter, update4).Result;
                    w5.Stop();
                    ILog log5 = LogManager.GetLogger("QueryLogger");
                    log5.Info(" Elapsed time: " + w5.Elapsed + ", service's consumerKey update(ES)");
                    var update5 = Builders<ServiceInstance>.Update
        .Set("consumerSecret", Request.Params["ctl00$MainContent$PasswordTB"]);
                    Stopwatch w6 = Stopwatch.StartNew();
                    var result5 = serviceInstanceCollection.UpdateOneAsync(filter, update5).Result;
                    w6.Stop();
                    ILog log6 = LogManager.GetLogger("QueryLogger");
                    log6.Info(" Elapsed time: " + w6.Elapsed + ", service's consumerSecret update(ES)");
                }
            }
            Response.Redirect("Services.aspx");
        }

    }

}