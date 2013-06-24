using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DotNetOpenAuth.AspNet;
using DotNetOpenAuth.AspNet.Clients;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth;
using DotNetOpenAuth.OAuth.ChannelElements;
using DotNetOpenAuth.OAuth.Messages;
using TweetSharp;

namespace OpenAuthWebForms.Clients
{
    /// <summary>
    ///     Represents a Twitter client
    /// </summary>
    public class CustomTwitterClient : OAuthClient
    {
        #region Constants and Fields

        /// <summary>
        ///     The description of Twitter's OAuth protocol URIs for use with their "Sign in with Twitter" feature.
        /// </summary>
        public static readonly ServiceProviderDescription TwitterServiceDescription = new ServiceProviderDescription
            {
                RequestTokenEndpoint =
                    new MessageReceivingEndpoint(
                        "https://api.twitter.com/oauth/request_token",
                        HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
                UserAuthorizationEndpoint =
                    new MessageReceivingEndpoint(
                        "https://api.twitter.com/oauth/authenticate",
                        HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
                AccessTokenEndpoint =
                    new MessageReceivingEndpoint(
                        "https://api.twitter.com/oauth/access_token",
                        HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
                TamperProtectionElements =
                    new ITamperProtectionChannelBindingElement[] {new HmacSha1SigningBindingElement()},
            };

        private readonly string _consumerKey;
        private readonly string _consumerSecret;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="DotNetOpenAuth.AspNet.Clients.TwitterClient" /> class with the specified consumer key and consumer secret.
        /// </summary>
        /// <remarks>
        ///     Tokens exchanged during the OAuth handshake are stored in cookies.
        /// </remarks>
        /// <param name="consumerKey">
        ///     The consumer key.
        /// </param>
        /// <param name="consumerSecret">
        ///     The consumer secret.
        /// </param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "We can't dispose the object because we still need it through the app lifetime.")]
        public CustomTwitterClient(string consumerKey, string consumerSecret)
            : this(consumerKey, consumerSecret, new PersistentCookieOAuthTokenManagerCustom())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DotNetOpenAuth.AspNet.Clients.TwitterClient" /> class.
        /// </summary>
        /// <param name="consumerKey">The consumer key.</param>
        /// <param name="consumerSecret">The consumer secret.</param>
        /// <param name="tokenManager">The token manager.</param>
        public CustomTwitterClient(string consumerKey, string consumerSecret, IOAuthTokenManager tokenManager)
            : base(
                "twitter", TwitterServiceDescription,
                new SimpleConsumerTokenManager(consumerKey, consumerSecret, tokenManager))
        {
            _consumerKey = consumerKey;
            _consumerSecret = consumerSecret;
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
        ///     Authentication result
        /// </returns>
        protected override AuthenticationResult VerifyAuthenticationCore(AuthorizedTokenResponse response)
        {
            string accessToken = response.AccessToken;
            string userId = response.ExtraData["user_id"];
            string userName = response.ExtraData["screen_name"];
            string tokenSecret = (response as ITokenSecretContainingMessage).TokenSecret;

            // In v1.1, all API calls require authentication
            var service = new TwitterService(_consumerKey, _consumerSecret);
            service.AuthenticateWith(response.AccessToken, tokenSecret);

            TwitterUser profile = service.GetUserProfile(new GetUserProfileOptions());
            var extraData = new Dictionary<string, string>();

            extraData.Add("description", profile.Description);
            extraData.Add("profile_image_url", profile.ProfileImageUrl);
            extraData.Add("name", profile.Name);
            extraData.Add("location", profile.Location);
            extraData.Add("id", profile.Id.ToString());
            extraData.Add("url", profile.Url);
            extraData.Add("time_zone", profile.TimeZone);
            extraData.Add("status", profile.Status.Text);
            extraData.Add("screen_name", profile.ScreenName);
            extraData.Add("accesstoken", accessToken);

            return new AuthenticationResult(
                isSuccessful: true, provider: ProviderName, providerUserId: userId, userName: userName,
                extraData: extraData);
        }

        #endregion
    }
}