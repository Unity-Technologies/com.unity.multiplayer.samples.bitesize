using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Friends;
using Unity.Services.Friends.Exceptions;
using Unity.Services.Friends.Models;
using UnityEngine;

namespace Services
{
    public class FriendsManager : MonoBehaviour
    {
        public List<Member> GetFriendList => GetNonBlockedMembers(FriendsService.Instance.Friends);
        public List<Member> GetIncomingFriendRequests => GetNonBlockedMembers(FriendsService.Instance.IncomingFriendRequests);
        public List<Member> GetOutgoingFriendRequests => GetNonBlockedMembers(FriendsService.Instance.OutgoingFriendRequests);

        public static FriendsManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async Task InitializeFriendsAsync()
        {
            await FriendsService.Instance.InitializeAsync();
        }

        public async Task AddFriendAsync(string friendName)
        {
            try
            {
                var success = await SendFriendRequest(friendName);
                if (success)
                {
                    Debug.Log("We are friends");
                }
            }
            catch (FriendsServiceException exception)
            {
                Debug.Log($"Unable to delete {friendName} - {exception}");
            }
        }

        public async Task DeleteFriendRequestAsync(string friendName)
        {
            try
            {
                await FriendsService.Instance.DeleteFriendAsync(friendName);
            }
            catch (FriendsServiceException exception)
            {
                Debug.Log($"Unable to delete {friendName} - {exception}");
            }
        }

        public async Task BlockFriendRequestAsync(string friendName)
        {
            try
            {
                await FriendsService.Instance.AddBlockAsync(friendName);
            }
            catch (FriendsServiceException exception)
            {
                Debug.Log($"Unable to block {friendName} - {exception}");
            }
        }

        public async Task UnblockFriendRequestAsync(string friendName)
        {
            try
            {
                await FriendsService.Instance.DeleteBlockAsync(friendName);
            }
            catch (FriendsServiceException exception)
            {
                Debug.Log($"Unable to unblock {friendName} - {exception}");
            }
        }

        public async Task AcceptFriendRequest(string friendName)
        {
            try
            {
                await SendFriendRequest(friendName);
            }
            catch (FriendsServiceException exception)
            {
                Debug.Log($"Unable to accept {friendName} as a friend - {exception}");
            }
        }

        async Task<bool> SendFriendRequest(string friendName)
        {
            try
            {
                var relationship = await FriendsService.Instance.AddFriendByNameAsync(friendName);
                return relationship.Type is RelationshipType.FriendRequest or RelationshipType.Friend;
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return false;
            }
        }

        private async Task ListFriendrequest()
        {
            try
            {
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        public List<Member> GetNonBlockedMembers(IReadOnlyList<Relationship> relationships)
        {
            var blocksFriends = FriendsService.Instance.Blocks;
            return relationships
                .Where(relationship => blocksFriends.All(blockedRelationship => blockedRelationship.Member.Id != relationship.Member.Id))
                .Select(relationship => relationship.Member)
                .ToList();
        }
    }
}
