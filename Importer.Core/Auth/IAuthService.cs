using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Core.Auth
{
    /// <summary>
    /// Contrato de autenticação.
    /// Implementação real será no Client/API.
    /// </summary>
    public interface IAuthService
    {
        Task<bool> LoginAsync(string username, string password);
        Task LogoutAsync();
        Task<UserProfile?> GetCurrentUserAsync();
    }
}
