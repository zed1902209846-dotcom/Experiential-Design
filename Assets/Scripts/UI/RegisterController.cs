using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR
{
    public class RegisterController : MonoBehaviour
    {
        [SerializeField] private InputField usernameInput;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private InputField confirmInput;
        [SerializeField] private Text messageText;

        public void Submit()
        {
            if (passwordInput.text != confirmInput.text)
            {
                if (messageText != null) messageText.text = "Passwords do not match.";
                return;
            }

            var result = AccountStore.Register(usernameInput.text, passwordInput.text);
            if (result == AccountResult.Success)
            {
                SessionState.SetUser(usernameInput.text.Trim());
                SceneNavigator.GoTo(SceneNavigator.MainMenu);
                return;
            }

            if (messageText == null) return;
            switch (result)
            {
                case AccountResult.InvalidName:
                    messageText.text = "Username must be 3-16 characters, no spaces.";
                    break;
                case AccountResult.InvalidPassword:
                    messageText.text = "Password must be at least 6 characters.";
                    break;
                case AccountResult.NameTaken:
                    messageText.text = "Username is already taken.";
                    break;
            }
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
