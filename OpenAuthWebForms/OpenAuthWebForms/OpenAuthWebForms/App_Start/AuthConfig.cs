using DotNetOpenAuth.AspNet.Clients;
using DotNetOpenAuth.FacebookOAuth2;
using DotNetOpenAuth.GoogleOAuth2;
using Microsoft.AspNet.Membership.OpenAuth;
using OpenAuthWebForms.Clients;

namespace OpenAuthWebForms
{
    internal static class AuthConfig
    {
        public static void RegisterOpenAuth()
        {
            // See http://go.microsoft.com/fwlink/?LinkId=252803 for details on setting up this ASP.NET
            // application to support logging in via external services.

            OpenAuth.AuthenticationClients.Add("Facebook", () => new FacebookOAuth2Client("430032487095089",
                                                                                          "eb9f9db3a2b62b15f695ed4052458413",
                                                                                          new[]
                                                                                              {"email", "user_about_me"}));
            OpenAuth.AuthenticationClients.Add("Twitter",
                                               () => new CustomTwitterClient(consumerKey: "kFc4rnvlWQIrPGG3Fnw",
                                                                             consumerSecret:
                                                                                 "iiLQ7YRVx7vbDkWJRCHs9rTguDgatsUvPhIrIOsAzhs"));

            OpenAuth.AuthenticationClients.Add("LinkedIn", () => new LinkedInCustomClient("40v80acctnq5", "Tsdc0fJoV5aEPH99"));
            //OpenAuth.AuthenticationClients.Add("LinkedIn", () => new LinkedInClient("40v80acctnq5", "Tsdc0fJoV5aEPH99"));

            OpenAuth.AuthenticationClients.Add("Google",
                                               () => new GoogleOAuth2Client("607713003287.apps.googleusercontent.com",
                                                                            "Yqk0HAcM7VIXy2if_nLkMaAN"));

            //OpenAuth.AuthenticationClients.AddGoogle();

            //OpenAuth.AuthenticationClients.AddMicrosoft(
            //    clientId: "your Microsoft account client id",
            //    clientSecret: "your Microsoft account client secret");
        }
    }
}