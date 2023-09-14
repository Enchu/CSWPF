using System.Collections.Generic;
using System.IO;
using CSWPF.Directory;
using CSWPF.MVVM.Model.Interface;
using Newtonsoft.Json;

namespace CSWPF.MVVM.Model;

public class UserService: IUserService
{
    private List<User> _users = new List<User>();

    public UserService()
    {
        foreach (var filename in System.IO.Directory.GetFiles(System.IO.Directory.GetCurrentDirectory() + @"\Account\", "*.json"))
        {
            User allUsers = JsonConvert.DeserializeObject<User>(File.ReadAllText(filename));
            allUsers.CheckAccount();
            _users.Add(allUsers);
        }
    }

    public List<User> GetAllUsers()
    {
        return _users;
    }
}