using Unity.Netcode.Components;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Unity.Netcode.Samples.MultiplayerUseCases.Anticipation
{
    public class PlayerMovableObject : NetworkBehaviour
    {
        [SerializeField]
        GameObject m_GhostPrefab;

        public Transform GhostTrasform
        {
            get
            {
                if (m_GhostTrasform == null)
                {
                    m_GhostTrasform = Instantiate(m_GhostPrefab, transform.position, transform.rotation).transform;
                }
                return m_GhostTrasform;
            }
        }
        Transform m_GhostTrasform;
        AnticipatedNetworkTransform m_MyTransform;

        [SerializeField]
        InputManager m_InputManager;
        public float SmoothTime = 0.1f;
        public float SmoothDistance = 3f;

        Vector3 m_LastTeleportLocation;
        Quaternion m_LastTeleportRotation;

        void Awake()
        {
            m_MyTransform = GetComponent<AnticipatedNetworkTransform>();
        }

        /// <summary>
        /// Handles movement for a given frame, moving the player according to the delta time recorded
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="deltaTime"></param>
        public void Move(InputList inputs, bool replay = false)
        {
            if ((inputs & InputList.Up) != 0)
            {
                var newPosition = transform.position + transform.right * (Time.fixedDeltaTime * 4);
                m_MyTransform.AnticipateMove(newPosition);
            }

            if ((inputs & InputList.Down) != 0)
            {
                var newPosition = transform.position - transform.right * (Time.fixedDeltaTime * 4);
                m_MyTransform.AnticipateMove(newPosition);
            }

            if ((inputs & InputList.Left) != 0)
            {
                transform.Rotate(Vector3.up, -180f * Time.fixedDeltaTime);
                m_MyTransform.AnticipateRotate(transform.rotation);
            }

            if ((inputs & InputList.Right) != 0)
            {
                transform.Rotate(Vector3.up, 180f * Time.fixedDeltaTime);
                m_MyTransform.AnticipateRotate(transform.rotation);
            }

            if ((inputs & InputList.RandomTeleport) != 0)
            {
                var newPosition = new Vector3(Random.Range(-5, 5), 0, Random.Range(-10, 10));
                var newRotation = Quaternion.LookRotation(new Vector3(Random.Range(-5, 5), 0, Random.Range(-10, 10)));
                // This ensures consistent replays: When we teleport on a replay, we want to go back to the same place
                // we went originally. Otherwise, every replay will have us bouncing around random teleports every frame.
                if (replay)
                {
                    newPosition = m_LastTeleportLocation;
                    newRotation = m_LastTeleportRotation;
                }
                else
                {
                    m_LastTeleportLocation = newPosition;
                    m_LastTeleportRotation = newRotation;
                }
                m_MyTransform.AnticipateMove(newPosition);
                m_MyTransform.AnticipateRotate(newRotation);
            }

            if ((inputs & InputList.SmallRandomTeleport) != 0)
            {
                var newPosition = new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
                var newRotation = Quaternion.LookRotation(new Vector3(Random.Range(-0.5f, 0.5f), 0, 1));
                if (replay)
                {
                    newPosition = m_LastTeleportLocation;
                    newRotation = m_LastTeleportRotation;
                }
                else
                {
                    m_LastTeleportLocation = newPosition;
                    m_LastTeleportRotation = newRotation;
                }
                m_MyTransform.AnticipateMove(newPosition);
                m_MyTransform.AnticipateRotate(newRotation);
            }

            if ((inputs & InputList.PredictableTeleport) != 0)
            {
                var newPosition = new Vector3(0, 0, 0);
                var newRotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
                if (replay)
                {
                    newPosition = m_LastTeleportLocation;
                    newRotation = m_LastTeleportRotation;
                }
                else
                {
                    m_LastTeleportLocation = newPosition;
                    m_LastTeleportRotation = newRotation;
                }
                m_MyTransform.AnticipateMove(newPosition);
                m_MyTransform.AnticipateRotate(newRotation);
            }
        }

        public override void OnReanticipate(double lastRoundTripTime)
        {
            // Have to store the transform's previous state because calls to AnticipateMove() and
            // AnticipateRotate() will overwrite it.
            var previousState = m_MyTransform.PreviousAnticipatedState;

            var authorityTime = NetworkManager.LocalTime.Time - lastRoundTripTime;
            // Here we re-anticipate the new position of the player based on the updated server position.
            // We do this by taking the current authoritative position and replaying every input we have received
            // since the reported authority time, re-applying all the movement we have applied since then
            // to arrive at a new anticipated player location.

            foreach (var item in m_InputManager.GetHistory())
            {
                if (item.Time <= authorityTime)
                {
                    continue;
                }

                Move(item.Item, true);
            }
            // Clear out all the input history before the given authority time. We don't need anything before that
            // anymore as we won't get any more updates from the server from before this one. We keep the current
            // authority time because theoretically another system may need that.
            m_InputManager.RemoveBefore(authorityTime);
            // It's not always desirable to smooth the transform. In cases of very large discrepencies in state,
            // it can sometimes be desirable to simply teleport to the new position. We use the SmoothDistance
            // value (and use SqrMagnitude instead of Distance for efficiency) as a threshold for teleportation.
            // This could also use other mechanisms of detection: For example, when the Telport input is included
            // in the replay set, we could set a flag to disable smoothing because we know we are teleporting.
            if (SmoothTime != 0.0)
            {
                var sqDist = Vector3.SqrMagnitude(previousState.Position - m_MyTransform.AnticipatedState.Position);
                if (sqDist <= 0.25 * 0.25)
                {
                    // This prevents small amounts of wobble from slight differences.
                    m_MyTransform.AnticipateState(previousState);
                }
                else if (sqDist < SmoothDistance * SmoothDistance)
                {
                    // Server updates are not necessarily smooth, so applying reanticipation can also result in
                    // hitchy, unsmooth animations. To compensate for that, we call this to smooth from the previous
                    // anticipated state (stored in "anticipatedValue") to the new state (which, because we have used
                    // the "Move" method that updates the anticipated state of the transform, is now the current
                    // transform anticipated state)
                    m_MyTransform.Smooth(previousState, m_MyTransform.AnticipatedState, SmoothTime);
                }
            }

        }

        /// <summary>
        /// When we apply changes to the latency and jitter, it respawns everything.
        /// We want to make sure there's no input left over from before that by clearing it.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            m_InputManager.Clear();
        }

        /// <summary>
        /// Pass client inputs to the server so the server can mirror the client simulation.
        ///
        /// This is sent once per FixedUpdate frame from the client, so even though this does not necessarily happen
        /// in FixedUpdate on the server, it will be processed using Time.fixedUpdateTime to ensure consistency of
        /// simulations between the two.
        /// </summary>
        /// <param name="inputs"></param>
        [Rpc(SendTo.Server)]
        private void ServerMoveRpc(InputList inputs)
        {
            var currentPosition = m_MyTransform.AnticipatedState;
            // Calling Anticipate functions on the authority sets the authority value, too, so we can
            // just reuse the same method here with no problem.
            Move(inputs);
            // Server can use Smoothing for interpolation purposes as well.
            m_MyTransform.Smooth(currentPosition, m_MyTransform.AuthoritativeState, SmoothTime);
        }

        public void Update()
        {
            // The "ghost transform" here is a little smaller player object that shows the current authority position,
            // which is a few frames behind our anticipated value. This helps render the difference.
            GhostTrasform.position = m_MyTransform.AuthoritativeState.Position;
            GhostTrasform.rotation = m_MyTransform.AuthoritativeState.Rotation;
            GhostTrasform.localScale = m_MyTransform.AuthoritativeState.Scale * 0.75f;
        }

        // Input processing happens in FixedUpdate rather than Update because the frame rate of server and client
        // may not exactly match, and if that is the case, doing movement in Update based on Time.deltaTime could
        // result in significantly different calculations between the server and client, meaning greater opportunities
        // for desync. Performing updates in FixedUpdate does not guarantee no desync, but it makes the calculations
        // more consistent between the two. It also means that we don't have to worry about delta times when replaying
        // inputs when we get updates - we can assume a fixed amount of time for each input. Otherwise, we would have
        // to store the delta time of each input and replay using those delta times to get consistent results.
        public void FixedUpdate()
        {
            if (!NetworkManager.IsConnectedClient)
            {
                return;
            }
            if (!IsServer)
            {
                var inputs = m_InputManager.GetInput();
                Move(inputs);
                ServerMoveRpc(inputs);
            }
        }
    }
}
