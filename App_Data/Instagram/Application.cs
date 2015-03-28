using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InstaAPI.Auth;
using InstaAPI.Endpoints.Unauthenticated;
using InstaAPI.Endpoints.OptionalParameters;
using InstaAPI.Entities;
using DownGramer.App_Data.Entities;

namespace DownGramer.App_Data.Instagram
{
    public class Application
    {
        private String ClientId = "c53c72f4d6624ea5a97773a179f02804";
        private String ClientSecret = "c21b1f4554d44c079c4a2f6f62f4f561";
        private String RedirectUri = "http://downgramer.netai.net/";
        private List<Scope> Scopes = new List<Scope>(){Scope.basic};
        private InstaConfig Config = null;
        private LogException MyException = null;

        /// <summary>
        ///     <para>constructor with zero parms</para>
        /// </summary>
        public Application()
        {
            try
            {
                MyException = new LogException();
                Config = new InstaConfig(ClientId, ClientSecret, RedirectUri, Scopes);
            }
            catch (Exception Ex)
            {
                Console.Write(Ex.Message);
            }
        }
        
        /***************************************************************************************************/

        /// <summary>
        ///     <para>gets InstaConfig prepared for the current application</para>
        /// </summary>
        /// <returns></returns>
        public InstaConfig GetInstaConfig()
        {
            return Config;
        }

        /***************************************************************************************************/

        /// <summary>
        ///     <para>gets the authentication uri to which the user has to be redirected</para>
        /// </summary>
        /// <returns></returns>
        public String GetAuthenticationUri()
        {
            String AuthenticationUri = String.Empty;

            if (Config != null)
            {
                AuthenticationUri = Config.GetAuthenticationUriString();
            }
            else
            {
                throw new Exception("InstaConfig is null");
            }

            return AuthenticationUri;
        }

        /***************************************************************************************************/

        /// <summary>
        ///     <para>gets the popular feed at the moment</para>
        /// </summary>
        /// <returns></returns>
        public List<String> GetPopularFeed()
        {
            List<String> PopularFeed = new List<String>();

            try
            {
                if (Config == null)
                {
                    throw new Exception("IntaConfig is null");
                }
                
                UnMedia Media = new UnMedia(Config);
                MediaPopular Popular = Media.GetMediaPopular();

                foreach (var Feed in Popular.Data)
                {
                    PopularFeed.Add(Feed.Images.Thumbnail.url);
                }
            }
            catch (Exception Ex)
            {
                MyException.EnterLog(Ex);
            }

            return PopularFeed;
        }
    }
}
