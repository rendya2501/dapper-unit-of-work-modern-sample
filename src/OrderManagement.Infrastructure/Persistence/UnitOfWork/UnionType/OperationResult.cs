namespace OrderManagement.Infrastructure.Persistence.UnitOfWork.UnionType;

public enum ResultStatus { Success, Cancel, Error }

public class OperationResult
{
    public ResultStatus Status { get; }
    public string Message { get; }
    public bool IsSuccess => Status == ResultStatus.Success;

    protected OperationResult(ResultStatus status, string message)
    {
        Status = status;
        Message = message;
    }

    public static OperationResult Ok() => new(ResultStatus.Success, "OK");
    public static OperationResult Cancel(string reason) => new(ResultStatus.Cancel, reason);
    public static OperationResult Error(string message) => new(ResultStatus.Error, message);
}
