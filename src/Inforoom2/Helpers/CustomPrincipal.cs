using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Inforoom2.Models;

namespace Inforoom2.Helpers
{
	/// <summary>
	/// Расширенная модель пользователя ASP.Identity
	/// </summary>
	public class CustomPrincipal : IPrincipal
	{
		public IList<Permission> Permissions { get; set; }
		public IList<Role> Roles { get; set; }

		public CustomPrincipal(IIdentity identity, IList<Permission> permissions, IList<Role> roles)
		{
			Identity = identity;
			Permissions = permissions;
			Roles = roles;
		}

		public CustomPrincipal()
		{
		}

		public virtual bool IsInRole(string role)
		{
			return Roles.Any(r => r.Name == role);
		}

		public virtual bool HasPermissions(string permissions)
		{
			return Permissions
				.Any(permission => permission != null && permissions!= null && permissions.Contains(permission.Name))
			       || Roles.Any(role => role != null && role.Permissions.Any(perm => perm != null && permissions.Contains(perm.Name)));
		}

		public virtual bool HasRoles(string roles)
		{
			return Roles.Any(role => role != null && roles.Contains(role.Name));
		}

		public IIdentity Identity { get; private set; }
	}
}