using System.Threading.Tasks;

namespace TXT.WEAVR.Player.Controller
{
    public interface IAuthenticationController : IController
    {
        Task<bool> ChangePassword(string currentPassword, string newPassword);
        Task Login();
        Task Logout();
    }
}