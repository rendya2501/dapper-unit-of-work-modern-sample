using System.Diagnostics.CodeAnalysis;

namespace Domain.Common.Results;

/// <summary>
/// 操作結果を表現する基底型（classベース）
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        // 不正な組み合わせをチェック
        if (isSuccess && error != null)
        {
            throw new ArgumentException("Success result cannot have an error", nameof(error));
        }

        if (!isSuccess && error == null)
        {
            throw new ArgumentException("Failure result must have an error", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    // ===== プロパティ =====

    /// <summary>
    /// 成功かどうかを判定
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 失敗かどうかを判定
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// エラー情報を取得（失敗時のみ値を持つ）
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    /// 値なし成功を生成
    /// </summary>
    public static Result Success() => new(true, null);

    /// <summary>
    /// 値あり成功を生成
    /// </summary>
    public static Result<T> Success<T>(T value) => new(value, true, null);

    /// <summary>
    /// 値なし失敗を生成
    /// </summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// 値あり失敗を生成
    /// </summary>
    /// <remarks>
    /// 型推論が効かない場合のみ使用。
    /// 通常は Failure(error) で十分（戻り値の型から推論される）。
    /// </remarks>
    public static Result<T> Failure<T>(Error error) => new(default, false, error);

    /// <summary>
    /// エラーからの暗黙的変換
    /// </summary>
    public static implicit operator Result(Error error) => Failure(error);
}


/// <summary>
/// 値を返す操作の結果
/// </summary>
/// <typeparam name="TValue">成功時に返す値の型</typeparam>
public class Result<TValue>(TValue? value, bool isSuccess, Error? error) : Result(isSuccess, error)
{
    /// <summary>
    /// 成功時の値を取得
    /// </summary>
    /// <exception cref="InvalidOperationException">失敗結果で値にアクセスした場合</exception>
    [NotNull]
    public TValue Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("Cannot access value of a failure result");

    /// <summary>
    /// エラーからの暗黙的変換
    /// </summary>
    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);

    /// <summary>
    /// 値からの暗黙的変換
    /// </summary>
    // public static implicit operator Result<TValue>(TValue value) =>
    //    value is not null ? Success(value) : Failure<TValue>(Error.NullValue);
}
