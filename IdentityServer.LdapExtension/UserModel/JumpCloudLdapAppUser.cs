﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using IdentityModel;
using IdentityServer.LdapExtension.Extensions;
using Novell.Directory.Ldap;

namespace IdentityServer.LdapExtension.UserModel
{
    /// <summary>
    /// Application User Details. Note that these details are mainly used for the claims.
    /// </summary>
    /// <seealso cref="IdentityServer.LdapExtension.UserModel.IAppUser" />
    /// <remarks>In the future, this might become a base class instead of inherithing from an interface.</remarks>
    public class JumpCloudLdapAppUser : IAppUser
    {
        private string _subjectId = null;

        public string SubjectId
        {
            get => _subjectId ?? Username;
            set => _subjectId = value;
        }

        public string ProviderSubjectId { get; set; }
        public string ProviderName { get; set; }

        public string DisplayName { get; set; }
        public string Username { get; set; }

        public bool IsActive
        {
            get { return true; } // Always true for us, but we should look if the account have been locked out.
            set { }
        }

        public ICollection<Claim> Claims { get; set; }

        public string[] LdapAttributes => Enum<JumpCloudLdapAttributes>.Descriptions;

        /// <summary>
        /// Fills the claims.
        /// </summary>
        /// <param name="user">The user.</param>
        public void FillClaims(LdapEntry user)
        {
            // Example in LDAP we have display name as displayName (normal field)
            this.Claims = new List<Claim>
                {
                    GetClaimFromLdapAttributes(user, JwtClaimTypes.Name, JumpCloudLdapAttributes.DisplayName),
                    GetClaimFromLdapAttributes(user, JwtClaimTypes.FamilyName, JumpCloudLdapAttributes.LastName),
                    GetClaimFromLdapAttributes(user, JwtClaimTypes.GivenName, JumpCloudLdapAttributes.FirstName),
                    GetClaimFromLdapAttributes(user, JwtClaimTypes.Email, JumpCloudLdapAttributes.EMail),
                    GetClaimFromLdapAttributes(user, JwtClaimTypes.PhoneNumber, JumpCloudLdapAttributes.TelephoneNumber)
                };

            // Add claims based on the user groups
            // add the groups as claims -- be careful if the number of groups is too large
            if (true)
            {
                try
                {
                    var userRoles = user.getAttribute(JumpCloudLdapAttributes.MemberOf.ToDescriptionString()).StringValues;
                    while (userRoles.MoveNext())
                    {
                        // My hack to make the role claim to include only the name of the role versus the full DN
                        this.Claims.Add(new Claim(JwtClaimTypes.Role, userRoles.Current.ToString().Split(',')[0].Replace("cn=", "")));
                    }
                    //var roles = userRoles.Current (x => new Claim(JwtClaimTypes.Role, x.Value));
                    //id.AddClaims(roles);
                    //Claims = this.Claims.Concat(new List<Claim>()).ToList();
                }
                catch (Exception)
                {
                    // No roles exists it seems.
                }
            }
        }

        public static string[] RequestedLdapAttributes()
        {
            throw new NotImplementedException();
        }

        internal Claim GetClaimFromLdapAttributes(LdapEntry user, string claim, JumpCloudLdapAttributes ldapAttribute)
        {
            string value = string.Empty;

            try
            {
                value = user.getAttribute(ldapAttribute.ToDescriptionString()).StringValue;
                return new Claim(claim, value);
            }
            catch (Exception)
            {
                // Should do something... But basically the attribute is not found
                // We swallow for now, since we might not care.
            }

            return new Claim(claim, value);
        }

        public void SetBaseDetails(LdapEntry ldapEntry, string providerName)
        {
            DisplayName = ldapEntry.getAttribute(JumpCloudLdapAttributes.DisplayName.ToDescriptionString()).StringValue;
            Username = ldapEntry.getAttribute(JumpCloudLdapAttributes.UserName.ToDescriptionString()).StringValue;
            ProviderName = providerName;
            SubjectId = Username; // Extra: We could use the uidNumber instead in a sha algo.
            ProviderSubjectId = Username;
            FillClaims(ldapEntry);
        }
    }
}
