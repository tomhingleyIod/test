using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using DotNetOpenAuth.AspNet;
using DotNetOpenAuth.AspNet.Clients;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth;
using DotNetOpenAuth.OAuth.ChannelElements;
using DotNetOpenAuth.OAuth.Messages;
using Newtonsoft.Json;

namespace OpenAuthWebForms.Clients
{
    /// <summary>
    ///     Represents Facebook authentication client.
    /// </summary>
    public class FacebookCustomClient : OAuthClient
    {
        #region Constants and Fields

        /// <summary>
        ///     Describes the OAuth service provider endpoints for LinkedIn.
        /// </summary>
        public static readonly ServiceProviderDescription ServiceDescription = new ServiceProviderDescription
            {
                RequestTokenEndpoint =
                    new MessageReceivingEndpoint(
                        "https://www.facebook.com/dialog/oauth?scope=email user_about_me",
                        HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
                UserAuthorizationEndpoint =
                    new MessageReceivingEndpoint(
                        "https://www.facebook.com/dialog/oauth?scope=email user_about_me",
                        HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
                AccessTokenEndpoint =
                    new MessageReceivingEndpoint(
                        "https://graph.facebook.com/oauth/access_token",
                        HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
                TamperProtectionElements =
                    new ITamperProtectionChannelBindingElement[] {new HmacSha1SigningBindingElement()},
            };

        private readonly string[] _scopes;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="DotNetOpenAuth.AspNet.Clients.LinkedInClient" /> class.
        /// </summary>
        /// <remarks>
        ///     Tokens exchanged during the OAuth handshake are stored in cookies.
        ///     Authorised to return full profile, email address, and contact info.
        /// </remarks>
        /// <param name="consumerKey">
        ///     The LinkedIn app's consumer key.
        /// </param>
        /// <param name="consumerSecret">
        ///     The LinkedIn app's consumer secret.
        /// </param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "We can't dispose the object because we still need it through the app lifetime.")]
        public FacebookCustomClient(string consumerKey, string consumerSecret)
            : this(
                consumerKey, consumerSecret,
                new[] {"email", "user_about_me"}, new CookieOAuthTokenManager())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DotNetOpenAuth.AspNet.Clients.LinkedInClient" /> class.
        /// </summary>
        /// <remarks>
        ///     Tokens exchanged during the OAuth handshake are stored in cookies.
        /// </remarks>
        /// <param name="consumerKey">
        ///     The LinkedIn app's consumer key.
        /// </param>
        /// <param name="consumerSecret">
        ///     The LinkedIn app's consumer secret.
        /// </param>
        /// <param name="fields"></param>
        public FacebookCustomClient(string consumerKey, string consumerSecret, string[] scopes)
            : this(consumerKey, consumerSecret, scopes, new CookieOAuthTokenManager())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DotNetOpenAuth.AspNet.Clients.LinkedInClient" /> class.
        /// </summary>
        /// <param name="consumerKey">The consumer key.</param>
        /// <param name="consumerSecret">The consumer secret.</param>
        /// <param name="tokenManager">The token manager.</param>
        /// <param name="fields"></param>
        public FacebookCustomClient(string consumerKey, string consumerSecret, string[] scopes,
                                    IOAuthTokenManager tokenManager)
            : base(
                "facebook", ServiceDescription,
                new SimpleConsumerTokenManager(consumerKey, consumerSecret, tokenManager))
        {
            _scopes = scopes;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Check if authentication succeeded after user is redirected back from the service provider.
        /// </summary>
        /// <param name="response">
        ///     The response token returned from service provider
        /// </param>
        /// <returns>
        ///     Authentication result.
        /// </returns>
        protected override AuthenticationResult VerifyAuthenticationCore(AuthorizedTokenResponse response)
        {
            const string profileRequestUrl = "https://graph.facebook.com/me";
            string accessToken = response.AccessToken;
            var profileEndpoint = new MessageReceivingEndpoint(profileRequestUrl, HttpDeliveryMethods.GetRequest);
            HttpWebRequest request = WebWorker.PrepareAuthorizedRequest(profileEndpoint, accessToken);

            try
            {
                using (WebResponse profileResponse = request.GetResponse())
                {
                    using (Stream responseStream = profileResponse.GetResponseStream())
                    {
                        using (var streamReader = new StreamReader(responseStream))
                        {
                            var document = XDocument.Load(responseStream);
                            string userId = document.Root.Element("id").Value;
                            string userName = document.Root.Element("email-address").Value;
                            var extraData =
                                JsonConvert.DeserializeObject<Dictionary<string, object>>(streamReader.ReadToEnd())
                                           .ToDictionary((x => x.Key), (x => x.Value.ToString()));

                            extraData.Add("picture",
                                          string.Format("https://graph.facebook.com/{0}/picture", extraData["id"]));

                            return new AuthenticationResult(
                                isSuccessful: true, provider: ProviderName, providerUserId: userId,
                                userName: userName,
                                extraData: extraData);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                return new AuthenticationResult(exception);
            }
        }

        #endregion
    }
}