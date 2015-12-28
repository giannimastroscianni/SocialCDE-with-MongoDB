using System;
using System.IO;
using System.Web.UI.HtmlControls;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Diagnostics;
using System.Data;
using log4net;
using log4net.Config;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;

namespace It.Uniba.Di.Cdg.SocialTfs.ProxyServer.AdminPanel
{
    public partial class Weights : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebUtility.CheckCredentials(this);
            if (Request.RequestType == "GET")
                LoadWeights();
            else if (Request.RequestType == "POST")
                SaveWeights();
        }

        private void LoadWeights()
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var featureScoreCollection = db.FeatureScores();
            var serviceInstanceCollection = db.ServiceInstances();
            try
            {
                var filter = Builders<FeatureScore>.Filter.Empty;
                Stopwatch w = Stopwatch.StartNew();
                List<FeatureScore> fScores = featureScoreCollection.AsQueryable().ToList();
                w.Stop();
                ILog log = LogManager.GetLogger("QueryLogger");
                log.Info(" Elapsed time: " + w.Elapsed + ", select all feature scores");
                foreach (var item in fScores)
                {
                    HtmlTableCell service = new HtmlTableCell();
                    HtmlTableCell feature = new HtmlTableCell();
                    HtmlTableCell weight = new HtmlTableCell();
                    service.InnerText = item.serviceInstance.name;
                    feature.InnerText = item.feature;
                    weight.InnerText = item.score.ToString();
                    weight.Attributes.Add("class", "center");
                    weight.Attributes.Add("contenteditable", "true");
                    HtmlTableRow tr = new HtmlTableRow();
                    tr.Cells.Add(service);
                    tr.Cells.Add(feature);
                    tr.Cells.Add(weight);
                    WeightTable.Rows.Add(tr);
                }
            }
            catch (AggregateException) { }
        }

        private void SaveWeights()
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var featureScoreCollection = db.FeatureScores();
            var serviceInstanceCollection = db.ServiceInstances();
            bool isSaved;
            XmlDocument requestXml = new XmlDocument();
            requestXml.Load(new XmlTextReader(new StreamReader(Request.InputStream)));
            try
            {
                foreach (XmlNode item in requestXml.SelectNodes("//weights/item"))
                {
                    Stopwatch w = Stopwatch.StartNew();
                    FeatureScore featureScore = featureScoreCollection.AsQueryable().Where(fs => fs.serviceInstance.name == item.SelectSingleNode("service").InnerText && fs.feature == item.SelectSingleNode("feature").InnerText).Single();
                    w.Stop();
                    ILog log = LogManager.GetLogger("QueryLogger");
                    log.Info(" Elapsed time: " + w.Elapsed + ", select feature scores");
                    var filter = Builders<FeatureScore>.Filter.Eq("_id", featureScore._id);
                    var update = Builders<FeatureScore>.Update
                            .Set("score", Int32.Parse(item.SelectSingleNode("weight").InnerText));
                    featureScoreCollection.UpdateOneAsync(filter, update);
                }
                isSaved = true;
            }
            catch (Exception)
            {
                isSaved = false;
            }
            XDocument xml = new XDocument(
                            new XElement("Root",
                                new XElement("Saved", isSaved)));
            Response.Clear();
            Response.ContentType = "text/xml";
            Response.Write(xml);
            Response.End();
        }

    }

}