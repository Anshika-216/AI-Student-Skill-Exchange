using Microsoft.EntityFrameworkCore;
using AIstudentskillexchange.Data;
using AIstudentskillexchange.Models;

namespace AIstudentskillexchange.Tests.TestHelpers
{
    public static class TestDb
    {
        public static ApplicationDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
                .EnableSensitiveDataLogging()
                .Options;

            return new ApplicationDbContext(options);
        }

        public static ApplicationUser AddUser(
            this ApplicationDbContext context, string id, string fullName, string? bio = null)
        {
            var user = new ApplicationUser
            {
                Id = id,
                FullName = fullName,
                Bio = bio,
                UserName = $"{id}@test.local",
                Email = $"{id}@test.local"
            };

            context.Users.Add(user);
            return user;
        }

        public static Skill AddSkill(
            this ApplicationDbContext context, int id, string name, string category)
        {
            var skill = new Skill { Id = id, Name = name, Category = category };
            context.Skills.Add(skill);
            return skill;
        }

        public static StudentSkill AddStudentSkill(
            this ApplicationDbContext context,
            string studentId,
            int skillId,
            SkillType type,
            ProficiencyLevel level = ProficiencyLevel.Intermediate)
        {
            var studentSkill = new StudentSkill
            {
                StudentId = studentId,
                SkillId = skillId,
                Type = type,
                Level = level
            };

            context.StudentSkills.Add(studentSkill);
            return studentSkill;
        }

        public static LearningRequest AddRequest(
            this ApplicationDbContext context,
            int id,
            string senderId,
            string receiverId,
            int skillId,
            RequestStatus status = RequestStatus.Pending)
        {
            var request = new LearningRequest
            {
                Id = id,
                SenderId = senderId,
                ReceiverId = receiverId,
                SkillId = skillId,
                Status = status
            };

            context.LearningRequests.Add(request);
            return request;
        }

        public static LearningSession AddSession(
            this ApplicationDbContext context,
            int id,
            int requestId,
            SessionStatus status = SessionStatus.Scheduled,
            DateTime? scheduledTime = null)
        {
            var session = new LearningSession
            {
                Id = id,
                RequestId = requestId,
                Status = status,
                ScheduledTime = scheduledTime ?? DateTime.UtcNow.AddDays(1)
            };

            context.LearningSessions.Add(session);
            return session;
        }
    }
}
