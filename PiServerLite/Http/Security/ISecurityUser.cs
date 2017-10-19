namespace PiServerLite.Http.Security
{
    public interface ISecurityUser
    {
        string Username {get; set;}
        string Password {get; set;}
    }
}
