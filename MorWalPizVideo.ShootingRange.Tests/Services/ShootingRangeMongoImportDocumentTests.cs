using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MorWalPizVideo.ShootingRange.Migration.LegacyMongo;
using MorWalPizVideo.ShootingRange.Models;

[Trait("Category", "TestGroup:ShootingRange")]
public sealed class ShootingRangeMongoImportDocumentTests
{
    [Fact]
    public void Legacy_ObjectId_and_normalized_user_fields_are_read_without_modification()
    {
        var id = ObjectId.GenerateNewId();
        var document = new BsonDocument
        {
            ["_id"] = id,
            ["username"] = " range-user ",
            ["normalizedUsername"] = "range-user",
            ["passwordHash"] = "legacy-hash",
            ["status"] = (int)ShootingRangeAccountStatus.Approved,
            ["securityVersion"] = "legacy-security-version"
        };

        var user = BsonSerializer.Deserialize<LegacyUser>(document);

        user.Id.Should().Be(id.ToString());
        user.Username.Should().Be(" range-user ");
        user.NormalizedUsername.Should().Be("range-user");
        user.PasswordHash.Should().Be("legacy-hash");
        user.Status.Should().Be(ShootingRangeAccountStatus.Approved);
        user.SecurityVersion.Should().Be("legacy-security-version");
    }

    [Fact]
    public void Legacy_config_defaults_and_time_spans_are_preserved()
    {
        var id = ObjectId.GenerateNewId();
        var config = BsonSerializer.Deserialize<LegacyConfig>(new BsonDocument { ["_id"] = id });

        config.Id.Should().Be(id.ToString());
        config.OpeningDays.Should().Equal(DayOfWeek.Saturday, DayOfWeek.Sunday);
        config.MorningStart.Should().Be(new TimeSpan(9, 0, 0));
        config.AfternoonEnd.Should().Be(new TimeSpan(18, 0, 0));
        config.HourlyMinutes.Should().Be(60);
        config.ReservedReleaseDaysBefore.Should().Be(2);
    }

    [Fact]
    public void Legacy_booking_dates_and_thread_message_shape_round_trip()
    {
        var booking = new LegacyBooking
        {
            Id = "legacy-string-id",
            LocalDate = new DateOnly(2026, 12, 5),
            StartUtc = new DateTime(2026, 12, 5, 8, 0, 0, DateTimeKind.Utc),
            EndUtc = new DateTime(2026, 12, 5, 9, 0, 0, DateTimeKind.Utc)
        };
        var thread = new LegacyThread
        {
            Id = "thread-id",
            Messages = [new("legacy-user", false, "text", new DateTime(2026, 12, 5, 8, 0, 0, DateTimeKind.Utc))]
        };

        var persistedBooking = BsonSerializer.Deserialize<LegacyBooking>(booking.ToBsonDocument());
        var threadDocument = thread.ToBsonDocument();
        var persistedThread = BsonSerializer.Deserialize<LegacyThread>(threadDocument);

        persistedBooking.Id.Should().Be(booking.Id);
        persistedBooking.LocalDate.Should().Be(booking.LocalDate);
        persistedBooking.StartUtc.Should().Be(booking.StartUtc);
        threadDocument["messages"].AsBsonArray[0].AsBsonDocument.Names.Should()
            .Contain(["AuthorUserId", "FromAdmin", "Text", "CreatedUtc"]);
        persistedThread.Messages.Should().ContainSingle()
            .Which.Should().Be(thread.Messages[0]);
    }
}
