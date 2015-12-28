using System;
using System.Linq;
using System.Reflection;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using It.Uniba.Di.Cdg.SocialTfs.ServiceLibrary;
using System.Collections.Generic;
using System.Diagnostics;
using log4net;
using log4net.Config;
using MongoDB.Bson;
using MongoDB.Driver;

namespace It.Uniba.Di.Cdg.SocialTfs.ProxyServer.AdminPanel
{
    public partial class NewService : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebUtility.CheckCredentials(this);
            if (Request.RequestType == "GET")
            {
                if (ServiceSE.Items.Count == 0)
                    PopulateServices();
                if (!String.IsNullOrEmpty(Request.QueryString["id"]))
                    ServiceFields();
            }
            else if (Request.RequestType == "POST")
                SaveService();
            else
                Response.Redirect("Services.aspx");
        }

        private void PopulateServices()
        {
            ServiceSE.Items.Add(new ListItem());
            ConnectorDataContext db = new ConnectorDataContext();
            var serviceCollection = db.Services();
            var serviceInstanceCollection = db.ServiceInstances();
            Stopwatch w = Stopwatch.StartNew();
            List<Service> serv = serviceCollection.AsQueryable().Where(s => s.name != "SocialTFS").ToList();
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", select all services different from 'SocialTFS'");
            foreach (Service item in serv)
            {
                try
                {
                    IService iService = ServiceFactory.getService(item.name);
                    var filter = Builders<ServiceInstance>.Filter.Eq("service._id", item._id);
                    Stopwatch w1 = Stopwatch.StartNew();
                    var service = serviceInstanceCollection.CountAsync(filter).Result;
                    w1.Stop();
                    ILog log1 = LogManager.GetLogger("QueryLogger");
                    log1.Info(" Elapsed time: " + w1.Elapsed + ", check if the service instance contains the service");
                    if (iService.GetPrivateFeatures().Contains(FeaturesType.MoreInstance) || service == 0)
                        ServiceSE.Items.Add(new ListItem(item.name, item._id.ToString()));
                }
                catch (Exception) { }
            }
        }

        private void ServiceFields()
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var collection = db.Services();
            try
            {
                Stopwatch w2 = Stopwatch.StartNew();
                String serv = collection.AsQueryable().Where(s => s._id == Int32.Parse(Request.QueryString["id"])).Single().name;
                w2.Stop();
                ILog log2 = LogManager.GetLogger("QueryLogger");
                log2.Info(" Elapsed time: " + w2.Elapsed + ", select the name of the service");
                IService iService = ServiceFactory.getService(serv);
                XDocument xml = new XDocument(
                    new XElement("Root",
                        new XElement("CanHaveMoreInstance", iService.GetPrivateFeatures().Contains(FeaturesType.MoreInstance)),
                        new XElement("NeedOAuth", iService.GetPrivateFeatures().Contains(FeaturesType.OAuth1)),
                        new XElement("NeedGitHubLabel", iService.Name.Equals("GitHub"))));
                Response.Clear();
                Response.ContentType = "text/xml";
                Response.Write(xml);
                Response.End();
            }
            catch (TargetInvocationException)
            {
                XDocument xml = new XDocument(
                       new XElement("Root",
                           new XElement("CanHaveMoreInstance", false),
                           new XElement("NeedOAuth", false),
                           new XElement("NeedGitHubLabel", false)));
                Response.Clear();
                Response.ContentType = "text/xml";
                Response.Write(xml);
                Response.End();
            }
            catch (InvalidOperationException)
            {
                Response.Redirect("Services.aspx");
            }
        }

        private void SaveService()
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var serviceCollection = db.Services();
            var serviceInstanceCollection = db.ServiceInstances();
            var featureScoreCollection = db.FeatureScores();
            var preregisteredServiceCollection = db.PreregisteredServices();
            Service service = new Service();
            try
            {
                Stopwatch w3 = Stopwatch.StartNew();
                service = serviceCollection.AsQueryable().Where(s => s._id == Int32.Parse(Request.Params["ctl00$MainContent$ServiceSE"])).Single();
                w3.Stop();
                ILog log3 = LogManager.GetLogger("QueryLogger");
                log3.Info(" Elapsed time: " + w3.Elapsed + ", select the service to save");
            }
            catch
            {
                ErrorPA.Style.Remove("display");
            }
            IService iService = ServiceFactory.getService(service.name);
            ServiceInstance serviceInstance = new ServiceInstance();
            if (!iService.GetPrivateFeatures().Contains(FeaturesType.MoreInstance))
            {
                Stopwatch w4 = Stopwatch.StartNew();
                PreregisteredService preser = preregisteredServiceCollection.AsQueryable().Where(ps => ps.service._id == service._id).Single();
                w4.Stop();
                ILog log4 = LogManager.GetLogger("QueryLogger");
                log4.Info(" Elapsed time: " + w4.Elapsed + ", service id: " + service._id + ", select the preregistered service to save the serviceinstance");
                serviceInstance = new ServiceInstance()
                {
                    _id = GenerateServiceInstanceId(db),
                    name = preser.name,
                    host = preser.host,
                    service = preser.service,
                    consumerKey = preser.consumerKey,
                    consumerSecret = preser.consumerSecret
                };
                Stopwatch w5 = Stopwatch.StartNew();
                serviceInstanceCollection.InsertOneAsync(serviceInstance);
                w5.Stop();
                ILog log5 = LogManager.GetLogger("QueryLogger");
                log5.Info(" Elapsed time: " + w5.Elapsed + ", insert the service instance");
            }
            else
            {
                string consumerKey = null, consumerSecret = null;
                string host = Request.Params["ctl00$MainContent$HostTB"];
                if (host.EndsWith(@"/"))
                    host = host.Remove(host.Length - 1);
                if (iService.GetPrivateFeatures().Contains(FeaturesType.OAuth1))
                {
                    consumerKey = Request.Params["ctl00$MainContent$ConsumerKeyTB"];
                    consumerSecret = Request.Params["ctl00$MainContent$ConsumerSecretTB"];
                }
                serviceInstance = new ServiceInstance()
                {
                    _id = GenerateServiceInstanceId(db),
                    name = Request.Params["ctl00$MainContent$NameTB"],
                    host = host,
                    service = service,
                    consumerKey = consumerKey,
                    consumerSecret = consumerSecret
                };
                Stopwatch w6 = Stopwatch.StartNew();
                serviceInstanceCollection.InsertOneAsync(serviceInstance);
                w6.Stop();
                ILog log6 = LogManager.GetLogger("QueryLogger");
                log6.Info(" Elapsed time: " + w6.Elapsed + ", insert the service instance");
            }

            if (iService.GetPrivateFeatures().Contains(FeaturesType.Labels))
            {
                iService.Get(FeaturesType.Labels, Request.Params["ctl00$MainContent$GitHubLabelTB"]);
            }
            foreach (FeaturesType featureType in iService.GetScoredFeatures())
            {
                FeatureScore fScore = new FeatureScore()
                {
                    serviceInstance = serviceInstance,
                    feature = featureType.ToString(),
                    score = 1
                };
                Stopwatch w8 = Stopwatch.StartNew();
                featureScoreCollection.InsertOneAsync(fScore);
                w8.Stop();
                ILog log8 = LogManager.GetLogger("QueryLogger");
                log8.Info(" Elapsed time: " + w8.Elapsed + ", insert the relative feature score");
            }
            //TODO update the new version (leave comment from the next line)
            //dbService.version = newServiceVersion;
            //   Stopwatch w9 = Stopwatch.StartNew();
            //   db.SubmitChanges();
            //  w9.Stop();
            //  ILog log9 = LogManager.GetLogger("QueryLogger");
            //  log9.Info(" Elapsed time: " + w9.Elapsed + ", insert the feature score");
            Response.Redirect("Services.aspx");
        }

        private int GenerateServiceInstanceId(ConnectorDataContext db)
        {
            var collection = db.ServiceInstances();
            Stopwatch w = Stopwatch.StartNew();
            List<int> serviceInstances = collection.AsQueryable().Select(u => u._id).ToList();
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", select all service instances to create a new id");
            int toReturn = (serviceInstances.Max()) + 1;
            return toReturn;
        }

    }

}