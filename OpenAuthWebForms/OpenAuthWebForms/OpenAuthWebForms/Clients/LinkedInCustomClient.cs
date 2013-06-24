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
                        "https://www.linkedin.com/uas/oauth/authenticate",
                        HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
                AccessTokenEndpoint =
                    new MessageReceivingEndpoint(
                        "https://api.linkedin.com/uas/oauth/accessToken",
                        HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
                TamperProtectionElements =
                    new ITamperProtectionChannelBindingElement[] {new HmacSha1SigningBindingElement()},
            };

        #endregion

        #region Constructors and Destructors

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
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "We can't dispose the object because we still need it through the app lifetime.")]
        public LinkedInCustomClient(string consumerKey, string consumerSecret)
            : this(consumerKey, consumerSecret, new CookieOAuthTokenManager())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DotNetOpenAuth.AspNet.Clients.LinkedInClient" /> class.
        /// </summary>
        /// <param name="consumerKey">The consumer key.</param>
        /// <param name="consumerSecret">The consumer secret.</param>
        /// <param name="tokenManager">The token manager.</param>
        public LinkedInCustomClient(string consumerKey, string consumerSecret, IOAuthTokenManager tokenManager)
            : base(
                "linkedIn", LinkedInServiceDescription,
                new SimpleConsumerTokenManager(consumerKey, consumerSecret, tokenManager))
        {
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
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We don't care if the request fails.")]
        protected override AuthenticationResult VerifyAuthenticationCore(AuthorizedTokenResponse response)
        {
            // See here for Field Selectors API http://developer.linkedin.com/docs/DOC-1014
            const string ProfileRequestUrl =
                "https://api.linkedin.com/v1/people/~:(id,first-name,last-name,headline,industry,summary,interests,email-address)";

            // permissions set in developer site (r_fullprofile, r_emailaddress, r_contactinfo)
            //const string ProfileRequestUrl =
                //"https://api.linkedin.com/v1/people/~:)";

            string accessToken = response.AccessToken;

            var profileEndpoint = new MessageReceivingEndpoint(ProfileRequestUrl, HttpDeliveryMethods.GetRequest);
            HttpWebRequest request = WebWorker.PrepareAuthorizedRequest(profileEndpoint, accessToken);

            try
            {
                using (WebResponse profileResponse = request.GetResponse())
                {
                    using (Stream responseStream = profileResponse.GetResponseStream())
                    {
                        using (StreamReader streamReader = new StreamReader(responseStream))
                        {
                            XDocument document = XDocument.Load(responseStream);
                            string userId = document.Root.Element("id").Value;
                            string userName = document.Root.Element("email-address").Value;

                            //id,first-name,last-name,headline,industry,summary,interests
                            var extraData = new Dictionary<string, string>();
                            extraData.Add("accesstoken", accessToken);
                            extraData.Add("id", userId);
                            extraData.Add("email-address", userName);
                            extraData.Add("first-name", document.Root.Element("first-name").Value);
                            extraData.Add("last-name", document.Root.Element("last-name").Value);
                            extraData.Add("headline", document.Root.Element("headline").Value);
                            extraData.Add("industry", document.Root.Element("industry").Value);
                            //extraData.Add("summary", document.Root.Element("summary").Value);
                            //extraData.Add("interests", document.Root.Element("interests").Value);

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