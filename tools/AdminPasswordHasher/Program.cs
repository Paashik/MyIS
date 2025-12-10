using System;
using MyIS.Core.Infrastructure.Auth;

class Program
{
    static void Main()
    {
        var hasher = new BcryptPasswordHasher();
        var hash = hasher.HashPassword("admin");
        Console.WriteLine(hash);
    }
}
