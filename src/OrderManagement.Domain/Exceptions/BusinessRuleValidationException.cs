namespace OrderManagement.Domain.Exceptions;

/// <summary>
/// ビジネスルール違反の場合にスローされる例外
/// </summary>
/// <remarks>
/// InvalidOperationException の代わりに使用する、
/// より明示的なビジネス例外。
/// </remarks>
public class BusinessRuleValidationException : Exception
{
    public BusinessRuleValidationException(string message) : base(message)
    {
    }

    public BusinessRuleValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
