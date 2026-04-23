using BankSystem.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace BankSystem.Views
{
    public partial class UserEditDialog : Window
    {
        private User _user;
        private List<Role> _roles;

        public UserEditDialog(User user, List<Role> roles)
        {
            InitializeComponent();
            _user = user;
            _roles = roles;

            txtLogin.Text = user.Login;
            txtEmail.Text = user.Email;
            cmbRole.ItemsSource = roles;
            cmbRole.SelectedValue = user.RoleId;
            chkActive.IsChecked = user.IsActive;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            btnSave.IsEnabled = false;

            try
            {
                // ИСПРАВЛЕНО: используем ConnectionString вместо _connectionString
                using (var connection = new SqlConnection(App.Database.ConnectionString))
                {
                    string query = "UPDATE Users SET email = @email, id_role = @roleId, is_active = @isActive WHERE id_user = @userId";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@email", txtEmail.Text);
                        command.Parameters.AddWithValue("@roleId", (int)cmbRole.SelectedValue);
                        command.Parameters.AddWithValue("@isActive", chkActive.IsChecked ?? true);
                        command.Parameters.AddWithValue("@userId", _user.Id);

                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSave.IsEnabled = true;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}