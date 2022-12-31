namespace QMailSender.Handlers.Abstract;

public interface IResult
{
    bool Success { get; }
    string Message { get; }
}