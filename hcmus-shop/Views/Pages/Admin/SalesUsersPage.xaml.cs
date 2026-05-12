using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.Models.DTOs;
using hcmus_shop.ViewModels.Admin;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace hcmus_shop.Views.Pages.Admin
{
    public sealed partial class SalesUsersPage : Page
    {
        public SalesUsersViewModel ViewModel { get; }

        public SalesUsersPage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<SalesUsersViewModel>();
            DataContext = this;
            ViewModel.RequestUserEditorAsync = ShowUserEditorAsync;
            ViewModel.ConfirmDeleteUserAsync = ShowDeleteConfirmAsync;
            Loaded += SalesUsersPage_Loaded;
            Unloaded += SalesUsersPage_Unloaded;
        }

        private async void SalesUsersPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsInitialized)
            {
                await ViewModel.InitializeCommand.ExecuteAsync(null);
            }
        }

        private void SalesUsersPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Loaded -= SalesUsersPage_Loaded;
            Unloaded -= SalesUsersPage_Unloaded;
            ViewModel.RequestUserEditorAsync = null;
            ViewModel.ConfirmDeleteUserAsync = null;
        }

        private async Task<UserEditorResult?> ShowUserEditorAsync(UserEditorState state)
        {
            var usernameBox = new TextBox { Text = state.Username, PlaceholderText = "Username" };
            var fullNameBox = new TextBox { Text = state.FullName, PlaceholderText = "Full name" };
            var passwordBox = new PasswordBox { PlaceholderText = state.IsEditMode ? "Leave blank to keep current password" : "Password" };

            var panel = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "Username" },
                    usernameBox,
                    new TextBlock { Text = "Full Name" },
                    fullNameBox,
                    new TextBlock { Text = "Password" },
                    passwordBox
                }
            };

            var dialog = new ContentDialog
            {
                Title = state.IsEditMode ? "Edit Sales User" : "Create Sales User",
                Content = panel,
                PrimaryButtonText = state.IsEditMode ? "Save" : "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            {
                return null;
            }

            return new UserEditorResult
            {
                Username = usernameBox.Text.Trim(),
                FullName = fullNameBox.Text.Trim(),
                Password = string.IsNullOrWhiteSpace(passwordBox.Password) ? null : passwordBox.Password.Trim()
            };
        }

        private async Task<bool> ShowDeleteConfirmAsync(UserDto user)
        {
            var dialog = new ContentDialog
            {
                Title = "Delete Sales User",
                Content = $"Delete sales user '{user.Username}'?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }
    }
}
