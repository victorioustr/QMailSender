using QMailSender.Handlers.Abstract;
using IResult = QMailSender.Handlers.Abstract.IResult;

namespace QMailSender.Handlers.Concrete;

public class Result : IResult
{
    public Result(bool success, string message)
        : this(success)
    {
        Message = message;
    }

    public Result(bool success)
    {
        Success = success;
    }

    public bool Success { get; }
    public string Message { get; }
}