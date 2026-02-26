using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Core.Auth
{
    /// <summary>
    /// Representa o utilizador autenticado.
    /// </summary>
    public class UserProfile
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public UserRole Role { get; set; }
    }
}
