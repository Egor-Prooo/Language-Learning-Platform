using LanguageLearningPlatform.Data;
using LanguageLearningPlatform.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LanguageLearningPlatform.Web.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
                await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
            await base.OnConnectedAsync();
        }

        // ── Conversation room management ──────────────────────────────────────
        public async Task JoinConversation(string teacherId, string studentId, string courseId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ConvGroup(teacherId, studentId, courseId));
        }

        public async Task LeaveConversation(string teacherId, string studentId, string courseId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ConvGroup(teacherId, studentId, courseId));
        }

        // ── Send a message ────────────────────────────────────────────────────
        public async Task SendMessage(string teacherId, string studentId, string courseId, string message)
        {
            message = message?.Trim() ?? "";
            if (string.IsNullOrEmpty(message)) return;

            var senderId = Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isFromTeacher = senderId == teacherId;

            // Validate that sender is a participant
            if (senderId != teacherId && senderId != studentId) return;

            if (!Guid.TryParse(courseId, out var courseGuid)) return;

            var msg = new TeacherMessage
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                StudentId = studentId,
                CourseId = courseGuid,
                Message = message,
                IsFromTeacher = isFromTeacher,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.TeacherMessages.Add(msg);
            await _context.SaveChangesAsync();

            var payload = BuildPayload(msg);

            // Push to everyone in the conversation room (both participants if online)
            await Clients.Group(ConvGroup(teacherId, studentId, courseId))
                .SendAsync("ReceiveMessage", payload);

            // Notify the other party's personal group (for sidebar badge updates)
            var notifyUserId = isFromTeacher ? studentId : teacherId;
            await Clients.Group(UserGroup(notifyUserId))
                .SendAsync("NewMessageNotification", payload);
        }

        public async Task TypingStarted(string teacherId, string studentId, string courseId)
        {
            var senderId = Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await Clients.OthersInGroup(ConvGroup(teacherId, studentId, courseId))
                .SendAsync("UserTyping", new { senderId, isTyping = true });
        }

        public async Task TypingStopped(string teacherId, string studentId, string courseId)
        {
            var senderId = Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await Clients.OthersInGroup(ConvGroup(teacherId, studentId, courseId))
                .SendAsync("UserTyping", new { senderId, isTyping = false });
        }

        private static object BuildPayload(TeacherMessage m) => new
        {
            id = m.Id.ToString(),
            message = m.Message,
            isFromTeacher = m.IsFromTeacher,
            sentAt = m.SentAt.ToString("HH:mm"),
            sentDate = m.SentAt.Date.ToString("yyyy-MM-dd"),
            teacherId = m.TeacherId,
            studentId = m.StudentId,
            courseId = m.CourseId.ToString()
        };

        public static string ConvGroup(string teacherId, string studentId, string courseId)
            => $"conv:{teacherId}:{studentId}:{courseId}";

        public static string UserGroup(string userId)
            => $"user:{userId}";
    }
}