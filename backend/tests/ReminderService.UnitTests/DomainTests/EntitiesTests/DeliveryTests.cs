using FluentAssertions;
using ReminderService.Domain.Entities;
using ReminderService.Domain.Enums;

namespace ReminderService.UnitTests.DomainTests.EntitiesTests;

[TestFixture]
public class DeliveryTests
{
    private static readonly DateTime Now =
        new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);

    // Берём существующее значение, не привязываясь
    // к конкретному набору каналов первой версии.
    private static readonly ChannelType ValidChannelType =
        Enum.GetValues<ChannelType>()[0];

    private static Delivery CreateDelivery()
    {
        return new Delivery(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ValidChannelType,
            """{"chatId":"-1001234567890"}""",
            Now);
    }

    private static (Delivery Delivery, Guid Token) CreateProcessingDelivery()
    {
        var delivery = CreateDelivery();
        var token = Guid.NewGuid();

        delivery.Claim(token, Now.AddMinutes(5), Now);

        return (delivery, token);
    }

    // Снимок всех открытых свойств для проверки,
    // что неудачная операция ничего не изменила.
    private static Dictionary<string, object?> Snapshot(Delivery delivery)
    {
        return typeof(Delivery)
            .GetProperties()
            .ToDictionary(
                property => property.Name,
                property => property.GetValue(delivery));
    }

    private static void AssertRejected<TException>(
        Delivery delivery,
        Action action)
        where TException : Exception
    {
        var before = Snapshot(delivery);

        action.Should().Throw<TException>();

        Snapshot(delivery).Should().BeEquivalentTo(before);
    }

    [Test]
    public void Constructor_ValidArguments_CreatesPendingDelivery()
    {
        var id = Guid.NewGuid();
        var occurrenceId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        const string destination = """{"chatId":"123"}""";

        var delivery = new Delivery(
            id,
            occurrenceId,
            channelId,
            ValidChannelType,
            destination,
            Now);

        delivery.Id.Should().Be(id);
        delivery.OccurrenceId.Should().Be(occurrenceId);
        delivery.ChannelId.Should().Be(channelId);
        delivery.ChannelTypeSnapshot.Should().Be(ValidChannelType);
        delivery.DestinationSnapshot.Should().Be(destination);

        delivery.Status.Should().Be(DeliveryStatus.Pending);
        delivery.AttemptCount.Should().Be(0);
        delivery.NextAttemptAt.Should().Be(Now);
        delivery.CreatedAt.Should().Be(Now);
        delivery.UpdatedAt.Should().Be(Now);

        delivery.LeaseUntil.Should().BeNull();
        delivery.ClaimToken.Should().BeNull();
        delivery.SentAt.Should().BeNull();
        delivery.LastError.Should().BeNull();
        delivery.ExternalMessageId.Should().BeNull();
    }

    [TestCase(0, "id",
        TestName = "Constructor_EmptyId_Throws")]
    [TestCase(1, "occurrenceId",
        TestName = "Constructor_EmptyOccurrenceId_Throws")]
    [TestCase(2, "channelId",
        TestName = "Constructor_EmptyChannelId_Throws")]
    public void Constructor_EmptyIdentifier_Throws(
        int emptyIndex,
        string parameterName)
    {
        var ids = new[]
        {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        ids[emptyIndex] = Guid.Empty;

        Action action = () => new Delivery(
            ids[0],
            ids[1],
            ids[2],
            ValidChannelType,
            "{}",
            Now);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName(parameterName);
    }

    [TestCase(null, TestName = "Constructor_NullDestination_Throws")]
    [TestCase("", TestName = "Constructor_EmptyDestination_Throws")]
    [TestCase("   ", TestName = "Constructor_WhitespaceDestination_Throws")]
    public void Constructor_InvalidDestination_Throws(string? destination)
    {
        Action action = () => new Delivery(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ValidChannelType,
            destination!,
            Now);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("destinationSnapshot");
    }

    [Test]
    public void Constructor_UnknownChannelType_Throws()
    {
        // Находим значение, которого точно нет в enum.
        var value = int.MinValue;

        while (Enum.IsDefined(typeof(ChannelType), (ChannelType)value))
            value++;

        Action action = () => new Delivery(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            (ChannelType)value,
            "{}",
            Now);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestCase(DateTimeKind.Local)]
    [TestCase(DateTimeKind.Unspecified)]
    public void Constructor_NonUtcCreatedAt_Throws(DateTimeKind kind)
    {
        Action action = () => new Delivery(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ValidChannelType,
            "{}",
            DateTime.SpecifyKind(Now, kind));

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("createdAt");
    }

    [Test]
    public void Claim_ReadyDelivery_StartsProcessing()
    {
        var delivery = CreateDelivery();
        var token = Guid.NewGuid();
        var leaseUntil = Now.AddMinutes(5);

        delivery.Claim(token, leaseUntil, Now);

        delivery.Status.Should().Be(DeliveryStatus.Processing);
        delivery.ClaimToken.Should().Be(token);
        delivery.LeaseUntil.Should().Be(leaseUntil);
        delivery.AttemptCount.Should().Be(1);
        delivery.NextAttemptAt.Should().BeNull();
        delivery.UpdatedAt.Should().Be(Now);
    }

    [Test]
    public void Claim_EmptyToken_ThrowsWithoutChangingState()
    {
        var delivery = CreateDelivery();

        AssertRejected<ArgumentException>(
            delivery,
            () => delivery.Claim(Guid.Empty, Now.AddMinutes(5), Now));
    }

    [TestCase(-1)]
    [TestCase(0)]
    public void Claim_LeaseDoesNotEndInFuture_Throws(int seconds)
    {
        var delivery = CreateDelivery();

        AssertRejected<ArgumentException>(
            delivery,
            () => delivery.Claim(
                Guid.NewGuid(),
                Now.AddSeconds(seconds),
                Now));
    }

    [Test]
    public void Claim_BeforeRetryTime_ThrowsWithoutChangingState()
    {
        var (delivery, token) = CreateProcessingDelivery();
        var failedAt = Now.AddMinutes(1);
        var retryAt = Now.AddMinutes(10);

        delivery.ScheduleRetry(token, "Временная ошибка", retryAt, failedAt);

        AssertRejected<InvalidOperationException>(
            delivery,
            () => delivery.Claim(
                Guid.NewGuid(),
                retryAt.AddMinutes(5),
                retryAt.AddTicks(-1)));
    }

    [Test]
    public void Claim_ExactlyAtRetryTime_StartsNextAttempt()
    {
        var (delivery, token) = CreateProcessingDelivery();
        var retryAt = Now.AddMinutes(10);

        delivery.ScheduleRetry(
            token,
            "Временная ошибка",
            retryAt,
            Now.AddMinutes(1));

        var newToken = Guid.NewGuid();

        delivery.Claim(newToken, retryAt.AddMinutes(5), retryAt);

        delivery.Status.Should().Be(DeliveryStatus.Processing);
        delivery.ClaimToken.Should().Be(newToken);
        delivery.AttemptCount.Should().Be(2);
        delivery.NextAttemptAt.Should().BeNull();
    }

    [Test]
    public void Claim_BeforeLeaseExpires_ThrowsWithoutChangingState()
    {
        var (delivery, _) = CreateProcessingDelivery();

        AssertRejected<InvalidOperationException>(
            delivery,
            () => delivery.Claim(
                Guid.NewGuid(),
                Now.AddMinutes(10),
                Now.AddMinutes(5).AddTicks(-1)));
    }

    [Test]
    public void Claim_ExactlyAtLeaseExpiration_ReplacesClaim()
    {
        var (delivery, oldToken) = CreateProcessingDelivery();
        var newToken = Guid.NewGuid();
        var reclaimedAt = Now.AddMinutes(5);
        var newLeaseUntil = reclaimedAt.AddMinutes(5);

        delivery.Claim(newToken, newLeaseUntil, reclaimedAt);

        delivery.Status.Should().Be(DeliveryStatus.Processing);
        delivery.ClaimToken.Should().Be(newToken);
        delivery.ClaimToken.Should().NotBe(oldToken);
        delivery.LeaseUntil.Should().Be(newLeaseUntil);
        delivery.AttemptCount.Should().Be(2);
        delivery.UpdatedAt.Should().Be(reclaimedAt);
    }

    [Test]
    public void Claim_ExpiredLeaseWithSameToken_Throws()
    {
        var (delivery, token) = CreateProcessingDelivery();

        AssertRejected<ArgumentException>(
            delivery,
            () => delivery.Claim(
                token,
                Now.AddMinutes(10),
                Now.AddMinutes(5)));
    }

    [TestCase(null)]
    [TestCase("external-message-123")]
    public void MarkSent_ActiveClaim_CompletesDelivery(string? messageId)
    {
        var (delivery, token) = CreateProcessingDelivery();
        var sentAt = Now.AddMinutes(1);

        delivery.MarkSent(token, sentAt, messageId);

        delivery.Status.Should().Be(DeliveryStatus.Sent);
        delivery.SentAt.Should().Be(sentAt);
        delivery.ExternalMessageId.Should().Be(messageId);
        delivery.LastError.Should().BeNull();
        delivery.ClaimToken.Should().BeNull();
        delivery.LeaseUntil.Should().BeNull();
        delivery.NextAttemptAt.Should().BeNull();
        delivery.UpdatedAt.Should().Be(sentAt);
        delivery.AttemptCount.Should().Be(1);
    }

    [TestCase("MarkSent")]
    [TestCase("ScheduleRetry")]
    [TestCase("Block")]
    public void SaveResult_WrongToken_ThrowsWithoutChangingState(
        string operation)
    {
        var (delivery, _) = CreateProcessingDelivery();

        AssertRejected<InvalidOperationException>(
            delivery,
            () => SaveResult(
                delivery,
                operation,
                Guid.NewGuid(),
                Now.AddMinutes(1)));
    }

    [TestCase("MarkSent")]
    [TestCase("ScheduleRetry")]
    [TestCase("Block")]
    public void SaveResult_ExactlyAtLeaseExpiration_Throws(
        string operation)
    {
        var (delivery, token) = CreateProcessingDelivery();

        AssertRejected<InvalidOperationException>(
            delivery,
            () => SaveResult(
                delivery,
                operation,
                token,
                Now.AddMinutes(5)));
    }

    [TestCase("MarkSent")]
    [TestCase("ScheduleRetry")]
    [TestCase("Block")]
    public void SaveResult_OldTokenAfterReclaim_Throws(string operation)
    {
        var (delivery, oldToken) = CreateProcessingDelivery();
        var reclaimedAt = Now.AddMinutes(5);

        delivery.Claim(
            Guid.NewGuid(),
            reclaimedAt.AddMinutes(5),
            reclaimedAt);

        AssertRejected<InvalidOperationException>(
            delivery,
            () => SaveResult(
                delivery,
                operation,
                oldToken,
                reclaimedAt.AddSeconds(1)));
    }

    [Test]
    public void ScheduleRetry_ActiveClaim_SchedulesNextAttempt()
    {
        var (delivery, token) = CreateProcessingDelivery();
        var failedAt = Now.AddMinutes(1);
        var retryAt = Now.AddMinutes(10);

        delivery.ScheduleRetry(token, "Сервис недоступен", retryAt, failedAt);

        delivery.Status.Should().Be(DeliveryStatus.RetryScheduled);
        delivery.LastError.Should().Be("Сервис недоступен");
        delivery.NextAttemptAt.Should().Be(retryAt);
        delivery.ClaimToken.Should().BeNull();
        delivery.LeaseUntil.Should().BeNull();
        delivery.SentAt.Should().BeNull();
        delivery.UpdatedAt.Should().Be(failedAt);
        delivery.AttemptCount.Should().Be(1);
    }

    [TestCase(-1)]
    [TestCase(0)]
    public void ScheduleRetry_TimeNotInFuture_Throws(int seconds)
    {
        var (delivery, token) = CreateProcessingDelivery();
        var failedAt = Now.AddMinutes(1);

        AssertRejected<ArgumentException>(
            delivery,
            () => delivery.ScheduleRetry(
                token,
                "Ошибка",
                failedAt.AddSeconds(seconds),
                failedAt));
    }

    [TestCase("ScheduleRetry", null)]
    [TestCase("ScheduleRetry", "")]
    [TestCase("ScheduleRetry", "   ")]
    [TestCase("Block", null)]
    [TestCase("Block", "")]
    [TestCase("Block", "   ")]
    public void SaveFailure_InvalidError_Throws(
        string operation,
        string? error)
    {
        var (delivery, token) = CreateProcessingDelivery();
        var failedAt = Now.AddMinutes(1);

        AssertRejected<ArgumentException>(
            delivery,
            () =>
            {
                if (operation == "Block")
                    delivery.Block(token, error!, failedAt);
                else
                    delivery.ScheduleRetry(
                        token,
                        error!,
                        failedAt.AddMinutes(10),
                        failedAt);
            });
    }

    [Test]
    public void Block_ActiveClaim_BlocksDelivery()
    {
        var (delivery, token) = CreateProcessingDelivery();
        var blockedAt = Now.AddMinutes(1);

        delivery.Block(token, "Нет доступа к чату", blockedAt);

        delivery.Status.Should().Be(DeliveryStatus.Blocked);
        delivery.LastError.Should().Be("Нет доступа к чату");
        delivery.NextAttemptAt.Should().BeNull();
        delivery.ClaimToken.Should().BeNull();
        delivery.LeaseUntil.Should().BeNull();
        delivery.UpdatedAt.Should().Be(blockedAt);
    }

    [Test]
    public void Resume_BlockedDelivery_MakesItReadyForRetry()
    {
        var (delivery, token) = CreateProcessingDelivery();
        delivery.Block(token, "Ошибка", Now.AddMinutes(1));

        var destination = delivery.DestinationSnapshot;
        var resumedAt = Now.AddMinutes(2);

        delivery.Resume(resumedAt);

        delivery.Status.Should().Be(DeliveryStatus.RetryScheduled);
        delivery.NextAttemptAt.Should().Be(resumedAt);
        delivery.UpdatedAt.Should().Be(resumedAt);
        delivery.LastError.Should().Be("Ошибка");
        delivery.DestinationSnapshot.Should().Be(destination);
        delivery.AttemptCount.Should().Be(1);
        delivery.ClaimToken.Should().BeNull();
        delivery.LeaseUntil.Should().BeNull();
    }

    [Test]
    public void MarkSent_AfterRetry_ClearsPreviousError()
    {
        var (delivery, token) = CreateProcessingDelivery();
        var retryAt = Now.AddMinutes(10);

        delivery.ScheduleRetry(
            token,
            "Временная ошибка",
            retryAt,
            Now.AddMinutes(1));

        var newToken = Guid.NewGuid();
        delivery.Claim(newToken, retryAt.AddMinutes(5), retryAt);

        delivery.MarkSent(newToken, retryAt.AddMinutes(1));

        delivery.Status.Should().Be(DeliveryStatus.Sent);
        delivery.LastError.Should().BeNull();
        delivery.AttemptCount.Should().Be(2);
    }

    [TestCase(DeliveryStatus.Pending)]
    [TestCase(DeliveryStatus.Processing)]
    [TestCase(DeliveryStatus.RetryScheduled)]
    [TestCase(DeliveryStatus.Sent)]
    [TestCase(DeliveryStatus.Cancelled)]
    public void Resume_NotBlockedDelivery_Throws(DeliveryStatus status)
    {
        var delivery = CreateInStatus(status);

        AssertRejected<InvalidOperationException>(
            delivery,
            () => delivery.Resume(Now.AddMinutes(2)));
    }

    [TestCase(DeliveryStatus.Pending)]
    [TestCase(DeliveryStatus.Processing)]
    [TestCase(DeliveryStatus.RetryScheduled)]
    [TestCase(DeliveryStatus.Blocked)]
    public void Cancel_UnfinishedDelivery_CancelsAndClearsClaim(
        DeliveryStatus status)
    {
        var delivery = CreateInStatus(status);
        var cancelledAt = Now.AddMinutes(2);
        var attemptCount = delivery.AttemptCount;

        delivery.Cancel(cancelledAt);

        delivery.Status.Should().Be(DeliveryStatus.Cancelled);
        delivery.NextAttemptAt.Should().BeNull();
        delivery.ClaimToken.Should().BeNull();
        delivery.LeaseUntil.Should().BeNull();
        delivery.UpdatedAt.Should().Be(cancelledAt);
        delivery.AttemptCount.Should().Be(attemptCount);
    }

    [Test]
    public void Cancel_SentDelivery_ThrowsWithoutChangingState()
    {
        var delivery = CreateInStatus(DeliveryStatus.Sent);

        AssertRejected<InvalidOperationException>(
            delivery,
            () => delivery.Cancel(Now.AddMinutes(2)));
    }

    [Test]
    public void Cancel_AlreadyCancelled_DoesNotChangeState()
    {
        var delivery = CreateInStatus(DeliveryStatus.Cancelled);
        var before = Snapshot(delivery);

        delivery.Cancel(Now.AddMinutes(2));

        Snapshot(delivery).Should().BeEquivalentTo(before);
    }

    [TestCase(DeliveryStatus.Blocked)]
    [TestCase(DeliveryStatus.Sent)]
    [TestCase(DeliveryStatus.Cancelled)]
    public void Claim_UnavailableStatus_Throws(DeliveryStatus status)
    {
        var delivery = CreateInStatus(status);

        AssertRejected<InvalidOperationException>(
            delivery,
            () => delivery.Claim(
                Guid.NewGuid(),
                Now.AddMinutes(10),
                Now.AddMinutes(2)));
    }

    [Test]
    public void MarkSent_AlreadySent_ThrowsWithoutChangingState()
    {
        var (delivery, token) = CreateProcessingDelivery();
        delivery.MarkSent(token, Now.AddMinutes(1));

        AssertRejected<InvalidOperationException>(
            delivery,
            () => delivery.MarkSent(token, Now.AddMinutes(2)));
    }

    [Test]
    public void MarkSent_AfterCancellation_ThrowsWithoutChangingState()
    {
        var (delivery, token) = CreateProcessingDelivery();
        delivery.Cancel(Now.AddMinutes(1));

        AssertRejected<InvalidOperationException>(
            delivery,
            () => delivery.MarkSent(token, Now.AddMinutes(2)));
    }

    [TestCase(DateTimeKind.Local)]
    [TestCase(DateTimeKind.Unspecified)]
    public void Claim_NonUtcLeaseUntil_Throws(DateTimeKind kind)
    {
        var delivery = CreateDelivery();

        AssertRejected<ArgumentException>(
            delivery,
            () => delivery.Claim(
                Guid.NewGuid(),
                DateTime.SpecifyKind(Now.AddMinutes(5), kind),
                Now));
    }

    [TestCase(DateTimeKind.Local)]
    [TestCase(DateTimeKind.Unspecified)]
    public void ScheduleRetry_NonUtcNextAttemptAt_Throws(DateTimeKind kind)
    {
        var (delivery, token) = CreateProcessingDelivery();

        AssertRejected<ArgumentException>(
            delivery,
            () => delivery.ScheduleRetry(
                token,
                "Ошибка",
                DateTime.SpecifyKind(Now.AddMinutes(10), kind),
                Now.AddMinutes(1)));
    }

    [TestCase("Claim")]
    [TestCase("MarkSent")]
    [TestCase("ScheduleRetry")]
    [TestCase("Block")]
    [TestCase("Resume")]
    [TestCase("Cancel")]
    public void Operation_TimeBeforePreviousChange_Throws(string operation)
    {
        var (delivery, token) = CreateForOperation(operation);

        AssertRejected<ArgumentException>(
            delivery,
            () => InvokeOperation(
                delivery,
                token,
                operation,
                delivery.UpdatedAt.AddTicks(-1)));
    }

    [TestCase("Claim", DateTimeKind.Local)]
    [TestCase("Claim", DateTimeKind.Unspecified)]
    [TestCase("MarkSent", DateTimeKind.Local)]
    [TestCase("MarkSent", DateTimeKind.Unspecified)]
    [TestCase("ScheduleRetry", DateTimeKind.Local)]
    [TestCase("ScheduleRetry", DateTimeKind.Unspecified)]
    [TestCase("Block", DateTimeKind.Local)]
    [TestCase("Block", DateTimeKind.Unspecified)]
    [TestCase("Resume", DateTimeKind.Local)]
    [TestCase("Resume", DateTimeKind.Unspecified)]
    [TestCase("Cancel", DateTimeKind.Local)]
    [TestCase("Cancel", DateTimeKind.Unspecified)]
    public void Operation_NonUtcTime_Throws(
        string operation,
        DateTimeKind kind)
    {
        var (delivery, token) = CreateForOperation(operation);
        var invalidTime = DateTime.SpecifyKind(
            delivery.UpdatedAt.AddSeconds(1).UtcDateTime,
            kind);

        AssertRejected<ArgumentException>(
            delivery,
            () => InvokeOperation(
                delivery,
                token,
                operation,
                invalidTime));
    }

    private static Delivery CreateInStatus(DeliveryStatus status)
    {
        var delivery = CreateDelivery();

        if (status == DeliveryStatus.Pending)
            return delivery;

        var token = Guid.NewGuid();
        delivery.Claim(token, Now.AddMinutes(5), Now);

        switch (status)
        {
            case DeliveryStatus.Processing:
                break;

            case DeliveryStatus.RetryScheduled:
                delivery.ScheduleRetry(
                    token,
                    "Ошибка",
                    Now.AddMinutes(10),
                    Now.AddMinutes(1));
                break;

            case DeliveryStatus.Blocked:
                delivery.Block(token, "Ошибка", Now.AddMinutes(1));
                break;

            case DeliveryStatus.Sent:
                delivery.MarkSent(token, Now.AddMinutes(1));
                break;

            case DeliveryStatus.Cancelled:
                delivery.Cancel(Now.AddMinutes(1));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }

        return delivery;
    }

    private static (Delivery Delivery, Guid Token) CreateForOperation(
        string operation)
    {
        if (operation == "Claim")
            return (CreateDelivery(), Guid.NewGuid());

        var (delivery, token) = CreateProcessingDelivery();

        if (operation == "Resume")
            delivery.Block(token, "Ошибка", Now.AddMinutes(1));

        return (delivery, token);
    }

    private static void SaveResult(
        Delivery delivery,
        string operation,
        Guid token,
        DateTimeOffset now)
    {
        switch (operation)
        {
            case "MarkSent":
                delivery.MarkSent(token, now);
                break;

            case "ScheduleRetry":
                delivery.ScheduleRetry(
                    token,
                    "Ошибка",
                    now.AddMinutes(10),
                    now);
                break;

            case "Block":
                delivery.Block(token, "Ошибка", now);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private static void InvokeOperation(
        Delivery delivery,
        Guid token,
        string operation,
        DateTimeOffset now)
    {
        switch (operation)
        {
            case "Claim":
                delivery.Claim(token, Now.AddMinutes(10), now);
                break;

            case "Resume":
                delivery.Resume(now);
                break;

            case "Cancel":
                delivery.Cancel(now);
                break;

            default:
                SaveResult(delivery, operation, token, now);
                break;
        }
    }
}