using IdentityModel.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;

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

    private readonly HttpClient _oauth2Http;
    private readonly string OAuth2TokenEndpoint;
    private readonly string OAuth2TokenRevocationEndpoint;
    private readonly string OAuth2AuthorizationEndpoint;
    private readonly string IONAPIBaseUrl;

    private readonly string ServiceAccountAccessKey;
    private readonly string ServiceAccountSecretKey;

    private DateTime expireTime = new DateTime(1900, 1, 1);
    private TokenResponse _token;
    private string _sToken;
    
    private readonly Object _tokenLock = new Object();
    private string _lastMessage;

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
        OAuth2TokenEndpoint = pu.EndsWith("/") ? _pu + _ot : _pu + "/" + _ot;
        OAuth2TokenRevocationEndpoint = pu.EndsWith("/") ? _pu + _or : _pu + "/" + _or;
        OAuth2AuthorizationEndpoint = pu.EndsWith("/") ? _pu + _oa : _pu + "/" + _oa;
        IONAPIBaseUrl = _pu;
        ServiceAccountAccessKey = _saak;
        ServiceAccountSecretKey = _sask;
        _oauth2Http = new HttpClient();

    }

    public string LastMessage => _lastMessage;

    private void StoreToken(string token, string url)
    {
        lock (_tokenLock)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");

            string tokenFile = AppDomain.CurrentDomain.BaseDirectory +
                               rgx.Replace(url, "").Replace("https", "").Replace("http", "").Replace("443", "")
                               + ".token.bin";

            string[] tokenStore = new string[2];
            tokenStore[0] = token;
            tokenStore[1] = url;

            using (FileStream fs = new FileStream(tokenFile, FileMode.OpenOrCreate))
            {
                BinaryFormatter xs = new BinaryFormatter();
                xs.Serialize(fs, tokenStore);

                fs.Close();
            }

            File.SetLastWriteTime(tokenFile, expireTime);
        }
    }

    private string LoadToken(string oAuth2TokenEndpoint)
    {
        lock (_tokenLock)
        {
            try
            {
                Regex rgx = new Regex("[^a-zA-Z0-9 -]");

                string tokenFile = AppDomain.CurrentDomain.BaseDirectory +
                                   rgx.Replace(oAuth2TokenEndpoint, "").Replace("https", "").Replace("http", "")
                                       .Replace("443", "")
                                   + ".token.bin";

                if (!File.Exists(tokenFile))
                    return null;

                DateTime expiresFileDate = File.GetLastWriteTime(tokenFile);

                if (expiresFileDate < DateTime.Now)
                    return null;

                using (FileStream fs = new FileStream(tokenFile, FileMode.Open))
                {
                    BinaryFormatter xs = new BinaryFormatter();

                    string[] tokenStore = (string[])xs.Deserialize(fs);

                    fs.Close();

                    string token = tokenStore[0];
                    string url = tokenStore[1];
                    if (token == null || token.Equals("") || url != oAuth2TokenEndpoint)
                        return null;

                    expireTime = expiresFileDate;
                    return token;
                }

            }
            catch
            {
                return null;
            }
        }
    }

    public string GetBearerAuthorization()
    {
        lock (_tokenLock)
        {
            /*If current token is valid, use it.*/
            if (_token != null && expireTime > DateTime.Now)
                return _token.AccessToken;

            /*If TokenString is valid*/
            if (_sToken != null && !_sToken.Equals("") && expireTime > DateTime.Now)
                return _sToken;

            /*Load token from file if saved*/
            _sToken = LoadToken(OAuth2TokenEndpoint);

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
                StoreToken(_sToken, OAuth2TokenEndpoint);
                return _token.AccessToken;
            }

            /*Load a new token*/
            PasswordTokenRequest request = new PasswordTokenRequest
            {
                Address = OAuth2TokenEndpoint,
                ClientId = _ci,
                ClientSecret = _cs,
                AuthorizationHeaderStyle = BasicAuthenticationHeaderStyle.Rfc2617,
                ClientCredentialStyle = ClientCredentialStyle.AuthorizationHeader,
                UserName = _saak,
                Password = _sask
            };

            _token = _oauth2Http.RequestPasswordTokenAsync(request).Result;

            /* Save and Return Token if we have a good one*/
            if (_token != null && !_token.IsError)
            {
                /*Set Token Expire date*/
                expireTime = DateTime.Now.AddSeconds(_token.ExpiresIn - 300);
                _sToken = _token.AccessToken;
                StoreToken(_sToken, OAuth2TokenEndpoint);
                return _token.AccessToken;
            }
            else
            {
                if (_token != null)
                    _lastMessage = _token.ErrorDescription;
                return null;
            }
        }
    }

    private TokenResponse RefreshToken(string refreshToken)
    {
        Console.WriteLine(refreshToken);

        RefreshTokenRequest request = new RefreshTokenRequest
        {
            Address = OAuth2TokenEndpoint,
            ClientId = _ci,
            ClientSecret = _cs,
            RefreshToken = refreshToken
        };

        return _oauth2Http.RequestRefreshTokenAsync(request).Result;
    }

    public void RevokeToken()
    {

        HttpClient client = new HttpClient();
        client.SetBasicAuthentication(_ci, _cs);
        if (_token == null)
            return;
        var postBody = new Dictionary<string, string>
            {
                {"token", _token.AccessToken},
                {"token_type_hint", _token.TokenType}
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

    }

    public static IONAPI LoadIONAPI(string connection)
    {

        string connInfo = System.IO.File.ReadAllText(connection);

        dynamic dyn = JsonConvert.DeserializeObject(connInfo);

        if (dyn.saak == null || dyn.sask == null)
            return null;

        string or = dyn.or;
        string cn = dyn.cn;
        string ci = dyn.ci;
        string cs = dyn.cs;
        string pu = dyn.pu;
        string saak = dyn.saak;
        string sask = dyn.sask;
        string oa = dyn.oa;
        string ot = dyn.ot;

        IONAPI quickAPI = new IONAPI(cn, ci, cs, pu, saak, sask, oa, ot, or);

        return quickAPI;
    }
}


