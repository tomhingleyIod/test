using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Xml.Linq;
using DotNetOpenAuth.AspNet;
using DotNetOpenAuth.AspNet.Clients;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth;
using DotNetOpenAuth.OAuth.ChannelElements;
using DotNetOpenAuth.OAuth.Messages;

namespace OpenAuthWebForms.Clients
{
    /// <summary>
    ///     Represents LinkedIn authentication client.
    /// </summary>
    public class LinkedInCustomClient : OAuthClient
    {
        #region Constants and Fields

        /// <summary>
        ///     Describes the OAuth service provider endpoints for LinkedIn.
        /// </summary>
        public static readonly ServiceProviderDescription LinkedInServiceDescription = new ServiceProviderDescription
            {
                RequestTokenEndpoint =
                    new MessageReceivingEndpoint(
                        "https://api.linkedin.com/uas/oauth/requestToken",
                        HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
                UserAuthorizationEndpoint =
                    new MessageReceivingEndpoint(
                        "https://www.linkedin.com/uas/oauth/authenticate?scope=r_fullprofile+r_emailaddress+r_contactinfo",
                        HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
                AccessTokenEndpoint =
                    new MessageReceivingEndpoint(
                        "https://api.linkedin.com/uas/oauth/accessToken",
                        HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
                TamperProtectionElements =
                    new ITamperProtectionChannelBindingElement[] {new HmacSha1SigningBindingElement()},
            };

        private readonly string[] _fields;

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
        public LinkedInCustomClient(string consumerKey, string consumerSecret)
            : this(
                consumerKey, consumerSecret,
                "id,first-name,last-name,maiden-name,formatted-name,headline,location:(name),current-share,specialties,picture-url,public-profile-url,industry,summary,interests,email-address,phone-numbers,main-address,primary-twitter-account,honors,mfeed-rss-url,date-of-birth"
                    .Split(','), new CookieOAuthTokenManager())
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
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "We can't dispose the object because we still need it through the app lifetime.")]
        public LinkedInCustomClient(string consumerKey, string consumerSecret, string[] fields)
            : this(consumerKey, consumerSecret, fields, new CookieOAuthTokenManager())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DotNetOpenAuth.AspNet.Clients.LinkedInClient" /> class.
        /// </summary>
        /// <param name="consumerKey">The consumer key.</param>
        /// <param name="consumerSecret">The consumer secret.</param>
        /// <param name="tokenManager">The token manager.</param>
        /// <param name="fields"></param>
        public LinkedInCustomClient(string consumerKey, string consumerSecret, string[] fields,
                                    IOAuthTokenManager tokenManager)
            : base(
                "linkedIn", LinkedInServiceDescription,
                new SimpleConsumerTokenManager(consumerKey, consumerSecret, tokenManager))
        {
            _fields = fields;
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
            // permissions set in developer site (r_fullprofile, r_emailaddress, r_contactinfo) and defined in auth scope 
            // See here for Field Selectors API http://developer.linkedin.com/docs/DOC-1014
            // http://developer.linkedin.com/documents/profile-fields
            string profileRequestUrl = string.Format("https://api.linkedin.com/v1/people/~:({0})",
                                                     string.Join(",", _fields));
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
                            XDocument document = XDocument.Load(responseStream);
                            string userId = document.Root.Element("id").Value;
                            string userName = document.Root.Element("email-address").Value;
                            var extraData = new Dictionary<string, string>();

                            foreach (XElement element in document.Root.Elements())
                                extraData.Add(element.Name.LocalName, element.Value);

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