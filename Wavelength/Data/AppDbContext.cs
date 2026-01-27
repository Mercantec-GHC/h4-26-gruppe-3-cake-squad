using Commons.Models;
using Commons.Models.QuizModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Wavelength.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<QuestionPicture> QuestionPictures { get; set; }
        public DbSet<Questionnaire> Questionnaires { get; set; }
        public DbSet<ProfilePicture> ProfilePictures { get; set; }
        public DbSet<QuizScore> QuestionScores { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define a ValueConverter for Quiz to JSON string
            var quizConverter = new ValueConverter<Quiz?, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => DeserializeQuiz(v)
            );

            // Configure the Questionnaire entity to use the Quiz converter
            modelBuilder.Entity<Questionnaire>()
                .Property(q => q.Quiz)
                .HasConversion(quizConverter)
                .HasColumnType("json");

            // Configure the one-to-one relationship between User and Questionnaire
            modelBuilder.Entity<Questionnaire>()
                .HasOne(q => q.User)
                .WithOne(u => u.Questionnaire)
                .HasForeignKey<Questionnaire>(q => q.UserId);

            // Configure the relationship between UserRole and User
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            // Configure the relationship between ProfilePicture and User
            modelBuilder.Entity<ProfilePicture>()
                .HasOne(pp => pp.User)
                .WithMany(u => u.ProfilePictures)
                .HasForeignKey(pp => pp.UserId);

            // Configure the relationship between RefreshToken and User
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId);

            // Configure the relationship between QuestionPicture and Questionnaire
            modelBuilder.Entity<QuestionPicture>()
                .HasOne(qp => qp.Questionnaire)
                .WithMany(q => q.Pictures)
                .HasForeignKey(qp => qp.QuestionnaireId);

            // Configure the one-to-one relationship between QuizScore and User (Player)
            modelBuilder.Entity<QuizScore>()
                .HasOne(qs => qs.Player)
                .WithOne()
                .HasForeignKey<QuizScore>(qs => qs.PlayerId);

            // Configure the one-to-one relationship between QuizScore and User (QuizOwner)
            modelBuilder.Entity<QuizScore>()
                .HasOne(qs => qs.QuizOwner)
                .WithOne()
                .HasForeignKey<QuizScore>(qs => qs.QuizOwnerId);
        }

        /// <summary>
        /// Deserializes a JSON string into a <see cref="Quiz"/> object.
        /// </summary>
        /// <remarks>Returns <see langword="null"/> if the input is invalid or if deserialization fails
        /// due to malformed JSON.</remarks>
        /// <param name="v">The JSON string representing a quiz to deserialize. Cannot be null, empty, or whitespace.</param>
        /// <returns>A <see cref="Quiz"/> object if deserialization is successful; otherwise, <see langword="null"/>.</returns>
        private static Quiz? DeserializeQuiz(string v)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(v)) return null;
                
                return JsonSerializer.Deserialize<Quiz>(v, (JsonSerializerOptions)null) ?? null;
            } 
            catch
            {
                return null;
            }
        }
    }
}
