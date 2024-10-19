using Chefster.Services;

namespace Chefster.Common;

/*
    Service result class that allows us to handle errors more gracefully in our Services
*/
public class ServiceResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }

    public static ServiceResult<T> SuccessResult(T data)
    {
        return new ServiceResult<T>
        {
            Success = true,
            Data = data,
            Error = null
        };
    }

    public static ServiceResult<T> ErrorResult(string error, LoggingService logger)
    {
        logger.Log(error, LogLevels.Error);
        return new ServiceResult<T>
        {
            Success = false,
            Data = default,
            Error = error
        };
    }
}
