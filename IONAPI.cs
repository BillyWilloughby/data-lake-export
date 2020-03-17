using IdentityModel.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;



public class IONAPI
{
    private readonly string _application;
    public readonly string _ci;
    public readonly string _cs;
    public readonly string _pu;
    public readonly string _saak;
    public readonly string _sask;
    public readonly string _ot;
    public readonly string _or;
    public readonly string _oa;

    private readonly string OAuth2TokenEndpoint;
    private readonly string OAuth2TokenRevocationEndpoint;

    private TokenClient _oauth2;

    private DateTime expireTime = new DateTime(1900, 1, 1);
    private TokenResponse _token;
    private string _sToken;

    public IONAPI(string application, string ci, string cs, string pu, string saak,
        string sask, string oa, string ot, string or)
    {
        _application = application;
        _ci = ci;
        _cs = cs;
        _pu = pu;
        _saak = saak;
        _sask = sask;
        _ot = ot;
        _or = or;
        _oa = oa;
        OAuth2TokenEndpoint = _pu + "" + _ot;
        OAuth2TokenRevocationEndpoint = _pu + "" + _or;

        _oauth2 = new TokenClient(OAuth2TokenEndpoint, ci, cs);

    }

    private void storeToken(string token)
    {
        string tokenFile = AppDomain.CurrentDomain.BaseDirectory + "token.bin";
        using (FileStream fs = new FileStream(tokenFile, FileMode.OpenOrCreate))
        {

            BinaryFormatter xs = new BinaryFormatter();
            xs.Serialize(fs, token);

            fs.Close();
        }

        File.SetLastWriteTime(tokenFile, expireTime);
    }

    private string loadToken()
    {
        string tokenFile = AppDomain.CurrentDomain.BaseDirectory + "token.bin";

        if (!File.Exists(tokenFile))
            return null;

        DateTime expiresFileDate = File.GetLastWriteTime(tokenFile);

        if (expiresFileDate < DateTime.Now)
            return null;

        string thisToken;
        using (FileStream fs = new FileStream(tokenFile, FileMode.Open))
        {

            BinaryFormatter xs = new BinaryFormatter();
            thisToken = (string)xs.Deserialize(fs);

            fs.Close();
        }
        expireTime = expiresFileDate;
        return thisToken;
    }

    public string getBearerToken()
    {
        /*If current token is valid, use it.*/
        if (_token != null && expireTime > DateTime.Now)
            return _token.AccessToken;

        /*Load token from file if saved*/
        _sToken = loadToken();

        /*If token from file is valid, use it.*/
        if (_sToken != null && !_sToken.Equals("") && expireTime > DateTime.Now)
            return _sToken;

        /*If Token has a refresh value, use it*/
        if (_token != null && _token.RefreshToken != null)
            _token = RefreshToken(_token.RefreshToken);

        /* Save and Return Token if we have a good one*/
        if (_token != null && !_token.IsError)
        {
            /*Set Token Expire date*/
            expireTime = DateTime.Now.AddSeconds(_token.ExpiresIn - 300);
            _sToken = _token.AccessToken;
            storeToken(_sToken);
            return _token.AccessToken;
        }

        /*Load a new token*/
        _token = _oauth2.RequestResourceOwnerPasswordAsync
            (_saak, _sask).Result;

        /* Save and Return Token if we have a good one*/
        if (_token != null && !_token.IsError)
        {
            /*Set Token Expire date*/
            expireTime = DateTime.Now.AddSeconds(_token.ExpiresIn - 300);
            _sToken = _token.AccessToken;
            storeToken(_sToken);
            return _token.AccessToken;
        }
        else
            return null;
    }

    private TokenResponse RefreshToken(string refreshToken)
    {
        Console.WriteLine(refreshToken);
        return _oauth2.RequestRefreshTokenAsync(refreshToken).Result;
    }

    private void RevokeToken(string token, string tokenType)
    {
        HttpClient client = new HttpClient();
        client.SetBasicAuthentication(_ci, _cs);

        var postBody = new Dictionary<string, string>
            {
                {"token", token},
                {"token_type_hint", tokenType}
            };

        var result = client.PostAsync(OAuth2TokenRevocationEndpoint, new FormUrlEncodedContent(postBody)).Result;

        if (result.IsSuccessStatusCode)
        {
            Console.WriteLine("Successfully revoked token.");
        }
        else
        {
            Console.WriteLine("Error revoking token.");
        }

        Console.WriteLine("{1}, {0}", token, tokenType);
    }

}

