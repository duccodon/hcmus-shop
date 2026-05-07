using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.Contracts.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace hcmus_shop.Converters
{
    /// <summary>
    /// Visible only if the current user has the role passed as ConverterParameter.
    /// Usage:
    ///   Visibility="{Binding Converter={StaticResource RoleVisibility}, ConverterParameter=Admin}"
    ///
    /// Note: HasRole returns true for Admin regardless of the requested role
    /// (Admin can access everything). For "Admin-only" elements, set the param to "Admin".
    /// </summary>
    public class RoleVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var requiredRole = parameter as string ?? "Admin";
            var auth = Ioc.Default.GetService<IAuthService>();
            if (auth is null) return Visibility.Collapsed;

            // For "Admin"-only elements, do an exact match (Admin role only).
            // For other roles, use HasRole which permits Admin + matching role.
            var allowed = string.Equals(requiredRole, "Admin", StringComparison.OrdinalIgnoreCase)
                ? string.Equals(auth.CurrentUser?.Role, "Admin", StringComparison.OrdinalIgnoreCase)
                : auth.HasRole(requiredRole);

            return allowed ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
