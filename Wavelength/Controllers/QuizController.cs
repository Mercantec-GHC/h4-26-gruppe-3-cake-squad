using Commons.Enums;
using Commons.Models.Database;
using Commons.Models.Dtos;
using Commons.Models.QuizModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wavelength.Data;

namespace Wavelength.Controllers
{
    /// <summary>
    /// Provides API endpoints for managing and interacting with user quizzes, including retrieving, editing,
    /// submitting, and evaluating quizzes.
    /// </summary>
    /// <remarks>This controller requires authentication for most operations and is intended to be used by
    /// clients to manage their own quizzes or interact with quizzes created by other users. Endpoints enforce
    /// validation and authorization to ensure quiz integrity and proper access control. All routes are prefixed with
    /// 'Quiz' by default. Thread safety is managed by the underlying ASP.NET Core framework; each request is handled
    /// independently.</remarks>
    [ApiController]
    [Route("[controller]")]
    public class QuizController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the QuizController class using the specified database context.
        /// </summary>
        /// <param name="dbContext">The database context to be used for data access operations. Cannot be null.</param>
        public QuizController(AppDbContext dbContext) : base(dbContext)
        {
            this.SetDefaultUserQuery(q => q.Include(u => u.Questionnaire));
        }

        /// <summary>
        /// Retrieves the quiz associated with the currently authenticated user.
        /// </summary>
        /// <returns>An <see cref="ActionResult{Quiz}"/> containing the user's quiz if it exists; <see
        /// cref="UnauthorizedResult"/> if the user is not authenticated; or <see cref="NotFoundObjectResult"/> if the
        /// quiz has not been set up.</returns>
        [HttpGet("MyQuiz"), Authorize]
        public async Task<ActionResult<Quiz>> GetMyQuiz()
        {
            var user = await GetSignedInUserAsync();
            if (user == null) return StatusCode(500);

            var questionnaire = user.Questionnaire;
            if (questionnaire == null || questionnaire.Quiz == null) return NotFound("Your quiz has not been set up yet.");

            return Ok(user.Questionnaire.Quiz);
        }

