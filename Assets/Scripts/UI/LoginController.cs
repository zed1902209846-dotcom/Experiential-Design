using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR
{
    public class LoginController : MonoBehaviour
    {
        [SerializeField] private InputField usernameInput;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private Text messageText;
        [SerializeField] private GameObject registerPanel;

        public void Login()
        {
            var result = AccountStore.Login(usernameInput.text, passwordInput.text);
            if (result == AccountResult.Success)
            {
                SessionState.SetUser(usernameInput.text.Trim());
                SceneNavigator.GoTo(SceneNavigator.MainMenu);
                return;
            }
            ShowMessage(result);
        }

        public void Guest()
        {
            SessionState.SetGuest();
            SceneNavigator.GoTo(SceneNavigator.MainMenu);
        }

        public void OpenRegister()
        {
            if (registerPanel != null) registerPanel.SetActive(true);
        }

        private void ShowMessage(AccountResult r)
        {
            if (messageText == null) return;
            switch (r)
            {
                case AccountResult.NotFound:
                    messageText.text = "Account not found. Please sign up first.";
                    break;
                case AccountResult.WrongPassword:
                    messageText.text = "Incorrect password. Please try again.";
                    break;
                default:
                    messageText.text = "Login failed. Please check your input.";
                    break;
            }
        }
    }
}
