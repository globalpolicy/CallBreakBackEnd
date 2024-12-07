using CallBreakBackEnd.Models.Db;
using Microsoft.EntityFrameworkCore;

namespace CallBreakBackEnd.Models
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<User>()
				.HasIndex(user => user.Email)
				.IsUnique();

			modelBuilder.Entity<Score>()
				.HasIndex(score => new { score.GameId, score.PlayerId })
				.IsUnique();

			modelBuilder.Entity<Turn>()
				.HasIndex(turn => new { turn.RoundId, turn.PlayerId })
				.IsUnique();

			modelBuilder.Entity<Game>()
				.Property(game => game.CreationDate)
				.HasDefaultValueSql("NOW()");

			modelBuilder.Entity<Player>()
				.Property(player => player.JoinedAt)
				.HasDefaultValueSql("NOW()");

			modelBuilder.Entity<Room>()
				.Property(room => room.CreatedAt)
				.HasDefaultValueSql("NOW()");

			modelBuilder.Entity<Round>()
				.Property(round => round.CreatedAt)
				.HasDefaultValueSql("NOW()");

			modelBuilder.Entity<Score>()
				.Property(score => score.CreatedAt)
				.HasDefaultValueSql("NOW()");

			modelBuilder.Entity<Turn>()
				.Property(turn => turn.CreatedAt)
				.HasDefaultValueSql("NOW()");

			base.OnModelCreating(modelBuilder);
		}
		public DbSet<User> Users { get; set; }
		public DbSet<Room> Rooms { get; set; }
		public DbSet<Player> Players { get; set; }
		public DbSet<Game> Games { get; set; }
		public DbSet<Round> Rounds { get; set; }
		public DbSet<Turn> Turns { get; set; }
		public DbSet<Score> Scores { get; set; }

	}
}
