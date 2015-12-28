db.ServiceInstances.insert({
	_id : 1,
	name : "SocialTFS",
	host : "http://localhost",
	service : {
		_id : 1,
		name : "SocialTFS",
		image : "/Images/socialtfs.png",
		requestToken : null,
		authorize : null,
		accessToken : null,
		version : 1
	},
	consumerKey : null,
	consumerSecret : null
});