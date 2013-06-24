//using DotNetOpenAuth;
//using DotNetOpenAuth.AspNet.Clients;
//using DotNetOpenAuth.Messaging;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Net;
//using System.Web;

//namespace DotNetOpenAuth.FacebookOAuth2
//{
//  public class CustomFacebookClient : OAuth2Client
//  {
//        /// <summary>
//        /// The authorization endpoint.
//        /// 
//        /// </summary>
//        private const string AuthorizationEndpoint = "https://www.facebook.com/dialog/oauth";
//        /// <summary>
//        /// The token endpoint.
//        /// 
//        /// </summary>
//        private const string TokenEndpoint = "https://graph.facebook.com/oauth/access_token";
//        /// <summary>
//        /// The _app id.
//        /// 
//        /// </summary>
//        private readonly string appId;
//        /// <summary>
//        /// The _app secret.
//        /// 
//        /// </summary>
//        private readonly string appSecret;

//        private readonly string[] _requestedScopes;

//        /// <summary>
//        /// Initializes a new instance of the <see cref="T:DotNetOpenAuth.AspNet.Clients.FacebookClient"/> class.
//        /// 
//        /// </summary>
//        /// <param name="appId">The app id.
//        ///             </param><param name="appSecret">The app secret.
//        ///             </param>
//        public CustomFacebookClient(string appId, string appSecret, params string[] requestedScopes)
//            : base("facebook")
//        {
//            if (string.IsNullOrWhiteSpace(appId))
//                throw new ArgumentNullException("appId");
//            if (string.IsNullOrWhiteSpace(appSecret))
//                throw new ArgumentNullException("appSecret");
//            if (requestedScopes == null)
//                throw new ArgumentNullException("requestedScopes");
//            if (requestedScopes.Length == 0)
//                throw new ArgumentException("One or more scopes must be requested.", "requestedScopes");
//            this.appId = appId;
//            this.appSecret = appSecret;
//            this._requestedScopes = requestedScopes;
//        }

//        /// <summary>
//        /// The get service login url.
//        /// 
//        /// </summary>
//        /// <param name="returnUrl">The return url.
//        ///             </param>
//        /// <returns>
//        /// An absolute URI.
//        /// </returns>
//        protected override Uri GetServiceLoginUrl(Uri returnUrl)
//        {
//            UriBuilder builder = new UriBuilder("https://www.facebook.com/dialog/oauth");
//            MessagingUtilities.AppendQueryArgs(builder, (IEnumerable<KeyValuePair<string, string>>)new Dictionary<string, string>()
//      {
//        {
//          "client_id",
//          this.appId
//        },
//        {
//          "redirect_uri",
//          returnUrl.AbsoluteUri
//        },
//        {
//          "scope",
//          "email"
//        }
//      });
//            return builder.Uri;
//        }

//        /// <summary>
//        /// The get user data.
//        /// 
//        /// </summary>
//        /// <param name="accessToken">The access token.
//        ///             </param>
//        /// <returns>
//        /// A dictionary of profile data.
//        /// </returns>
//        protected override IDictionary<string, string> GetUserData(string accessToken)
//        {
//            FacebookGraphData facebookGraphData;
//            using (WebResponse response = WebRequest.Create("https://graph.facebook.com/me?access_token=" + MessagingUtilities.EscapeUriDataStringRfc3986(accessToken)).GetResponse())
//            {
//                using (Stream responseStream = response.GetResponseStream())
//                    facebookGraphData = JsonHelper.Deserialize<FacebookGraphData>(responseStream);
//            }
//            Dictionary<string, string> dictionary = new Dictionary<string, string>();
//            DictionaryExtensions.AddItemIfNotEmpty((IDictionary<string, string>)dictionary, "id", facebookGraphData.Id);
//            DictionaryExtensions.AddItemIfNotEmpty((IDictionary<string, string>)dictionary, "username", facebookGraphData.Email);
//            DictionaryExtensions.AddItemIfNotEmpty((IDictionary<string, string>)dictionary, "name", facebookGraphData.Name);
//            DictionaryExtensions.AddItemIfNotEmpty((IDictionary<string, string>)dictionary, "link", facebookGraphData.Link == (Uri)null ? (string)null : facebookGraphData.Link.AbsoluteUri);
//            DictionaryExtensions.AddItemIfNotEmpty((IDictionary<string, string>)dictionary, "gender", facebookGraphData.Gender);
//            DictionaryExtensions.AddItemIfNotEmpty((IDictionary<string, string>)dictionary, "birthday", facebookGraphData.Birthday);
//            return (IDictionary<string, string>)dictionary;
//        }

//        /// <summary>
//        /// Obtains an access token given an authorization code and callback URL.
//        /// 
//        /// </summary>
//        /// <param name="returnUrl">The return url.
//        ///             </param><param name="authorizationCode">The authorization code.
//        ///             </param>
//        /// <returns>
//        /// The access token.
//        /// 
//        /// </returns>
//        protected override string QueryAccessToken(Uri returnUrl, string authorizationCode)
//        {
//            UriBuilder builder = new UriBuilder("https://graph.facebook.com/oauth/access_token");
//            MessagingUtilities.AppendQueryArgs(builder, (IEnumerable<KeyValuePair<string, string>>)new Dictionary<string, string>()
//      {
//        {
//          "client_id",
//          this.appId
//        },
//        {
//          "redirect_uri",
//          FacebookClient.NormalizeHexEncoding(returnUrl.AbsoluteUri)
//        },
//        {
//          "client_secret",
//          this.appSecret
//        },
//        {
//          "code",
//          authorizationCode
//        },
//        {
//          "scope",
//          "email"
//        }
//      });
//            using (WebClient webClient = new WebClient())
//            {
//                string query = webClient.DownloadString(builder.Uri);
//                if (string.IsNullOrEmpty(query))
//                    return (string)null;
//                else
//                    return HttpUtility.ParseQueryString(query)["access_token"];
//            }
//        }

//        /// <summary>
//        /// Converts any % encoded values in the URL to uppercase.
//        /// 
//        /// </summary>
//        /// <param name="url">The URL string to normalize</param>
//        /// <returns>
//        /// The normalized url
//        /// </returns>
//        /// 
//        /// <example>
//        /// NormalizeHexEncoding("Login.aspx?ReturnUrl=%2fAccount%2fManage.aspx") returns "Login.aspx?ReturnUrl=%2FAccount%2FManage.aspx"
//        /// </example>
//        /// 
//        /// <remarks>
//        /// There is an issue in Facebook whereby it will rejects the redirect_uri value if
//        ///             the url contains lowercase % encoded values.
//        /// 
//        /// </remarks>
//        private static string NormalizeHexEncoding(string url)
//        {
//            char[] chArray = url.ToCharArray();
//            for (int index = 0; index < chArray.Length - 2; ++index)
//            {
//                if ((int)chArray[index] == 37)
//                {
//                    chArray[index + 1] = char.ToUpperInvariant(chArray[index + 1]);
//                    chArray[index + 2] = char.ToUpperInvariant(chArray[index + 2]);
//                    index += 2;
//                }
//            }
//            return new string(chArray);
//        }
//    }
//}