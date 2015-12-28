db.Services.insert({
	_id : 1,
	name : "SocialTFS",
	image : "/Images/socialtfs.png",
	requestToken : null,
	authorize : null,
	accessToken : null,
	version : 1
});
db.Services.insert({
	_id : 2,
	name : "Team Foundation Server",
	image : "/Images/tfs.png",
	requestToken : null,
	authorize : null,
	accessToken : null,
	version : 1
}); 
db.Services.insert({
	_id : 3,
	name : "StatusNet",
	image : "/Images/statusnet.png",
	requestToken : "/api/oauth/request_token",
	authorize : "/api/oauth/authorize",
	accessToken : "/api/oauth/access_token",
	version : 1
});
db.Services.insert({
	_id : 4,
	name : "Twitter",
	image : "/Images/twitter.png",
	requestToken : "/oauth/request_token",
	authorize : "/oauth/authorize",
	accessToken : "/oauth/access_token",
	version : 1
});
db.Services.insert({
	_id : 5,
	name : "LinkedIn",
	image : "/Images/linkedin.png",
	requestToken : "/uas/oauth/requestToken",
	authorize : "/uas/oauth/authorize",
	accessToken : "/uas/oauth/accessToken",
	version : 1
});
db.Services.insert({
	_id : 6,
	name : "CodePlex",
	image : "/Images/codeplex.png",
	requestToken : null,
	authorize : null,
	accessToken : null,
	version : 1
});
db.Services.insert({
	_id : 7,
	name : "Facebook",
	image : "/Images/facebook.png",
	requestToken : null,
	authorize : null,
	accessToken : null,
	version : 1
}); 
db.Services.insert({
	_id : 8,
	name : "Yammer",
	image : "/Images/yammer.png",
	requestToken : "/oauth/request_token",
	authorize : "/oauth/authorize",
	accessToken : "/oauth/access_token",
	version : 1
});
db.Services.insert({
	_id : 9,
	name : "GitHub",
	image : "/Images/github.png",
	requestToken : null,
	authorize : "/login/oauth/authorize",
	accessToken : "/login/oauth/access_token",
	version : 1
});