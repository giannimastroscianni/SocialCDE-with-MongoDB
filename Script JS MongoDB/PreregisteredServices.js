db.PreregisteredServices.insert({
	name : "Twitter",
	host : "https://api.twitter.com",
	service : {
		_id : 4,
		name : "Twitter",
		image : "/Images/twitter.png",
		requestToken : "/oauth/request_token",
		authorize : "/oauth/authorize",
		accessToken : "/oauth/access_token",
		version : 1
	},
	consumerKey : "Ryg0yHBpEBpVieFjFxxASA",
	consumerSecret : "sr37iJMr1mWKALVBtpB9LnKL8b5XkqoWmul9Vbw1WRQ"
});
db.PreregisteredServices.insert({
	name : "LinkedIn",
	host : "https://api.linkedin.com",
	service : {
		_id : 5,
		name : "LinkedIn",
		image : "/Images/linkedin.png",
		requestToken : "/uas/oauth/requestToken",
		authorize : "/uas/oauth/authorize",
		accessToken : "/uas/oauth/accessToken",
		version : 1
	},
	consumerKey : "77d7qxwlhoxk2f",
	consumerSecret : "pnJIg9G2EdKMnVwR"
});
db.PreregisteredServices.insert({
	name : "Facebook",
	host : "https://graph.facebook.com",
	service : {
		_id : 7,
		name : "Facebook",
		image : "/Image/facebook.png",
		requestToken : null,
		authorize : null,
		accessToken : null,
		version : 1
	},
	consumerKey : "288099617892180",
	consumerSecret : "a2825a223c90313815a29c878c907bac"
});
db.PreregisteredServices.insert({
	name : "CodePlex",
	host : "https://tfs.codeplex.com/tfs",
	service : {
		_id : 6,
		name : "CodePlex",
		image : "/Images/codeplex.png",
		requestToken : null,
		authorize : null,
		accessToken : null,
		version : 1
	},
	consumerKey : "not used",
	consumerSecret : "not used"
});
db.PreregisteredServices.insert({
	name : "Yammer",
	host : "https://www.yammer.com",
	service : {
		_id : 8,
		name : "Yammer",
		image : "/Image/yammer.png",
		requestToken : "/oauth/request_token",
		authorize : "/oauth/authorize",
		accessToken : "/oauth/access_token",
		version : 1
	},
	consumerKey : "8MQusNE6sqNWjTADp9mQ",
	consumerSecret : "cdPkz4EsQKJHNi2pZNY7cI5wCuU82luXaJCi8vLiloI"
});
db.PreregisteredServices.insert({
	name : "GitHub",
	host : "https://api.github.com/",
	service : {
		_id : 9,
		name : "GitHub",
		image : "/Image/github.png",
		requestToken : null,
		authorize : "/login/oauth/authorize",
		accessToken : "/login/oauth/access_token",
		version : 1
	},
	consumerKey : "3984a3280445ea55db70",
	consumerSecret : "5feaeae21d7c666a32ee1d8c61e2491557b5d101"
});