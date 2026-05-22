using Microsoft.AspNetCore.SignalR;

namespace LanguageLearningPlatform.Web.Hubs
{
    public class ForumHub : Hub
    {
        public async Task JoinPost(string postId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, PostGroup(postId));
        }

        public async Task LeavePost(string postId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, PostGroup(postId));
        }

        public static string PostGroup(string postId) => $"forum-post:{postId}";
    }
}
