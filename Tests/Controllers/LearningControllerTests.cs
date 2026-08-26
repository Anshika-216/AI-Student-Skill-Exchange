using NUnit.Framework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIstudentskillexchange.Controllers;
using AIstudentskillexchange.Data;
using AIstudentskillexchange.DTOs;
using AIstudentskillexchange.Models;
using AIstudentskillexchange.Tests.TestHelpers;

namespace AIstudentskillexchange.Tests.Controllers
{
    [TestFixture]
    public class LearningControllerTests
    {
        private const string Learner = "learner-id";
        private const string Mentor = "mentor-id";
        private const string Outsider = "outsider-id";

        private ApplicationDbContext _context = null!;

        [SetUp]
        public void SetUp()
        {
            _context = TestDb.Create();

            _context.AddUser(Learner, "Learner");
            _context.AddUser(Mentor, "Mentor");
            _context.AddUser(Outsider, "Outsider");
            _context.AddSkill(1, "C#", "Programming");
            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown() => _context.Dispose();

        private LearningController ControllerAs(string userId) =>
            new LearningController(_context, FakeUserManager.For(userId)).WithSignedInUser(userId);

        private static T Value<T>(IActionResult result) where T : class =>
            ((result as OkObjectResult)!.Value as T)!;

        [Test]
        public void Controller_RequiresAuthentication()
        {
            var attribute = typeof(LearningController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);

            Assert.That(attribute, Is.Not.Empty,
                "LearningController must be [Authorize]d: it creates requests and feedback.");
        }

        [Test]
        public async Task CreateRequest_RecordsTheSignedInUserAsSender()
        {
            var result = await ControllerAs(Learner)
                .CreateRequest(new CreateLearningRequestDto { ReceiverId = Mentor, SkillId = 1 }, default);

            var dto = Value<LearningRequestDto>(result);

            Assert.Multiple(() =>
            {
                Assert.That(dto.Sender.Id, Is.EqualTo(Learner));
                Assert.That(dto.Receiver.Id, Is.EqualTo(Mentor));
                Assert.That(dto.Status, Is.EqualTo(RequestStatus.Pending));
            });
        }

        [Test]
        public async Task CreateRequest_RejectsARequestToYourself()
        {
            var result = await ControllerAs(Learner)
                .CreateRequest(new CreateLearningRequestDto { ReceiverId = Learner, SkillId = 1 }, default);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task CreateRequest_ReturnsNotFound_ForAnUnknownReceiver()
        {
            var result = await ControllerAs(Learner)
                .CreateRequest(new CreateLearningRequestDto { ReceiverId = "ghost", SkillId = 1 }, default);

            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task CreateRequest_ReturnsNotFound_ForAnUnknownSkill()
        {
            var result = await ControllerAs(Learner)
                .CreateRequest(new CreateLearningRequestDto { ReceiverId = Mentor, SkillId = 404 }, default);

            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task CreateRequest_RejectsASecondOpenRequestForTheSameSkill()
        {
            _context.AddRequest(1, Learner, Mentor, 1);
            await _context.SaveChangesAsync();

            var result = await ControllerAs(Learner)
                .CreateRequest(new CreateLearningRequestDto { ReceiverId = Mentor, SkillId = 1 }, default);

            Assert.That(result, Is.TypeOf<ConflictObjectResult>());
        }

        [Test]
        public async Task CreateRequest_IsAllowedAgain_OnceAnEarlierRequestWasRejected()
        {
            _context.AddRequest(1, Learner, Mentor, 1, RequestStatus.Rejected);
            await _context.SaveChangesAsync();

            var result = await ControllerAs(Learner)
                .CreateRequest(new CreateLearningRequestDto { ReceiverId = Mentor, SkillId = 1 }, default);

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task UpdateRequestStatus_IsForbidden_ForTheSenderOfTheRequest()
        {
            _context.AddRequest(1, Learner, Mentor, 1);
            await _context.SaveChangesAsync();

            var result = await ControllerAs(Learner)
                .UpdateRequestStatus(1, new UpdateRequestStatusDto { Status = RequestStatus.Accepted }, default);

            Assert.That(result, Is.TypeOf<ForbidResult>(),
                "A learner must not be able to accept their own request.");
        }

        [Test]
        public async Task UpdateRequestStatus_IsForbidden_ForAnUninvolvedUser()
        {
            _context.AddRequest(1, Learner, Mentor, 1);
            await _context.SaveChangesAsync();

            var result = await ControllerAs(Outsider)
                .UpdateRequestStatus(1, new UpdateRequestStatusDto { Status = RequestStatus.Accepted }, default);

            Assert.That(result, Is.TypeOf<ForbidResult>());
        }

        [Test]
        public async Task UpdateRequestStatus_LetsTheMentorAccept()
        {
            _context.AddRequest(1, Learner, Mentor, 1);
            await _context.SaveChangesAsync();

            var result = await ControllerAs(Mentor)
                .UpdateRequestStatus(1, new UpdateRequestStatusDto { Status = RequestStatus.Accepted }, default);

            Assert.That(Value<LearningRequestDto>(result).Status, Is.EqualTo(RequestStatus.Accepted));
        }

        [Test]
        public async Task AcceptingARequest_CreatesTheLearningSession()
        {
            _context.AddRequest(1, Learner, Mentor, 1);
            await _context.SaveChangesAsync();

            await ControllerAs(Mentor)
                .UpdateRequestStatus(1, new UpdateRequestStatusDto { Status = RequestStatus.Accepted }, default);

            Assert.That(await _context.LearningSessions.AnyAsync(s => s.RequestId == 1), Is.True);
        }

        [Test]
        public async Task RejectingARequest_CreatesNoSession()
        {
            _context.AddRequest(1, Learner, Mentor, 1);
            await _context.SaveChangesAsync();

            await ControllerAs(Mentor)
                .UpdateRequestStatus(1, new UpdateRequestStatusDto { Status = RequestStatus.Rejected }, default);

            Assert.That(await _context.LearningSessions.AnyAsync(s => s.RequestId == 1), Is.False);
        }

        [Test]
        public async Task UpdateRequestStatus_RefusesToReopenASettledRequest()
        {
            _context.AddRequest(1, Learner, Mentor, 1, RequestStatus.Accepted);
            await _context.SaveChangesAsync();

            var result = await ControllerAs(Mentor)
                .UpdateRequestStatus(1, new UpdateRequestStatusDto { Status = RequestStatus.Rejected }, default);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task UpdateRequestStatus_RefusesAMoveBackToPending()
        {
            _context.AddRequest(1, Learner, Mentor, 1);
            await _context.SaveChangesAsync();

            var result = await ControllerAs(Mentor)
                .UpdateRequestStatus(1, new UpdateRequestStatusDto { Status = RequestStatus.Pending }, default);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetMyRequests_ShowsBothSentAndReceived()
        {
            _context.AddRequest(1, Learner, Mentor, 1);
            _context.AddRequest(2, Mentor, Learner, 1);
            await _context.SaveChangesAsync();

            var result = await ControllerAs(Learner).GetMyRequests(default);
            var requests = ((result as OkObjectResult)!.Value as IEnumerable<LearningRequestDto>)!.ToList();

            Assert.That(requests, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task GetMyRequests_HidesOtherPeoplesRequests()
        {
            _context.AddRequest(1, Learner, Mentor, 1);
            await _context.SaveChangesAsync();

            var result = await ControllerAs(Outsider).GetMyRequests(default);
            var requests = ((result as OkObjectResult)!.Value as IEnumerable<LearningRequestDto>)!.ToList();

            Assert.That(requests, Is.Empty);
        }

        [Test]
        public async Task GetMySessions_ReturnsDtosRatherThanEntities()
        {
            _context.AddRequest(1, Learner, Mentor, 1, RequestStatus.Accepted);
            _context.AddSession(1, 1);
            await _context.SaveChangesAsync();

            var result = await ControllerAs(Learner).GetMySessions(default);
            var sessions = ((result as OkObjectResult)!.Value as IEnumerable<LearningSessionDto>)!.ToList();

            Assert.Multiple(() =>
            {
                Assert.That(sessions, Has.Count.EqualTo(1));
                Assert.That(sessions[0].Learner.Id, Is.EqualTo(Learner));
                Assert.That(sessions[0].Mentor.Id, Is.EqualTo(Mentor));
            });
        }

        [Test]
        public async Task GetMySessions_HidesSessionsTheCallerIsNotPartOf()
        {
            _context.AddRequest(1, Learner, Mentor, 1, RequestStatus.Accepted);
            _context.AddSession(1, 1);
            await _context.SaveChangesAsync();

            var result = await ControllerAs(Outsider).GetMySessions(default);
            var sessions = ((result as OkObjectResult)!.Value as IEnumerable<LearningSessionDto>)!.ToList();

            Assert.That(sessions, Is.Empty);
        }

        [Test]
        public async Task UpdateSession_IsForbidden_ForAnUninvolvedUser()
        {
            _context.AddRequest(1, Learner, Mentor, 1, RequestStatus.Accepted);
            _context.AddSession(1, 1);
            await _context.SaveChangesAsync();

            var result = await ControllerAs(Outsider)
                .UpdateSession(1, new UpdateSessionDto { MeetingLink = "http://evil" }, default);

            Assert.That(result, Is.TypeOf<ForbidResult>());
        }

        [Test]
        public async Task UpdateSession_LetsAParticipantSetTheMeetingLink()
        {
            _context.AddRequest(1, Learner, Mentor, 1, RequestStatus.Accepted);
            _context.AddSession(1, 1);
            await _context.SaveChangesAsync();

            var result = await ControllerAs(Mentor)
                .UpdateSession(1, new UpdateSessionDto { MeetingLink = "https://meet.example/abc" }, default);

            Assert.That(Value<LearningSessionDto>(result).MeetingLink,
                Is.EqualTo("https://meet.example/abc"));
        }

        [Test]
        public async Task UpdateSession_RefusesToScheduleIntoThePast()
        {
            _context.AddRequest(1, Learner, Mentor, 1, RequestStatus.Accepted);
            _context.AddSession(1, 1);
            await _context.SaveChangesAsync();

            var result = await ControllerAs(Mentor)
                .UpdateSession(1, new UpdateSessionDto { ScheduledTime = DateTime.UtcNow.AddDays(-1) }, default);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task UpdateSession_RefusesToEditAFinishedSession()
        {
            _context.AddRequest(1, Learner, Mentor, 1, RequestStatus.Accepted);
            _context.AddSession(1, 1, SessionStatus.Completed);
            await _context.SaveChangesAsync();

            var result = await ControllerAs(Mentor)
                .UpdateSession(1, new UpdateSessionDto { MeetingLink = "https://late" }, default);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task UpdateSession_ReturnsNotFound_ForAnUnknownSession()
        {
            var result = await ControllerAs(Mentor)
                .UpdateSession(999, new UpdateSessionDto(), default);

            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        }

        private async Task GivenACompletedSession()
        {
            _context.AddRequest(1, Learner, Mentor, 1, RequestStatus.Accepted);
            _context.AddSession(1, 1, SessionStatus.Completed);
            await _context.SaveChangesAsync();
        }

        [Test]
        public async Task AddFeedback_RecordsTheSignedInUserAsReviewer()
        {
            await GivenACompletedSession();

            var result = await ControllerAs(Learner)
                .AddFeedback(new CreateFeedbackDto { SessionId = 1, Rating = 5, Comments = "Great" }, default);

            Assert.That(Value<FeedbackDto>(result).Reviewer.Id, Is.EqualTo(Learner));
        }

        [Test]
        public async Task AddFeedback_IsForbidden_ForAnUninvolvedUser()
        {
            await GivenACompletedSession();

            var result = await ControllerAs(Outsider)
                .AddFeedback(new CreateFeedbackDto { SessionId = 1, Rating = 1 }, default);

            Assert.That(result, Is.TypeOf<ForbidResult>());
        }

        [Test]
        public async Task AddFeedback_RefusesASessionThatHasNotHappenedYet()
        {
            _context.AddRequest(1, Learner, Mentor, 1, RequestStatus.Accepted);
            _context.AddSession(1, 1, SessionStatus.Scheduled);
            await _context.SaveChangesAsync();

            var result = await ControllerAs(Learner)
                .AddFeedback(new CreateFeedbackDto { SessionId = 1, Rating = 5 }, default);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>(),
                "Ratings feed the reputation score, so only real sessions may be rated.");
        }

        [Test]
        public async Task AddFeedback_RejectsASecondReviewFromTheSamePerson()
        {
            await GivenACompletedSession();
            var controller = ControllerAs(Learner);

            await controller.AddFeedback(new CreateFeedbackDto { SessionId = 1, Rating = 5 }, default);
            var second = await controller.AddFeedback(
                new CreateFeedbackDto { SessionId = 1, Rating = 1 }, default);

            Assert.That(second, Is.TypeOf<ConflictObjectResult>());
        }

        [Test]
        public async Task AddFeedback_LetsBothParticipantsReviewTheSameSession()
        {
            await GivenACompletedSession();

            await ControllerAs(Learner).AddFeedback(
                new CreateFeedbackDto { SessionId = 1, Rating = 5 }, default);
            var mentorReview = await ControllerAs(Mentor).AddFeedback(
                new CreateFeedbackDto { SessionId = 1, Rating = 4 }, default);

            Assert.Multiple(async () =>
            {
                Assert.That(mentorReview, Is.TypeOf<OkObjectResult>());
                Assert.That(await _context.Feedbacks.CountAsync(), Is.EqualTo(2));
            });
        }

        [Test]
        public async Task AddFeedback_ReturnsNotFound_ForAnUnknownSession()
        {
            var result = await ControllerAs(Learner)
                .AddFeedback(new CreateFeedbackDto { SessionId = 999, Rating = 5 }, default);

            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetFeedbackForSession_IsForbidden_ForAnUninvolvedUser()
        {
            await GivenACompletedSession();

            var result = await ControllerAs(Outsider).GetFeedbackForSession(1, default);

            Assert.That(result, Is.TypeOf<ForbidResult>());
        }

        [Test]
        public async Task GetFeedbackForSession_ReturnsTheReviewsToAParticipant()
        {
            await GivenACompletedSession();
            await ControllerAs(Learner).AddFeedback(
                new CreateFeedbackDto { SessionId = 1, Rating = 5, Comments = "Great" }, default);

            var result = await ControllerAs(Mentor).GetFeedbackForSession(1, default);
            var feedback = ((result as OkObjectResult)!.Value as IEnumerable<FeedbackDto>)!.ToList();

            Assert.Multiple(() =>
            {
                Assert.That(feedback, Has.Count.EqualTo(1));
                Assert.That(feedback[0].Rating, Is.EqualTo(5));
                Assert.That(feedback[0].Comments, Is.EqualTo("Great"));
            });
        }
    }
}
