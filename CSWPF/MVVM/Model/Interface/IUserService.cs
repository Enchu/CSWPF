using System.Collections.Generic;
using CSWPF.Directory;

namespace CSWPF.MVVM.Model.Interface;

public interface IUserService
{
    List<User> GetAllUsers();
}