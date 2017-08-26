﻿//******************************************************************************************************
//  AuthenticationHandler.cs - Gbtc
//
//  Copyright © 2017, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  08/25/2017 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GSF.Security;
using Microsoft.Owin.Security;

namespace GSF.Web.Security
{
    /// <summary>
    /// Handles authentication using the configured <see cref="ISecurityProvider"/> implementation in the Owin pipeline.
    /// </summary>
    public class AuthenticationHandler : Microsoft.Owin.Security.Infrastructure.AuthenticationHandler<AuthenticationOptions>
    {
        #region [ Properties ]

        // Reads the authorization type from the HTTP headers.
        private string AuthorizationType
        {
            get
            {
                string[] authorization = Request.Headers["Authorization"]?.Split(' ');

                if ((object)authorization == null)
                    return null;

                if (authorization.Length == 0)
                    return null;

                return authorization[0];
            }
        }

        // Reads the authorization credentials from the HTTP headers.
        private string AuthorizationCredentials
        {
            get
            {
                string[] authorization = Request.Headers["Authorization"]?.Split(' ');

                if ((object)authorization == null)
                    return null;

                if (authorization.Length < 2)
                    return null;

                return authorization[1];
            }
        }

        // Gets a principal that represents an unauthenticated anonymous user.
        private IPrincipal AnonymousPrincipal
        {
            get
            {
                IIdentity anonymousIdentity = new GenericIdentity("anonymous");
                return new GenericPrincipal(anonymousIdentity, new string[0]);
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// The core authentication logic which must be provided by the handler. Will be
        /// invoked at most once per request. Do not call directly, call the wrapping Authenticate
        /// method instead.
        /// </summary>
        /// <returns>The ticket data provided by the authentication logic</returns>
        protected override Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            return Task.Run(() =>
            {
                SecurityPrincipal securityPrincipal = null;

                // Attempt to read the session ID from the HTTP cookies
                Guid sessionID = SessionHandler.GetSessionIDFromCookie(Request, Options.SessionToken);

                // Attempt to retrieve the user's credentials that were cached to the user's session
                if (!s_authorizationCache.TryGetValue(sessionID, out securityPrincipal))
                {
                    // Pick the appropriate authentication logic based
                    // on the authorization type in the HTTP headers
                    if (AuthorizationType == "Basic")
                        securityPrincipal = AuthenticateBasic();
                    else
                        securityPrincipal = AuthenticatePassthrough();

                    // Attempt to cache the security principal to the session
                    if (sessionID != Guid.Empty && securityPrincipal?.Identity.IsAuthenticated == true)
                        s_authorizationCache[sessionID] = securityPrincipal;
                }

                // If the user fails to authenticate, adjust the HTTP response
                if (securityPrincipal?.Identity.IsAuthenticated != true)
                {
                    Context.Response.ReasonPhrase = GetReasonPhrase(securityPrincipal);
                    Context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

                    if ((object)securityPrincipal == null && AuthorizationType == "Basic")
                        Context.Response.Redirect(Options.LoginPage);
                }

                // Set the principal of the IOwinRequest so
                // that it can be propagated across the system
                Request.User = securityPrincipal ?? AnonymousPrincipal;

                return (AuthenticationTicket)null;
            });
        }

        /// <summary>
        /// Override this method to deal with 401 challenge concerns, if an authentication
        /// scheme in question deals an authentication interaction as part of its request
        /// flow. (like adding a response header, or changing the 401 result to 302 of a
        /// login page or external sign-in location.)
        /// </summary>
        protected override Task ApplyResponseChallengeAsync()
        {
            string realm = null;

            if (Response.StatusCode == (int)HttpStatusCode.Unauthorized)
            {
                if (!string.IsNullOrWhiteSpace(Options.Realm))
                    realm = " realm=\"" + Options.Realm + "\"";

                Request.Headers["WWW-Authenticate"] = "Basic" + realm;
            }

            return base.ApplyResponseChallengeAsync();
        }

        // Applies authentication for requests where credentials are passed directly in the HTTP headers.
        private SecurityPrincipal AuthenticateBasic()
        {
            string username, password;

            // Get the user's credentials from the HTTP headers
            if (!TryParseCredentials(AuthorizationCredentials, out username, out password))
                return null;

            // Create the security provider that will authenticate the user's credentials
            ISecurityProvider securityProvider = SecurityProviderCache.CreateProvider(username);
            securityProvider.Password = password;
            securityProvider.Authenticate();

            // Return the security principal that will be used for role-based authorization
            SecurityIdentity securityIdentity = new SecurityIdentity(securityProvider);
            return new SecurityPrincipal(securityIdentity);
        }

        // Applies authentication for requests using Windows passthrough authentication.
        private SecurityPrincipal AuthenticatePassthrough()
        {
            string username = Request.User?.Identity.Name;

            if ((object)username == null)
                return null;

            // Get the principal used for verifying the user's passthrough authentication
            IPrincipal passthroughPrincipal = Request.User;

            // Create the security provider that will verify the user's passthrough authentication
            ISecurityProvider securityProvider = SecurityProviderCache.CreateProvider(username);
            securityProvider.PassthroughPrincipal = passthroughPrincipal;
            securityProvider.Authenticate();

            // Return the security principal that will be used for role-based authorization
            SecurityIdentity securityIdentity = new SecurityIdentity(securityProvider);
            return new SecurityPrincipal(securityIdentity);
        }

        // Determines the reason phrase to return in the HTTP failure response.
        private string GetReasonPhrase(SecurityPrincipal securityPrincipal)
        {
            if ((object)securityPrincipal == null)
            {
                // Indicates either the credentials could not be
                // parsed or passthrough authentication failed
                return "Invalid user name or password";
            }
            else
            {
                // The security provider should be able to provide a reason for the failure
                string failureReason = securityPrincipal.Identity.Provider.AuthenticationFailureReason;

                if (!string.IsNullOrEmpty(failureReason))
                    return failureReason;

                // If no reason was provided by the security provider,
                // return a generic error message based on the authorization type
                if (AuthorizationType == "Basic")
                    return "Invalid credentials";
                else
                    return "Missing credentials";
            }
        }

        #endregion

        #region [ Static ]

        // Static Fields
        private static readonly ConcurrentDictionary<Guid, SecurityPrincipal> s_authorizationCache;

        // Static Constructor
        static AuthenticationHandler()
        {
            s_authorizationCache = new ConcurrentDictionary<Guid, SecurityPrincipal>();

            // Attach to razor view session expiration event so any cached authorizations can also be cleared
            Model.RazorView.SessionExpired += (sender, e) => ClearAuthorizationCache(e.Argument1);
        }

        // Static Methods

        /// <summary>
        /// Clears any cached authorizations for the specified <paramref name="sessionID"/>.
        /// </summary>
        /// <param name="sessionID">Identifier of session authorization to clear.</param>
        /// <returns><c>true</c> if session authorization was found and cleared; otherwise, <c>false</c>.</returns>
        public static bool ClearAuthorizationCache(Guid sessionID)
        {
            SecurityPrincipal securityPrincipal;
            return s_authorizationCache.TryRemove(sessionID, out securityPrincipal);
        }

        private static bool TryParseCredentials(string authorizationParameter, out string userName, out string password)
        {
            byte[] credentialBytes;

            userName = null;
            password = null;

            try
            {
                credentialBytes = Convert.FromBase64String(authorizationParameter);
            }
            catch (FormatException)
            {
                return false;
            }

            // The currently approved HTTP 1.1 specification says characters here are ISO-8859-1.
            // However, the current draft updated specification for HTTP 1.1 indicates this
            // encoding is infrequently used in practice and defines behavior only for ASCII.

            // Make a writable copy of the ASCII encoding to enable setting the decoder fall-back
            Encoding encoding = Encoding.ASCII.Clone() as Encoding;

            if ((object)encoding == null)
                return false;

            // Fail on invalid bytes rather than silently replacing and continuing
            encoding.DecoderFallback = DecoderFallback.ExceptionFallback;

            string credentials;

            try
            {
                credentials = encoding.GetString(credentialBytes);
            }
            catch (DecoderFallbackException)
            {
                return false;
            }

            if (string.IsNullOrEmpty(credentials))
                return false;

            int index = credentials.IndexOf(':');

            if (index == -1)
                return false;

            userName = credentials.Substring(0, index);
            password = credentials.Substring(index + 1);

            return true;
        }

        #endregion
    }
}
