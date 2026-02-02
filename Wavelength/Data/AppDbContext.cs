using Commons.Models.Database;
using Commons.Models.QuizModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        public DbSet<Participant> Participants { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }
        
        /// <summary>
        /// Updates the creation and modification timestamps for tracked entities derived from the Common<T> base class
        /// that are being added or modified in the current context.
        /// </summary>
        /// <remarks>This method sets the CreatedAt and UpdatedAt properties to the current UTC time for
        /// new entities, and updates the UpdatedAt property for modified entities. It should be called before saving
        /// changes to ensure timestamp consistency across entities.</remarks>
        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e =>
                    //e.Entity is Common<int> ||    
                    e.Entity.GetType().BaseType?.IsGenericType == true &&
                    e.Entity.GetType().BaseType?.GetGenericTypeDefinition() == typeof(Common<>))
                .Where(e =>
                    e.State == EntityState.Added ||
                    e.State == EntityState.Modified);

            var now = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                dynamic entity = entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = now;
                }

                entity.UpdatedAt = now;
            }
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

			// Configure the one-to-many relationship between QuizScore and User (Player)
			modelBuilder.Entity<QuizScore>()
                .HasOne(qs => qs.Player)
                .WithMany()
                .HasForeignKey(qs => qs.PlayerId);

			// Configure the one-to-many relationship between QuizScore and User (QuizOwner)
			modelBuilder.Entity<QuizScore>()
                .HasOne(qs => qs.QuizOwner)
                .WithMany()
                .HasForeignKey(qs => qs.QuizOwnerId);

			// Configure the relationship between Participant and User
			modelBuilder.Entity<Participant>()
				.HasOne(p => p.User)
				.WithMany(u => u.Participants)
				.HasForeignKey(p => p.UserId);

			// Configure the relationship between Participant and ChatRoom
			modelBuilder.Entity<Participant>()
				.HasOne(p => p.ChatRoom)
				.WithMany(cr => cr.Participants)
				.HasForeignKey(p => p.ChatRoomId);

			// Configure the relationship between ChatMessage and User (Sender)
			modelBuilder.Entity<ChatMessage>()
				.HasOne(cm => cm.Sender)
				.WithMany()
				.HasForeignKey(cm => cm.SenderId);

			// Configure the relationship between ChatMessage and ChatRoom
			modelBuilder.Entity<ChatMessage>()
				.HasOne(cm => cm.ChatRoom)
				.WithMany(cr => cr.ChatMessages)
				.HasForeignKey(cm => cm.ChatRoomId);
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
