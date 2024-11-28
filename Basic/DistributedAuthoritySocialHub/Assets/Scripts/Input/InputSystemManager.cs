using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity.Multiplayer.Samples.SocialHub.Input
{
    /// <summary>
    /// This class handles the cursor visibility during the game and mobile specific ActionInputs.
    /// </summary>
    class InputSystemManager : MonoBehaviour
    {
        internal static InputSystemManager Instance { get; private set; }

        /// <summary>
        /// This value returns True if Mobile Inputs should be enabled, false otherwise.
        /// </summary>
        /// <remarks>
        /// This field is a Task&lt;bool&gt; instead of a simple bool to avoid issues with script initialization order in Unity.
        /// </remarks>
        /// <example>
        /// The following example shows how to use this value from any other Monobehaviour.
        /// <code>
        /// <![CDATA[
        /// public class Example : MonoBehaviour
        /// {
        ///     async void OnEnable()
        ///     {
        ///         var isMobile = await InputSystemBehaviour.IsMobile;
        ///         if (isMobile)
        ///         {
        ///             // Do something for mobile handling...
        ///         }
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        /// <seealso cref="Touchscreen"/>
        /// <seealso cref="MobileGamepadBehaviour"/>
        internal static Task<bool> IsMobile => s_IsMobile.Task;
        static TaskCompletionSource<bool> s_IsMobile;

#if UNITY_EDITOR
        [Header("Debug"), Tooltip("This option is only used in Playmode in the Editor"), SerializeField]
        internal bool ForceMobileInput;
#endif

        AvatarActions.UIActions m_UIInputs;
        AvatarActions.PlayerActions m_GameplayInputs;

        /// <summary>
        /// This method makes sure that <see cref="s_IsMobile"/> is initialized when the game is started.
        /// </summary>
        /// <remarks>
        /// The setup is done in this method rather than in static constructors to ensure that multiple
        /// Editor Playmode sessions will be initialized properly if not assembly reloading is performed between sessions.
        /// </remarks>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitializeStatic() => s_IsMobile = new TaskCompletionSource<bool>();

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this);
#if UNITY_EDITOR
            if (ForceMobileInput)
#endif
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
            {
                // On mobile, the GameInput Actions are filtered to only allow the Mobile Scheme the MobileGamepadController class is using.
                GameInput.Actions.Disable();
                GameInput.Actions.bindingMask = InputBinding.MaskByGroup(GameInput.Actions.TouchScheme.bindingGroup);
                GameInput.Actions.Enable();
                s_IsMobile.SetResult(true);

#if UNITY_EDITOR
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
#endif
            }
#endif
#if UNITY_EDITOR
            else
#endif
#if UNITY_EDITOR || (!UNITY_ANDROID && !UNITY_IOS)
            {
                s_IsMobile.SetResult(false);
            }
#endif
        }

        void Start()
        {
            m_UIInputs = GameInput.Actions.UI;
            m_GameplayInputs = GameInput.Actions.Player;
        }

        internal void EnableUIInputs()
        {
            m_GameplayInputs.Disable();
            m_UIInputs.Enable();
        }

        internal void EnableGameplayInputs()
        {
            m_UIInputs.Disable();
            m_GameplayInputs.Enable();
        }
    }
}
