
namespace MiScaleExporter.Permission
{
    public interface IBluetoothConnectPermission
    {
        Task<PermissionStatus> CheckStatusAsync();
        Task<PermissionStatus> RequestAsync();
    }
}
