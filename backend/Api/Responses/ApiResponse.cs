using System.Text.Json.Serialization;

namespace ContasEmDia.Api.Responses;

public sealed class ApiResponse<TData>
{
    private ApiResponse(bool isSuccess, TData? data, IReadOnlyCollection<ApiError>? errors)
    {
        IsSuccess = isSuccess;
        Data = data;
        Errors = errors;
    }

    [JsonPropertyName("success")]
    public bool IsSuccess { get; }

    public TData? Data { get; }

    public IReadOnlyCollection<ApiError>? Errors { get; }

    public static ApiResponse<TData> Success(TData data) => new(isSuccess: true, data: data, errors: null);

    public static ApiResponse<TData> Failure(IReadOnlyCollection<ApiError> errors) => new(isSuccess: false, data: default, errors: errors);
}
