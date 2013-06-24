using System.Web;
using DotNetOpenAuth.AspNet.Clients;

namespace OpenAuthWebForms.Clients
{
    /// <summary>
    /// Stores OAuth tokens in the current request's cookie
    /// </summary>
    public class PersistentCookieOAuthTokenManagerCustom : AuthenticationOnlyCookieOAuthTokenManager
    {
        /// <summary>
        /// Key used for token cookie
        /// </summary>
        private const string TokenCookieKey = "OAuthTokenSecret";

        /// <summary>
        /// Primary request context.
        /// </summary>
        private readonly HttpContextBase primaryContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationOnlyCookieOAuthTokenManager"/> class.
        /// </summary>
        public PersistentCookieOAuthTokenManagerCustom()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationOnlyCookieOAuthTokenManager"/> class.
        /// </summary>
        /// <param name="context">The current request context.</param>
        public PersistentCookieOAuthTokenManagerCustom(HttpContextBase context)
            : base(context)
        {
            this.primaryContext = context;
        }

        /// <summary>
        /// Gets the effective HttpContext object to use.
        /// </summary>
        private HttpContextBase Context
        {
            get
            {
                return this.primaryContext ?? new HttpContextWrapper(HttpContext.Current);
            }
        }

        /// <summary>
        /// Replaces the request token with access token.
        /// </summary>
        /// <param name="requestToken">The request token.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="accessTokenSecret">The access token secret.</param>
        public new void ReplaceRequestTokenWithAccessToken(string requestToken, string accessToken, string accessTokenSecret)
        {
            //remove old requestToken Cookie
            //var cookie = new HttpCookie(TokenCookieKey)
            //{
            //    Value = string.Empty,
            //    Expires = DateTime.UtcNow.AddDays(-5)
            //};
            //this.Context.Response.Cookies.Set(cookie);

            //Add new AccessToken + secret Cookie
            StoreRequestToken(accessToken, accessTokenSecret);
        }
    }
}