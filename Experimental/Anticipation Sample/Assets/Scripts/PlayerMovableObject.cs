using System;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    public class PlayerMovableObject : NetworkBehaviour
    {
        public Transform GhostTrasform;
        public AnticipatedNetworkTransform MyTransform;
        public InputManager InputManager;
        public float SmoothTime = 0.1f;
        public float SmoothDistance = 3f;

        private Vector3 m_LastTeleportLocation;
        private Quaternion m_LastTeleportRotation;

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
                MyTransform.AnticipateMove(newPosition);
            }

            if ((inputs & InputList.Down) != 0)
            {
                var newPosition = transform.position - transform.right * (Time.fixedDeltaTime * 4);
                MyTransform.AnticipateMove(newPosition);
            }

            if ((inputs & InputList.Left) != 0)
            {
                transform.Rotate(Vector3.up, -180f * Time.fixedDeltaTime);
                MyTransform.AnticipateRotate(transform.rotation);
            }

            if ((inputs & InputList.Right) != 0)
            {
                transform.Rotate(Vector3.up, 180f * Time.fixedDeltaTime);
                MyTransform.AnticipateRotate(transform.rotation);
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
                MyTransform.AnticipateMove(newPosition);
                MyTransform.AnticipateRotate(newRotation);
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
                MyTransform.AnticipateMove(newPosition);
                MyTransform.AnticipateRotate(newRotation);
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
                MyTransform.AnticipateMove(newPosition);
                MyTransform.AnticipateRotate(newRotation);
            }
        }

        public override void OnNetworkSpawn()
        {
            MyTransform.OnReanticipate = (networkTransform, anticipatedValue, anticipationTime, authorityValue, authorityTime) =>
            {
                // Here we re-anticipate the new position of the player based on the updated server position.
                // We do this by taking the current authoritative position and replaying every input we have received
                // since the reported authority time, re-applying all the movement we have applied since then
                // to arrive at a new anticipated player location.
                foreach (var item in InputManager.GetHistory())
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
                InputManager.RemoveBefore(authorityTime);
                // It's not always desirable to smooth the transform. In cases of very large discrepencies in state,
                // it can sometimes be desirable to simply teleport to the new position. We use the SmoothDistance
                // value (and use SqrMagnitude instead of Distance for efficiency) as a threshold for teleportation.
                // This could also use other mechanisms of detection: For example, when the Telport input is included
                // in the replay set, we could set a flag to disable smoothing because we know we are teleporting.
                if (SmoothTime != 0.0 && Vector3.SqrMagnitude(anticipatedValue.Position - networkTransform.AnticipatedState.Position) < SmoothDistance * SmoothDistance)
                {
                    // Server updates are not necessarily smooth, so applying reanticipation can also result in
                    // hitchy, unsmooth animations. To compensate for that, we call this to smooth from the previous
                    // anticipated state (stored in "anticipatedValue") to the new state (which, because we have used
                    // the "Move" method that updates the anticipated state of the transform, is now the current
                    // transform anticipated state)
                    networkTransform.Smooth(anticipatedValue, networkTransform.AnticipatedState, SmoothTime);
                }
            };
            base.OnNetworkSpawn();

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
            // Calling Anticipate functions on the authority sets the authority value, too, so we can
            // just reuse the same method here with no problem.
            Move(inputs);
        }

        public void Update()
        {
            // The "ghost transform" here is a little smaller player object that shows the current authority position,
            // which is a few frames behind our anticipated value. This helps render the difference.
            GhostTrasform.position = MyTransform.AuthorityState.Position;
            GhostTrasform.rotation = MyTransform.AuthorityState.Rotation;
            GhostTrasform.localScale = MyTransform.AuthorityState.Scale * 0.75f;
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
                var inputs = InputManager.GetInput();
                Move(inputs);
                ServerMoveRpc(inputs);
            }
        }
    }
}
