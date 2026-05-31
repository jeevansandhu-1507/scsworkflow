namespace SCSPortal.Services;

public enum ToastKind { Success, Warning, Danger }

public class ToastService
{
    public event Action<ToastKind, string>? OnShow;
    public void Show(ToastKind kind, string message) => OnShow?.Invoke(kind, message);
}
