using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Web.WebPages.OAuth;
using OpenAuth.Models;

namespace OpenAuth
{
    public static class AuthConfig
    {
        public static void RegisterAuth()
        {
            // To let users of this site log in using their accounts from other sites such as Microsoft, Facebook, and Twitter,
            // you must update this site. For more information visit http://go.microsoft.com/fwlink/?LinkID=252166

            //OAuthWebSecurity.RegisterMicrosoftClient(
            //    clientId: "",
            //    clientSecret: "");

            OAuthWebSecurity.RegisterTwitterClient(
                consumerKey: "kFc4rnvlWQIrPGG3Fnw",
                consumerSecret: "iiLQ7YRVx7vbDkWJRCHs9rTguDgatsUvPhIrIOsAzhs");

            OAuthWebSecurity.RegisterFacebookClient(
                appId: "430032487095089",
                appSecret: "eb9f9db3a2b62b15f695ed4052458413");

            //OAuthWebSecurity.RegisterGoogleClient();
        }
    }
}
