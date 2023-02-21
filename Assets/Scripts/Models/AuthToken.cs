﻿using SQLite;

public class AuthToken : BaseModel
{

	[PrimaryKey]
	public string ApiToken { set; get; }

	public override string ToString()
	{
		return string.Format("authToken= {0}", ApiToken);
	}

    public AuthToken() { }
    public AuthToken(AuthTokenAPI token)
    {
		this.ApiToken = token.apitoken;
    }

}