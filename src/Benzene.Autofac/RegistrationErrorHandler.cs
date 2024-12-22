using Benzene.TempCore.DI;

namespace Benzene.Autofac;

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
    
        if (exception.Message.StartsWith("The requested service"))
        {
            return _registrationCheck!.CheckType(exception.Message.Split('\'')[1]);
        }
    
        return string.Empty;
    }
}