        /// <summary>
        /// Updates the quiz associated with the signed-in user's questionnaire.
        /// </summary>
        /// <remarks>The user must be authenticated to call this method. The total required score must not
        /// exceed the sum of all question scores. All text fields for questions and options must be
        /// non-empty.</remarks>
        /// <param name="quiz">The quiz to update, including all questions, options, and required score. Must contain at least one
        /// question, and each question must have at least two options, a valid correct answer index, and a positive
        /// score.</param>
        /// <returns>A 204 No Content response if the quiz is updated successfully; otherwise, a 400 Bad Request response if
        /// validation fails, or a 500 Internal Server Error response if the user is not found.</returns>
        [HttpPost("EditQuiz"), Authorize]
        public async Task<ActionResult> EditQuiz(Quiz quiz)
        {
            var user = await GetSignedInUserAsync();
            if (user == null) return StatusCode(500);

            if (quiz.ScoreRequired <= 0) return BadRequest("ScoreRequired must be greater than zero.");
            if (quiz.Questions == null || quiz.Questions.Count == 0) return BadRequest("Quiz must contain at least one question.");

            // Validate each question and calculate total possible score
            int totalScore = 0;
            foreach (var question in quiz.Questions)
            {
                if (string.IsNullOrWhiteSpace(question.QuestionText))
                    return BadRequest("All questions must have text.");
                if (question.Options == null || question.Options.Count < 2)
                    return BadRequest("Each question must have at least two answers.");
                if (question.CorrectOptionIndex < 0 || question.CorrectOptionIndex >= question.Options.Count)
                    return BadRequest("Each question must have a valid correct answer index.");
                //if (question.Score <= 0)
                //    return BadRequest("Each question must have a score greater than zero.");
                if (question.Score != 1)
                    return BadRequest("Each question must have a score 1.");

                totalScore += question.Score;

                foreach (var option in question.Options)
                {
                    if (string.IsNullOrWhiteSpace(option.Text))
                        return BadRequest("All answer options must have text.");
                }
            }
            if (quiz.ScoreRequired > totalScore)
                return BadRequest("ScoreRequired cannot be greater than the total possible score of all questions.");

            // Update or create the user's questionnaire and assign the quiz
            var questionnaire = user.Questionnaire;
            if (questionnaire == null)
            {
                questionnaire = new Questionnaire
                {
                    UserId = user.Id,
                };
                await DbContext.Questionnaires.AddAsync(questionnaire);
            }
            questionnaire.Quiz = quiz;
            await DbContext.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Retrieves the list of quiz questions assigned to the specified user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose quiz questions are to be retrieved. Cannot be null.</param>
        /// <returns>An <see cref="ActionResult{T}">ActionResult</see> containing a list of <see cref="QuestionBase"/> objects
        /// representing the user's quiz questions. Returns a 404 Not Found response if the user does not exist or if
        /// the user's quiz has not been set up.</returns>
        [HttpGet("UserQuiz/{userId}")]
        public async Task<ActionResult<List<QuestionBase>>> GetUserQuiz(string userId)
        {
            var user = await DbContext.Users
                .Where(u => u.Id == userId)
                .Include(u => u.Questionnaire)
                .FirstOrDefaultAsync();
            if (user == null) return NotFound("User not found.");
            if (user.Questionnaire == null || user.Questionnaire.Quiz == null) return NotFound("The user's quiz has not been set up yet.");

            var questions = user.Questionnaire.Quiz.Questions
                .Select(q => new QuestionBase
                {
                    QuestionText = q.QuestionText,
                    Type = q.Type,
                    Options = q.Options
                })
                .ToList();

            return Ok(questions);
        }

        /// <summary>
        /// Submits answers for a user's quiz and returns the result, including the match percentage and pass status.
        /// </summary>
        /// <remarks>A user cannot submit their own quiz or submit answers for the same quiz more than
        /// once. The quiz must be set up for the target user before answers can be submitted. The result includes
        /// whether the score meets the required threshold to pass.</remarks>
        /// <param name="dto">An object containing the quiz owner's user ID and the array of selected answer indices. The number of
        /// answers must match the number of questions in the quiz.</param>
        /// <returns>An <see cref="ActionResult{QuizResultDto}"/> containing the quiz result, including the match percentage and
        /// whether the quiz was passed. Returns an error response if the submission is invalid or not allowed.</returns>
        [HttpPost("SubmitQuiz"), Authorize]
        public async Task<ActionResult<QuizResultDto>> SubmitQuiz(QuizSubmitDto dto)
        {
            var user = await GetSignedInUserAsync();
            if (user == null) return StatusCode(500);

            if (user.Id == dto.UserId)
                return BadRequest("You cannot submit your own quiz.");

            var targetUser = await DbContext.Users
                .Where(u => u.Id == dto.UserId)
                .Include(u => u.Questionnaire)
                .FirstOrDefaultAsync();
            if (targetUser == null) return NotFound("User not found.");
            if (targetUser.Questionnaire == null || targetUser.Questionnaire.Quiz == null) return NotFound("The user's quiz has not been set up yet.");

            if (await DbContext.QuestionScores.AnyAsync(qs => qs.PlayerId == user.Id && qs.QuizOwnerId == targetUser.Id))
                return BadRequest("You have already submitted this quiz.");

            // Validate answer count
            var quiz = targetUser.Questionnaire.Quiz;
            if (dto.Answers.Length != quiz.Questions.Count)
                return BadRequest("Number of answers does not match number of questions.");

            // Calculate total score
            int totalScore = 0;
            for (int i = 0; i < quiz.Questions.Count; i++)
            {
                var question = quiz.Questions[i];
                if (dto.Answers[i] == question.CorrectOptionIndex)
                {
                    totalScore += question.Score;
                }
            }

            // Calculate match percentage and pass status
            int maxScore = quiz.Questions.Sum(q => q.Score);
            int matchPercent = (int)((double)totalScore / maxScore * 100);
            bool passed = totalScore >= quiz.ScoreRequired;

            // Store quiz score
            QuizScore quizScore = new QuizScore
            {
                PlayerId = user.Id,
                QuizOwnerId = targetUser.Id,
                MatchPercent = matchPercent
            };
			await DbContext.QuestionScores.AddAsync(quizScore);

            UserVisibility visibility = new UserVisibility
            {
                SourceUserId = user.Id,
                TargetUserId = targetUser.Id,
                Visibility = passed ? UserVisibilityEnum.Visible : UserVisibilityEnum.Dismissed
			};
            await DbContext.UserVisibilities.AddAsync(visibility);

			await DbContext.SaveChangesAsync();

            return Ok(new QuizResultDto
            {
                MatchPercent = matchPercent,
                Passed = passed
            });
        }

        /// <summary>
        /// Gets the match percentage for the quiz taken by the signed-in user, as created by the specified user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user who owns the quiz for which to retrieve the match percentage. Cannot be
        /// the same as the signed-in user's ID.</param>
        /// <returns>An HTTP 200 response containing the match percentage as an integer if found; otherwise, an appropriate error
        /// response such as 400 if the user requests their own quiz, 404 if no quiz score exists, or 500 if the
        /// signed-in user cannot be determined.</returns>
        [HttpGet("MatchPercentage/{userId}"), Authorize]
        public async Task<ActionResult<int>> GetMatchPercentage(string userId)
        {
            var user = await GetSignedInUserAsync();
            if (user == null) return StatusCode(500);

            if (user.Id == userId)
                return BadRequest("You cannot get a match percentage for your own quiz.");

            var quizScore = await DbContext.QuestionScores
                .Where(qs => qs.PlayerId == user.Id && qs.QuizOwnerId == userId)
                .FirstOrDefaultAsync();

            if (quizScore == null) return NotFound("No quiz score found for the specified users.");
            
            return Ok(quizScore.MatchPercent);
        }
    }
}
