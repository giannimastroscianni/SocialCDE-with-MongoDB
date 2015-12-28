using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.DataVisualization.Charting;
using System.Collections.Generic;
using System.Diagnostics;
using log4net;
using log4net.Config;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Reflection;

namespace It.Uniba.Di.Cdg.SocialTfs.ProxyServer.AdminPanel
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var userCollection = db.Users();
            var serviceInstanceCollection = db.ServiceInstances();
            var registrationCollection = db.Registrations();
            Series userSeries = RegisteredUser.Series[0];
            userSeries["PieLabelStyle"] = "Outside";
            userSeries.Points.Clear();
            try
            {
                Stopwatch w1 = Stopwatch.StartNew();
                int countActiveUsers = userCollection.AsQueryable().Where(u => u.active && !u.isAdmin).Count();
                w1.Stop();
                ILog log1 = LogManager.GetLogger("QueryLogger");
                log1.Info(" Elapsed time: " + w1.Elapsed + ", count the number of registered users(Default)");
                userSeries.Points.AddXY("Registered", countActiveUsers);
            }
            catch (TargetInvocationException) { }
            try
            {
                Stopwatch w2 = Stopwatch.StartNew();
                int countNotActiveUsers = userCollection.AsQueryable().Where(u => !u.active && !u.isAdmin).Count();
                w2.Stop();
                ILog log2 = LogManager.GetLogger("QueryLogger");
                log2.Info(" Elapsed time: " + w2.Elapsed + ", count the number of unregistered users(Default)");
                userSeries.Points.AddXY("Unregistered", countNotActiveUsers);
            }
            catch (TargetInvocationException) { }
            Series serviceSeries = RegisteredService.Series[0];
            serviceSeries["PieLabelStyle"] = "Outside";
            serviceSeries.Points.Clear();
            Stopwatch w = Stopwatch.StartNew();
            List<ServiceInstance> sInstances = serviceInstanceCollection.AsQueryable().Where(si => si.name != "SocialTFS").ToList();
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", select all service instances different from 'SocialTFS'(Default)");
            foreach (ServiceInstance item in sInstances)
            {
                var filter = Builders<Registration>.Filter.Eq("serviceInstance._id", item._id);
                var reg = registrationCollection.Find(filter);
                serviceSeries.Points.AddXY(item.name, reg.CountAsync().Result);
            }
        }

    }

}