using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LanguageLearningPlatform.Web.Hubs
{
    public class ForumHub : Hub
    {
        public async Task JoinPost(string postId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, PostGroup(postId));
        }

        /// Leave the SignalR group for a specific post.
        public async Task LeavePost(string postId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, PostGroup(postId));
        }

        public static string PostGroup(string postId) => $"forum-post:{postId}";
    }
}
