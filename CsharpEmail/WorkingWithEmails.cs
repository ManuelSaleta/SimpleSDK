
using System;
using System.Threading.Tasks;

// ============================================================================
// SECTION 1: DOMAIN MODELS (Immutable Records & Interfaces)
// ============================================================================

/// <summary>
/// Defines the contract for any email model.
/// 'init' properties ensure objects are immutable once created.
/// </summary>
public interface IEmailEntity
{
    string Sender { get; init; }
    string Recipient { get; init; }
}

/// <summary>
/// Abstract base record for all email types.
/// C# records provide value equality and non-destructive mutation ('with' expressions).
/// </summary>
public abstract record EmailEntity(
    string Sender = "",
    string Recipient = ""
) : IEmailEntity;

/// <summary>
/// Concrete email model representing a Welcome Email.
/// </summary>
public record WelcomeEmail(
    string Sender = "",
    string Recipient = "",
    string ActivationCode = ""
) : EmailEntity(Sender, Recipient);


// ============================================================================
// SECTION 2: FLUENT FUNCTIONAL EXTENSIONS
// ============================================================================

/// <summary>
/// Provides pure extension methods using non-destructive mutation ('with' expressions).
/// None of these methods modify the original object; they return brand-new copies.
/// </summary>
public static class EmailEntityExtensions
{
    public static T WithSender<T>(this T entity, string sender) where T : EmailEntity
    {
        return entity with { Sender = sender };
    }

    public static T WithRecipient<T>(this T entity, string recipient) where T : EmailEntity
    {
        return entity with { Recipient = recipient };
    }
}


// ============================================================================
// SECTION 3: THE RESULT MONAD (Map, Bind, Match)
// ============================================================================

/// <summary>
/// Represents a domain error instead of relying on unhandled exceptions.
/// </summary>
public record EmailError(string Code, string Message);

/// <summary>
/// Success payload returned after sending an email.
/// </summary>
public record EmailReceipt(string MessageId, DateTime SentAt);

/// <summary>
/// MONAD CONTAINER: Holds EITHER a Success value (T) OR a Failure error (E).
/// Guarantees compile-time safety and forces callers to handle both branches.
/// </summary>
public abstract record Result<T, E>
{
    private Result() { } // Prevents external inheritance

    public record Success(T Value) : Result<T, E>;
    public record Failure(E Error) : Result<T, E>;

    // Factory helper methods
    public static Result<T, E> Ok(T value) => new Success(value);
    public static Result<T, E> Fail(E error) => new Failure(error);

    /// <summary>
    /// MAP (Equivalent to LINQ Select):
    /// Transforms the inner Success value using 'func'. If Failure, does nothing.
    /// </summary>
    public Result<U, E> Map<U>(Func<T, U> func) => this switch
    {
        Success s => Result<U, E>.Ok(func(s.Value)),
        Failure f => Result<U, E>.Fail(f.Error)
    };

    /// <summary>
    /// BIND / FLATMAP (Equivalent to LINQ SelectMany):
    /// Chains another operation that ALSO returns a Result<U, E>.
    /// Flattens nested Result<Result<U, E>, E> down to Result<U, E>.
    /// </summary>
    public Result<U, E> Bind<U>(Func<T, Result<U, E>> func) => this switch
    {
        Success s => func(s.Value),
        Failure f => Result<U, E>.Fail(f.Error)
    };

    /// <summary>
    /// MATCH / FOLD (Equivalent to C# switch / pattern matching):
    /// Safely unwraps the monad container into a raw value by handling BOTH outcomes.
    /// </summary>
    public R Match<R>(Func<T, R> onSuccess, Func<E, R> onFailure) => this switch
    {
        Success s => onSuccess(s.Value),
        Failure f => onFailure(f.Error)
    };
}


// ============================================================================
// SECTION 4: SERVICE CONTRACT & IMPLEMENTATION
// ============================================================================

public interface IEmailService
{
    Task<Result<EmailReceipt, EmailError>> SendAsync<T>(T email) where T : EmailEntity;
}

