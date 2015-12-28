using System;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace It.Uniba.Di.Cdg.SocialTfs.ProxyServer
{
    /// <summary>
    /// Connection class. It provides a mapping between C# objects and database's entities.
    /// </summary>
    public class ConnectorDataContext
    {
        IMongoDatabase db;

        /// <summary>
        /// Class constructor. It allows to create an instance of the object e to connect to the database. 
        /// </summary>
        public ConnectorDataContext()
        {
            IMongoClient client = new MongoClient(Constants.CONNECTION_STRING);
            this.db = client.GetDatabase(Constants.NAME_DB);
        }
        /// <summary>
        /// Returns the Users collection.
        /// </summary>
        public IMongoCollection<User> Users()
        {
            return db.GetCollection<User>(Constants.USERS_COLLECTION);
        }

        /// <summary>
        /// Returns the Avatars collection.
        /// </summary>
        public IMongoCollection<Avatar> Avatars()
        {
            return db.GetCollection<Avatar>(Constants.AVATARS_COLLECTION);
        }

        /// <summary>
        /// Returns the ChosenFeatures collection.
        /// </summary>
        public IMongoCollection<ChosenFeature> ChosenFeatures()
        {
            return db.GetCollection<ChosenFeature>(Constants.CHOSEN_FEATURES_COLLECTION);
        }

        /// <summary>
        /// Returns the StaticFriends collection.
        /// </summary>
        public IMongoCollection<StaticFriend> StaticFriends()
        {
            return db.GetCollection<StaticFriend>(Constants.STATIC_FRIENDS_COLLECTION);
        }

        /// <summary>
        /// Returns the ServiceInstances collection.
        /// </summary>
        public IMongoCollection<ServiceInstance> ServiceInstances()
        {
            return db.GetCollection<ServiceInstance>(Constants.SERVICE_INSTANCES_COLLECTION);
        }

        /// <summary>
        /// Returns the Registrations collection.
        /// </summary>
        public IMongoCollection<Registration> Registrations()
        {
            return db.GetCollection<Registration>(Constants.REGISTRATIONS_COLLECTION);
        }

        /// <summary>
        /// Returns the Posts collection.
        /// </summary>
        public IMongoCollection<Post> Posts()
        {
            return db.GetCollection<Post>(Constants.POSTS_COLLECTION);
        }

        /// <summary>
        /// Returns the Services collection.
        /// </summary>
        public IMongoCollection<Service> Services()
        {
            return db.GetCollection<Service>(Constants.SERVICES_COLLECTION);
        }

        /// <summary>
        /// Returns the DynamicFriends collection.
        /// </summary>
        public IMongoCollection<DynamicFriend> DynamicFriends()
        {
            return db.GetCollection<DynamicFriend>(Constants.DYNAMIC_FRIENDS_COLLECTION);
        }

        /// <summary>
        /// Returns the Features collection.
        /// </summary>
        public IMongoCollection<Feature> Features()
        {
            return db.GetCollection<Feature>(Constants.FEATURES_COLLECTION);
        }

        /// <summary>
        /// Returns the FeatureScores collection.
        /// </summary>
        public IMongoCollection<FeatureScore> FeatureScores()
        {
            return db.GetCollection<FeatureScore>(Constants.FEATURE_SCORES_COLLECTION);
        }

        /// <summary>
        /// Returns the Hiddens collection.
        /// </summary>
        public IMongoCollection<Hidden> Hiddens()
        {
            return db.GetCollection<Hidden>(Constants.HIDDENS_COLLECTION);
        }

        /// <summary>
        /// Returns the InteractiveFriends collection.
        /// </summary>
        public IMongoCollection<InteractiveFriend> InteractiveFriends()
        {
            return db.GetCollection<InteractiveFriend>(Constants.INTERACTIVE_FRIENDS_COLLECTION);
        }

        /// <summary>
        /// Returns the PreregisteredServices collection.
        /// </summary>
        public IMongoCollection<PreregisteredService> PreregisteredServices()
        {
            return db.GetCollection<PreregisteredService>(Constants.PREREGISTERED_SERVICES_COLLECTION);
        }

        /// <summary>
        /// Returns the Settings collection.
        /// </summary>
        public IMongoCollection<Setting> Settings()
        {
            return db.GetCollection<Setting>(Constants.SETTINGS_COLLECTION);
        }

        /// <summary>
        /// Returns the Skills collection.
        /// </summary>
        public IMongoCollection<Skill> Skills()
        {
            return db.GetCollection<Skill>(Constants.SKILLS_COLLECTION);
        }

        /// <summary>
        /// Returns the Suggestions collection.
        /// </summary>
        public IMongoCollection<Suggestion> Suggestions()
        {
            return db.GetCollection<Suggestion>(Constants.SUGGESTIONS_COLLECTION);
        }

    }

    /// <summary>
    /// User class. It contains all users' attributes.
    /// </summary>
    public class User
    {
        public int _id { get; set; }
        public String username { get; set; }
        public String email { get; set; }
        public String password { get; set; }
        public String avatar { get; set; }
        public bool isAdmin { get; set; }
        public bool active { get; set; }

    }

    /// <summary>
    /// Avatar class. It contains all avatars' attributes.
    /// </summary>
    public class Avatar
    {
        [BsonId]
        public Uri uri { get; set; }
        public ChosenFeature chosenFeature { get; set; }

    }

    /// <summary>
    /// ChosenFeature class. It contains all chosen features' attributes.
    /// </summary>
    public class ChosenFeature
    {
        public long _id { get; set; }
        public User user { get; set; }
        public ServiceInstance serviceInstance { get; set; }
        public String feature { get; set; }
        public DateTime lastDownload { get; set; }

    }

    /// <summary>
    /// StaticFriend class. It contains all static friends' attributes.
    /// </summary>
    public class StaticFriend
    {
        public int _id { get; set; }
        public User user { get; set; }
        public User friend { get; set; }

    }

    /// <summary>
    /// ServiceInstance class. It contains all service instances' attributes.
    /// </summary>
    public class ServiceInstance
    {
        public int _id { get; set; }
        public String name { get; set; }
        public String host { get; set; }
        public Service service { get; set; }
        public String consumerKey { get; set; }
        public String consumerSecret { get; set; }

    }

    /// <summary>
    /// Registration class. It contains all registrations' attributes.
    /// </summary>
    public class Registration
    {
        public ObjectId _id { get; set; }
        public User user { get; set; }                            //PK
        public ServiceInstance serviceInstance { get; set; }      //PK
        public String nameOnService { get; set; }
        public String idOnService { get; set; }
        public String accessToken { get; set; }
        public String accessSecret { get; set; }

    }

    /// <summary>
    /// Post class. It contains all posts' attributes.
    /// </summary>
    public class Post
    {
        public long _id { get; set; }
        public ChosenFeature chosenFeature { get; set; }
        public long idOnService { get; set; }
        public String message { get; set; }
        public DateTime createAt { get; set; }

    }

    /// <summary>
    /// Service class. It contains all services' attributes.
    /// </summary>
    public class Service
    {
        public int _id { get; set; }
        public String name { get; set; }
        public String image { get; set; }
        public String requestToken { get; set; }
        public String authorize { get; set; }
        public String accessToken { get; set; }
        public int version { get; set; }

    }

    /// <summary>
    /// DynamicFriend class. It contains all dynamic friends' attributes.
    /// </summary>
    public class DynamicFriend
    {
        public ObjectId _id { get; set; }
        public User user { get; set; }
        public ChosenFeature chosenFeature { get; set; }

    }

    /// <summary>
    /// Feature class. It contains all features' attributes.
    /// </summary>
    public class Feature
    {
        [BsonId]
        public String name { get; set; }
        public String description { get; set; }
        public bool isPublic { get; set; }

    }

    /// <summary>
    /// FeatureScores class. It contains all feature scores' attributes.
    /// </summary>
    public class FeatureScore
    {
        public ObjectId _id { get; set; }
        public ServiceInstance serviceInstance { get; set; }          //PK
        public String feature { get; set; }                           //PK
        public int score { get; set; }

    }

    /// <summary>
    /// Hidden class. It contains all hiddens' attributes.
    /// </summary>
    public class Hidden
    {
        public ObjectId _id { get; set; }
        public User user { get; set; }
        public User friend { get; set; }
        public String timeline { get; set; }

    }

    /// <summary>
    /// InteractiveFriend class. It contains all interactive friends' attributes.
    /// </summary>
    public class InteractiveFriend
    {
        public ObjectId _id { get; set; }
        public User user { get; set; }
        public ChosenFeature chosenFeature { get; set; }
        public String collection { get; set; }
        public String interactiveObject { get; set; }
        public String objectType { get; set; }

    }

    /// <summary>
    /// PreregisteredService class. It contains all preregistered services' attributes.
    /// </summary>
    public class PreregisteredService
    {
        public ObjectId _id { get; set; }
        public String name { get; set; }
        public String host { get; set; }
        public Service service { get; set; }
        public String consumerKey { get; set; }
        public String consumerSecret { get; set; }

    }

    /// <summary>
    /// Setting class. It contains all settings' attributes.
    /// </summary>
    public class Setting
    {
        [BsonId]
        public String key { get; set; }
        public String value { get; set; }

    }

    /// <summary>
    /// Skill class. It contains all skills' attributes.
    /// </summary>
    public class Skill
    {
        public ChosenFeature chosenFeature { get; set; }                //PK
        public String skill { get; set; }                               //PK

    }

    /// <summary>
    /// Suggestion class. It contains all suggestions' attributes.
    /// </summary>
    public class Suggestion
    {
        public ObjectId _id { get; set; }
        public User user { get; set; }
        public ChosenFeature chosenFeature { get; set; }

    }

}