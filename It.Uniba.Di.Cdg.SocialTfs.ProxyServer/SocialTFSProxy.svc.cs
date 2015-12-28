using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using It.Uniba.Di.Cdg.SocialTfs.ServiceLibrary;
using It.Uniba.Di.Cdg.SocialTfs.SharedLibrary;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using It.Uniba.Di.Cdg.SocialTfs.ServiceLibrary.GitHub;
using log4net.Config;
using log4net;
using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;


namespace It.Uniba.Di.Cdg.SocialTfs.ProxyServer
{
    enum HiddenType
    {
        Suggestions,
        Dynamic,
        Interactive
    }

    /// <summary>
    /// Implements of all functions of the Proxy web service.
    /// </summary>
    /// <remarks>
    /// Lists all methods available on the web via REST requests
    /// to query or modify the services database.
    /// The complete list of methods' description is in It.Uniba.Di.Cdg.SocialTfs.SharedLibrary.ISocialTFSProxy.cs.
    /// </remarks>
    public class SocialTFSProxy : ISocialTFSProxy
    {
        private int postLimit = 20;
        private TimeSpan _postSpan = new TimeSpan(0, 1, 0);
        private TimeSpan _suggestionSpan = new TimeSpan(24, 0, 0);
        private TimeSpan _dynamicSpan = new TimeSpan(6, 0, 0);
        private TimeSpan _interactiveSpan = new TimeSpan(6, 0, 0);
        private TimeSpan _skillSpan = new TimeSpan(15, 0, 0, 0);

        /// <summary>
        /// This static constructor is called only one time, when the application is started. 
        /// It synchronizes the features available for each service with the features available in the database.
        /// </summary>
        static SocialTFSProxy()
        {
            ConnectorDataContext db = new ConnectorDataContext();
            XmlConfigurator.Configure(new Uri(System.Web.Hosting.HostingEnvironment.MapPath("~/log4net.config")));
            var featureCollection = db.Features();
            //add the completely new features
            IEnumerable<FeaturesType> features = FeaturesManager.GetFeatures();
            User admin = new User
            {
                _id = 1,
                username = "admin",
                password = Encrypt("admin"),
                email = "Admin@mail.com",
                active = false,
                isAdmin = true
            };
            db.Users().InsertOneAsync(admin);
            foreach (FeaturesType featureType in features)
            {
                var filter = Builders<Feature>.Filter.Eq("name", featureType.ToString());
                Stopwatch w = Stopwatch.StartNew();
                var res = featureCollection.CountAsync(filter).Result;
                w.Stop();
                ILog log = LogManager.GetLogger("QueryLogger");
                log.Info(" Elapsed time: " + w.Elapsed + ", feature's name: " + featureType.ToString() + ", check if the feature is available when the application is started");
                if (res == 0)
                {
                    Feature feature = new Feature
                    {
                        name = featureType.ToString(),
                        description = FeaturesManager.GetFeatureDescription(featureType),
                        isPublic = FeaturesManager.IsPublicFeature(featureType)
                    };
                    Stopwatch w1 = Stopwatch.StartNew();
                    featureCollection.InsertOneAsync(feature);
                    w1.Stop();
                    ILog log1 = LogManager.GetLogger("QueryLogger");
                    log1.Info(" Elapsed time: " + w1.Elapsed + ", feature's name: " + featureType.ToString() + ", description: " + FeaturesManager.GetFeatureDescription(featureType) + ", isPublic: " + FeaturesManager.IsPublicFeature(featureType) + ", insert a feature in a pending state");
                }
            }
        }

        public bool IsWebServiceRunning()
        {
            return true;
        }

        public bool IsAvailable(String username)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            ConnectorDataContext db = new ConnectorDataContext();
            var collection = db.Users();
            try
            {
                var builders = Builders<User>.Filter;
                var filter = builders.Eq("username", username) & builders.Eq("active", true);
                Stopwatch w = Stopwatch.StartNew();
                var user = collection.CountAsync(filter).Result;
                w.Stop();
                ILog log = LogManager.GetLogger("QueryLogger");
                log.Info(" Elapsed time: " + w.Elapsed + ", username: " + username + ", select an username to check if it is already used");
                if (user == 0) return true;
                return false;
            }
            catch (AggregateException)
            {
                return true;
            }
        }

        public int SubscribeUser(String email, String password, String username)
        {
            Contract.Requires(!String.IsNullOrEmpty(email));
            Contract.Requires(!String.IsNullOrEmpty(password));
            Contract.Requires(!String.IsNullOrEmpty(username));
            ConnectorDataContext db = new ConnectorDataContext();
            var userCollection = db.Users();
            var serviceInstanceCollection = db.ServiceInstances();
            var registrationCollection = db.Registrations();
            var chosenFeatureCollection = db.ChosenFeatures();
            User usr;
            try
            {
                Stopwatch w = Stopwatch.StartNew();
                usr = userCollection.AsQueryable().Where(u => u.email == email).SingleAsync().Result;
                w.Stop();
                ILog log = LogManager.GetLogger("QueryLogger");
                log.Info(" Elapsed time: " + w.Elapsed + ", user email: " + email + ", select the user to subscribe him");
            }
            catch (AggregateException)
            {
                return 1;
            }
            if (usr.password != Encrypt(password))
                return 2;
            if (!IsAvailable(username))
                return 3;
            var filter = Builders<User>.Filter.Eq("_id", usr._id);
            var update = Builders<User>.Update
                .Set("username", username);
            Stopwatch w4 = Stopwatch.StartNew();
            userCollection.UpdateOneAsync(filter, update);
            w4.Stop();
            ILog log4 = LogManager.GetLogger("QueryLogger");
            log4.Info(" Elapsed time: " + w4.Elapsed + ", update the username for the registration");
            var update1 = Builders<User>.Update
               .Set("active", true);
            Stopwatch w5 = Stopwatch.StartNew();
            userCollection.UpdateOneAsync(filter, update1);
            w5.Stop();
            ILog log5 = LogManager.GetLogger("QueryLogger");
            log5.Info(" Elapsed time: " + w5.Elapsed + ", update the active value for the registration");
            try
            {
                usr = userCollection.AsQueryable().Where(u => u._id == usr._id).SingleAsync().Result;             //rieffettuo la query per avere i dati aggiornati, query considerata poco sopra per il log
                var filter1 = Builders<ServiceInstance>.Filter.Eq("service.name", "SocialTFS");
                Stopwatch w1 = Stopwatch.StartNew();
                ServiceInstance sInstance = serviceInstanceCollection.Find(filter1).SingleAsync().Result;
                w1.Stop();
                ILog log1 = LogManager.GetLogger("QueryLogger");
                log1.Info(" Elapsed time: " + w1.Elapsed + ", select the service instance with name 'SocialTFS'");
                Registration registration = new Registration()
                {
                    user = usr,
                    serviceInstance = sInstance,
                    nameOnService = username,
                    idOnService = username
                };
                Stopwatch w2 = Stopwatch.StartNew();
                registrationCollection.InsertOneAsync(registration);
                w2.Stop();
                ILog log2 = LogManager.GetLogger("QueryLogger");
                log2.Info(" Elapsed time: " + w2.Elapsed + ", service instance's id: " + sInstance + ", name and id on service: " + username + ", insert a new registration");
                ChosenFeature cFeature = new ChosenFeature
                {
                    _id = GenerateChosenFeatureId(db),
                    user = registration.user,
                    serviceInstance = registration.serviceInstance,
                    feature = FeaturesType.Post.ToString(),
                    lastDownload = new DateTime(1900, 1, 1)
                };
                Stopwatch w3 = Stopwatch.StartNew();
                chosenFeatureCollection.InsertOneAsync(cFeature);
                w3.Stop();
                ILog log3 = LogManager.GetLogger("QueryLogger");
                log3.Info(" Elapsed time: " + w3.Elapsed + ", feature: " + FeaturesType.Post.ToString() + ", last download: " + new DateTime(1900, 1, 1) + ", insert a new Chosen feature");
            }
            catch (AggregateException) { }
            return 0;
        }