public class EmailService : IEmailService
{
    public async Task<Result<EmailReceipt, EmailError>> SendAsync<T>(T email) where T : EmailEntity
    {
        // Validation check 1: Missing recipient
        if (string.IsNullOrWhiteSpace(email.Recipient))
        {
            return Result<EmailReceipt, EmailError>.Fail(
                new EmailError("INVALID_RECIPIENT", "Recipient address cannot be empty.")
            );
        }

        // Validation check 2: Simulated blocked domain
        if (email.Recipient.EndsWith("@blocked.test"))
        {
            return Result<EmailReceipt, EmailError>.Fail(
                new EmailError("DOMAIN_BLOCKED", "Emails to this domain are rejected by policy.")
            );
        }

        // Simulate async operation (e.g., SMTP call)
        await Task.Delay(100);

        var receipt = new EmailReceipt(Guid.NewGuid().ToString("N"), DateTime.UtcNow);
        return Result<EmailReceipt, EmailError>.Ok(receipt);
    }
}


// ============================================================================
// SECTION 5: DEMONSTRATION & RUNTIME EXAMPLES
// ============================================================================

public class Program
{
    public static async Task Main()
    {
        IEmailService emailService = new EmailService();

        Console.WriteLine("==================================================");
        Console.WriteLine(" C# FUNCTIONAL & MONAD STUDY GUIDE ");
        Console.WriteLine("==================================================\n");

        // --- SCENARIO 1: Successful Send with Fluent Chaining ---
        Console.WriteLine("---> Scenario 1: Valid Email Flow");
        
        var validEmail = new WelcomeEmail(ActivationCode: "ACT-12345")
            .WithSender("support@company.com")
            .WithRecipient("john.doe@example.com");

        Result<EmailReceipt, EmailError> result1 = await emailService.SendAsync(validEmail);

        // MAP EXAMPLE: Transform EmailReceipt -> Formatted string (if success)
        Result<string, EmailError> mappedResult = result1.Map(r => $"Receipt ID: {r.MessageId} sent at {r.SentAt:HH:mm:ss}");

        // MATCH EXAMPLE: Safely unpack the container
        string output1 = mappedResult.Match(
            onSuccess: text => $"[SUCCESS] {text}",
            onFailure: err => $"[FAILURE] {err.Code}: {err.Message}"
        );
        Console.WriteLine(output1 + "\n");


        // --- SCENARIO 2: Handled Failure (Missing Recipient) ---
        Console.WriteLine("---> Scenario 2: Missing Recipient Error");

        var invalidEmail = new WelcomeEmail()
            .WithSender("support@company.com"); // Forgotten recipient!

        Result<EmailReceipt, EmailError> result2 = await emailService.SendAsync(invalidEmail);

        string output2 = result2.Match(
            onSuccess: receipt => $"[SUCCESS] Sent with ID {receipt.MessageId}",
            onFailure: err => $"[FAILURE] [{err.Code}] {err.Message}"
        );
        Console.WriteLine(output2 + "\n");


        // --- SCENARIO 3: Handled Failure (Blocked Domain) ---
        Console.WriteLine("---> Scenario 3: Blocked Domain Policy Error");

        var blockedEmail = new WelcomeEmail()
            .WithSender("support@company.com")
            .WithRecipient("hacker@blocked.test");

        Result<EmailReceipt, EmailError> result3 = await emailService.SendAsync(blockedEmail);

        string output3 = result3.Match(
            onSuccess: receipt => $"[SUCCESS] Sent with ID {receipt.MessageId}",
            onFailure: err => $"[FAILURE] [{err.Code}] {err.Message}"
        );
        Console.WriteLine(output3 + "\n");


        // --- SCENARIO 4: Monadic Chaining with BIND ---
        Console.WriteLine("---> Scenario 4: Chaining Operations using BIND");

        // Bind chains step 1 (Send) into step 2 (Audit Log), skipping step 2 if step 1 failed.
        Result<string, EmailError> boundResult = (await emailService.SendAsync(validEmail))
            .Bind(receipt => LogToAuditSystem(receipt));

        string output4 = boundResult.Match(
            onSuccess: confirmation => $"[CHAIN COMPLETED] {confirmation}",
            onFailure: err => $"[CHAIN BROKEN] [{err.Code}] {err.Message}"
        );
        Console.WriteLine(output4);
    }

    // Helper method for Scenario 4 demonstrating a second step returning a Result
    private static Result<string, EmailError> LogToAuditSystem(EmailReceipt receipt)
    {
        // Returns a new Result monad
        return Result<string, EmailError>.Ok($"Audit record written for ID {receipt.MessageId}");
    }
}
 