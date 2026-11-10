namespace GOpsHub.Application.Common;

/// <summary>
/// The owner's Google token is missing or could not be refreshed, so a Google API call
/// cannot run. Callers must not treat this as "the resource does not exist".
/// </summary>
public class GoogleAccountNotConnectedException : InvalidOperationException
{
    public GoogleAccountNotConnectedException(string apiName)
        : base($"Tài khoản Google chưa được kết nối hoặc token đã hết hạn ({apiName}). Vui lòng đăng nhập lại.")
    {
    }
}
