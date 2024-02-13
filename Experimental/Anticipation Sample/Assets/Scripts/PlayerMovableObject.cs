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

        /// <summary>
        /// Handles movement for a given frame, moving the player according to the delta time recorded
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="deltaTime"></param>
        public void Move(InputList inputs, float deltaTime)
        {
            if ((inputs & InputList.Up) != 0)
            {
                var newPosition = transform.position + transform.right * (deltaTime * 4);
                MyTransform.AnticipateMove(newPosition);
            }

            if ((inputs & InputList.Down) != 0)
            {
                var newPosition = transform.position - transform.right * (deltaTime * 4);
                MyTransform.AnticipateMove(newPosition);
            }

            if ((inputs & InputList.Left) != 0)
            {
                transform.Rotate(Vector3.up, -180f * deltaTime);
                MyTransform.AnticipateRotate(transform.rotation);
            }

            if ((inputs & InputList.Right) != 0)
            {
                transform.Rotate(Vector3.up, 180f * deltaTime);
                MyTransform.AnticipateRotate(transform.rotation);
            }

            if ((inputs & InputList.RandomTeleport) != 0)
            {
                var newPosition = new Vector3(Random.Range(-5, 5), 0, Random.Range(-10, 10));
                var newRotation = Quaternion.LookRotation(new Vector3(Random.Range(-5, 5), 0, Random.Range(-10, 10)));
                MyTransform.AnticipateMove(newPosition);
                MyTransform.AnticipateRotate(newRotation);
            }

            if ((inputs & InputList.SmallRandomTeleport) != 0)
            {
                var newPosition = new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
                var newRotation = Quaternion.LookRotation(new Vector3(Random.Range(-0.5f, 0.5f), 0, 1));
                MyTransform.AnticipateMove(newPosition);
                MyTransform.AnticipateRotate(newRotation);
            }

            if ((inputs & InputList.PredictableTeleport) != 0)
            {
                var newPosition = new Vector3(0, 0, 0);
                var newRotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
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

                    // Passing in the delta time from the previous frame keeps the movement we have applied consistent.
                    // Without this, we would reanticipate previous inputs as if every frame had the same delta time as
                    // the current frame. This could be done differently - for example, by using FixedUpdate instead
                    // and moving by a fixed amount each time.
                    Move(item.Item, item.DeltaTime);
                }
                // Clear out all the input history before the given authority time. We don't need anything before that
                // anymore as we won't get anymore updates from the server from before this one. We keep the current
                // authority time because theoretically another system may need that.
                InputManager.RemoveBefore(authorityTime);
                if (SmoothTime != 0.0)
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
        /// </summary>
        /// <param name="inputs"></param>
        [Rpc(SendTo.Server)]
        private void ServerMoveRpc(InputList inputs)
        {
            // Calling Anticipate functions on the authority sets the authority value, too, so we can
            // just reuse the same method here with no problem.
            Move(inputs, Time.deltaTime);
        }

        public void Update()
        {
            // The "ghost transform" here is a little smaller player object that shows the current authority position,
            // which is a few frames behind our anticipated value. This helps render the difference.
            GhostTrasform.position = MyTransform.AuthorityState.Position;
            GhostTrasform.rotation = MyTransform.AuthorityState.Rotation;
            GhostTrasform.localScale = MyTransform.AuthorityState.Scale * 0.75f;

            if (!NetworkManager.IsConnectedClient)
            {
                return;
            }
            if (!IsServer)
            {
                var inputs = InputManager.GetInput();
                Move(inputs, Time.deltaTime);
                ServerMoveRpc(inputs);
            }
        }
    }
}
