﻿//******************************************************************************************************
//  LdapSecurityProvider.cs - Gbtc
//
//  Copyright © 2012, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  07/08/2010 - Pinal C. Patel
//       Generated original version of source code.
//  12/03/2010 - Pinal C. Patel
//       Override the default behavior of TranslateRole() to translate a SID to its role name.
//  01/05/2011 - Pinal C. Patel
//       Added overrides to RefreshData(), UpdateData(), ResetPassword() and ChangePassword() methods.
//  02/14/2011 - J. Ritchie Carroll
//       Modified provider to be able to use local accounts when user is not connected to a domain.
//  06/09/2011 - Pinal C. Patel
//       Fixed a bug in the caching logic of RefreshData() method.
//  08/16/2011 - Pinal C. Patel
//       Made offline caching of user data for authentication purpose optional and turned on by default.
//  12/20/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

using GSF.Configuration;
using GSF.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Principal;
using System.Threading;

namespace GSF.Security
{
    /// <summary>
    /// Represents an <see cref="ISecurityProvider"/> that uses Active Directory for its backend datastore and credential authentication.
    /// </summary>
    /// <remarks>
    /// A <a href="http://en.wikipedia.org/wiki/Security_Identifier" target="_blank">Security Identifier</a> can also be specified in 
    /// <b>IncludedResources</b> instead of a role name in the format of 'SID:&lt;Security Identifier&gt;' (Example: SID:S-1-5-21-19610888-1443184010-1631745340-269783).
    /// </remarks>
    /// <example>
    /// Required config file entries:
    /// <code>
    /// <![CDATA[
    /// <?xml version="1.0"?>
    /// <configuration>
    ///   <configSections>
    ///     <section name="categorizedSettings" type="GSF.Configuration.CategorizedSettingsSection, GSF.Core" />
    ///   </configSections>
    ///   <categorizedSettings>
    ///     <securityProvider>
    ///       <add name="ApplicationName" value="" description="Name of the application being secured as defined in the backend security datastore."
    ///         encrypted="false" />
    ///       <add name="ConnectionString" value="LDAP://DC=COMPANY,DC=COM"
    ///         description="Connection string to be used for connection to the backend security datastore."
    ///         encrypted="false" />
    ///       <add name="ProviderType" value="GSF.Security.LdapSecurityProvider, GSF.Security"
    ///         description="The type to be used for enforcing security." encrypted="false" />
    ///       <add name="IncludedResources" value="*=*" description="Semicolon delimited list of resources to be secured along with role names."
    ///         encrypted="false" />
    ///       <add name="ExcludedResources" value="" description="Semicolon delimited list of resources to be excluded from being secured."
    ///         encrypted="false" />
    ///       <add name="NotificationSmtpServer" value="localhost" description="SMTP server to be used for sending out email notification messages."
    ///         encrypted="false" />
    ///       <add name="NotificationSenderEmail" value="sender@company.com" description="Email address of the sender of email notification messages." 
    ///         encrypted="false" />
    ///       <add name="EnableOfflineCaching" value="True" description="True to enable caching of user information for authentication in offline state, otherwise False."
    ///         encrypted="false" />
    ///       <add name="CacheRetryDelayInterval" value="200" description="Wait interval, in milliseconds, before retrying load of user data cache."
    ///         encrypted="false" />
    ///       <add name="CacheMaximumRetryAttempts" value="10" description="Maximum retry attempts allowed for loading user data cache."
    ///         encrypted="false" />
    ///     </securityProvider>
    ///     <activeDirectory>
    ///       <add name="PrivilegedDomain" value="" description="Domain of privileged domain user account."
    ///         encrypted="false" />
    ///       <add name="PrivilegedUserName" value="" description="Username of privileged domain user account."
    ///         encrypted="false" />
    ///       <add name="PrivilegedPassword" value="" description="Password of privileged domain user account."
    ///         encrypted="true" />
    ///     </activeDirectory>
    ///   </categorizedSettings>
    /// </configuration>
    /// ]]>
    /// </code>
    /// </example>
    public class LdapSecurityProvider : SecurityProviderBase
    {
        #region [ Members ]

        // Constants

