using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Xml.Linq;
using It.Uniba.Di.Cdg.SocialTfs.ServiceLibrary;
using System.Diagnostics;
using log4net;
using log4net.Config;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;

namespace It.Uniba.Di.Cdg.SocialTfs.ProxyServer.AdminPanel
{
    public partial class Services : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebUtility.CheckCredentials(this);
            if (Request.RequestType == "GET")
                LoadServices();
            else if (Request.RequestType == "POST")
                DeleteService(Int32.Parse(Request.Params["id"]));
        }

        private void LoadServices()
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var serviceCollection = db.Services();
            var serviceInstanceCollection = db.ServiceInstances();
            Stopwatch w = Stopwatch.StartNew();
            List<ServiceInstance> sInstance = serviceInstanceCollection.AsQueryable().Where(s => s.service.name != "SocialTFS").OrderBy(s => s.name).ToList();      //  ordinati per nome
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", select all service instances different from 'SocialTFS' to show them");
            foreach (var item in sInstance)
            {
                HtmlTableCell name = new HtmlTableCell();
                HtmlTableCell service = new HtmlTableCell();
                HtmlTableCell host = new HtmlTableCell();
                HtmlTableCell edit = new HtmlTableCell();
                HtmlTableCell delete = new HtmlTableCell();
                name.InnerText = item.name;
                service.InnerText = item.service.name;
                host.InnerText = item.host;
                IService iService = ServiceFactory.getService(item.service.name);
                if (iService.GetPrivateFeatures().Contains(FeaturesType.MoreInstance) || iService.GetPrivateFeatures().Contains(FeaturesType.Labels))
                {
                    HtmlInputButton editBT = new HtmlInputButton();
                    editBT.Attributes.Add("title", "Edit " + item.name);
                    editBT.Attributes.Add("class", "edit");
                    editBT.Value = item._id.ToString();
                    edit.Attributes.Add("class", "center");
                    edit.Controls.Add(editBT);
                }
                HtmlInputButton deleteBT = new HtmlInputButton();
                deleteBT.Attributes.Add("title", "Delete " + item.name);
                deleteBT.Attributes.Add("class", "delete");
                deleteBT.Value = item._id.ToString();
                delete.Attributes.Add("class", "center");
                delete.Controls.Add(deleteBT);
                HtmlTableRow tr = new HtmlTableRow();
                tr.ID = "Row" + item._id;
                tr.Cells.Add(name);
                tr.Cells.Add(service);
                tr.Cells.Add(host);
                tr.Cells.Add(edit);
                tr.Cells.Add(delete);
                ServiceTable.Rows.Add(tr);
            }
        }

        private void DeleteService(int id)
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var collection = db.ServiceInstances();
            var featureScoreCollection = db.FeatureScores();
            var chosenFeatureCollection = db.ChosenFeatures();
            var suggestionCollection = db.Suggestions();
            var dynamicFriendCollection = db.DynamicFriends();
            var postCollection = db.Posts();
            var registrationCollection = db.Registrations();
            var interactiveFriendCollection = db.InteractiveFriends();
            bool isDeleted;
            try
            {
                var filter = Builders<ServiceInstance>.Filter.Eq("_id", id);
                var filter1 = Builders<FeatureScore>.Filter.Eq("serviceInstance._id", id);
                var filter2 = Builders<ChosenFeature>.Filter.Eq("serviceInstance._id", id);
                var filter3 = Builders<Suggestion>.Filter.Eq("chosenFeature.serviceInstance._id", id);
                var filter4 = Builders<DynamicFriend>.Filter.Eq("chosenFeature.serviceInstance._id", id);
                var filter5 = Builders<InteractiveFriend>.Filter.Eq("chosenFeature.serviceInstance._id", id);
                var filter6 = Builders<Post>.Filter.Eq("chosenFeature.serviceInstance._id", id);
                var filter7 = Builders<Registration>.Filter.Eq("serviceInstance._id", id);
                Stopwatch w1 = Stopwatch.StartNew();
                collection.DeleteManyAsync(filter);
                chosenFeatureCollection.DeleteManyAsync(filter2);
                featureScoreCollection.DeleteManyAsync(filter1);
                suggestionCollection.DeleteManyAsync(filter3);
                dynamicFriendCollection.DeleteManyAsync(filter4);
                postCollection.DeleteManyAsync(filter6);
                interactiveFriendCollection.DeleteManyAsync(filter5);
                registrationCollection.DeleteManyAsync(filter7);
                w1.Stop();
                ILog log1 = LogManager.GetLogger("QueryLogger");
                log1.Info(" Elapsed time: " + w1.Elapsed + ", remove all service instances and feature scores associated");
                isDeleted = true;
            }
            catch (Exception)
            {
                isDeleted = false;
            }
            XDocument xml = new XDocument(
                            new XElement("Root",
                                new XElement("Deleted", isDeleted)));
            Response.Clear();
            Response.ContentType = "text/xml";
            Response.Write(xml);
            Response.End();
        }

    }

}