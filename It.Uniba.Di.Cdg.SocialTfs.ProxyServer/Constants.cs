using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace It.Uniba.Di.Cdg.SocialTfs.ProxyServer
{
    /// <summary>
    /// This class provides all the constants useful for the connection to the database.
    /// </summary>
    public class Constants
    {

        public const String ADDRESS_MONGO = "127.0.0.1";
        public const String NAME_DB = "SocialCDE";
        public const int PORT_DB = 27017;
        public const String PASSWORD_DB = "";
        public const String USER_DB = "";
        public const String CONNECTION_STRING = "mongodb://127.0.0.1:27017/SocialCE";
        public const String USERS_COLLECTION = "Users";
        public const String SERVICE_INSTANCES_COLLECTION = "ServiceInstances";
        public const String REGISTRATIONS_COLLECTION = "Registrations";
        public const String STATIC_FRIENDS_COLLECTION = "StaticFriends";
        public const String CHOSEN_FEATURES_COLLECTION = "ChosenFeatures";
        public const String POSTS_COLLECTION = "Posts";
        public const String SERVICES_COLLECTION = "Services";
        public const String AVATARS_COLLECTION = "Avatars";
        public const String DYNAMIC_FRIENDS_COLLECTION = "DynamicFriends";
        public const String FEATURES_COLLECTION = "Features";
        public const String FEATURE_SCORES_COLLECTION = "FeatureScores";
        public const String HIDDENS_COLLECTION = "Hiddens";
        public const String INTERACTIVE_FRIENDS_COLLECTION = "InteractiveFriends";
        public const String PREREGISTERED_SERVICES_COLLECTION = "PreregisteredServices";
        public const String SETTINGS_COLLECTION = "Settings";
        public const String SKILLS_COLLECTION = "Skills";
        public const String SUGGESTIONS_COLLECTION = "Suggestions";

    }

}