using FModel.ViewModels;

namespace FModel.Services
{
    public sealed class ApplicationService
    {
        public static ThreadWorkerViewModel ThreadWorkerView { get; } = new();
        public static ApplicationViewModel ApplicationView { get; } = new();
        public static ApiEndpointViewModel ApiEndpointView { get; } = new();
    }
}