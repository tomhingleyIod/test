using System;
using System.Collections.Specialized;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using DotNetOpenAuth.AspNet;
using DotNetOpenAuth.AspNet.Clients;
using DotNetOpenAuth.FacebookOAuth2;
using DotNetOpenAuth.GoogleOAuth2;
using Microsoft.AspNet.Membership.OpenAuth;

namespace OpenAuthWebForms.Account
{
    public partial class RegisterExternalLogin : Page
    {
        protected string ProviderName
        {
            get { return (string) ViewState["ProviderName"] ?? String.Empty; }
            private set { ViewState["ProviderName"] = value; }
        }

        protected string ProviderDisplayName
        {
            get { return (string) ViewState["ProviderDisplayName"] ?? String.Empty; }
            private set { ViewState["ProviderDisplayName"] = value; }
        }

        protected string ProviderUserId
        {
            get { return (string) ViewState["ProviderUserId"] ?? String.Empty; }
            private set { ViewState["ProviderUserId"] = value; }
        }

        protected string ProviderUserName
        {
            get { return (string) ViewState["ProviderUserName"] ?? String.Empty; }
            private set { ViewState["ProviderUserName"] = value; }
        }

        protected void Page_Load()
        {
            if (!IsPostBack)
            {
                ProcessProviderResult();
            }
        }

        protected void logIn_Click(object sender, EventArgs e)
        {
            CreateAndLoginUser();
        }

        protected void cancel_Click(object sender, EventArgs e)
        {
            RedirectToReturnUrl();
        }

        private static string JoinNvcToQs(NameValueCollection qs)
        {
            return string.Join("&", Array.ConvertAll(qs.AllKeys, key => string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(qs[key]))));
        }

        private void RewriteRequest()
        {
            HttpContext current = HttpContext.Current;
            string query = HttpUtility.UrlDecode(current.Request.QueryString["state"]);
            if (query == null || !query.Contains("__provider__="))
                return;
            NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(query);
            nameValueCollection.Add(current.Request.QueryString);
            nameValueCollection.Remove("state");
            current.RewritePath(current.Request.Path + (object)"?" + JoinNvcToQs(nameValueCollection));
        }

        private void ProcessProviderResult()
        {
            // custom
            // add this line
            //This is needed because Facebook requires that any extra querystring parameters for the redirect be 
            //packed into a single parameter called state. Since OAuthWebSecurity needs two parameters, 
            //__provider__ and __sid__ - we have to rewrite the url.
            // ref https://github.com/mj1856/DotNetOpenAuth.FacebookOAuth2
            //FacebookOAuth2Client.RewriteRequest();
            //GoogleOAuth2Client.RewriteRequest();

            RewriteRequest();

            // Process the result from an auth provider in the request
            ProviderName = OpenAuth.GetProviderNameFromCurrentRequest();

            if (String.IsNullOrEmpty(ProviderName))
            {
                Response.Redirect(FormsAuthentication.LoginUrl);
            }

            // Build the redirect url for OpenAuth verification
            string redirectUrl = "~/Account/RegisterExternalLogin";
            string returnUrl = Request.QueryString["ReturnUrl"];
            if (!String.IsNullOrEmpty(returnUrl))
            {
                redirectUrl += "?ReturnUrl=" + HttpUtility.UrlEncode(returnUrl);
            }

            // Verify the OpenAuth payload
            AuthenticationResult authResult = OpenAuth.VerifyAuthentication(redirectUrl);
            ProviderDisplayName = OpenAuth.GetProviderDisplayName(ProviderName);
            if (!authResult.IsSuccessful)
            {
                Title = "External login failed";
                userNameForm.Visible = false;

                ModelState.AddModelError("Provider", String.Format("External login {0} failed.", ProviderDisplayName));

                // To view this error, enable page tracing in web.config (<system.web><trace enabled="true"/></system.web>) and visit ~/Trace.axd
                Trace.Warn("OpenAuth",
                           String.Format("There was an error verifying authentication with {0})", ProviderDisplayName),
                           authResult.Error);
                return;
            }

            // User has logged in with provider successfully
            // Check if user is already registered locally
            if (OpenAuth.Login(authResult.Provider, authResult.ProviderUserId, createPersistentCookie: false))
            {
                RedirectToReturnUrl();
            }

            // Store the provider details in ViewState
            ProviderName = authResult.Provider;
            ProviderUserId = authResult.ProviderUserId;
            ProviderUserName = authResult.UserName;

            // Strip the query string from action
            Form.Action = ResolveUrl(redirectUrl);

            if (User.Identity.IsAuthenticated)
            {
                // User is already authenticated, add the external login and redirect to return url
                OpenAuth.AddAccountToExistingUser(ProviderName, ProviderUserId, ProviderUserName, User.Identity.Name);
                RedirectToReturnUrl();
            }
            else
            {
                // User is new, ask for their desired membership name
                userName.Text = authResult.UserName;
                //LinkedInClient
                foreach (var x in authResult.ExtraData)
                {
                    var li = new HtmlGenericControl("li");
                    var txt = new TextBox {ID = "txt" + x.Key, Text = x.Value};
                    var lbl = new Label { ID = "lbl" + x.Key, Text = x.Key, AssociatedControlID = txt.ID };

                    li.Controls.Add(lbl);
                    li.Controls.Add(txt);

                    plcFields.Controls.Add(li);
                }
            }
        }

        private void CreateAndLoginUser()
        {
            if (!IsValid)
            {
                return;
            }

            CreateResult createResult = OpenAuth.CreateUser(ProviderName, ProviderUserId, ProviderUserName,
                                                            userName.Text);
            if (!createResult.IsSuccessful)
            {
                ModelState.AddModelError("UserName", createResult.ErrorMessage);
            }
            else
            {
                // User created & associated OK
                if (OpenAuth.Login(ProviderName, ProviderUserId, createPersistentCookie: false))
                {
                    RedirectToReturnUrl();
                }
            }
        }

        private void RedirectToReturnUrl()
        {
            string returnUrl = Request.QueryString["ReturnUrl"];
            if (!String.IsNullOrEmpty(returnUrl) && OpenAuth.IsLocalUrl(returnUrl))
            {
                Response.Redirect(returnUrl);
            }
            else
            {
                Response.Redirect("~/");
            }
        }
    }
}