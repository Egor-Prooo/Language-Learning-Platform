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

        // Called automatically on connect — join the user's personal notification group
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
                await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));

            await base.OnConnectedAsync();
        }

        // Join a specific conversation room so ReceiveMessage only fires for the open chat
        public async Task JoinConversation(string teacherId, string studentId, string courseId)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                ConvGroup(teacherId, studentId, courseId));
        }

        // Leave a conversation room (called when switching conversations)
        public async Task LeaveConversation(string teacherId, string studentId, string courseId)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                ConvGroup(teacherId, studentId, courseId));
        }

        // ── Student sends a message to a teacher ─────────────────────
        public async Task StudentSend(string teacherId, string courseId, string message)
        {
            message = message?.Trim() ?? "";
            if (string.IsNullOrEmpty(message)) return;

            var studentId = Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!;

            if (!Guid.TryParse(courseId, out var courseGuid)) return;

            var msg = await SaveMessage(teacherId, studentId, courseGuid, message, isFromTeacher: false);
            var payload = BuildPayload(msg);

            // Push into the conversation group (both parties see it if online in this chat)
            await Clients.Group(ConvGroup(teacherId, studentId, courseId))
                         .SendAsync("ReceiveMessage", payload);

            // Notify the teacher's personal group (updates sidebar badge without page reload)
            await Clients.Group(UserGroup(teacherId))
                         .SendAsync("SidebarRefresh", payload);
        }

        // ── Teacher sends a reply ─────────────────────────────────────
        public async Task TeacherSend(string studentId, string courseId, string message)
        {
            message = message?.Trim() ?? "";
            if (string.IsNullOrEmpty(message)) return;

            var teacherId = Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!;

            if (!Guid.TryParse(courseId, out var courseGuid)) return;

            var msg = await SaveMessage(teacherId, studentId, courseGuid, message, isFromTeacher: true);
            var payload = BuildPayload(msg);

            await Clients.Group(ConvGroup(teacherId, studentId, courseId))
                         .SendAsync("ReceiveMessage", payload);

            // Notify the student's personal group
            await Clients.Group(UserGroup(studentId))
                         .SendAsync("SidebarRefresh", payload);
        }

        // ── Helpers ───────────────────────────────────────────────────

        private async Task<TeacherMessage> SaveMessage(
            string teacherId, string studentId, Guid courseId,
            string message, bool isFromTeacher)
        {
            var msg = new TeacherMessage
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                StudentId = studentId,
                CourseId = courseId,
                Message = message,
                IsFromTeacher = isFromTeacher,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.TeacherMessages.Add(msg);
            await _context.SaveChangesAsync();
            return msg;
        }

        private static object BuildPayload(TeacherMessage m) => new
        {
            id = m.Id,
            message = m.Message,
            isFromTeacher = m.IsFromTeacher,
            sentAt = m.SentAt.ToString("HH:mm"),
            teacherId = m.TeacherId,
            studentId = m.StudentId,
            courseId = m.CourseId.ToString()
        };

        // Stable, deterministic group-name helpers (public so views can construct them)
        public static string ConvGroup(string teacherId, string studentId, string courseId)
            => $"conv|{teacherId}|{studentId}|{courseId}";

        public static string UserGroup(string userId)
            => $"user|{userId}";
    }
}