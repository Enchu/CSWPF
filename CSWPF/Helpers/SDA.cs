using CSWPF.Directory;
using CSWPF.Windows;
using SteamAuth;
using SteamKit2.Authentication;
using SteamKit2.Internal;
using SteamKit2;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSWPF.Direct;

namespace CSWPF.Helpers;

public static class SDA
{
    public static SteamGuardAccount account;
    public static async void AddSDA(string Login, string Password)
    {
        string username = Login;
        string password = Password;

        SteamClient steamClient = new SteamClient();
        steamClient.Connect();

        while (!steamClient.IsConnected)
        {
            await Task.Delay(500);
        }

        CredentialsAuthSession authSession;
        try
        {
            authSession = await steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(
                new AuthSessionDetails
                {
                    Username = username,
                    Password = password,
                    IsPersistentSession = false,
                    PlatformType = EAuthTokenPlatformType.k_EAuthTokenPlatformType_MobileApp,
                    ClientOSType = EOSType.Android9,
                    Authenticator = new UserFormAuthenticator(account),
                });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Steam Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        AuthPollResult pollResponse;
        try
        {
            pollResponse = await authSession.PollingWaitForResultAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Steam Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        SessionData sessionData = new SessionData()
        {
            SteamID = authSession.SteamID.ConvertToUInt64(),
        };

        MessageBox.Show(sessionData.ToString());

        var result = MessageBox.Show("Steam account login succeeded. Press OK to continue adding SDA as your authenticator.", "Steam Login", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
        if (result == System.Windows.Forms.DialogResult.Cancel)
        {
            MessageBox.Show("Adding authenticator aborted.", "Steam Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        AuthenticatorLinker linker = new AuthenticatorLinker(sessionData);
        AuthenticatorLinker.LinkResult linkResponse = AuthenticatorLinker.LinkResult.GeneralFailure;

        while (linkResponse != AuthenticatorLinker.LinkResult.AwaitingFinalization)
        {
            try
            {
                linkResponse = linker.AddAuthenticator();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding your authenticator: " + ex.Message, "Steam Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        Manifest manifest = Manifest.GetManifest();
        string passKey = null;
        if (manifest.Entries.Count == 0)
        {
            passKey = manifest.PromptSetupPassKey("Please enter an encryption passkey. Leave blank or hit cancel to not encrypt (VERY INSECURE).");
        }
        else if (manifest.Entries.Count > 0 && manifest.Encrypted)
        {
            bool passKeyValid = false;
            while (!passKeyValid)
            {
                InputForm passKeyForm = new InputForm("Please enter your current encryption passkey.");
                passKeyForm.ShowDialog();
                if (!passKeyForm.Canceled)
                {
                    passKey = passKeyForm.txtBox.Text;
                    passKeyValid = manifest.VerifyPasskey(passKey);
                    if (!passKeyValid)
                    {
                        MessageBox.Show("That passkey is invalid. Please enter the same passkey you used for your other accounts.");
                    }
                }
                else
                {
                    return;
                }
            }
        }

        //Save the file immediately; losing this would be bad.
        if (!manifest.SaveAccount(linker.LinkedAccount, passKey != null, passKey))
        {
            manifest.RemoveAccount(linker.LinkedAccount);
            MessageBox.Show("Unable to save mobile authenticator file. The mobile authenticator has not been linked.");
            return;
        }

        MessageBox.Show("The Mobile Authenticator has not yet been linked. Before finalizing the authenticator, please write down your revocation code: " + linker.LinkedAccount.RevocationCode);

        AuthenticatorLinker.FinalizeResult finalizeResponse = AuthenticatorLinker.FinalizeResult.GeneralFailure;
        while (finalizeResponse != AuthenticatorLinker.FinalizeResult.Success)
        {
            InputForm smsCodeForm = new InputForm("Please input the SMS code sent to your phone.");
            smsCodeForm.ShowDialog();
            if (smsCodeForm.Canceled)
            {
                manifest.RemoveAccount(linker.LinkedAccount);
                return;
            }

            InputForm confirmRevocationCode = new InputForm("Please enter your revocation code to ensure you've saved it.");
            confirmRevocationCode.ShowDialog();
            if (confirmRevocationCode.txtBox.Text.ToUpper() != linker.LinkedAccount.RevocationCode)
            {
                MessageBox.Show("Revocation code incorrect; the authenticator has not been linked.");
                manifest.RemoveAccount(linker.LinkedAccount);
                return;
            }

            string smsCode = smsCodeForm.txtBox.Text;
            finalizeResponse = linker.FinalizeAddAuthenticator(smsCode);

            switch (finalizeResponse)
            {
                case AuthenticatorLinker.FinalizeResult.BadSMSCode:
                    continue;

                case AuthenticatorLinker.FinalizeResult.UnableToGenerateCorrectCodes:
                    MessageBox.Show("Unable to generate the proper codes to finalize this authenticator. The authenticator should not have been linked. In the off-chance it was, please write down your revocation code, as this is the last chance to see it: " + linker.LinkedAccount.RevocationCode);
                    manifest.RemoveAccount(linker.LinkedAccount);
                    return;

                case AuthenticatorLinker.FinalizeResult.GeneralFailure:
                    MessageBox.Show("Unable to finalize this authenticator. The authenticator should not have been linked. In the off-chance it was, please write down your revocation code, as this is the last chance to see it: " + linker.LinkedAccount.RevocationCode);
                    manifest.RemoveAccount(linker.LinkedAccount);
                    return;
            }
        }

        //Linked, finally. Re-save with FullyEnrolled property.
        manifest.SaveAccount(linker.LinkedAccount, passKey != null, passKey);
        MessageBox.Show("Mobile authenticator successfully linked. Please write down your revocation code: " + linker.LinkedAccount.RevocationCode);
        //Add new user
        User newUser = new User();
        HelperCS.SaveNew(newUser, linker.LinkedAccount.IdentitySecret, linker.LinkedAccount.SharedSecret);
        
    }
}