        private long GenerateChosenFeatureId(ConnectorDataContext db)
        {
            var collection = db.ChosenFeatures();
            Stopwatch w = Stopwatch.StartNew();
            List<long> chosenFeatures = collection.AsQueryable().Select(u => u._id).ToList();
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", select all chosen features to create a new id");
            if (chosenFeatures.Count() == 0) return 1;
            long toReturn = (chosenFeatures.Max()) + 1;
            return toReturn;
        }

        private long GeneratePostId(ConnectorDataContext db)
        {
            var collection = db.Posts();
            Stopwatch w = Stopwatch.StartNew();
            List<long> posts = collection.AsQueryable().Select(u => u._id).ToList();
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", select all posts to create a new id");
            if (posts.Count() == 0) return 1;
            long toReturn = (posts.Max()) + 1;
            return toReturn;
        }

        private int GenerateStaticFriendId(ConnectorDataContext db)
        {
            var collection = db.StaticFriends();
            Stopwatch w = Stopwatch.StartNew();
            List<int> statFriends = collection.AsQueryable().Select(u => u._id).ToList();
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", select all posts to create a new id");
            if (statFriends.Count() == 0) return 1;
            int toReturn = (statFriends.Max()) + 1;
            return toReturn;
        }

        public bool ChangePassword(String username, String oldPassword, String newPassword)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(oldPassword));
            Contract.Requires(!String.IsNullOrEmpty(newPassword));
            ConnectorDataContext db = new ConnectorDataContext();
            var collection = db.Users();
            User user = CheckCredentials(db, username, oldPassword);
            if (user == null)
                return false;
            var filter = Builders<User>.Filter.Eq("_id", user._id);
            Stopwatch w = Stopwatch.StartNew();
            var update = Builders<User>.Update
                .Set("password", Encrypt(newPassword));
            var result = collection.UpdateOneAsync(filter, update).Result;
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", change password");
            return true;
        }

        public static string EncDecRc4(string key, string input)
        {
            StringBuilder result = new StringBuilder();
            int x, y, j = 0;
            int[] box = new int[256];
            for (int i = 0; i < 256; i++)
            {
                box[i] = i;
            }
            for (int i = 0; i < 256; i++)
            {
                j = (key[i % key.Length] + box[i] + j) % 256;
                x = box[i];
                box[i] = box[j];
                box[j] = x;
            }
            for (int i = 0; i < input.Length; i++)
            {
                y = i % 256;
                j = (box[y] + j) % 256;
                x = box[y];
                box[y] = box[j];
                box[j] = x;

                result.Append((char)(input[i] ^ box[(box[y] + box[j]) % 256]));
            }
            return result.ToString();
        }

        public WService[] GetServices(String username, String password)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var serviceInstanceCollection = db.ServiceInstances();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return new WService[0];
            // ILog log = LogManager.GetLogger("PanelLogger");
            //log.Info(user.id + ",S");
            List<WService> result = new List<WService>();
            var filter = Builders<ServiceInstance>.Filter.Ne("service.name", "SocialTFS");
            Stopwatch w1 = Stopwatch.StartNew();
            List<ServiceInstance> sInstance = serviceInstanceCollection.Find(filter).ToListAsync().Result;
            w1.Stop();
            ILog log1 = LogManager.GetLogger("QueryLogger");
            log1.Info(" Elapsed time: " + w1.Elapsed + ", select all service instances with the service different from 'SocialTFS'");
            foreach (ServiceInstance item in sInstance)
            {
                result.Add(Converter.ServiceInstanceToWService(db, user, item, true));
            }
            return result.ToArray();
        }

        public WUser GetUser(String username, String password)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var user = CheckCredentials(db, username, password);
            if (user == null)
                return null;
            //  ILog log = LogManager.GetLogger("PanelLogger");
            // log.Info(user.id + ",U");
            return Converter.UserToWUser(db, user, user, true);
        }

        public WUser GetColleagueProfile(String username, String password, int colleagueId)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var userCollection = db.Users();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return null;
            //  ILog log = LogManager.GetLogger("PanelLogger");
            // log.Info(user.id + ",C");
            User colleague = null;
            try
            {
                Stopwatch w1 = Stopwatch.StartNew();
                colleague = userCollection.AsQueryable().Where(u => u._id == colleagueId).SingleAsync().Result;
                w1.Stop();
                ILog log1 = LogManager.GetLogger("QueryLogger");
                log1.Info(" Elapsed time: " + w1.Elapsed + ", collegue id: " + colleagueId + ", select user profile");
            }
            catch (AggregateException)
            {
                return null;
            }
            return Converter.UserToWUser(db, user, colleague, true);
        }

        public WOAuthData GetOAuthData(string username, string password, int service)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var serviceInstanceCollection = db.ServiceInstances();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return null;
            Stopwatch w = Stopwatch.StartNew();
            ServiceInstance si = serviceInstanceCollection.AsQueryable().Where(s => s._id == service).SingleAsync().Result;
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", service instance: " + service + ", select service instance for OAuth 1 authentication procedure");
            IService iService = ServiceFactory.getServiceOauth(si.service.name, si.host, si.consumerKey, si.consumerSecret, null, null);
            if (iService.GetPrivateFeatures().Contains(FeaturesType.OAuth1))
            {
                OAuthAccessData oauthData = iService.Get(FeaturesType.OAuth1, OAuth1Phase.RequestOAuthData, si.host + si.service.requestToken, si.host + si.service.authorize) as OAuthAccessData;
                return new WOAuthData()
                {
                    AuthorizationLink = oauthData.RequestUri,
                    AccessToken = oauthData.AccessToken,
                    AccessSecret = oauthData.AccessSecret
                };
            }
            else if (iService.GetPrivateFeatures().Contains(FeaturesType.OAuth2))
            {
                String authorizationLink = String.Empty;
                if (si.service.name.Equals("LinkedIn"))
                {
                    authorizationLink = iService.Get(FeaturesType.OAuth2, si.consumerKey) as String;
                }
                else
                {
                    authorizationLink = iService.Get(FeaturesType.OAuth2) as String;
                }
                return new WOAuthData()
                {
                    AuthorizationLink = authorizationLink
                };
            }
            return null;
        }

        public bool Authorize(string username, string password, int service, string verifier, string accessToken, string accessSecret)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            Contract.Requires(!String.IsNullOrEmpty(accessToken));
            ConnectorDataContext db = new ConnectorDataContext();
            var serviceInstanceCollection = db.ServiceInstances();
            var serviceCollection = db.Services();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return false;
            Stopwatch w = Stopwatch.StartNew();
            ServiceInstance si = serviceInstanceCollection.AsQueryable().Where(s => s._id == service).SingleAsync().Result;
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", service id: " + service + ", select service id from serviceinstance");
            IService iService = ServiceFactory.getService(si.service.name);
            if (iService.GetPrivateFeatures().Contains(FeaturesType.OAuth1))
            {
                iService = ServiceFactory.getServiceOauth(si.service.name, si.host, si.consumerKey, si.consumerSecret, accessToken, accessSecret);
                OAuthAccessData oauthData = iService.Get(FeaturesType.OAuth1, OAuth1Phase.Authorize, si.host + si.service.accessToken, verifier) as OAuthAccessData;
                if (oauthData == null)
                    return false;
                IUser iUser = iService.VerifyCredential();
                return RegisterUserOnAService(db, user, si, iUser, oauthData.AccessToken, oauthData.AccessSecret);
            }
            else if (iService.GetPrivateFeatures().Contains(FeaturesType.OAuth2))
            {
                if (si.service.name.Equals("GitHub") || si.service.name.Equals("LinkedIn"))
                {
                    accessToken = iService.Get(FeaturesType.OAuth2, si.service.name, si.host, si.consumerKey, si.consumerSecret, accessToken) as string;
                }
                iService = ServiceFactory.getServiceOauth(si.service.name, si.host, si.consumerKey, si.consumerSecret, accessToken, null);
                IUser iUser = iService.VerifyCredential();
                bool registered = RegisterUserOnAService(db, user, si, iUser, accessToken, null);
                return registered;
            }
            return false;
        }

        public bool RecordService(string username, string password, int service, string usernameOnService, string passwordOnService, string domain)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            Contract.Requires(!String.IsNullOrEmpty(usernameOnService));
            ConnectorDataContext db = new ConnectorDataContext();
            var collection = db.ServiceInstances();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return false;
            Stopwatch w = Stopwatch.StartNew();
            ServiceInstance serviceInstance = collection.AsQueryable().Where(s => s._id == service).SingleAsync().Result;
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", service id: " + service + ", record a service without OAuth authentication procedure");
            IService iService = ServiceFactory.getService(
                    serviceInstance.service.name,
                    usernameOnService,
                    passwordOnService,
                    domain,
                    serviceInstance.host);
            IUser iUser = iService.VerifyCredential();
            return RegisterUserOnAService(db, user, serviceInstance, iUser, EncDecRc4("key", passwordOnService), (String)iUser.Get(UserFeaturesType.Domain));
        }

        private bool RegisterUserOnAService(ConnectorDataContext db, User user, ServiceInstance serviceInstance, IUser iUser, String accessToken, String accessSecret)
        {
            var collection = db.Registrations();
            try
            {
                Registration reg = new Registration();
                reg.user = user;
                reg.serviceInstance = serviceInstance;
                reg.nameOnService = iUser.UserName != null ? iUser.UserName : iUser.Id;
                reg.idOnService = iUser.Id.ToString();
                reg.accessToken = accessToken;
                reg.accessSecret = accessSecret;
                Stopwatch w = Stopwatch.StartNew();
                collection.InsertOneAsync(reg);
                w.Stop();
                ILog log = LogManager.GetLogger("QueryLogger");
                log.Info(" Elapsed time: " + w.Elapsed + ", user id: " + user._id + ", id on service: " + iUser.Id.ToString() + ", access token: " + accessToken + ", access secret: " + accessSecret + ", register user on a service");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool DeleteRegistredService(String username, String password, int service)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var collection = db.Registrations();
            var chosenFeatureCollection = db.ChosenFeatures();
            var dynamicFriendCollection = db.DynamicFriends();
            var postCollection = db.Posts();
            var interactiveFriendCollection = db.InteractiveFriends();
            var skillCollection = db.Skills();
            var suggestionCollection = db.Suggestions();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return false;
            try
            {
                var builders = Builders<Registration>.Filter;
                var filter = builders.Eq("user._id", user._id) & builders.Eq("serviceInstance._id", service);
                var filter1 = Builders<ChosenFeature>.Filter.Eq("serviceInstance._id", service);
                var filter2 = Builders<DynamicFriend>.Filter.Eq("chosenFeature.serviceInstance._id", service);
                var filter3 = Builders<Post>.Filter.Eq("chosenFeature.serviceInstance._id", service);
                var filter4 = Builders<InteractiveFriend>.Filter.Eq("chosenFeature.serviceInstance._id", service);
                var filter5 = Builders<Skill>.Filter.Eq("chosenFeature.serviceInstance._id", service);
                var filter6 = Builders<Suggestion>.Filter.Eq("chosenFeature.serviceInstance._id", service);
                Stopwatch w = Stopwatch.StartNew();
                var result = collection.DeleteManyAsync(filter).Result;
                var result1 = chosenFeatureCollection.DeleteManyAsync(filter1).Result;
                var result2 = dynamicFriendCollection.DeleteManyAsync(filter2).Result;
                var result3 = postCollection.DeleteManyAsync(filter3).Result;
                var result4 = interactiveFriendCollection.DeleteManyAsync(filter4).Result;
                var result5 = skillCollection.DeleteManyAsync(filter5).Result;
                var result6 = suggestionCollection.DeleteManyAsync(filter6).Result;
                w.Stop();
                ILog log = LogManager.GetLogger("QueryLogger");
                log.Info(" Elapsed time: " + w.Elapsed + ", user id: " + user._id + ", service id: " + service + ", unsubscribe service and delete everything related to that service");
            }
            catch (AggregateException)
            {
                return false;
            }
            return true;
        }

        public WPost[] GetHomeTimeline(string username, string password, long since, long to)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var staticFriendCollection = db.StaticFriends();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return new WPost[0];
            //ILog log = LogManager.GetLogger("PanelLogger");
            //log.Info(user.id + ",HT");
            Stopwatch w1 = Stopwatch.StartNew();
            List<int> authors = staticFriendCollection.AsQueryable().Where(sf => sf.user._id == user._id).Select(sf => sf.friend._id).ToList();
            w1.Stop();
            ILog log1 = LogManager.GetLogger("QueryLogger");
            log1.Info(" Elapsed time: " + w1.Elapsed + ", user id: " + user._id + ", select all friends of an author(home timeline) to show all posts");
            authors.Add(user._id);
            return GetTimeline(db, user, authors, since, to);
        }

        public WPost[] GetUserTimeline(string username, string password, string ownerName, long since, long to)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var collection = db.Users();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return new WPost[0];
            //   ILog log = LogManager.GetLogger("PanelLogger");
            // log.Info(user.id + ",UT");
            Stopwatch w1 = Stopwatch.StartNew();
            List<int> authors = new List<int> { collection.AsQueryable().Where(u => u.username == ownerName).Select(u => u._id).SingleAsync().Result };
            w1.Stop();
            ILog log1 = LogManager.GetLogger("QueryLogger");
            log1.Info(" Elapsed time: " + w1.Elapsed + ", username: " + ownerName + ", select all users' ids to get their posts");
            return GetTimeline(db, user, authors, since, to);
        }


        public WPost[] GetIterationTimeline(string username, string password, long since, long to)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var hiddenCollection = db.Hiddens();
            var dynamicFriendCollection = db.DynamicFriends();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return new WPost[0];
            //  ILog log = LogManager.GetLogger("PanelLogger");
            // log.Info(user.id + ",IT");
            Stopwatch w1 = Stopwatch.StartNew();
            List<int> hiddenAuthors = hiddenCollection.AsQueryable().Where(h => h.user._id == user._id && h.timeline == HiddenType.Dynamic.ToString()).Select(h => h.friend._id).ToList();
            w1.Stop();
            ILog log1 = LogManager.GetLogger("QueryLogger");
            log1.Info(" Elapsed time: " + w1.Elapsed + ", user id: " + user._id + ", timeline: " + HiddenType.Dynamic.ToString() + ", select all dynamic friends hidden by an user in the iteration timeline");
            Stopwatch w2 = Stopwatch.StartNew();
            List<int> authors = dynamicFriendCollection.AsQueryable().Where(f => f.chosenFeature.user._id == user._id && !hiddenAuthors.Contains(f.user._id)).Select(f => f.user._id).ToList();
            w2.Stop();
            ILog log2 = LogManager.GetLogger("QueryLogger");
            log2.Info(" Elapsed time: " + w2.Elapsed + ", user id: " + user._id + ", select all users whose posts can appear in the iteration timeline of an user");
            WPost[] timeline = GetTimeline(db, user, authors, since, to);
            new Thread(thread => UpdateDynamicFriend(user)).Start();
            return timeline;
        }

        private String GetFriendString(int userId, Dictionary<int, HashSet<int>> friends)
        {
            String friendsString = "";
            foreach (KeyValuePair<int, HashSet<int>> friend in friends)
            {
                if (friend.Key == userId)
                    continue;

                friendsString += friend.Key + "[";
                String servicesString = "";
                foreach (int service in friend.Value)
                {
                    servicesString += service + ";";
                }
                if (servicesString.Length > 0)
                    servicesString = servicesString.Substring(0, servicesString.Length - 1);
                friendsString += servicesString + "];";
            }
            if (friendsString.Length > 0)
                friendsString = friendsString.Substring(0, friendsString.Length - 1);
            return friendsString;
        }

        private void UpdateDynamicFriend(User user)
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var chosenFeatureCollection = db.ChosenFeatures();
            var registrationCollection = db.Registrations();
            var dynamicFriendCollection = db.DynamicFriends();
            Dictionary<int, HashSet<int>> logFriends = new Dictionary<int, HashSet<int>>();
            bool needToLog = false;
            Stopwatch w = Stopwatch.StartNew();
            List<ChosenFeature> cFeature = chosenFeatureCollection.AsQueryable().Where(cf => cf.user._id == user._id && cf.feature == FeaturesType.IterationNetwork.ToString()).ToListAsync().Result;
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", user id: " + user._id + ", feature's name: " + FeaturesType.IterationNetwork.ToString() + ", select all chosen feature of an author and his feature 'Iteration Network'");
            foreach (ChosenFeature chosenFeature in cFeature)
            {
                var filter = Builders<ChosenFeature>.Filter.Eq("_id", chosenFeature._id);
                Stopwatch w1 = Stopwatch.StartNew();
                ChosenFeature temp = chosenFeatureCollection.Find(filter).SingleAsync().Result;
                w1.Stop();
                ILog log1 = LogManager.GetLogger("QueryLogger");
                log1.Info(" Elapsed time: " + w1.Elapsed + ", chosen feature's id: " + chosenFeature._id + ", select a chosen feature associated to the dynamic friend to update");
                if (temp.lastDownload > DateTime.UtcNow - _dynamicSpan)
                    continue;
                else
                {
                    var update = Builders<ChosenFeature>.Update
                            .Set("lastDownload", DateTime.UtcNow);
                    Stopwatch w3 = Stopwatch.StartNew();
                    var result = chosenFeatureCollection.UpdateOneAsync(filter, update).Result;
                    w3.Stop();
                    ILog log3 = LogManager.GetLogger("QueryLogger");
                    log3.Info(" Elapsed time: " + w3.Elapsed + ", update last download of the chosen feature(dynamic friend)");
                }
                needToLog = true;
                IService service;
                //submit new friendship for the current chosen feature
                var builders = Builders<Registration>.Filter;
                var filter1 = builders.Eq("user._id", temp.user._id) & builders.Eq("serviceInstance._id", temp.serviceInstance._id);
                Stopwatch w7 = Stopwatch.StartNew();
                Registration regist = registrationCollection.Find(filter1).SingleAsync().Result;
                w7.Stop();
                ILog log7 = LogManager.GetLogger("QueryLogger");
                log7.Info(" Elapsed time: " + w7.Elapsed + ", select the registration from the chosen feature(dynamic friend)");
                if (regist.serviceInstance.service.name.Equals("GitHub"))
                {
                    service = ServiceFactory.getServiceOauth(regist.serviceInstance.service.name, regist.serviceInstance.host, regist.serviceInstance.consumerKey, regist.serviceInstance.consumerSecret, regist.accessToken, null);   
                }
                else
                {
                    service = ServiceFactory.getService(
                        regist.serviceInstance.service.name,
                        regist.nameOnService,
                       EncDecRc4("key", regist.accessToken),
                        regist.accessSecret,
                        regist.serviceInstance.host);
                }
                //this line must be before the deleting
                String[] dynamicFriends = (String[])service.Get(FeaturesType.IterationNetwork, null);
                //delete old friendship for the current chosen feature
                var filter2 = Builders<DynamicFriend>.Filter.Eq("chosenFeature._id", temp._id);
                Stopwatch w4 = Stopwatch.StartNew();
                dynamicFriendCollection.DeleteManyAsync(filter2);
                w4.Stop();
                ILog log4 = LogManager.GetLogger("QueryLogger");
                log4.Info(" Elapsed time: " + w4.Elapsed + ", chosen feature's id: " + temp._id + ", delete old friendship for the current chosen feature");
                foreach (String dynamicFriend in dynamicFriends)
                {
                    Stopwatch w5 = Stopwatch.StartNew();
                    IEnumerable<User> friendsInDb = registrationCollection.AsQueryable().Where(r => r.nameOnService == dynamicFriend && r.serviceInstance._id == temp.serviceInstance._id).Select(r => r.user);
                    w5.Stop();
                    ILog log5 = LogManager.GetLogger("QueryLogger");
                    log5.Info(" Elapsed time: " + w5.Elapsed + ", : dynamic friend" + dynamicFriend + ", service instance: " + temp.serviceInstance._id + ", select user to add as dynamic friend");
                    foreach (User friendInDb in friendsInDb)
                    {
                        DynamicFriend dynFriend = new DynamicFriend()
                        {
                            chosenFeature = temp,
                            user = friendInDb
                        };
                        Stopwatch w6 = Stopwatch.StartNew();
                        dynamicFriendCollection.InsertOneAsync(dynFriend);
                        w6.Stop();
                        ILog log6 = LogManager.GetLogger("QueryLogger");
                        log6.Info(" Elapsed time: " + w6.Elapsed + ", chosen feature's id: " + temp._id + ", friend id: " + friendInDb._id + ", insert a new dynamic friend");
                        if (!logFriends.ContainsKey(friendInDb._id))
                            logFriends[friendInDb._id] = new HashSet<int>();
                        logFriends[friendInDb._id].Add(regist.serviceInstance._id);
                    }
                }
            }
            if (needToLog)
            {
                //    ILog log8 = LogManager.GetLogger("NetworkLogger");
                //  log8.Info(user.id + ",I,[" + GetFriendString(user.id, logFriends) + "]");
            }
        }

        public WPost[] GetInteractiveTimeline(string username, string password, string collectionUri, string interactiveObject, string objectType, long since, long to)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var hiddenCollection = db.Hiddens();
            var interactiveFriendCollection = db.InteractiveFriends();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return new WPost[0];
            // ILog log = LogManager.GetLogger("PanelLogger");
            //log.Info(user.id + ",JT");
            if (String.IsNullOrEmpty(collectionUri))
            {
                collectionUri = FindGithubRepository(user, interactiveObject);
            }
            else if (collectionUri.Contains("git://"))
            {  //in this case the interactive object is an github issue
                GetUsersIssuesInvolved(user, collectionUri, interactiveObject);
            }
            Stopwatch w1 = Stopwatch.StartNew();
            List<int> hiddenAuthors = hiddenCollection.AsQueryable().Where(h => h.user._id == user._id && h.timeline == HiddenType.Interactive.ToString()).Select(h => h.friend._id).ToList();
            w1.Stop();
            ILog log1 = LogManager.GetLogger("QueryLogger");
            log1.Info(" Elapsed time: " + w1.Elapsed + ", user id: " + user._id + ", timeline: " + HiddenType.Interactive.ToString() + ", select all friends hidden by an user in the interactive timeline");
            Stopwatch w2 = Stopwatch.StartNew();
            List<int> authors = interactiveFriendCollection.AsQueryable().Where(f => f.collection == collectionUri && f.interactiveObject.EndsWith(interactiveObject) && f.objectType == objectType && !hiddenAuthors.Contains(f.user._id)).Select(f => f.user._id).ToList();        
            w2.Stop();
            ILog log2 = LogManager.GetLogger("QueryLogger");
            log2.Info(" Elapsed time: " + w2.Elapsed + ", collection of projects: " + collectionUri + ", interactive object: " + interactiveObject + ", object type: " + objectType + ", select all users whose posts can appear in the interactive timeline of an user");
            WPost[] timeline = GetTimeline(db, user, authors, since, to);
            new Thread(thread => UpdateInteractiveFriend(user)).Start();
            //  log = LogManager.GetLogger("NetworkLogger");
            String authorsString = "";
            foreach (int author in authors)
            {
                if (author == user._id)
                    continue;
                authorsString += author + ";";
            }
            if (authorsString.Length > 0)
                authorsString = authorsString.Substring(0, authorsString.Length - 1);
            //   log.Info(user.id + ",J,[" + authorsString + "]," + collectionUri + "," + objectType + "," + interactiveObject);
            return timeline;
        }

        private void GetUsersIssuesInvolved(User user, string collectionUri, string issueId)
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var chosenFeatureCollection = db.ChosenFeatures();
            var registrationCollection = db.Registrations();
            var interactiveFriendCollection = db.InteractiveFriends();
            Boolean flag = false;
            IService service = null;
            ChosenFeature temp = null;
            Stopwatch w = Stopwatch.StartNew();
            List<ChosenFeature> cFeatures = chosenFeatureCollection.AsQueryable().Where(cf => cf.user._id == user._id && cf.feature == FeaturesType.InteractiveNetwork.ToString()).ToListAsync().Result;
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", user id: " + user._id + ", feature: " + FeaturesType.InteractiveNetwork.ToString() + ", select all chosen features of an user with feature 'InteractiveNetwork'");
            foreach (ChosenFeature chosenFeature in cFeatures)
            {
                var filter = Builders<ChosenFeature>.Filter.Eq("_id", chosenFeature._id);
                Stopwatch w1 = Stopwatch.StartNew();
                temp = chosenFeatureCollection.Find(filter).SingleAsync().Result;
                w1.Stop();
                ILog log1 = LogManager.GetLogger("QueryLogger");
                log1.Info(" Elapsed time: " + w1.Elapsed + ", chosen feature's id: " + chosenFeature._id + ", select a chosen feature to get users' issues involved");
                if (temp.serviceInstance.service.name.Equals("GitHub"))
                {
                    var builders = Builders<Registration>.Filter;
                    var filter1 = builders.Eq("serviceInstance._id", temp.serviceInstance._id) & builders.Eq("user._id", temp.user._id);
                    Stopwatch w10 = Stopwatch.StartNew();
                    var registration = registrationCollection.Find(filter1).SingleAsync().Result;
                    w10.Stop();
                    ILog log10 = LogManager.GetLogger("QueryLogger");
                    log10.Info(" Elapsed time: " + w10.Elapsed + ", chosen feature's id: " + chosenFeature._id + ", select the registration from the chosen feature(getusersissuesinvoled)");
                    service = ServiceFactory.getServiceOauth(registration.serviceInstance.service.name, registration.serviceInstance.host, registration.serviceInstance.consumerKey, registration.serviceInstance.consumerSecret, registration.accessToken, null);
                    flag = true;
                }
            }
            if (flag)
            {
                //obtaining users involved in the issue
                String[] users = (String[])service.Get(FeaturesType.UsersIssuesInvolved, new Object[2] { collectionUri, issueId });
                SWorkItem workitem = new SWorkItem()
                {
                    Name = issueId,
                    InvolvedUsers = users
                };
                var builders = Builders<InteractiveFriend>.Filter;
                var filter2 = builders.Eq("chosenFeature._id", temp._id) & builders.Eq("objectType", "WorkItem");
                Stopwatch w2 = Stopwatch.StartNew();
                interactiveFriendCollection.DeleteManyAsync(filter2);
                w2.Stop();
                ILog log2 = LogManager.GetLogger("QueryLogger");
                log2.Info(" Elapsed time: " + w2.Elapsed + ", chosen feature's id: " + temp._id + ", delete all interactive friends according to the feature and objecttype 'WorkItem'");
                Stopwatch w3 = Stopwatch.StartNew();
                IEnumerable<User> friendsInDb = registrationCollection.AsQueryable().Where(r => workitem.InvolvedUsers.Contains(r.nameOnService) || workitem.InvolvedUsers.Contains(r.accessSecret + "\\" + r.nameOnService)).Select(r => r.user);
                w3.Stop();
                ILog log3 = LogManager.GetLogger("QueryLogger");
                log3.Info(" Elapsed time: " + w3.Elapsed + ", select all users that are working on the same workitem(GetUsersIssuesInvolved)");
                foreach (User friendInDb in friendsInDb)
                {
                    InteractiveFriend interactiveFriend = new InteractiveFriend()
                    {
                        user = friendInDb,
                        chosenFeature = temp,
                        collection = collectionUri,
                        interactiveObject = workitem.Name,
                        objectType = "WorkItem"
                    };
                    Stopwatch w4 = Stopwatch.StartNew();
                    interactiveFriendCollection.InsertOneAsync(interactiveFriend);
                    w4.Stop();
                    ILog log4 = LogManager.GetLogger("QueryLogger");
                    log4.Info(" Elapsed time: " + w4.Elapsed + ", user id: " + friendInDb._id + ", chosen feature: " + temp._id + ", collection uri: " + collectionUri + ", interactive object: " + workitem.Name + ", insert an interactive friend which is working on a workitem");
                }
            }
        }

        private string FindGithubRepository(User user, string interactiveObject)
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var chosenFeatureCollection = db.ChosenFeatures();
            var registrationCollection = db.Registrations();
            var serviceInstanceCollection = db.ServiceInstances();
            var serviceCollection = db.Services();
            Boolean flag = false;
            IService service = null;
            var builders = Builders<ChosenFeature>.Filter;
            var filter = builders.Eq("user._id", user._id) & builders.Eq("feature", FeaturesType.InteractiveNetwork.ToString());
            Stopwatch w = Stopwatch.StartNew();
            List<ChosenFeature> cFeature = chosenFeatureCollection.Find(filter).ToListAsync().Result;
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", user id: " + user._id + ", feature's name: " + FeaturesType.InteractiveNetwork.ToString() + ", select all chosen features of an user and his feature 'interactive network'");
            foreach (ChosenFeature chosenFeature in cFeature)
            {
                Stopwatch w1 = Stopwatch.StartNew();
                ChosenFeature temp = chosenFeatureCollection.AsQueryable().Where(cf => cf._id == chosenFeature._id).Single();
                w1.Stop();
                ILog log1 = LogManager.GetLogger("QueryLogger");
                log1.Info(" Elapsed time: " + w1.Elapsed + ", chosen feature's id: " + chosenFeature._id + ", select chosen feature's id");
                var builders1 = Builders<Registration>.Filter;
                var filter1 = builders1.Eq("serviceInstance._id", temp.serviceInstance._id) & builders1.Eq("user._id", temp.user._id);
                Stopwatch w3 = Stopwatch.StartNew();
                var registration = registrationCollection.Find(filter1).SingleAsync().Result;
                w3.Stop();
                ILog log3 = LogManager.GetLogger("QueryLogger");
                log3.Info(" Elapsed time: " + w3.Elapsed + ", chosen feature's id: " + chosenFeature._id + ", select the registration from the chosen feature(findgithubrepository)");
                if (registration.serviceInstance.service.name.Equals("GitHub"))
                {
                    service = ServiceFactory.getServiceOauth(registration.serviceInstance.service.name, registration.serviceInstance.host, registration.serviceInstance.consumerKey, registration.serviceInstance.consumerSecret, registration.accessToken, null);
                    flag = true;
                }
            }
            if (flag)
            {
                return (String)service.Get(FeaturesType.Repository, new Object[1] { interactiveObject });
            }
            else
            {
                return string.Empty;
            }
        }

        private void UpdateInteractiveFriend(User user)
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var chosenFeatureCollection = db.ChosenFeatures();
            var registrationCollection = db.Registrations();
            var interactiveFriendCollection = db.InteractiveFriends();
            Stopwatch w = Stopwatch.StartNew();
            List<ChosenFeature> cFeature = chosenFeatureCollection.AsQueryable().Where(cf => cf.user._id == user._id && cf.feature == FeaturesType.InteractiveNetwork.ToString()).ToListAsync().Result;
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", user id: " + user._id + ", feature's name: " + FeaturesType.InteractiveNetwork.ToString() + ", select all chosen features of an author and his feature 'interactive network'");
            foreach (ChosenFeature chosenFeature in cFeature)
            {
                var filter = Builders<ChosenFeature>.Filter.Eq("_id", chosenFeature._id);
                Stopwatch w2 = Stopwatch.StartNew();
                ChosenFeature temp = chosenFeatureCollection.Find(filter).SingleAsync().Result;
                w2.Stop();
                ILog log2 = LogManager.GetLogger("QueryLogger");
                log2.Info(" Elapsed time: " + w2.Elapsed + ", chosen feature's id: " + chosenFeature._id + ", select a chosen feature");
                if (temp.lastDownload > DateTime.UtcNow - _interactiveSpan)
                    continue;
                else
                {
                    var update = Builders<ChosenFeature>.Update
                            .Set("lastDownload", DateTime.UtcNow);
                    Stopwatch w7 = Stopwatch.StartNew();
                    var result = chosenFeatureCollection.UpdateOneAsync(filter, update).Result;
                    w7.Stop();
                    ILog log7 = LogManager.GetLogger("QueryLogger");
                    log7.Info(" Elapsed time: " + w7.Elapsed + ", chosen feature's id: " + chosenFeature._id + ", update lastDownload of the chosen feature(interactive friend)");
                }
                IService service;
                var builders = Builders<Registration>.Filter;
                var filter1 = builders.Eq("user._id", temp.user._id) & builders.Eq("serviceInstance._id", temp.serviceInstance._id);
                Stopwatch w8 = Stopwatch.StartNew();
                Registration regist = registrationCollection.Find(filter1).SingleAsync().Result;
                w8.Stop();
                ILog log8 = LogManager.GetLogger("QueryLogger");
                log8.Info(" Elapsed time: " + w8.Elapsed + ", chosen feature's id: " + chosenFeature._id + ", select the registration from the chosen feature(updateinteractivefriend)");
                if (regist.serviceInstance.service.name.Equals("GitHub"))
                {
                    service = ServiceFactory.getServiceOauth(regist.serviceInstance.service.name, regist.serviceInstance.host, regist.serviceInstance.consumerKey, regist.serviceInstance.consumerSecret, regist.accessToken, null);  
                }
                else
                {
                    //submit new friendship for the current chosen feature
                    service = ServiceFactory.getService(
                      regist.serviceInstance.service.name,
                       regist.nameOnService,
                       EncDecRc4("key", regist.accessToken),
                       regist.accessSecret,
                       regist.serviceInstance.host);
                }
                //this line must be before the deleting
                SCollection[] collections = (SCollection[])service.Get(FeaturesType.InteractiveNetwork);
                try
                {
                    //vecchio metodo
                    var filter2 = Builders<InteractiveFriend>.Filter.Eq("chosenFeature._id", temp._id);
                    Stopwatch w3 = Stopwatch.StartNew();
                    interactiveFriendCollection.DeleteManyAsync(filter2);
                    w3.Stop();
                    ILog log3 = LogManager.GetLogger("QueryLogger");
                    log3.Info(" Elapsed time: " + w3.Elapsed + ", chosen feature's id: " + temp._id + ", remove all old interactive friends according to the chosen feature");
                    foreach (SCollection collection in collections)
                    {
                        foreach (SWorkItem workitem in collection.WorkItems)
                        {

                            Stopwatch w4 = Stopwatch.StartNew();
                            IEnumerable<User> friendsInDb = registrationCollection.AsQueryable().Where(r => workitem.InvolvedUsers.Contains(r.nameOnService) /*|| workitem.InvolvedUsers.Contains(r.accessSecret + "\\" + r.nameOnService)*/).Select(r => r.user);   // problemi con il path
                            w4.Stop();
                            ILog log4 = LogManager.GetLogger("QueryLogger");
                            log4.Info(" Elapsed time: " + w4.Elapsed + ", select all users that are working on the same workitem");
                            foreach (User friendInDb in friendsInDb)
                            {
                                InteractiveFriend intFriend = new InteractiveFriend()
                                {
                                    user = friendInDb,
                                    chosenFeature = temp,
                                    collection = collection.Uri,
                                    interactiveObject = workitem.Name,
                                    objectType = "WorkItem"
                                };
                                Stopwatch w5 = Stopwatch.StartNew();
                                interactiveFriendCollection.InsertOneAsync(intFriend);
                                w5.Stop();
                                ILog log5 = LogManager.GetLogger("QueryLogger");
                                log5.Info(" Elapsed time: " + w5.Elapsed + ", user id: " + friendInDb._id + ", chosen feature: " + temp._id + ", collection uri: " + collection.Uri + ", interactive object: " + workitem.Name + ", insert an interactive friend which is working on a workitem in a pending state");
                            }
                        }
                        foreach (SFile file in collection.Files)
                        {
                            Stopwatch w6 = Stopwatch.StartNew();
                            IEnumerable<User> friendsInDb = registrationCollection.AsQueryable().Where(r => file.InvolvedUsers.Contains(r.nameOnService) /*|| file.InvolvedUsers.Contains(r.accessSecret + "\\" + r.nameOnService)*/).Select(r => r.user);     //   problemi con il path
                            w6.Stop();
                            ILog log6 = LogManager.GetLogger("QueryLogger");
                            log6.Info(" Elapsed time: " + w6.Elapsed + ", select all users that are working on the same file");
                            foreach (User friendInDb in friendsInDb)
                            {
                                InteractiveFriend intFriend1 = new InteractiveFriend()
                                {
                                    user = friendInDb,
                                    chosenFeature = temp,
                                    collection = collection.Uri,
                                    interactiveObject = file.Name,
                                    objectType = "File"
                                };
                                Stopwatch w7 = Stopwatch.StartNew();
                                interactiveFriendCollection.InsertOneAsync(intFriend1);
                                w7.Stop();
                                ILog log7 = LogManager.GetLogger("QueryLogger");
                                log7.Info(" Elapsed time: " + w7.Elapsed + ", user id: " + friendInDb._id + ", chosen feature: " + temp._id + ", collection uri: " + collection.Uri + ", interactive object: " + file.Name + ", insert an interactive friend which is working on a file in a pending state");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.StackTrace);
                }
            }
        }

        public static string Encrypt(string input)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] bs = System.Text.Encoding.UTF8.GetBytes(input);
            bs = x.ComputeHash(bs);
            System.Text.StringBuilder s = new System.Text.StringBuilder();
            foreach (byte b in bs)
            {
                s.Append(b.ToString("x2").ToLower());
            }
            string hash = s.ToString();
            return hash;
        }

        private WPost[] GetTimeline(ConnectorDataContext db, User user, List<int> authors, long since, long to)
        {
            var postCollection = db.Posts();
            var staticFriendCollection = db.StaticFriends();
            List<WPost> result = new List<WPost>();
            try
            {
                if (since == 0 && to != 0)
                {
                    //checked if we have enought older posts
                    Stopwatch w = Stopwatch.StartNew();
                    DateTime lastPostDate = postCollection.AsQueryable().Where(tp => tp._id == to).Select(tp => tp.createAt).Single();
                    w.Stop();
                    ILog log = LogManager.GetLogger("QueryLogger");
                    log.Info(" Elapsed time: " + w.Elapsed + ", post id: " + to + ", select last post");
                    Stopwatch w1 = Stopwatch.StartNew();
                    var oldPosts = postCollection.AsQueryable().Where(p => p.createAt < lastPostDate && authors.Contains(p.chosenFeature.user._id)).ToListAsync().Result;
                    int olderPostCounter = oldPosts.ToArray().Length;
                    w1.Stop();
                    ILog log1 = LogManager.GetLogger("QueryLogger");
                    log1.Info(" Elapsed time: " + w1.Elapsed + ", number of posts before a certain post written by an user using a certain service");
                    if (olderPostCounter < postLimit)
                    {
                        foreach (int item in authors)
                            DownloadOlderPost(item);
                    }
                }
                else
                {
                    new Thread(delegate()
                        {
                            foreach (int item in authors)
                                DownloadNewerPost(item);
                        }).Start();
                }
            }
            catch (Exception)
            {
                return result.ToArray();
            }
            IEnumerable<Post> posts = new List<Post>();
            if (since == 0 && to == 0)
            {
                Stopwatch w2 = Stopwatch.StartNew();
                posts = postCollection.AsQueryable().Where(p => authors.Contains(p.chosenFeature.user._id)).OrderByDescending(p => p.createAt).Take(postLimit);
                w2.Stop();
                ILog log2 = LogManager.GetLogger("QueryLogger");
                log2.Info(" Elapsed time: " + w2.Elapsed + ", select top posts of an user");
            }
            else if (since != 0)
            {
                DateTime sin = postCollection.AsQueryable().Where(sp => sp._id == since).Select(sp => sp.createAt).Single();
                Stopwatch w3 = Stopwatch.StartNew();
                posts = postCollection.AsQueryable().Where(p => authors.Contains(p.chosenFeature.user._id) && p.createAt > sin).OrderByDescending(p => p.createAt).Take(postLimit);
                w3.Stop();
                ILog log3 = LogManager.GetLogger("QueryLogger");
                log3.Info(" Elapsed time: " + w3.Elapsed + ", post id: " + since + ", select top posts of an user chronologically written after a certain post");
            }
            else
            {
                var maxDate = postCollection.AsQueryable().Where(tp => tp._id == to).Single().createAt;
                Stopwatch w4 = Stopwatch.StartNew();
                posts = postCollection.AsQueryable().Where(p => authors.Contains(p.chosenFeature.user._id) && p.createAt < maxDate).OrderByDescending(p => p.createAt).Take(postLimit);
                w4.Stop();
                ILog log4 = LogManager.GetLogger("QueryLogger");
                log4.Info(" Elapsed time: " + w4.Elapsed + ", post id: " + to + ", select top posts of an user chronologically written before a certain post");
            }
            Stopwatch w5 = Stopwatch.StartNew();
            IEnumerable<int> followings = staticFriendCollection.AsQueryable().Where(f => f.user._id == user._id).Select(f => f.friend._id);
            w5.Stop();
            ILog log5 = LogManager.GetLogger("QueryLogger");
            log5.Info(" Elapsed time: " + w5.Elapsed + ", user id: " + user._id + ", select all static friends that follow that user");
            foreach (Post post in posts)
            {
                result.Add(Converter.PostToWPost(db, user, post));
            }
            return result.ToArray();
        }

        public bool Post(String username, String password, String message)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            Contract.Requires(!String.IsNullOrEmpty(message));
            ConnectorDataContext db = new ConnectorDataContext();
            var serviceInstanceCollection = db.ServiceInstances();
            var chosenFeatureCollection = db.ChosenFeatures();
            var registrationCollection = db.Registrations();
            var postCollection = db.Posts();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return false;
            //  ILog log = LogManager.GetLogger("PanelLogger");
            // log.Info(user.id + ",P");
            Stopwatch w1 = Stopwatch.StartNew();
            int service = serviceInstanceCollection.AsQueryable().Where(si => si.service.name == "SocialTFS").Single()._id;
            w1.Stop();
            ILog log1 = LogManager.GetLogger("QueryLogger");
            log1.Info(" Elapsed time: " + w1.Elapsed + ", select service instance's id of the service 'SocialTFS'");
            ChosenFeature chosenFeature;
            try
            {
                var builders = Builders<ChosenFeature>.Filter;
                var filter = builders.Eq("user._id", user._id) & builders.Eq("serviceInstance._id", service) & builders.Eq("feature", FeaturesType.Post.ToString());
                Stopwatch w2 = Stopwatch.StartNew();
                chosenFeature = chosenFeatureCollection.Find(filter).FirstAsync().Result;
                w2.Stop();
                ILog log2 = LogManager.GetLogger("QueryLogger");
                log2.Info(" Elapsed time: " + w2.Elapsed + ", user id: " + user._id + ", service instance: " + service + ", feature's name: " + FeaturesType.Post.ToString() + ", select chosen feature's id");
            }
            catch (Exception)
            {
                try
                {
                    Stopwatch w3 = Stopwatch.StartNew();
                    registrationCollection.AsQueryable().Where(r => r.user._id == user._id && r.serviceInstance._id == service).Single();
                    w3.Stop();
                    ILog log3 = LogManager.GetLogger("QueryLogger");
                    log3.Info(" Elapsed time: " + w3.Elapsed + ", user id: " + user._id + ", service instance: " + service + ", select registration of a service");
                }
                catch
                {
                    Registration registration = new Registration()
                    {
                        user = user,
                        serviceInstance = serviceInstanceCollection.AsQueryable().Where(si => si.service.name == "SocialTFS").Single(),   //considerata poco sopra per il log
                        nameOnService = username,
                        idOnService = username
                    };
                    Stopwatch w4 = Stopwatch.StartNew();
                    registrationCollection.InsertOneAsync(registration);
                    w4.Stop();
                    ILog log4 = LogManager.GetLogger("QueryLogger");
                    log4.Info(" Elapsed time: " + w4.Elapsed + ", insert a new registration of a service");
                }
                ChosenFeature newChoseFeature = new ChosenFeature()
                {
                    _id = GenerateChosenFeatureId(db),
                    user = registrationCollection.AsQueryable().Where(r => r.user._id == user._id && r.serviceInstance._id == service).Single().user,
                    serviceInstance = registrationCollection.AsQueryable().Where(r => r.user._id == user._id && r.serviceInstance._id == service).Single().serviceInstance,
                    feature = FeaturesType.Post.ToString(),
                    lastDownload = new DateTime(1900, 1, 1)
                };
                Stopwatch w5 = Stopwatch.StartNew();
                chosenFeatureCollection.InsertOneAsync(newChoseFeature);
                w5.Stop();
                ILog log5 = LogManager.GetLogger("QueryLogger");
                log5.Info(" Elapsed time: " + w5.Elapsed + ", feature's name: " + FeaturesType.Post.ToString() + ", last download: " + new DateTime(1900, 1, 1) + ", insert a new chosen feature to save a post");
                chosenFeature = newChoseFeature;
            }
            Post pst = new Post()
            {
                _id = GeneratePostId(db),
                chosenFeature = chosenFeature,
                message = message,
                createAt = DateTime.UtcNow
            };
            Stopwatch w6 = Stopwatch.StartNew();
            postCollection.InsertOneAsync(pst);
            w6.Stop();
            ILog log6 = LogManager.GetLogger("QueryLogger");
            log6.Info(" Elapsed time: " + w6.Elapsed + ", message: " + message + ", date time: " + DateTime.UtcNow + ", insert the post");
            return true;
        }

        private User CheckCredentials(ConnectorDataContext db, String username, String password)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            var collection = db.Users();
            try
            {
                var builder = Builders<User>.Filter;
                var filter = builder.Eq("username", username) & builder.Eq("password", Encrypt(password)) & builder.Eq("active", true);
                Stopwatch w = Stopwatch.StartNew();
                User user = collection.Find(filter).SingleAsync().Result;
                w.Stop();
                ILog log = LogManager.GetLogger("QueryLogger");
                log.Info(" Elapsed time: " + w.Elapsed + ", username: " + username + ", check credentials");
                return user;
            }
            catch (AggregateException)
            {
                return null;
            }
        }

        private void DownloadNewerPost(int author)
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var chosenFeatureCollection = db.ChosenFeatures();
            var registrationCollection = db.Registrations();
            var postCollection = db.Posts();
            Stopwatch w = Stopwatch.StartNew();

            List<ChosenFeature> chosenFeatures = chosenFeatureCollection.AsQueryable().Where(cf => cf.user._id == author && cf.feature == FeaturesType.UserTimeline.ToString()).ToListAsync().Result;
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", user id: " + author + ", feature's name: " + FeaturesType.UserTimeline.ToString() + ", select all chosen features of an author and his feature 'user timeline'(new post)");
            foreach (ChosenFeature item in chosenFeatures)
            {
                Stopwatch w1 = Stopwatch.StartNew();
                ChosenFeature cfTemp = chosenFeatureCollection.AsQueryable().Where(cf => cf._id == item._id).SingleAsync().Result;
                w1.Stop();
                ILog log1 = LogManager.GetLogger("QueryLogger");
                log1.Info(" Elapsed time: " + w1.Elapsed + ", chosen feature's id: " + item._id + ", select a chosen feature");
                if (cfTemp.lastDownload >= DateTime.UtcNow - _postSpan)
                    continue;
                else
                {
                    //   faccio l'update
                    var filter = Builders<ChosenFeature>.Filter.Eq("_id", cfTemp._id);
                    var update = Builders<ChosenFeature>.Update
                            .Set("lastDownload", DateTime.UtcNow);
                    Stopwatch w6 = Stopwatch.StartNew();
                    var result = chosenFeatureCollection.UpdateOneAsync(filter, update).Result;
                    w6.Stop();
                    ILog log6 = LogManager.GetLogger("QueryLogger");
                    log6.Info(" Elapsed time: " + w6.Elapsed + ", chosen feature's id: " + item._id + ", update lastDownload of the chosen feature(downloadnewerpost");
                }
                long sinceId;
                DateTime sinceDate = new DateTime();
                try
                {
                    Stopwatch w2 = Stopwatch.StartNew();
                    Post sincePost = postCollection.AsQueryable().Where(p => p.chosenFeature._id == cfTemp._id).OrderByDescending(p => p.createAt).FirstAsync().Result;
                    w2.Stop();
                    ILog log2 = LogManager.GetLogger("QueryLogger");
                    log2.Info(" Elapsed time: " + w2.Elapsed + ", chosen feature's id: " + cfTemp._id + ", select the most recent post");
                    sinceId = sincePost.idOnService;
                    sinceDate = sincePost.createAt;
                }
                catch (Exception)
                {
                    sinceId = 0;
                }
                var build = Builders<Registration>.Filter;
                var filt = build.Eq("user._id", cfTemp.user._id) & build.Eq("serviceInstance._id", cfTemp.serviceInstance._id);
                Stopwatch w5 = Stopwatch.StartNew();
                Registration regist = registrationCollection.Find(filt).SingleAsync().Result;
                w5.Stop();
                ILog log5 = LogManager.GetLogger("QueryLogger");
                log5.Info(" Elapsed time: " + w5.Elapsed + ", select the registration from the chosen feature(downloadnewerpost");
                IService service = ServiceFactory.getServiceOauth(regist.serviceInstance.service.name, regist.serviceInstance.host, regist.serviceInstance.consumerKey, regist.serviceInstance.consumerSecret, regist.accessToken, regist.accessSecret);
                IPost[] timeline = new IPost[0];
                if (service.Name.Equals("Facebook"))
                {
                    timeline = (IPost[])service.Get(FeaturesType.UserTimeline, long.Parse(regist.idOnService), sinceId, sinceDate);
                }
                else
                {
                    timeline = (IPost[])service.Get(FeaturesType.UserTimeline, int.Parse(regist.idOnService), sinceId, sinceDate);
                }
                Stopwatch w3 = Stopwatch.StartNew();
                IEnumerable<long> postInDb = postCollection.AsQueryable().Where(p => p.chosenFeature._id == item._id).Select(p => p.idOnService).ToList();
                w3.Stop();
                ILog log3 = LogManager.GetLogger("QueryLogger");
                log3.Info(" Elapsed time: " + w3.Elapsed + ", chosen feature's id: " + item._id + ", select id on service of the newer post");
                if (timeline != null)
                    foreach (IPost post in timeline)
                    {
                        if (!postInDb.Contains(post.Id))
                        {
                            Post pst = new Post
                            {
                                _id = GeneratePostId(db),
                                chosenFeature = cfTemp,
                                idOnService = post.Id,
                                message = post.Text,
                                createAt = post.CreatedAt
                            };
                            Stopwatch w4 = Stopwatch.StartNew();
                            postCollection.InsertOneAsync(pst);
                            w4.Stop();
                            ILog log4 = LogManager.GetLogger("QueryLogger");
                            log4.Info(" Elapsed time: " + w4.Elapsed + ", chosen feature's id: " + cfTemp._id + ", id on service: " + post.Id + ", message: " + post.Text + ", date time of creation: " + post.CreatedAt + ", insert the newer post");
                        }
                    }
            }
        }

        private void DownloadOlderPost(int author)
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var chosenFeatureCollection = db.ChosenFeatures();
            var postCollection = db.Posts();
            var registrationCollection = db.Registrations();
            Stopwatch w = Stopwatch.StartNew();
            List<ChosenFeature> chosenFeatures = chosenFeatureCollection.AsQueryable().Where(cf => cf.user._id == author && cf.feature == FeaturesType.UserTimeline.ToString()).ToListAsync().Result;
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", user id: " + author + ", feature's name: " + FeaturesType.UserTimeline.ToString() + ", select all chosen features of an author and his feature 'user timeline'(old post)");
            foreach (ChosenFeature item in chosenFeatures)
            {
                long maxId;
                DateTime maxDate = new DateTime();
                try
                {
                    Stopwatch w1 = Stopwatch.StartNew();
                    Post maxPost = postCollection.AsQueryable().Where(p => p.chosenFeature._id == item._id).OrderBy(p => p.createAt).First();
                    w1.Stop();
                    ILog log1 = LogManager.GetLogger("QueryLogger");
                    log1.Info(" Elapsed time: " + w1.Elapsed + ", chosen feature's id: " + item._id + ", select the oldest post");
                    maxId = maxPost.idOnService;
                    maxDate = maxPost.createAt;
                }
                catch (AggregateException)
                {
                    maxId = 0;
                }
                var build = Builders<Registration>.Filter;
                var filt = build.Eq("user._id", item.user._id) & build.Eq("serviceInstance._id", item.serviceInstance._id);
                Stopwatch w6 = Stopwatch.StartNew();
                Registration regist = registrationCollection.Find(filt).SingleAsync().Result;
                w6.Stop();
                ILog log6 = LogManager.GetLogger("QueryLogger");
                log6.Info(" Elapsed time: " + w6.Elapsed + ", select the registration from the chosen feature(downloadolderpost");
                IService service = ServiceFactory.getServiceOauth(
                    regist.serviceInstance.service.name,
                    regist.serviceInstance.host,
                    regist.serviceInstance.consumerKey,
                    regist.serviceInstance.consumerSecret,
                    regist.accessToken,
                    regist.accessSecret);
                IPost[] timeline = null;
                if (service.Name.Equals("Facebook"))
                {
                    timeline = (IPost[])service.Get(FeaturesType.UserTimelineOlderPosts, long.Parse(regist.idOnService), maxId, maxDate);
                }
                else
                {
                    timeline = (IPost[])service.Get(FeaturesType.UserTimelineOlderPosts, int.Parse(regist.idOnService), maxId, maxDate);
                }
                Stopwatch w2 = Stopwatch.StartNew();
                IEnumerable<long> postInDb = postCollection.AsQueryable().Where(p => p.chosenFeature._id == item._id).Select(p => p.idOnService);
                w2.Stop();
                ILog log2 = LogManager.GetLogger("QueryLogger");
                log2.Info(" Elapsed time: " + w2.Elapsed + ", chosen feature's id: " + item._id + ", select id on service of the oldest post");
                foreach (IPost post in timeline)
                {
                    if (!postInDb.Contains(post.Id))
                    {
                        Post pst = new Post()
                        {
                            _id = GeneratePostId(db),
                            chosenFeature = item,
                            idOnService = post.Id,
                            message = post.Text,
                            createAt = post.CreatedAt
                        };
                        Stopwatch w3 = Stopwatch.StartNew();
                        postCollection.InsertOneAsync(pst);
                        w3.Stop();
                        ILog log3 = LogManager.GetLogger("QueryLogger");
                        log3.Info(" Elapsed time: " + w3.Elapsed + ", chosen feature's id: " + item._id + ", id on service: " + post.Id + ", message: " + post.Text + ", date time of creation: " + post.CreatedAt + ", insert the oldest post");
                    }
                }
            }
        }

        public bool Follow(string username, string password, int followId)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var staticFriendCollection = db.StaticFriends();
            var userCollection = db.Users();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return false;
            try
            {
                var filter = Builders<User>.Filter.Eq("_id", followId);
                Stopwatch w1 = Stopwatch.StartNew();
                User friend = userCollection.Find(filter).SingleAsync().Result;
                w1.Stop();
                ILog log1 = LogManager.GetLogger("QueryLogger");
                log1.Info(" Elapsed time: " + w1.Elapsed + ", select the user to follow");
                StaticFriend staticFriend = new StaticFriend
                {
                    _id = GenerateStaticFriendId(db),
                    user = user,
                    friend = friend
                };
                Stopwatch w = Stopwatch.StartNew();
                staticFriendCollection.InsertOneAsync(staticFriend);
                w.Stop();
                ILog log = LogManager.GetLogger("QueryLogger");
                log.Info(" Elapsed time: " + w.Elapsed + ", user id: " + user._id + ", static friend's id: " + followId + ", insert an user as static friend");
                return true;
            }
            catch (AggregateException)
            {
                return false;
            }
        }

        public bool Unfollow(string username, string password, int followId)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var staticFriendCollection = db.StaticFriends();
            var userCollection = db.Users();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return false;
            try
            {
                var builders = Builders<StaticFriend>.Filter;
                var filter = builders.Eq("user._id", user._id) & builders.Eq("friend._id", followId);
                Stopwatch w1 = Stopwatch.StartNew();
                var result = staticFriendCollection.DeleteOneAsync(filter).Result;
                w1.Stop();
                ILog log1 = LogManager.GetLogger("QueryLogger");
                log1.Info(" Elapsed time: " + w1.Elapsed + ", unfollow an user");
                return true;
            }
            catch (AggregateException)
            {
                return false;
            }
        }

        public WUser[] GetFollowings(string username, string password)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var staticFriendCollection = db.StaticFriends();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return new WUser[0];
            //ILog log = LogManager.GetLogger("PanelLogger");
            //log.Info(user.id + ",FG");
            List<WUser> users = new List<WUser>();
            Stopwatch w1 = Stopwatch.StartNew();
            List<StaticFriend> sFriends = staticFriendCollection.AsQueryable().Where(sf => sf.user._id == user._id).ToList();
            w1.Stop();
            ILog log1 = LogManager.GetLogger("QueryLogger");
            log1.Info(" Elapsed time: " + w1.Elapsed + ", select all users followed by an user");
            foreach (StaticFriend item in sFriends)
            {
                users.Add(Converter.UserToWUser(db, user, item.friend, false));
            }
            return users.ToArray();
        }

        public WUser[] GetFollowers(string username, string password)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var staticFriendCollection = db.StaticFriends();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return new WUser[0];
            //  ILog log = LogManager.GetLogger("PanelLogger");
            // log.Info(user.id + ",FR");
            List<WUser> users = new List<WUser>();
            Stopwatch w1 = Stopwatch.StartNew();
            IEnumerable<int> followings = staticFriendCollection.AsQueryable().Where(sf => sf.user._id == user._id).Select(sf => sf.friend._id).ToList();
            w1.Stop();
            ILog log1 = LogManager.GetLogger("QueryLogger");
            log1.Info(" Elapsed time: " + w1.Elapsed + ", user id: " + user._id + ", select all static friends' ids of an user");
            Stopwatch w2 = Stopwatch.StartNew();
            List<StaticFriend> sFriends = staticFriendCollection.AsQueryable().Where(sf => sf.friend._id == user._id).ToList();
            w2.Stop();
            ILog log2 = LogManager.GetLogger("QueryLogger");
            log2.Info(" Elapsed time: " + w2.Elapsed + ", select all users that follow an user");
            foreach (StaticFriend item in sFriends)
            {
                users.Add(Converter.UserToWUser(db, user, item.user, false));
            }
            return users.ToArray();
        }

        public WUser[] GetSuggestedFriends(string username, string password)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var suggestionCollection = db.Suggestions();
            var hiddenCollection = db.Hiddens();
            var staticFriendCollection = db.StaticFriends();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return new WUser[0];
            //   ILog log = LogManager.GetLogger("PanelLogger");
            // log.Info(user.id + ",SF");
            List<WUser> suggestedFriends = new List<WUser>();
            Stopwatch w2 = Stopwatch.StartNew();
            List<Suggestion> suggest = (from s in suggestionCollection.AsQueryable() where (s.chosenFeature.user._id == user._id) select s).ToList();
            w2.Stop();
            ILog log2 = LogManager.GetLogger("QueryLogger");
            log2.Info(" Elapsed time: " + w2.Elapsed + ", select all suggestions");
            foreach (Suggestion i in suggest)
            {
                bool hidden = false;
                bool sta = false;
                var hidd = hiddenCollection.AsQueryable().Where(h => h.user._id == i.chosenFeature.user._id && h.timeline == HiddenType.Suggestions.ToString()).Select(h => h.friend._id).ToList();
                if (hidd.Contains(i.user._id)) hidden = true;
                var stat = staticFriendCollection.AsQueryable().Where(sf => sf.user._id == i.chosenFeature.user._id).Select(sf => sf.friend._id).ToList();
                if (stat.Contains(i.user._id)) sta = true;
                Stopwatch w1 = Stopwatch.StartNew();
                var suggestion = (from s in suggestionCollection.AsQueryable() where s.chosenFeature.user._id == user._id && s._id == i._id && !hidden && !sta select s.user).ToList();
                w1.Stop();
                ILog log1 = LogManager.GetLogger("QueryLogger");
                log1.Info(" Elapsed time: " + w1.Elapsed + ", select all suggested user not in Hidden or in StaticFriend");
                foreach (User item in suggestion)
                {
                    suggestedFriends.Add(Converter.UserToWUser(db, user, item, false));
                }
            }
            new Thread(thread => UpdateSuggestion(user)).Start();
            var toReturn = suggestedFriends.Distinct().ToArray();
            return toReturn;
        }

        private void UpdateSuggestion(User user)
        {
            ConnectorDataContext db = new ConnectorDataContext();
            var chosenFeatureCollection = db.ChosenFeatures();
            var registrationCollection = db.Registrations();
            var suggestionCollection = db.Suggestions();
            Dictionary<int, HashSet<int>> logFriends = new Dictionary<int, HashSet<int>>();
            bool needToLog = false;
            Stopwatch w = Stopwatch.StartNew();
            IEnumerable<ChosenFeature> chosenFeatures = chosenFeatureCollection.AsQueryable().Where(
                    cf => (cf.feature.Equals(FeaturesType.Followings.ToString()) ||
                        cf.feature.Equals(FeaturesType.Followers.ToString()) ||
                        cf.feature.Equals(FeaturesType.TFSCollection.ToString()) ||
                        cf.feature.Equals(FeaturesType.TFSTeamProject.ToString())) && cf.user._id == user._id);
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", user id: " + user._id + ", select all chosen features of an author and his feature 'followings' or 'followers' or 'TFSCollection' or 'TFSTeamProject'");
            foreach (ChosenFeature chosenFeature in chosenFeatures)
            {
                var filter = Builders<ChosenFeature>.Filter.Eq("_id", chosenFeature._id);
                Stopwatch w2 = Stopwatch.StartNew();
                ChosenFeature temp = chosenFeatureCollection.Find(filter).SingleAsync().Result;
                w2.Stop();
                ILog log2 = LogManager.GetLogger("QueryLogger");
                log2.Info(" Elapsed time: " + w2.Elapsed + ", chosen feature's id: " + chosenFeature._id + ", select a chosen feature to update suggestions");
                if (temp.lastDownload > DateTime.UtcNow - _suggestionSpan)
                    continue;
                else
                {
                    var update = Builders<ChosenFeature>.Update
                         .Set("lastDownload", DateTime.UtcNow);
                    Stopwatch w6 = Stopwatch.StartNew();
                    var result = chosenFeatureCollection.UpdateOneAsync(filter, update).Result;
                    w6.Stop();
                    ILog log6 = LogManager.GetLogger("QueryLogger");
                    log6.Info(" Elapsed time: " + w6.Elapsed + ", update lastDownload of the chosen feature (updatesuggestions)");
                }
                needToLog = true;
                IService service = null;
                var builders = Builders<Registration>.Filter;
                var filter1 = builders.Eq("user._id", chosenFeature.user._id) & builders.Eq("serviceInstance._id", chosenFeature.serviceInstance._id);
                Stopwatch w1 = Stopwatch.StartNew();
                Registration regist = registrationCollection.Find(filter1).SingleAsync().Result;
                w1.Stop();
                ILog log1 = LogManager.GetLogger("QueryLogger");
                log1.Info(" Elapsed time: " + w1.Elapsed + ", select the registration from the chosen feature(updatesuggestion)");
                var filter2 = builders.Eq("user._id", temp.user._id) & builders.Eq("serviceInstance._id", temp.serviceInstance._id);
                Registration registr = registrationCollection.Find(filter2).SingleAsync().Result;         //considerata poco sopra per il log
                if (chosenFeature.feature.Equals(FeaturesType.Followings.ToString()) ||
                    chosenFeature.feature.Equals(FeaturesType.Followers.ToString()))
                {
                    service = ServiceFactory.getServiceOauth(
                         regist.serviceInstance.service.name,
                          regist.serviceInstance.host,
                          regist.serviceInstance.consumerKey,
                          regist.serviceInstance.consumerSecret,
                          regist.accessToken,
                          regist.accessSecret);
                }
                else if (chosenFeature.feature.Equals(FeaturesType.TFSCollection.ToString()) ||
                    chosenFeature.feature.Equals(FeaturesType.TFSTeamProject.ToString()))
                {
                    if (registr.serviceInstance.service.name.Equals("GitHub"))
                    {
                        service = ServiceFactory.getServiceOauth(registr.serviceInstance.service.name, registr.serviceInstance.host, registr.serviceInstance.consumerKey, registr.serviceInstance.consumerSecret, registr.accessToken, null); 
                    }
                    else
                    {
                        service = ServiceFactory.getService(
                             regist.serviceInstance.service.name,
                             regist.nameOnService,
                             EncDecRc4("key", regist.accessToken),
                             regist.accessSecret,
                             regist.serviceInstance.host);
                    }
                }
                string[] friends = null;
                if (chosenFeature.feature.Equals(FeaturesType.Followings.ToString()))
                    friends = (string[])service.Get(FeaturesType.Followings, null);
                else if (chosenFeature.feature.Equals(FeaturesType.Followers.ToString()))
                    friends = (string[])service.Get(FeaturesType.Followers, null);
                else if (chosenFeature.feature.Equals(FeaturesType.TFSCollection.ToString()))
                    friends = (string[])service.Get(FeaturesType.TFSCollection, null);
                else if (chosenFeature.feature.Equals(FeaturesType.TFSTeamProject.ToString()))
                    friends = (string[])service.Get(FeaturesType.TFSTeamProject, null);
                if (friends != null)
                {
                    //Delete suggestions for this chosen feature in the database
                    var filter3 = Builders<Suggestion>.Filter.Eq("chosenFeature._id", chosenFeature._id);
                    Stopwatch w3 = Stopwatch.StartNew();
                    suggestionCollection.DeleteManyAsync(filter3);
                    w3.Stop();
                    ILog log3 = LogManager.GetLogger("QueryLogger");
                    log3.Info(" Elapsed time: " + w3.Elapsed + ", chosen feature's id: " + chosenFeature._id + ", delete suggestions according to the chosen feature");
                    foreach (string friend in friends)
                    {
                        Stopwatch w4 = Stopwatch.StartNew();
                        IEnumerable<User> friendInSocialTfs = registrationCollection.AsQueryable().Where(r => r.idOnService == friend &&
                           r.serviceInstance._id == chosenFeature.serviceInstance._id).Select(r => r.user);
                        w4.Stop();
                        ILog log4 = LogManager.GetLogger("QueryLogger");
                        log4.Info(" Elapsed time: " + w4.Elapsed + ", user id: " + friend + ", service instance id: " + chosenFeature.serviceInstance._id + ", select all users that can be possible friends in SocialTFS");
                        if (friendInSocialTfs.Count() == 1)                 
                        {
                            User suggestedFriend = friendInSocialTfs.First();
                            if (friend != regist.idOnService)
                            {
                                Suggestion sugg = new Suggestion()
                                {
                                    user = suggestedFriend,
                                    chosenFeature = chosenFeature
                                };
                                Stopwatch w5 = Stopwatch.StartNew();
                                suggestionCollection.InsertOneAsync(sugg);
                                w5.Stop();
                                ILog log5 = LogManager.GetLogger("QueryLogger");
                                log5.Info(" Elapsed time: " + w5.Elapsed + ", user id: " + suggestedFriend._id + ", chosen feature id: " + chosenFeature._id + ", insert a suggestion");
                                if (!logFriends.ContainsKey(suggestedFriend._id))
                                    logFriends[suggestedFriend._id] = new HashSet<int>();
                                logFriends[suggestedFriend._id].Add(registr.serviceInstance._id);
                            }
                        }
                    }
                }
            }
            if (needToLog)
            {
                //  ILog log7 = LogManager.GetLogger("NetworkLogger");
                //log7.Info(user.id + ",S,[" + GetFriendString(user.id, logFriends) + "]");
            }
        }

        public string[] GetSkills(string username, string password, string ownerName)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var userCollection = db.Users();
            var skillCollection = db.Skills();
            var chosenFeatureCollection = db.ChosenFeatures();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return new string[0];
            //    ILog log = LogManager.GetLogger("PanelLogger");
            //   log.Info(user.id + ",SK");
            User owner;
            try
            {
                Stopwatch w1 = Stopwatch.StartNew();
                owner = userCollection.AsQueryable().Where(u => u.username == ownerName).SingleAsync().Result;
                w1.Stop();
                ILog log1 = LogManager.GetLogger("QueryLogger");
                log1.Info(" Elapsed time: " + w1.Elapsed + ", username: " + ownerName + ", select the username of the user to get his skills");
            }
            catch (Exception)
            {
                return new string[0];
            }
            DownloadSkills(owner, db);
            //get the names of the skills from the database
            Stopwatch w2 = Stopwatch.StartNew();
            IEnumerable<string> skills = skillCollection.AsQueryable().Where(s => s.chosenFeature.user._id == owner._id).Select(s => s.skill);
            w2.Stop();
            ILog log2 = LogManager.GetLogger("QueryLogger");
            log2.Info(" Elapsed time: " + w2.Elapsed + ", user id: " + owner._id + ", select all skills of an user");
            return skills.Distinct().ToArray<string>();
        }

        private void DownloadSkills(User currentUser, ConnectorDataContext db)
        {
            var chosenFeatureCollection = db.ChosenFeatures();
            var skillCollection = db.Skills();
            var registrationCollection = db.Registrations();
            Stopwatch w = Stopwatch.StartNew();
            List<ChosenFeature> chosenFeatures = chosenFeatureCollection.AsQueryable().Where(cf => cf.user._id == currentUser._id && cf.feature == FeaturesType.Skills.ToString() && cf.lastDownload < DateTime.UtcNow - _skillSpan).ToListAsync().Result;
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", user id: " + currentUser._id + ", feature's name: " + FeaturesType.Skills.ToString() + ", select all chosen features of an user and his feature 'skills'");
            foreach (ChosenFeature item in chosenFeatures)
            {
                //delete the user's skills in the database
                var filter = Builders<Skill>.Filter.Eq("chosenFeature._id", item._id);
                Stopwatch w1 = Stopwatch.StartNew();
                skillCollection.DeleteManyAsync(filter);
                w1.Stop();
                ILog log1 = LogManager.GetLogger("QueryLogger");
                log1.Info(" Elapsed time: " + w1.Elapsed + ", delete user's skills");
                var builders = Builders<Registration>.Filter;
                var filter1 = builders.Eq("user._id", item.user._id) & builders.Eq("chosenFeature._id", item._id);
                Stopwatch w4 = Stopwatch.StartNew();
                Registration registration = registrationCollection.Find(filter1).SingleAsync().Result;
                w4.Stop();
                ILog log4 = LogManager.GetLogger("QueryLogger");
                log4.Info(" Elapsed time: " + w4.Elapsed + ", select the registration from the chosen feature(downloadskills)");
                IService service = ServiceFactory.getServiceOauth(registration.serviceInstance.service.name, registration.serviceInstance.host, registration.serviceInstance.consumerKey, registration.serviceInstance.consumerSecret, registration.accessToken, registration.accessSecret);
                string[] userSkills = (string[])service.Get(FeaturesType.Skills, null);
                //insert skills in the database
                foreach (string userSkill in userSkills)
                {
                    Skill skill = new Skill
                     {
                         chosenFeature = item,
                         skill = userSkill
                     };
                    Stopwatch w2 = Stopwatch.StartNew();
                    skillCollection.InsertOneAsync(skill);
                    w2.Stop();
                    ILog log2 = LogManager.GetLogger("QueryLogger");
                    log2.Info(" Elapsed time: " + w2.Elapsed + ", chosen feature: " + item._id + ", skill: " + userSkill + ", insert the new skill");
                }
                // effettuo l'update
                var filter2 = Builders<ChosenFeature>.Filter.Eq("_id", item._id);
                var update = Builders<ChosenFeature>.Update
                 .Set("lastDownload", DateTime.UtcNow);
                Stopwatch w3 = Stopwatch.StartNew();
                var result = chosenFeatureCollection.UpdateOneAsync(filter2, update).Result;
                w3.Stop();
                ILog log3 = LogManager.GetLogger("QueryLogger");
                log3.Info(" Elapsed time: " + w3.Elapsed + ", update lastDownload of the chosen feature(downloadskills)");
            }
        }

        public bool UpdateChosenFeatures(string username, string password, int serviceInstanceId, string[] chosenFeatures)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var chosenFeatureCollection = db.ChosenFeatures();
            var userCollection = db.Users();
            var serviceInstanceCollection = db.ServiceInstances();
            bool suggestion = false, dynamic = false, interactive = false;
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return false;
            //remove the old chosen features
            var builders = Builders<ChosenFeature>.Filter;
            var filter = builders.Eq("user._id", user._id) & builders.Nin("feature", chosenFeatures) & builders.Eq("serviceInstance._id", serviceInstanceId);
            Stopwatch w = Stopwatch.StartNew();
            chosenFeatureCollection.DeleteManyAsync(filter);
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", remove the old chosen features");
            //add the new chosen features
            foreach (string chosenFeature in chosenFeatures)
            {
                Stopwatch w1 = Stopwatch.StartNew();
                bool cFeature = chosenFeatureCollection.AsQueryable().Where(c => c.user._id == user._id && c.feature == chosenFeature && c.serviceInstance._id == serviceInstanceId).AnyAsync().Result;
                w1.Stop();
                ILog log1 = LogManager.GetLogger("QueryLogger");
                log1.Info(" Elapsed time: " + w1.Elapsed + ", user id: " + user._id + ", feature's name: " + chosenFeature + ", service instance's id: " + serviceInstanceId + ", check if there is a chosen feature with these parameters");
                if (!cFeature)
                {
                    if (chosenFeature == FeaturesType.Followers.ToString()
                        || chosenFeature == FeaturesType.Followings.ToString()
                        || chosenFeature == FeaturesType.TFSTeamProject.ToString()
                        || chosenFeature == FeaturesType.TFSTeamProject.ToString())
                        suggestion = true;
                    else if (chosenFeature == FeaturesType.IterationNetwork.ToString())
                        dynamic = true;
                    else if (chosenFeature == FeaturesType.InteractiveNetwork.ToString())
                        interactive = true;
                    var filter1 = Builders<ServiceInstance>.Filter.Eq("_id", serviceInstanceId);
                    Stopwatch w3 = Stopwatch.StartNew();
                    ServiceInstance serviceInstance = serviceInstanceCollection.Find(filter1).SingleAsync().Result;
                    w3.Stop();
                    ILog log3 = LogManager.GetLogger("QueryLogger");
                    log3.Info(" Elapsed time: " + w3.Elapsed + ", select the service instance from its id");
                    ChosenFeature chosenFeat = new ChosenFeature()
                    {
                        _id = GenerateChosenFeatureId(db),
                        user = user,
                        serviceInstance = serviceInstance,
                        feature = chosenFeature,
                        lastDownload = new DateTime(1900, 1, 1)
                    };
                    Stopwatch w2 = Stopwatch.StartNew();
                    chosenFeatureCollection.InsertOneAsync(chosenFeat);
                    w2.Stop();
                    ILog log2 = LogManager.GetLogger("QueryLogger");
                    log2.Info(" Elapsed time: " + w2.Elapsed + ", user id: " + user._id + ", service instance's id: " + serviceInstanceId + ", feature: " + chosenFeature + ", last download: " + new DateTime(1900, 1, 1) + ", insert a new chosen feature");
                }
            }
            if (suggestion)
                new Thread(thread => UpdateSuggestion(user)).Start();
            if (dynamic)
                new Thread(thread => UpdateDynamicFriend(user)).Start();
            if (interactive)
                new Thread(thread => UpdateInteractiveFriend(user)).Start();
            return true;
        }

        public WFeature[] GetChosenFeatures(string username, string password, int serviceInstanceId)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var serviceCollection = db.Services();
            var serviceInstanceCollection = db.ServiceInstances();
            var chosenFeatureCollection = db.ChosenFeatures();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return new WFeature[0];
            //  ILog log = LogManager.GetLogger("PanelLogger");
            // log.Info(user.id + ",CF");
            List<WFeature> result = new List<WFeature>();
            Stopwatch w1 = Stopwatch.StartNew();
            ServiceInstance sInstance = serviceInstanceCollection.AsQueryable().Where(si => si._id == serviceInstanceId).SingleAsync().Result;
            w1.Stop();
            ILog log1 = LogManager.GetLogger("QueryLogger");
            log1.Info(" Elapsed time: " + w1.Elapsed + ", service instance's id: " + serviceInstanceId + ", select service instance to get the chosen features");
            foreach (FeaturesType item in ServiceFactory.getService(sInstance.service.name).GetPublicFeatures())
            {
                bool chosen = false;
                Stopwatch w2 = Stopwatch.StartNew();
                var chose = chosenFeatureCollection.AsQueryable().Where(cf => cf.serviceInstance._id == serviceInstanceId && cf.user._id == user._id).Select(cf => cf.feature).ToList();
                w2.Stop();
                ILog log2 = LogManager.GetLogger("QueryLogger");
                log2.Info(" Elapsed time: " + w2.Elapsed + ", service istance's id: " + serviceInstanceId + ", user id: " + user._id + ", check if a chosen feature has been chosen by an user");
                foreach (var it in chose)
                {
                    if (it.Equals(item.ToString())) { chosen = true; }
                }
                WFeature feature = new WFeature()
                {
                    Name = item.ToString(),
                    Description = FeaturesManager.GetFeatureDescription(item),
                    IsChosen = chosen
                };
                result.Add(feature);
            }
            return result.ToArray();
        }

        public WUser[] GetHiddenUsers(string username, string password)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var hiddenCollection = db.Hiddens();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return new WUser[0];
            // ILog log = LogManager.GetLogger("PanelLogger");
            // log.Info(user.id + ",HU");
            List<WUser> result = new List<WUser>();
            Stopwatch w1 = Stopwatch.StartNew();
            List<User> usr = hiddenCollection.AsQueryable().Where(h => h.user._id == user._id).Select(h => h.friend).Distinct().ToList();
            w1.Stop();
            ILog log1 = LogManager.GetLogger("QueryLogger");
            log1.Info(" Elapsed time: " + w1.Elapsed + ", user id: " + user._id + ", select all users hidden by an user");
            foreach (User item in usr)
            {
                result.Add(Converter.UserToWUser(db, user, item, false));
            }
            return result.ToArray();
        }

        public WHidden GetUserHideSettings(string username, string password, int userId)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var collection = db.Hiddens();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return null;
            WHidden result = new WHidden();
            Stopwatch w = Stopwatch.StartNew();
            List<Hidden> hide = collection.AsQueryable().Where(h => h.user._id == user._id && h.friend._id == userId).ToListAsync().Result;
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", user id: " + user._id + ", friend's id: " + userId + ", select all hidden friends of an user and set the visibility according to the timeline");
            foreach (Hidden item in hide)
                if (item.timeline == HiddenType.Suggestions.ToString())
                    result.Suggestions = true;
                else if (item.timeline == HiddenType.Dynamic.ToString())
                    result.Dynamic = true;
                else if (item.timeline == HiddenType.Interactive.ToString())
                    result.Interactive = true;
            return result;
        }

        public bool UpdateHiddenUser(string username, string password, int userId, bool suggestions, bool dynamic, bool interactive)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var hiddenCollection = db.Hiddens();
            var userCollection = db.Users();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return false;
            try
            {
                var filt = Builders<User>.Filter.Eq("_id", userId);
                Stopwatch w5 = Stopwatch.StartNew();
                User hiddenFriend = userCollection.Find(filt).SingleAsync().Result;
                w5.Stop();
                ILog log5 = LogManager.GetLogger("QueryLogger");
                log5.Info(" Elapsed time: " + w5.Elapsed + ", select the hidden user from his id");
                var builders = Builders<Hidden>.Filter;
                var filter = builders.Eq("user._id", user._id) & builders.Eq("friend._id", userId);
                Stopwatch w = Stopwatch.StartNew();
                hiddenCollection.DeleteManyAsync(filter);
                w.Stop();
                ILog log = LogManager.GetLogger("QueryLogger");
                log.Info(" Elapsed time: " + w.Elapsed + ", user id: " + user._id + ", friend's id: " + userId + ", remove all hidden friends of an user");
                if (suggestions)
                {
                    Hidden hidden = new Hidden()
                    {
                        user = user,
                        friend = hiddenFriend,
                        timeline = HiddenType.Suggestions.ToString()
                    };
                    Stopwatch w2 = Stopwatch.StartNew();
                    hiddenCollection.InsertOneAsync(hidden);
                    w2.Stop();
                    ILog log2 = LogManager.GetLogger("QueryLogger");
                    log2.Info(" Elapsed time: " + w2.Elapsed + ", user id: " + user._id + ", friend's id: " + userId + ", timeline: " + HiddenType.Suggestions.ToString() + ", insert a hidden friend in the suggestion timeline");
                }
                if (dynamic)
                {
                    Hidden hidden1 = new Hidden()
                    {
                        user = user,
                        friend = hiddenFriend,
                        timeline = HiddenType.Dynamic.ToString()
                    };
                    Stopwatch w3 = Stopwatch.StartNew();
                    hiddenCollection.InsertOneAsync(hidden1);
                    w3.Stop();
                    ILog log3 = LogManager.GetLogger("QueryLogger");
                    log3.Info(" Elapsed time: " + w3.Elapsed + ", user id: " + user._id + ", friend's id: " + userId + ", timeline: " + HiddenType.Dynamic.ToString() + ", insert a hidden friend in the dynamic timeline");
                }
                if (interactive)
                {
                    Hidden hidden2 = new Hidden()
                    {
                        user = user,
                        friend = hiddenFriend,
                        timeline = HiddenType.Interactive.ToString()
                    };
                    Stopwatch w4 = Stopwatch.StartNew();

                    hiddenCollection.InsertOneAsync(hidden2);
                    w4.Stop();
                    ILog log4 = LogManager.GetLogger("QueryLogger");
                    log4.Info(" Elapsed time: " + w4.Elapsed + ", user id: " + user._id + ", friend's id: " + userId + ", timeline: " + HiddenType.Interactive.ToString() + ", insert a hidden friend in the interactive timeline");
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public Uri[] GetAvailableAvatars(string username, string password)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var chosenFeatureCollection = db.ChosenFeatures();
            var registrationCollection = db.Registrations();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return new Uri[0];
            //   ILog log = LogManager.GetLogger("PanelLogger");
            //  log.Info(user.id + ",AA");
            List<Uri> avatars = new List<Uri>();
            Stopwatch w1 = Stopwatch.StartNew();
            List<ChosenFeature> cFeatures = chosenFeatureCollection.AsQueryable().Where(cf => cf.user._id == user._id && cf.feature == FeaturesType.Avatar.ToString()).ToListAsync().Result;
            w1.Stop();
            ILog log1 = LogManager.GetLogger("QueryLogger");
            log1.Info(" Elapsed time: " + w1.Elapsed + ", user id: " + user._id + ", feature's name: " + FeaturesType.Avatar.ToString() + ", select all user's avatars");
            foreach (ChosenFeature chosenFeature in cFeatures)
            {
                var builders = Builders<Registration>.Filter;
                var filter = builders.Eq("user._id", chosenFeature.user._id) & builders.Eq("serviceInstance._id", chosenFeature.serviceInstance._id);
                Stopwatch w2 = Stopwatch.StartNew();
                Registration registration = registrationCollection.Find(filter).SingleAsync().Result;
                w2.Stop();
                ILog log2 = LogManager.GetLogger("QueryLogger");
                log2.Info(" Elapsed time: " + w2.Elapsed + ", select the registration from the chosen feature");
                IService service = ServiceFactory.getServiceOauth(registration.serviceInstance.service.name, registration.serviceInstance.host, registration.serviceInstance.consumerKey, registration.serviceInstance.consumerSecret, registration.accessToken, registration.accessSecret);
                Uri avatar = null;
                try { avatar = (Uri)service.Get(FeaturesType.Avatar); }
                catch (Exception) { }
                if (avatar != null)
                {
                    avatars.Add(avatar);
                }
            }
            return avatars.ToArray();
        }

        public bool SaveAvatar(string username, string password, Uri avatar)
        {
            Contract.Requires(!String.IsNullOrEmpty(username));
            Contract.Requires(!String.IsNullOrEmpty(password));
            ConnectorDataContext db = new ConnectorDataContext();
            var collection = db.Users();
            var chosenFeatureCollection = db.ChosenFeatures();
            var dynamicFriendCollection = db.DynamicFriends();
            var hiddenCollection = db.Hiddens();
            var interactiveFriendCollection = db.InteractiveFriends();
            var postCollection = db.Posts();
            var registrationCollection = db.Registrations();
            var staticFriendCollection = db.StaticFriends();
            var suggestionCollection = db.Suggestions();
            User user = CheckCredentials(db, username, password);
            if (user == null)
                return false;
            var builders = Builders<User>.Filter;
            var filter = builders.Eq("_id", user._id);
            var update = Builders<User>.Update
                    .Set("avatar", avatar.AbsoluteUri);
            var builders1 = Builders<ChosenFeature>.Filter;
            var filter1 = builders1.Eq("user._id", user._id);
            var update1 = Builders<ChosenFeature>.Update
                    .Set("user.avatar", avatar.AbsoluteUri);
            var builders2 = Builders<DynamicFriend>.Filter;
            var filter2 = builders2.Eq("chosenFeature.user._id", user._id);
            var update2 = Builders<DynamicFriend>.Update
                    .Set("chosenFeature.user.avatar", avatar.AbsoluteUri);
            var builders3 = Builders<Hidden>.Filter;
            var filter3 = builders3.Eq("user._id", user._id);
            var update3 = Builders<Hidden>.Update
                    .Set("user.avatar", avatar.AbsoluteUri);
            var builders4 = Builders<InteractiveFriend>.Filter;
            var filter4 = builders4.Eq("chosenFeature.user._id", user._id);
            var update4 = Builders<InteractiveFriend>.Update
                    .Set("chosenFeature.user.avatar", avatar.AbsoluteUri);
            var builders5 = Builders<Post>.Filter;
            var filter5 = builders5.Eq("chosenFeature.user._id", user._id);
            var update5 = Builders<Post>.Update
                    .Set("chosenFeature.user.avatar", avatar.AbsoluteUri);
            var builders6 = Builders<Registration>.Filter;
            var filter6 = builders6.Eq("user._id", user._id);
            var update6 = Builders<Registration>.Update
                    .Set("user.avatar", avatar.AbsoluteUri);
            var builders7 = Builders<StaticFriend>.Filter;
            var filter7 = builders7.Eq("user._id", user._id);
            var update7 = Builders<StaticFriend>.Update
                    .Set("user.avatar", avatar.AbsoluteUri);
            var builders8 = Builders<Suggestion>.Filter;
            var filter8 = builders8.Eq("chosenFeature.user._id", user._id);
            var update8 = Builders<Suggestion>.Update
                    .Set("chosenFeature.user.avatar", avatar.AbsoluteUri);
            Stopwatch w = Stopwatch.StartNew();
            var result = collection.UpdateOneAsync(filter, update).Result;
            var result1 = chosenFeatureCollection.UpdateManyAsync(filter1, update1).Result;
            var result2 = dynamicFriendCollection.UpdateManyAsync(filter2, update2).Result;
            var result3 = hiddenCollection.UpdateManyAsync(filter3, update3).Result;
            var result4 = interactiveFriendCollection.UpdateManyAsync(filter4, update4).Result;
            var result5 = postCollection.UpdateManyAsync(filter5, update5).Result;
            var result6 = registrationCollection.UpdateManyAsync(filter6, update6).Result;
            var result7 = staticFriendCollection.UpdateManyAsync(filter7, update7).Result;
            var result8 = suggestionCollection.UpdateManyAsync(filter8, update8).Result;
            w.Stop();
            ILog log = LogManager.GetLogger("QueryLogger");
            log.Info(" Elapsed time: " + w.Elapsed + ", uri: " + avatar.AbsoluteUri + ", save avatar");
            return true;
        }

    }

}
