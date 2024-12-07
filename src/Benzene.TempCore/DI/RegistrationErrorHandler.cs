using System;
using System.Linq;
using Benzene.TempCore.DI;
using Benzene.TempCore.Helper;

namespace Benzene.Core.DI;

public static class RegistrationErrorHandler
{
    private static RegistrationCheck _registrationCheck;

    private static void Init()
    {
        _registrationCheck = RegistrationCheck.Create(Utils.GetAllTypes().ToArray());
    }

    public static string CheckType(Type type)
    {
        if (_registrationCheck == null)
        {
            Init();
        }

        return _registrationCheck!.CheckType(type.FullName);
    }
    
    public static string CheckException(Exception exception)
    {
        if (_registrationCheck == null)
        {
            Init();
        }

        if (exception.Message.StartsWith("Unable to resolve service for type") ||
            exception.Message.StartsWith("No service for type"))
        {
            return _registrationCheck!.CheckType(exception.Message.Split('\'')[1]);
        }

        return string.Empty;
    }
}