        /// <summary>
        /// Defines the provider ID for the <see cref="LdapSecurityProvider"/>.
        /// </summary>
        public const int ProviderID = 0;

        /// <summary>
        /// Specifies the default value for the <see cref="EnableOfflineCaching"/> property.
        /// </summary>
        public const bool DefaultEnableOfflineCaching = true;

        /// <summary>
        /// Specifies the default value for the <see cref="CacheRetryDelayInterval"/> property.
        /// </summary>
        public const double DefaultCacheRetryDelayInterval = 200.0D;

        /// <summary>
        /// Specifies the default value for the <see cref="CacheMaximumRetryAttempts"/> property.
        /// </summary>
        public const int DefaultCacheMaximumRetryAttempts = 10;

        // Fields
        private bool m_enableOfflineCaching;
        private double m_cacheRetryDelayInterval;
        private int m_cacheMaximumRetryAttempts;
        private WindowsPrincipal m_windowsPrincipal;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the <see cref="LdapSecurityProvider"/> class.
        /// </summary>
        /// <param name="username">Name that uniquely identifies the user.</param>
        public LdapSecurityProvider(string username)
            : this(username, true, false, false, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LdapSecurityProvider"/> class.
        /// </summary>
        /// <param name="username">Name that uniquely identifies the user.</param>
        /// <param name="canRefreshData">true if the security provider can refresh <see cref="UserData"/> from the backend datastore, otherwise false.</param>
        /// <param name="canUpdateData">true if the security provider can update <see cref="UserData"/> in the backend datastore, otherwise false.</param>
        /// <param name="canResetPassword">true if the security provider can reset user password, otherwise false.</param>
        /// <param name="canChangePassword">true if the security provider can change user password, otherwise false.</param>
        protected LdapSecurityProvider(string username, bool canRefreshData, bool canUpdateData, bool canResetPassword, bool canChangePassword)
            : base(username, canRefreshData, canUpdateData, canResetPassword, canChangePassword)
        {
            m_enableOfflineCaching = DefaultEnableOfflineCaching;
            m_cacheRetryDelayInterval = DefaultCacheRetryDelayInterval;
            m_cacheMaximumRetryAttempts = DefaultCacheMaximumRetryAttempts;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets a boolean value that indicates whether user information is to be cached for offline authentication.
        /// </summary>
        public bool EnableOfflineCaching
        {
            get
            {
                return m_enableOfflineCaching;
            }
            set
            {
                m_enableOfflineCaching = value;
            }
        }

        /// <summary>
        /// Gets or sets the wait interval (in milliseconds) before retrying load of offline user data cache.
        /// </summary>
        public double CacheRetryDelayInterval
        {
            get
            {
                return m_cacheRetryDelayInterval;
            }
            set
            {
                m_cacheRetryDelayInterval = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum retry attempts allowed for loading offline user data cache.
        /// </summary>
        public int CacheMaximumRetryAttempts
        {
            get
            {
                return m_cacheMaximumRetryAttempts;
            }
            set
            {
                m_cacheMaximumRetryAttempts = value;
            }
        }

        /// <summary>
        /// Gets the original <see cref="WindowsPrincipal"/> of the user if the user exists in Active Directory.
        /// </summary>
        public WindowsPrincipal WindowsPrincipal
        {
            get
            {
                return m_windowsPrincipal;
            }
            protected set
            {
                m_windowsPrincipal = value;
            }
        }

        #endregion

        #region [ Methods ]

        #region [ Not Supported ]

        /// <summary>
        /// Updates the <see cref="UserData"/> in the backend datastore.
        /// </summary>
        /// <returns>true if <see cref="UserData"/> is updated, otherwise false.</returns>
        /// <exception cref="NotSupportedException">Always</exception>
        public override bool UpdateData()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Resets user password in the backend datastore.
        /// </summary>
        /// <param name="securityAnswer">Answer to the user's security question.</param>
        /// <returns>true if the password is reset, otherwise false.</returns>
        /// <exception cref="NotSupportedException">Always</exception>
        public override bool ResetPassword(string securityAnswer)
        {
            throw new NotSupportedException();
        }

        #endregion

        /// <summary>
        /// Saves <see cref="LdapSecurityProvider"/> settings to the config file if the <see cref="SecurityProviderBase.PersistSettings"/> property is set to true.
        /// </summary>
        public override void SaveSettings()
        {
            base.SaveSettings();
            if (PersistSettings)
            {
                // Save settings under the specified category.
                ConfigurationFile config = ConfigurationFile.Current;
                CategorizedSettingsElementCollection settings = config.Settings[SettingsCategory];
                settings["EnableOfflineCaching", true].Update(m_enableOfflineCaching);
                settings["CacheRetryDelayInterval", true].Update(m_cacheRetryDelayInterval);
                settings["CacheMaximumRetryAttempts", true].Update(m_cacheMaximumRetryAttempts);
                config.Save();
            }
        }

        /// <summary>
        /// Loads saved <see cref="LdapSecurityProvider"/> settings from the config file if the <see cref="SecurityProviderBase.PersistSettings"/> property is set to true.
        /// </summary>
        public override void LoadSettings()
        {
            base.LoadSettings();
            if (PersistSettings)
            {
                // Load settings from the specified category.
                ConfigurationFile config = ConfigurationFile.Current;
                CategorizedSettingsElementCollection settings = config.Settings[SettingsCategory];
                settings.Add("EnableOfflineCaching", m_enableOfflineCaching, "True to enable caching of user information for authentication in offline state, otherwise False.");
                settings.Add("CacheRetryDelayInterval", m_cacheRetryDelayInterval, "Wait interval, in milliseconds, before retrying load of user data cache.");
                settings.Add("CacheMaximumRetryAttempts", m_cacheMaximumRetryAttempts, "Maximum retry attempts allowed for loading user data cache.");
                EnableOfflineCaching = settings["EnableOfflineCaching"].ValueAs(m_enableOfflineCaching);
                CacheRetryDelayInterval = settings["CacheRetryDelayInterval"].ValueAs(m_cacheRetryDelayInterval);
                CacheMaximumRetryAttempts = settings["CacheMaximumRetryAttempts"].ValueAs(m_cacheMaximumRetryAttempts);
            }
        }

        /// <summary>
        /// Authenticates the user.
        /// </summary>
        /// <param name="password">Password to be used for authentication.</param>
        /// <returns>true if the user is authenticated, otherwise false.</returns>
        public override bool Authenticate(string password)
        {
            // Check prerequisites.
            if (!UserData.IsDefined || UserData.IsDisabled || UserData.IsLockedOut ||
                (UserData.PasswordChangeDateTime != DateTime.MinValue && UserData.PasswordChangeDateTime <= DateTime.UtcNow))
                return false;

            if (string.IsNullOrEmpty(password))
            {
                // Validate with current thread principal.
                m_windowsPrincipal = Thread.CurrentPrincipal as WindowsPrincipal;
                UserData.IsAuthenticated = m_windowsPrincipal != null && !string.IsNullOrEmpty(UserData.LoginID) &&
                                                string.Compare(m_windowsPrincipal.Identity.Name, UserData.LoginID, true) == 0 && m_windowsPrincipal.Identity.IsAuthenticated;
            }
            else
            {
                // Validate by performing network logon.
                string[] userParts = UserData.LoginID.Split('\\');
                m_windowsPrincipal = UserInfo.AuthenticateUser(userParts[0], userParts[1], password) as WindowsPrincipal;
                UserData.IsAuthenticated = m_windowsPrincipal != null && m_windowsPrincipal.Identity.IsAuthenticated;
            }

            return UserData.IsAuthenticated;
        }

        /// <summary>
        /// Refreshes the <see cref="UserData"/> from the backend datastore.
        /// </summary>
        /// <returns>true if <see cref="SecurityProviderBase.UserData"/> is refreshed, otherwise false.</returns>
        public override bool RefreshData()
        {
            bool result;

            // For consistency with WindowIdentity principal, user groups are loaded into Roles collection
            if (result = RefreshData(UserData.Roles, LdapSecurityProvider.ProviderID))
            {
                string[] parts;

                // Remove domain name prefixes from user group names (again to match WindowIdentity principal implementation)
                for (int i = 0; i < UserData.Roles.Count; i++)
                {
                    parts = UserData.Roles[i].Split('\\');

                    if (parts.Length == 2)
                        UserData.Roles[i] = parts[1];
                }
            }

            return result;
        }

        /// <summary>
        /// Refreshes the <see cref="UserData"/> from the backend datastore loading user groups into desired collection.
        /// </summary>
        /// <param name="groupCollection">Target collection for user groups.</param>
        /// <param name="providerID">Unique provider ID used to distinguish cached user data that may be different based on provider.</param>
        /// <returns>true if <see cref="SecurityProviderBase.UserData"/> is refreshed, otherwise false.</returns>
        protected virtual bool RefreshData(List<string> groupCollection, int providerID)
        {
            if (groupCollection == null)
                throw new ArgumentNullException("groupCollection");

            if (string.IsNullOrEmpty(UserData.Username))
                return false;

            // Initialize user data
            UserData.Initialize();

            // Populate user data
            UserInfo user = null;
            UserDataCache userDataCache = null;
            try
            {
                // Get current local user data cache
                if (m_enableOfflineCaching)
                {
                    userDataCache = UserDataCache.GetCurrentCache(providerID);
                    userDataCache.RetryDelayInterval = m_cacheRetryDelayInterval;
                    userDataCache.MaximumRetryAttempts = m_cacheMaximumRetryAttempts;
                    // TODO: Reload on change is disabled for now by default to eliminate GC handle leaks, if .NET fixes bug http://support.microsoft.com/kb/2628838
                    // then this can be safely reenabled. For now this will prevent automatic runtime reloading of user data cached by another application.
                    userDataCache.ReloadOnChange = false;
                    userDataCache.AutoSave = true;
                    userDataCache.Load();
                }

                // Create user info object using specified LDAP path if provided
                string ldapPath = GetLdapPath();
                if (string.IsNullOrEmpty(ldapPath))
                    user = new UserInfo(UserData.Username);
                else
                    user = new UserInfo(UserData.Username, ldapPath);

                user.PersistSettings = true;

                // Attempt to determine if user exists (this will initialize user object if not initialized already)
                UserData.IsDefined = user.Exists;
                UserData.LoginID = user.LoginID;

                if (UserData.IsDefined)
                {
                    // Fill in user information from domain data if it is available
                    if (user.DomainAvailable)
                    {
                        // Copy relevant user information
                        UserData.FirstName = user.FirstName;
                        UserData.LastName = user.LastName;
                        UserData.CompanyName = user.Company;
                        UserData.PhoneNumber = user.Telephone;
                        UserData.EmailAddress = user.Email;
                        UserData.IsLockedOut = user.AccountIsLockedOut;
                        UserData.IsDisabled = user.AccountIsDisabled;
                        UserData.PasswordChangeDateTime = user.NextPasswordChangeDate;
                        UserData.AccountCreatedDateTime = user.AccountCreationDate;

                        // Assign all groups the user is a member of
                        foreach (string groupName in user.Groups)
                        {
                            if (!groupCollection.Contains(groupName, StringComparer.InvariantCultureIgnoreCase))
                                groupCollection.Add(groupName);
                        }

                        if (userDataCache != null)
                        {
                            // Cache user data so that information can be loaded later if domain is unavailable
                            userDataCache[UserData.LoginID] = UserData;

                            // Wait for pending serialization since cache is scoped locally to this method and will be disposed before exit
                            userDataCache.WaitForSave();
                        }
                    }
                    else
                    {
                        // Attempt to load previously cached user information when domain is offline
                        UserData cachedUserData = null;
                        if (userDataCache != null && userDataCache.TryGetUserData(UserData.LoginID, out cachedUserData))
                        {
                            // Copy relevant cached user information
                            UserData.FirstName = cachedUserData.FirstName;
                            UserData.LastName = cachedUserData.LastName;
                            UserData.CompanyName = cachedUserData.CompanyName;
                            UserData.PhoneNumber = cachedUserData.PhoneNumber;
                            UserData.EmailAddress = cachedUserData.EmailAddress;
                            UserData.IsLockedOut = cachedUserData.IsLockedOut;
                            UserData.IsDisabled = cachedUserData.IsDisabled;
                            UserData.Roles.AddRange(cachedUserData.Roles);
                            UserData.Groups.AddRange(cachedUserData.Groups);

                            // If domain is offline, a password change cannot be initiated
                            UserData.PasswordChangeDateTime = DateTime.MaxValue;
                            UserData.AccountCreatedDateTime = cachedUserData.AccountCreatedDateTime;
                        }
                        else
                        {
                            // No previous user data was cached but Windows allowed authentication, so all we know is that user exists
                            UserData.IsLockedOut = false;
                            UserData.IsDisabled = false;
                            UserData.PasswordChangeDateTime = DateTime.MaxValue;
                            UserData.AccountCreatedDateTime = DateTime.MinValue;
                        }
                    }
                }

                return UserData.IsDefined;
            }
            finally
            {
                if (user != null)
                    user.Dispose();

                if (userDataCache != null)
                    userDataCache.Dispose();
            }
        }

        /// <summary>
        /// Changes user password in the backend datastore.
        /// </summary>
        /// <param name="oldPassword">User's current password.</param>
        /// <param name="newPassword">User's new password.</param>
        /// <returns>true if the password is changed, otherwise false.</returns>
        /// <remarks>
        /// This method always returns <c>false</c> under Mono deployments.
        /// </remarks>
        public override bool ChangePassword(string oldPassword, string newPassword)
        {
#if MONO
            return false;
#else
            // Check prerequisites
            if (!UserData.IsDefined || UserData.IsDisabled || UserData.IsLockedOut)
                return false;

            UserInfo user = null;
            WindowsImpersonationContext context = null;

            try
            {
                string ldapPath = GetLdapPath();

                // Create user info object using specified LDAP path if provided
                if (string.IsNullOrEmpty(ldapPath))
                    user = new UserInfo(UserData.Username);
                else
                    user = new UserInfo(UserData.Username, ldapPath);

                // Initialize user entry
                user.PersistSettings = true;
                user.Initialize();

                // Impersonate privileged user
                context = user.ImpersonatePrivilegedAccount();

                // Change user password
                user.UserEntry.Invoke("ChangePassword", oldPassword, newPassword);

                // Commit changes (required for non-local accounts)
                if (!user.IsWinNTEntry)
                    user.UserEntry.CommitChanges();

                return true;
            }
            catch (TargetInvocationException ex)
            {
                // Propagate password change error
                if (ex.InnerException == null)
                    throw new SecurityException(ex.Message, ex);
                else
                    throw new SecurityException(ex.InnerException.Message, ex);
            }
            finally
            {
                if (user != null)
                    user.Dispose();

                if (context != null)
                    UserInfo.EndImpersonation(context);

                RefreshData();
            }
#endif
        }

        /// <summary>
        /// Performs a translation of the specified user <paramref name="role"/>.
        /// </summary>
        /// <param name="role">The user role to be translated.</param>
        /// <returns>The user role that the specified user <paramref name="role"/> translates to.</returns>
        public override string TranslateRole(string role)
        {
            // Perform a translation from SID to Role only if the input starts with 'SID:'
            if (role.StartsWith("SID:", StringComparison.CurrentCultureIgnoreCase))
                return new SecurityIdentifier(role.Remove(0, 4)).Translate(typeof(NTAccount)).ToString().Split('\\')[1];

            return role;
        }

        private string GetLdapPath()
        {
            if (ConnectionString.StartsWith("LDAP://", StringComparison.InvariantCultureIgnoreCase) ||
                ConnectionString.StartsWith("LDAPS://", StringComparison.InvariantCultureIgnoreCase))
            {
                return ConnectionString;
            }
            else
            {
                foreach (KeyValuePair<string, string> pair in ConnectionString.ParseKeyValuePairs())
                {
                    if (pair.Value.StartsWith("LDAP://", StringComparison.InvariantCultureIgnoreCase) ||
                        pair.Value.StartsWith("LDAPS://", StringComparison.InvariantCultureIgnoreCase))
                        return pair.Value;
                }
            }

            return null;
        }

        #endregion
    }
}
