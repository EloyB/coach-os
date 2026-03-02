# Projectanalyse: Tennis/Padel Planning Tool

## Studio Swyft - Eloy

**Versie:** 2.0 (.NET Backend Architectuur)  
**Datum:** 12 februari 2026  
**Status:** Concept analyse voor MVP ontwikkeling

---

## 1. Executive Summary

### 1.1 Projectoverzicht

Een web-gebaseerde SaaS applicatie voor tennis- en padelscholen en individuele trainers om hun lessen, lessenreeksen en betalingen efficiënt te beheren. De tool richt zich specifiek op de Belgische en Nederlandse markt met focus op gebruiksgemak en betaalbaarheid voor kleine tot middelgrote organisaties.

### 1.2 Kernwaardepropositie

- **Specifiek voor tennis/padel** - niet generiek sportbeheer
- **Gebruiksvriendelijk** - minder complex dan bestaande oplossingen
- **Betaalbaar** - toegankelijk voor kleine trainers en scholen
- **Lokaal** - Nederlandse taal, Belgische/Nederlandse wetgeving en betalingsmethoden

### 1.3 Primaire doelgroep

- Kleine tennisscholen (2-5 trainers)
- Grotere tennisclubs met lesaanbod
- Focus op organisaties waar meerdere trainers banen/resources moeten delen

---

## 2. Probleemstelling & Validatie

### 2.1 Huidige situatie

Trainers en scholen worstelen met:

- **Excel/papier gebaseerde planning** - foutgevoelig en tijdrovend
- **Complexe communicatie** - moeilijk leerlingen op de hoogte houden van wijzigingen
- **Administratieve last** - te veel tijd kwijt aan facturatie en planning i.p.v. lesgeven

### 2.2 Bestaande markt

- **Weinig concurrentie** in de specifieke niche voor België/Nederland
- Bestaande algemene sporttools zijn te complex of te duur
- Kennisvalidatie: directe toegang tot 1-2 scholen als pilotklanten

### 2.3 Pijnpunten prioritering

1. **Kritisch:** Planning chaos en dubbele boekingen
2. **Hoog:** Tijdrovende administratie rondom inschrijvingen
3. **Hoog:** Betalingen bijhouden en factureren
4. **Medium:** Communicatie met leerlingen over wijzigingen

---

## 3. MVP Scope & Functionaliteiten

### 3.1 MVP Definitie (versie 1.0)

**Doel:** Werkend product binnen 3-6 maanden voor pilotklanten  
**Platform:** Web applicatie (responsive voor desktop en mobile browsers)  
**Taal:** Nederlands

### 3.2 Must-Have Functionaliteiten

#### 3.2.1 Planning & Kalender

- Lessen inplannen (los en als reeks)
- Overzicht per trainer, per baan, per week
- Basis kalenderweergave
- **Niet in MVP:** Automatische conflict detectie bij baanbezetting

#### 3.2.2 Inschrijvingen

- Publieke inschrijvingspagina per school
- Leerlingen kunnen zich inschrijven op beschikbare lessenreeksen
- Leerlingen kunnen zich inschrijven op losse lessen
- **Betalingsopties bij inschrijving:**
  - Direct online betalen via Mollie
  - Later betalen (uitgestelde betaling)
- Bevestigingsemails bij inschrijving

#### 3.2.3 Betalingen & Facturatie

- Integratie met Mollie of Stripe voor online betalingen
- **Payment status tracking:**
  - Pending (wacht op betaling)
  - Paid (betaald)
  - Overdue (te laat betaald)
  - Cancelled (geannuleerd)
- Automatische factuur generatie
- **Betalingsherinneringen:**
  - Automatische reminder bij uitgestelde betaling (bijv. 3 dagen voor deadline)
  - Herinnering voor openstaande betalingen
- **Enrollment status op basis van betaling:**
  - Confirmed (betaald, kan deelnemen)
  - Pending Payment (ingeschreven maar moet nog betalen)
  - Cancelled (niet betaald binnen deadline)

#### 3.2.4 Communicatie

- Email notificaties bij:
  - Nieuwe inschrijving
  - Betalingsbevestiging
  - Leswijziging/annulering

**Niet in MVP:** SMS notificaties, push notifications

### 3.3 Gebruikersrollen & Interfaces

#### 3.3.1 Admin Panel (Schoolbeheer)

**Gebruiker:** Eigenaar/manager van tennisschool  
**Functionaliteiten:**

- Trainers aanmaken en beheren
- Banen/locaties configureren
- Organisatie-instellingen
- Rapportages (inkomsten, bezetting)
- Toegang tot alle trainer dashboards

#### 3.3.2 Trainer Dashboard

**Gebruiker:** Individuele trainers  
**Functionaliteiten:**

- Eigen lessen inplannen
- Lessenreeksen aanmaken
- Leerlingen bekijken die zijn ingeschreven
- Baanbeschikbaarheid zien
- Eigen planning beheren

#### 3.3.3 Leerling Portal

**Gebruiker:** Leerlingen/ouders  
**Functionaliteiten:**

- Inschrijven op beschikbare lessen/reeksen
- Overzicht van eigen inschrijvingen
- Betalingsstatus bekijken
- Afmelden voor lessen (binnen regels)

#### 3.3.4 Publieke Pagina

**Gebruiker:** Potentiële nieuwe leerlingen  
**Functionaliteiten:**

- Overzicht beschikbare lessenreeksen
- Informatie over trainers
- Direct inschrijvingsformulier
- Prijsinformatie

---

## 4. Technische Architectuur

### 4.1 Voorgestelde Tech Stack

Moderne, schaalbare architectuur met gescheiden backend en frontend:

#### Backend (.NET API)

- **Framework:** ASP.NET Core Web API (.NET 8 LTS)
- **Architectuur:** Clean Architecture met CQRS patterns
- **Database:** PostgreSQL (Scaleway Managed Database)
- **ORM:** Entity Framework Core 8
- **Authenticatie:** JWT tokens (stateless, schaalbaar)
- **API Docs:** Swagger/OpenAPI

**Key NuGet Packages:**

- **MediatR** - CQRS command/query handling
- **FluentValidation** - Input validatie
- **AutoMapper** - DTO mapping
- **Serilog** - Structured logging
- **EF Core** - Database access
- **Npgsql.EntityFrameworkCore.PostgreSQL** - PostgreSQL provider
- **Microsoft.Extensions.Localization** - i18n support (future-ready)

#### Frontend

- **Framework:** Next.js 15 (App Router)
- **UI Library:** Shadcn/ui components
- **Styling:** Tailwind CSS
- **State Management:** React Context / Zustand (indien nodig)
- **API Client:** Fetch API / Axios (consumeert .NET API direct)
- **TypeScript:** Strongly typed met API contracts
- **i18n:** next-intl (multi-language ready)

#### Betalingen

- **Provider:** Mollie (voor NL/BE markt)
- **Webhooks:** .NET API endpoints voor payment status updates
- **Library:** Mollie.Api NuGet package

#### Notificaties

- **Email:** Scaleway Transactional Email (TEM)
  - SMTP relay (smtp.tem.scw.cloud) + REST API
  - Free tier: 300 emails/maand, daarna €0.25 per 1000 emails
  - EU-based, GDPR-compliant
  - SPF, DKIM, DMARC authenticatie
  - Analytics via Scaleway Cockpit
- **Templates:** Razor templates in .NET voor HTML generation
- **Queue:** Hangfire voor background jobs (emails, reminders)
- **Library:** System.Net.Mail voor SMTP (native .NET)

#### Hosting & Infrastructure

- **Platform:** Scaleway
- **Backend Container:** Docker container (.NET 8 runtime)
- **Frontend Container:** Docker container (Next.js standalone)
- **Database:** Scaleway Managed PostgreSQL
- **Storage:** Scaleway Object Storage voor facturen/uploads
- **CI/CD:** GitHub Actions (separate workflows per app)
- **Reverse Proxy:** Nginx of Traefik voor routing

### 4.2 Database Schema (EF Core Entities)

**Multi-tenancy strategie:** Shared database met OrganizationId filtering via global query filters

````csharp
// Core Entities (Domain Layer)

1. Organization
   - Id (Guid, PK)
   - Name (string)
   - Slug (string, unique)
   - LogoUrl (string)
   - Settings (JSON: timezone, currency, terms)
   - CreatedAt, UpdatedAt
   - Navigation: Users, Courts, Lessons, LessonSeries, Subscription

1B. Subscription
   - Id (Guid, PK)
   - OrganizationId (Guid, FK) - unique
   - Plan (enum: Starter, Professional, Business, Enterprise)
   - Status (enum: Trial, Active, PastDue, Cancelled, Suspended)
   - TrialEndsAt (DateTime?)
   - CurrentPeriodStart (DateTime)
   - CurrentPeriodEnd (DateTime)
   - CancelledAt (DateTime?)
   - CreatedAt, UpdatedAt
   - Navigation: Organization

**Subscription Plans (hardcoded in code):**
```csharp
public enum SubscriptionPlan
{
    Starter = 1,      // €29/maand - 1 trainer
    Professional = 2, // €79/maand - 2-5 trainers
    Business = 3,     // €149/maand - 6-15 trainers
    Enterprise = 4    // €299/maand - 16+ trainers
}

public static class PlanLimits
{
    public static int MaxTrainers(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Starter => 1,
        SubscriptionPlan.Professional => 5,
        SubscriptionPlan.Business => 15,
        SubscriptionPlan.Enterprise => int.MaxValue,
        _ => 0
    };
}
````

2. User (ApplicationUser : IdentityUser)
   - Id (string, PK)
   - OrganizationId (Guid, FK)
   - Role (enum: Admin, Trainer, Student)
   - FirstName, LastName
   - PhoneNumber
   - ProfileImageUrl (string)
   - IsActive (bool) - voor trainer deactivation bij downgrade
   - LastLoginAt (DateTime?) - voor determining minst actieve trainers
   - CreatedAt, UpdatedAt
   - Navigation: Organization, LessonsAsTrainer, Enrollments

3. Court (Banen)
   - Id (Guid, PK)
   - OrganizationId (Guid, FK)
   - Name (string)
   - Location (string)
   - IsActive (bool)
   - AvailabilitySchedule (JSON)
   - CreatedAt, UpdatedAt
   - Navigation: Organization, Lessons

4. Lesson
   - Id (Guid, PK)
   - OrganizationId (Guid, FK)
   - Title, Description
   - StartDateTime (DateTime)
   - DurationMinutes (int)
   - CourtId (Guid, FK)
   - TrainerId (string, FK to User)
   - MaxParticipants (int)
   - Price (decimal)
   - Status (enum: Scheduled, Completed, Cancelled)
   - LessonSeriesId (Guid?, FK - nullable)
   - CreatedAt, UpdatedAt
   - Navigation: Organization, Court, Trainer, LessonSeries, Enrollments

5. LessonSeries
   - Id (Guid, PK)
   - OrganizationId (Guid, FK)
   - Title, Description
   - TotalPrice (decimal)
   - StartDate, EndDate (DateTime)
   - IsPublished (bool)
   - MaxParticipants (int)
   - CreatedAt, UpdatedAt
   - Navigation: Organization, Lessons, Enrollments

6. Enrollment
   - Id (Guid, PK)
   - OrganizationId (Guid, FK)
   - StudentId (string, FK to User)
   - LessonId (Guid?, FK - nullable)
   - LessonSeriesId (Guid?, FK - nullable)
   - Status (enum: PendingPayment, Confirmed, Cancelled, Completed)
   - PaymentDeadline (DateTime? - voor uitgestelde betalingen)
   - EnrolledAt (DateTime)
   - CancelledAt (DateTime?)
   - CreatedAt, UpdatedAt
   - Navigation: Organization, Student, Lesson, LessonSeries, Payment

7. Payment
   - Id (Guid, PK)
   - OrganizationId (Guid, FK)
   - EnrollmentId (Guid, FK)
   - Amount (decimal)
   - Currency (string, default "EUR")
   - Status (enum: Pending, Paid, Overdue, Failed, Refunded, Cancelled)
   - PaymentType (enum: Immediate, Deferred)
   - PaymentProviderId (string? - Mollie payment ID, null for deferred)
   - PaymentMethod (string? - ideal, creditcard, banktransfer, cash, etc)
   - DueDate (DateTime? - voor uitgestelde betalingen)
   - PaidAt (DateTime?)
   - InvoicePdfUrl (string)
   - CreatedAt, UpdatedAt
   - Navigation: Organization, Enrollment

8. NotificationLog
   - Id (Guid, PK)
   - OrganizationId (Guid, FK)
   - Type (enum: EnrollmentConfirmation, PaymentReceived, LessonReminder, etc)
   - RecipientEmail (string)
   - Subject, Body (string)
   - Status (enum: Pending, Sent, Failed)
   - SentAt (DateTime?)
   - Error (string?)
   - CreatedAt
   - Navigation: Organization

```

**EF Core Configuratie Highlights:**
- Global query filter voor OrganizationId (multi-tenancy isolation)
- Soft delete pattern voor belangrijke entities (IsDeleted vlag)
- Audit fields (CreatedAt, UpdatedAt, CreatedBy) via interceptors
- Value objects voor Money (Amount + Currency)
- Indexes op frequent queried fields (OrganizationId, Status, Dates)

### 4.3 Clean Architecture Structuur

**.NET Solution structuur:**

```

TennisPlanningTool.sln
│
├── src/
│ ├── Core/
│ │ ├── TennisPlanningTool.Domain/ # Entities, Enums, Interfaces
│ │ │ ├── Entities/
│ │ │ │ ├── Organization.cs
│ │ │ │ ├── User.cs
│ │ │ │ ├── Lesson.cs
│ │ │ │ └── ...
│ │ │ ├── Enums/
│ │ │ │ ├── UserRole.cs
│ │ │ │ ├── PaymentStatus.cs
│ │ │ │ └── ...
│ │ │ ├── Common/
│ │ │ │ ├── BaseEntity.cs
│ │ │ │ └── IAuditableEntity.cs
│ │ │ └── Exceptions/
│ │ │ └── DomainException.cs
│ │ │
│ │ └── TennisPlanningTool.Application/ # Business Logic, CQRS
│ │ ├── Common/
│ │ │ ├── Interfaces/
│ │ │ │ ├── IApplicationDbContext.cs
│ │ │ │ ├── IDateTime.cs
│ │ │ │ └── ICurrentUserService.cs
│ │ │ ├── Mappings/
│ │ │ │ └── MappingProfile.cs # AutoMapper
│ │ │ └── Models/
│ │ │ ├── Result.cs # Result pattern
│ │ │ └── PaginatedList.cs
│ │ ├── Lessons/
│ │ │ ├── Commands/
│ │ │ │ ├── CreateLesson/
│ │ │ │ │ ├── CreateLessonCommand.cs
│ │ │ │ │ ├── CreateLessonCommandHandler.cs
│ │ │ │ │ └── CreateLessonCommandValidator.cs
│ │ │ │ └── UpdateLesson/
│ │ │ └── Queries/
│ │ │ ├── GetLessons/
│ │ │ │ ├── GetLessonsQuery.cs
│ │ │ │ └── GetLessonsQueryHandler.cs
│ │ │ └── GetLessonById/
│ │ ├── Enrollments/
│ │ │ ├── Commands/
│ │ │ └── Queries/
│ │ ├── Payments/
│ │ │ ├── Commands/
│ │ │ │ └── ProcessPaymentWebhook/
│ │ │ └── Queries/
│ │ └── DTOs/
│ │ ├── LessonDto.cs
│ │ ├── EnrollmentDto.cs
│ │ └── ...
│ │
│ ├── Infrastructure/
│ │ └── TennisPlanningTool.Infrastructure/ # External concerns
│ │ ├── Persistence/
│ │ │ ├── ApplicationDbContext.cs
│ │ │ ├── Configurations/ # EF Core configs
│ │ │ │ ├── OrganizationConfiguration.cs
│ │ │ │ ├── UserConfiguration.cs
│ │ │ │ └── ...
│ │ │ └── Migrations/
│ │ ├── Identity/
│ │ │ ├── IdentityService.cs
│ │ │ └── JwtTokenGenerator.cs
│ │ ├── Services/
│ │ │ ├── DateTimeService.cs
│ │ │ ├── EmailService.cs
│ │ │ └── MolliePaymentService.cs
│ │ └── DependencyInjection.cs
│ │
│ └── Presentation/
│ └── TennisPlanningTool.API/ # Web API
│ ├── Controllers/
│ │ ├── LessonsController.cs
│ │ ├── EnrollmentsController.cs
│ │ ├── PaymentsController.cs
│ │ └── ...
│ ├── Filters/
│ │ ├── ApiExceptionFilterAttribute.cs
│ │ └── ValidateModelStateAttribute.cs
│ ├── Middleware/
│ │ ├── ExceptionHandlingMiddleware.cs
│ │ └── CurrentOrganizationMiddleware.cs
│ ├── Program.cs
│ ├── appsettings.json
│ └── Dockerfile
│
└── tests/
├── TennisPlanningTool.Application.Tests/
├── TennisPlanningTool.Infrastructure.Tests/
└── TennisPlanningTool.API.Tests/

````

**CQRS Flow Voorbeeld (CreateLesson):**

```csharp
// 1. Command (Application Layer)
public class CreateLessonCommand : IRequest<Result<Guid>>
{
    public string Title { get; set; }
    public DateTime StartDateTime { get; set; }
    public int DurationMinutes { get; set; }
    public Guid CourtId { get; set; }
    public string TrainerId { get; set; }
    // ... more properties
}

// 2. Validator (FluentValidation)
public class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
{
    public CreateLessonCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DurationMinutes).GreaterThan(0);
        // ... more rules
    }
}

// 3. Handler (MediatR)
public class CreateLessonCommandHandler : IRequestHandler<CreateLessonCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public async Task<Result<Guid>> Handle(CreateLessonCommand request, CancellationToken ct)
    {
        var lesson = new Lesson
        {
            Title = request.Title,
            StartDateTime = request.StartDateTime,
            // ... map properties
            OrganizationId = _currentUser.OrganizationId
        };

        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(lesson.Id);
    }
}

// 4. Controller (API Layer)
[ApiController]
[Route("api/[controller]")]
public class LessonsController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateLessonCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }
}
````

### 4.4 Externe Integraties

#### Mollie Payment Integration

**Implementation in .NET:**

```csharp
// Infrastructure/Services/MolliePaymentService.cs
public class MolliePaymentService : IPaymentService
{
    private readonly PaymentClient _mollieClient;

    public async Task<PaymentResponse> CreatePaymentAsync(CreatePaymentRequest request)
    {
        var payment = await _mollieClient.CreatePaymentAsync(new PaymentRequest
        {
            Amount = new Amount(Currency.EUR, request.Amount),
            Description = request.Description,
            RedirectUrl = request.RedirectUrl,
            WebhookUrl = $"{_baseUrl}/api/webhooks/mollie",
            Metadata = new { EnrollmentId = request.EnrollmentId }
        });

        return new PaymentResponse
        {
            PaymentId = payment.Id,
            CheckoutUrl = payment.Links.Checkout.Href
        };
    }
}

// API/Controllers/WebhooksController.cs
[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    [HttpPost("mollie")]
    public async Task<IActionResult> MollieWebhook([FromForm] string id)
    {
        var command = new ProcessPaymentWebhookCommand { PaymentId = id };
        await _mediator.Send(command);
        return Ok();
    }
}
```

#### Email Service

**Scaleway Transactional Email (TEM) implementation:**

```csharp
// Infrastructure/Services/ScalewayEmailService.cs
public class ScalewayEmailService : IEmailService
{
    private readonly SmtpClient _smtpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<ScalewayEmailService> _logger;

    public ScalewayEmailService(IConfiguration config, ILogger<ScalewayEmailService> logger)
    {
        _config = config;
        _logger = logger;

        _smtpClient = new SmtpClient
        {
            Host = "smtp.tem.scw.cloud",
            Port = 587,
            EnableSsl = true,
            Credentials = new NetworkCredential(
                _config["Scaleway:Email:Username"],  // Your domain email
                _config["Scaleway:Email:Password"]   // SMTP password from console
            )
        };
    }

    public async Task SendEnrollmentConfirmationAsync(string to, EnrollmentDto enrollment)
    {
        var htmlBody = EmailTemplates.EnrollmentConfirmation(enrollment);

        var message = new MailMessage
        {
            From = new MailAddress("noreply@tennisplanner.be", "Tennis Planner"),
            Subject = "Inschrijving bevestiging",
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(to);

        try
        {
            await _smtpClient.SendMailAsync(message);
            _logger.LogInformation("Enrollment confirmation sent to {Email}", to);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", to);
            throw;
        }
    }
}

// Email Templates (HTML generation)
public static class EmailTemplates
{
    public static string EnrollmentConfirmation(EnrollmentDto enrollment)
    {
        return $@"
<!DOCTYPE html>
<html lang='nl'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #1A4D2E; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .button {{
            display: inline-block;
            padding: 12px 24px;
            background: #D4794D;
            color: white;
            text-decoration: none;
            border-radius: 4px;
            margin: 20px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Inschrijving Bevestigd! 🎾</h1>
        </div>
        <div class='content'>
            <p>Beste {enrollment.StudentName},</p>

            <p>Je inschrijving voor <strong>{enrollment.LessonSeriesTitle}</strong> is bevestigd!</p>

            <h3>Details:</h3>
            <ul>
                <li><strong>Start:</strong> {enrollment.StartDate:dd MMMM yyyy}</li>
                <li><strong>Locatie:</strong> {enrollment.CourtName}</li>
                <li><strong>Trainer:</strong> {enrollment.TrainerName}</li>
                <li><strong>Prijs:</strong> €{enrollment.Price:F2}</li>
            </ul>

            <p>We kijken ernaar uit om je te zien op de baan!</p>

            <p>Met sportieve groet,<br>
            Het Tennis Planner Team</p>
        </div>
    </div>
</body>
</html>";
    }

    public static string PaymentConfirmation(PaymentDto payment) {{ /* ... */ }}
    public static string LessonReminder(LessonDto lesson) {{ /* ... */ }}
}
```

**Configuration (appsettings.json):**

```json
{
  "Scaleway": {
    "Email": {
      "Username": "your-email@tennisplanner.be",
      "Password": "scw_smtp_password_from_console",
      "FromEmail": "noreply@tennisplanner.be",
      "FromName": "Tennis Planner"
    }
  }
}
```

**Background Jobs met Hangfire:**

```csharp
// Queue emails en scheduled reminders
BackgroundJob.Enqueue<IEmailService>(x =>
    x.SendEnrollmentConfirmationAsync(email, enrollment));

RecurringJob.AddOrUpdate<IEmailService>(
    "lesson-reminders",
    x => x.SendLessonRemindersAsync(),
    Cron.Daily(8)); // Elke dag om 08:00
```

#### Subscription Management

**Plan Change met Downgrade Validation:**

```csharp
// Application/Subscriptions/Commands/ChangePlan/ChangePlanCommand.cs
public class ChangePlanCommand : IRequest<Result>
{
    public Guid OrganizationId { get; set; }
    public SubscriptionPlan TargetPlan { get; set; }
}

public class ChangePlanCommandValidator : AbstractValidator<ChangePlanCommand>
{
    private readonly IApplicationDbContext _context;

    public ChangePlanCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x)
            .MustAsync(BeValidPlanChange)
            .WithMessage(x => GetPlanChangeError(x));
    }

    private async Task<bool> BeValidPlanChange(
        ChangePlanCommand command,
        CancellationToken ct)
    {
        var org = await _context.Organizations
            .Include(o => o.Subscription)
            .FirstAsync(o => o.Id == command.OrganizationId, ct);

        // Upgrade: altijd toegestaan
        if (command.TargetPlan > org.Subscription.Plan)
            return true;

        // Downgrade: check limits
        var activeTrainerCount = await _context.Users
            .CountAsync(u =>
                u.OrganizationId == org.Id &&
                u.Role == UserRole.Trainer &&
                u.IsActive,
                ct);

        var targetLimit = PlanLimits.MaxTrainers(command.TargetPlan);

        return activeTrainerCount <= targetLimit;
    }

    private string GetPlanChangeError(ChangePlanCommand command)
    {
        var org = _context.Organizations.Find(command.OrganizationId);
        var activeCount = _context.Users.Count(u =>
            u.OrganizationId == org.Id &&
            u.Role == UserRole.Trainer &&
            u.IsActive);

        var targetLimit = PlanLimits.MaxTrainers(command.TargetPlan);

        return $"Downgrade naar {command.TargetPlan} niet mogelijk. " +
               $"Je hebt {activeCount} actieve trainers, maar deze tier heeft maximaal {targetLimit} trainers. " +
               $"Deactiveer eerst {activeCount - targetLimit} trainers.";
    }
}

// Handler
public class ChangePlanCommandHandler : IRequestHandler<ChangePlanCommand, Result>
{
    public async Task<Result> Handle(ChangePlanCommand request, CancellationToken ct)
    {
        var org = await _context.Organizations
            .Include(o => o.Subscription)
            .FirstAsync(o => o.Id == request.OrganizationId, ct);

        var oldPlan = org.Subscription.Plan;
        org.Subscription.Plan = request.TargetPlan;
        org.Subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        // Log plan change
        _logger.LogInformation(
            "Organization {OrgId} changed plan from {OldPlan} to {NewPlan}",
            org.Id, oldPlan, request.TargetPlan);

        // Send confirmation email
        await _emailService.SendPlanChangeConfirmationAsync(org);

        return Result.Success();
    }
}

// API Controller
[ApiController]
[Route("api/organizations/{organizationId}/subscription")]
public class SubscriptionController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost("change-plan")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ChangePlan(
        Guid organizationId,
        [FromBody] ChangePlanRequest request)
    {
        var command = new ChangePlanCommand
        {
            OrganizationId = organizationId,
            TargetPlan = request.TargetPlan
        };

        var result = await _mediator.Send(command);

        return result.Succeeded
            ? Ok(new { message = "Plan gewijzigd" })
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("check-downgrade")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DowngradeCheckResult>> CheckDowngrade(
        Guid organizationId,
        [FromBody] CheckDowngradeRequest request)
    {
        var query = new CheckDowngradeEligibilityQuery
        {
            OrganizationId = organizationId,
            TargetPlan = request.TargetPlan
        };

        var result = await _mediator.Send(query);

        return Ok(result);
    }
}
```

**Frontend Implementation:**

```typescript
// frontend/src/lib/api/subscriptions.ts
export async function changePlan(
  organizationId: string,
  targetPlan: SubscriptionPlan
): Promise<{ success: boolean; error?: string }> {
  try {
    await fetch(`/api/organizations/${organizationId}/subscription/change-plan`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ targetPlan })
    });
    return { success: true };
  } catch (error) {
    return {
      success: false,
      error: error.message
    };
  }
}

export interface DowngradeCheckResult {
  eligible: boolean;
  currentTrainers: number;
  targetLimit: number;
  excessTrainers: number;
  trainersToDeactivate?: Array<{ id: string; name: string; lastLogin: Date }>;
}

// frontend/src/components/subscription/PlanChangeDialog.tsx
export function PlanChangeDialog({ currentPlan, targetPlan, onConfirm }) {
  const [checking, setChecking] = useState(true);
  const [eligibility, setEligibility] = useState<DowngradeCheckResult | null>(null);

  useEffect(() => {
    if (isDowngrade(currentPlan, targetPlan)) {
      checkDowngradeEligibility(orgId, targetPlan)
        .then(setEligibility)
        .finally(() => setChecking(false));
    } else {
      setChecking(false);
    }
  }, [targetPlan]);

  if (checking) return <Spinner />;

  if (eligibility && !eligibility.eligible) {
    return (
      <Alert variant="destructive">
        <AlertTriangle className="h-4 w-4" />
        <AlertTitle>Downgrade niet mogelijk</AlertTitle>
        <AlertDescription>
          Je hebt {eligibility.currentTrainers} actieve trainers, maar het{' '}
          {targetPlan} plan heeft maximaal {eligibility.targetLimit} trainers.

          <div className="mt-4">
            <p className="font-semibold">Deactiveer eerst {eligibility.excessTrainers} trainers:</p>
            <Button
              onClick={() => navigate('/settings/trainers?prepare-downgrade=true')}
              className="mt-2"
            >
              Beheer Trainers
            </Button>
          </div>
        </AlertDescription>
      </Alert>
    );
  }

  return (
    <Dialog>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            {isDowngrade ? 'Downgrade naar' : 'Upgrade naar'} {targetPlan}?
          </DialogTitle>
        </DialogHeader>
        <DialogDescription>
          {isDowngrade
            ? `Je nieuwe prijs wordt €${PLAN_PRICES[targetPlan]}/maand vanaf de volgende factuurperiode.`
            : `Je krijgt toegang tot meer capaciteit. Nieuwe prijs: €${PLAN_PRICES[targetPlan]}/maand.`
          }
        </DialogDescription>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel}>Annuleren</Button>
          <Button onClick={onConfirm}>Bevestigen</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
```

---

## 4.5 Internationalization (i18n) Strategy

### **Strategie: i18n-Ready Architecture (Minimal Overhead)**

**Target Markets:**

- 🇧🇪 België: Nederlands (Vlaanderen) + Frans (Wallonië)
- 🇳🇱 Nederland: Nederlands

**MVP Approach:** Start Nederlands-only maar **voorbereid** voor meertaligheid.

**Waarom nu voorbereiden:**

- ✅ Later toevoegen talen = 2-3 dagen i.p.v. 2-3 weken refactoring
- ✅ Cleaner code (strings gescheiden van logica)
- ✅ Professional best practice
- ✅ België is tweetalig → Frans komt waarschijnlijk snel

**Investering:** ~5 uur setup in MVP bespaart 2-3 weken later (20x ROI)

---

### **Backend Implementation (.NET)**

#### **Setup (Week 1-2)**

**1. Package installeren:**

```bash
dotnet add package Microsoft.Extensions.Localization
```

**2. Configuration in Program.cs:**

```csharp
// Program.cs
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

builder.Services.AddControllers()
    .AddDataAnnotationsLocalization();
```

**3. Resource files structuur:**

```
backend/
├── Resources/
│   ├── SharedResources.nl.resx     # Algemene strings
│   ├── SharedResources.fr.resx     # (Later toevoegen)
│   ├── EmailTemplates.nl.resx      # Email templates
│   └── ValidationMessages.nl.resx  # Validatie errors
```

**4. Usage in Services:**

```csharp
// Infrastructure/Services/EmailService.cs
public class EmailService : IEmailService
{
    private readonly IStringLocalizer<EmailTemplates> _localizer;

    public EmailService(IStringLocalizer<EmailTemplates> localizer)
    {
        _localizer = localizer;
    }

    public async Task SendEnrollmentConfirmationAsync(
        string to,
        EnrollmentDto enrollment,
        string language = "nl")  // Default Nederlands
    {
        // Set culture for this request
        CultureInfo.CurrentCulture = new CultureInfo(language);
        CultureInfo.CurrentUICulture = new CultureInfo(language);

        var subject = _localizer["Enrollment.Confirmation.Subject"];
        var greeting = _localizer["Enrollment.Confirmation.Greeting", enrollment.StudentName];
        var body = _localizer["Enrollment.Confirmation.Body", enrollment.LessonSeriesTitle];

        var htmlBody = $@"
            <h1>{subject}</h1>
            <p>{greeting}</p>
            <p>{body}</p>
        ";

        await SendEmailAsync(to, subject, htmlBody);
    }
}
```

**5. Resource file content:**

```xml
<!-- Resources/EmailTemplates.nl.resx -->
<data name="Enrollment.Confirmation.Subject">
  <value>Inschrijving Bevestigd!</value>
</data>
<data name="Enrollment.Confirmation.Greeting">
  <value>Beste {0},</value>
</data>
<data name="Enrollment.Confirmation.Body">
  <value>Je bent ingeschreven voor {0}. We kijken ernaar uit je te zien!</value>
</data>
```

**6. Validation Messages:**

```csharp
// Application/Lessons/Commands/CreateLesson/CreateLessonCommandValidator.cs
public class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
{
    private readonly IStringLocalizer<ValidationMessages> _localizer;

    public CreateLessonCommandValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage(_localizer["Lesson.Title.Required"])
            .MaximumLength(200)
            .WithMessage(_localizer["Lesson.Title.MaxLength", 200]);

        RuleFor(x => x.MaxParticipants)
            .GreaterThan(0)
            .WithMessage(_localizer["Lesson.MaxParticipants.GreaterThanZero"]);
    }
}
```

---

### **Frontend Implementation (Next.js)**

#### **Setup (Week 1-2)**

**1. Package installeren:**

```bash
npm install next-intl
```

**2. Configuration:**

```typescript
// frontend/src/i18n.ts
import { getRequestConfig } from "next-intl/server";

export default getRequestConfig(async ({ locale }) => {
  // Voor MVP: hardcoded 'nl'
  // Later: locale from URL, cookie, of user preference

  return {
    messages: (await import(`./messages/${locale}.json`)).default,
  };
});

// frontend/next.config.js
const createNextIntlPlugin = require("next-intl/plugin");
const withNextIntl = createNextIntlPlugin("./src/i18n.ts");

module.exports = withNextIntl({
  // ... rest of config
});
```

**3. Translation files structuur:**

```
frontend/
├── src/
│   ├── messages/
│   │   ├── nl.json     # Nederlands (MVP)
│   │   └── fr.json     # Frans (later)
```

**4. Translation file example:**

```json
// src/messages/nl.json
{
  "common": {
    "save": "Opslaan",
    "cancel": "Annuleren",
    "delete": "Verwijderen",
    "edit": "Bewerken",
    "loading": "Laden...",
    "error": "Er is iets misgegaan"
  },
  "auth": {
    "login": "Inloggen",
    "logout": "Uitloggen",
    "email": "E-mailadres",
    "password": "Wachtwoord",
    "forgotPassword": "Wachtwoord vergeten?"
  },
  "enrollment": {
    "title": "Inschrijven voor {lessonTitle}",
    "submit": "Bevestig inschrijving",
    "paymentOptions": "Betalingsopties",
    "payNow": "Nu betalen",
    "payLater": "Later betalen",
    "deadline": "Betaal voor {date}",
    "success": "Je bent succesvol ingeschreven!",
    "error": "Inschrijving mislukt. Probeer opnieuw."
  },
  "dashboard": {
    "welcome": "Welkom terug, {name}",
    "myLessons": "Mijn lessen",
    "upcomingLessons": "Aankomende lessen",
    "noLessons": "Je hebt nog geen lessen"
  },
  "subscription": {
    "currentPlan": "Huidig abonnement",
    "upgrade": "Upgraden",
    "downgrade": "Downgraden",
    "trainers": "{count} van {max} trainers actief",
    "downgradError": "Je hebt {current} trainers, maar het {plan} plan heeft maximaal {max} trainers."
  }
}
```

**5. Usage in Components:**

```typescript
// frontend/src/components/EnrollmentForm.tsx
'use client';

import { useTranslations } from 'next-intl';

export function EnrollmentForm({ lesson }: { lesson: Lesson }) {
  const t = useTranslations('enrollment');
  const tCommon = useTranslations('common');

  return (
    <form>
      <h1>{t('title', { lessonTitle: lesson.title })}</h1>

      <div className="space-y-4">
        <h2>{t('paymentOptions')}</h2>

        <Button type="submit" name="payment" value="immediate">
          {t('payNow')}
        </Button>

        <Button type="submit" name="payment" value="deferred" variant="outline">
          {t('payLater')}
        </Button>
      </div>

      <Button type="button" onClick={onCancel}>
        {tCommon('cancel')}
      </Button>
    </form>
  );
}
```

**6. With formatting:**

```typescript
// frontend/src/components/LessonCard.tsx
import { useTranslations, useFormatter } from 'next-intl';

export function LessonCard({ lesson }: { lesson: Lesson }) {
  const t = useTranslations('dashboard');
  const format = useFormatter();

  return (
    <Card>
      <CardHeader>
        <CardTitle>{lesson.title}</CardTitle>
        <CardDescription>
          {format.dateTime(lesson.startDateTime, {
            dateStyle: 'full',
            timeStyle: 'short'
          })}
        </CardDescription>
      </CardHeader>

      <CardContent>
        <p>{format.number(lesson.price, { style: 'currency', currency: 'EUR' })}</p>
      </CardContent>
    </Card>
  );
}
```

---

### **Database Considerations**

#### **Optie A: Single Language Column (MVP)** ⭐

**Voor MVP voldoende:**

```csharp
public class LessonSeries
{
    public string Title { get; set; }        // "Beginners Tennis"
    public string Description { get; set; }  // "Voor spelers zonder ervaring"
}
```

**Trainers voeren content in Nederlands in. Later migratie mogelijk.**

#### **Optie B: JSON Translations (Future)**

```csharp
public class LessonSeries
{
    public Guid Id { get; set; }

    // JSON: { "nl": "Beginners Tennis", "fr": "Tennis pour débutants" }
    public string TitleTranslations { get; set; }

    // Helper property
    public string GetTitle(string language = "nl")
    {
        var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(TitleTranslations);
        return translations.GetValueOrDefault(language, translations["nl"]);
    }
}
```

#### **Optie C: Separate Translation Table (v2+)**

```csharp
public class LessonSeries
{
    public Guid Id { get; set; }
    public ICollection<LessonSeriesTranslation> Translations { get; set; }
}

public class LessonSeriesTranslation
{
    public Guid LessonSeriesId { get; set; }
    public string Language { get; set; }  // "nl", "fr", "en"
    public string Title { get; set; }
    public string Description { get; set; }
}
```

**Voor MVP:** Optie A. Migratie naar B of C is straightforward later.

---

### **User Language Preference**

**Storage in User entity:**

```csharp
public class User : IdentityUser
{
    public string PreferredLanguage { get; set; } = "nl";  // Default Nederlands
    // ... other properties
}
```

**Usage in API:**

```csharp
// Middleware or filter to set culture based on user preference
public class UserLanguageMiddleware
{
    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser)
    {
        if (currentUser.IsAuthenticated)
        {
            var language = currentUser.PreferredLanguage ?? "nl";
            var culture = new CultureInfo(language);

            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        await _next(context);
    }
}
```

---

### **Migration Path: Adding French (Post-MVP)**

**When:** Eerste Franstalige school in pilot

**Effort:** 2-3 dagen

**Steps:**

**1. Create French translations (1 dag):**

```bash
# Backend
cp Resources/EmailTemplates.nl.resx Resources/EmailTemplates.fr.resx
# Edit and translate all strings

# Frontend
cp src/messages/nl.json src/messages/fr.json
# Edit and translate all strings
```

**2. Add language switcher UI (0.5 dag):**

```typescript
// frontend/src/components/LanguageSwitcher.tsx
export function LanguageSwitcher() {
  const router = useRouter();
  const pathname = usePathname();

  const changeLanguage = (locale: string) => {
    // Update user preference via API
    updateUserLanguage(locale);

    // Reload page with new locale
    router.refresh();
  };

  return (
    <Select defaultValue="nl" onValueChange={changeLanguage}>
      <SelectItem value="nl">🇳🇱 Nederlands</SelectItem>
      <SelectItem value="fr">🇫🇷 Français</SelectItem>
    </Select>
  );
}
```

**3. Update email service to use user language (0.5 dag):**

```csharp
public async Task SendEnrollmentConfirmationAsync(
    string to,
    EnrollmentDto enrollment)
{
    // Get user's preferred language
    var user = await _userManager.FindByEmailAsync(to);
    var language = user?.PreferredLanguage ?? "nl";

    // Rest of implementation uses language parameter
}
```

**4. Test & deploy (0.5 dag)**

---

### **What NOT to Do in MVP**

❌ **Multi-language content management UI** - trainers enter content in one language only  
❌ **Automatic language detection** - use default "nl", add switcher later  
❌ **Translate all content** - only UI strings, not user-generated content  
❌ **Complex fallback logic** - simple default to "nl" is enough

---

### **Roadmap Integration**

**Week 1-2 (MVP Setup):**

- [ ] Install i18n packages (backend + frontend)
- [ ] Create resource file structure
- [ ] Setup next-intl configuration
- [ ] Add PreferredLanguage to User entity
- [ ] Create nl.json with all UI strings
- [ ] Create EmailTemplates.nl.resx

**Week 3-12 (MVP Development):**

- [ ] Use translations in all components (gewenning)
- [ ] No hardcoded strings in code
- [ ] Document all translation keys

**Post-MVP (Month 4-6 if needed):**

- [ ] Create French translations (copy + translate)
- [ ] Add language switcher UI
- [ ] Test with Franstalige pilot school
- [ ] Update email templates for user language

---

### **Cost-Benefit Summary**

| Aspect              | Cost Now             | Benefit Later                  |
| ------------------- | -------------------- | ------------------------------ |
| Setup time          | 5 uur                | Bespaart 2-3 weken refactoring |
| Learning curve      | Minimal              | Best practice pattern          |
| Code complexity     | +5% (resource files) | Cleaner separation of concerns |
| Adding French       | N/A                  | 2-3 dagen i.p.v. 2-3 weken     |
| Adding 3rd language | N/A                  | 1 dag (copy + translate)       |

**ROI:** 5 uur investering → 20x return als tweede taal nodig is

**Recommendation:** Setup i18n architecture in MVP, activate French when first Franstalige school signs up.

---

## 5. User Flows

### 5.1 Happy Flow: Leerling schrijft in op lessenreeks

1. **Ontdekking**
   - Leerling bezoekt publieke pagina school
   - Ziet overzicht van beschikbare lessenreeksen
   - Filtert op niveau/tijdstip

2. **Inschrijving**
   - Klikt op "Inschrijven" bij gewenste reeks
   - Vult inschrijvingsformulier in (naam, email, telefoonnummer)
   - Accepteert voorwaarden

3. **Betaling (keuze)**
   - **Optie A: Direct betalen**
     - Wordt doorgestuurd naar Mollie payment link
     - Betaalt via iDEAL/bancontact/creditcard
     - Ontvangt bevestiging van betaling
     - Enrollment status → "Confirmed"
   - **Optie B: Later betalen**
     - Kiest "Ik betaal later"
     - Ziet betalingsdeadline (bijv. "Betaal voor [datum]")
     - Enrollment status → "Pending Payment"
     - Payment status → "Pending" met DueDate

4. **Confirmatie**
   - **Direct betaald:**
     - Systeem markeert inschrijving als "Confirmed"
     - Leerling ontvangt bevestigingsmail met:
       - Lesgegevens (data, tijden, locatie)
       - Trainernaam
       - Factuur PDF (gemarkeerd als "Betaald")
   - **Later betalen:**
     - Systeem markeert inschrijving als "Pending Payment"
     - Leerling ontvangt bevestigingsmail met:
       - Lesgegevens (data, tijden, locatie)
       - Trainernaam
       - Factuur PDF (gemarkeerd als "Nog te betalen")
       - Betaalinstructies en deadline
       - Payment link voor online betaling
     - Trainer ontvangt notificatie: "Nieuwe inschrijving (betaling pending)"

5. **Leerling toegang**
   - Leerling krijgt automatisch account aangemaakt
   - Kan inloggen in leerling portal
   - Ziet eigen lessen in kalender
   - **Bij pending payment:** Ziet waarschuwing "Betaling nog te voltooien voor [deadline]"

### 5.1B Flow: Later betalen - Betalingsherinneringen

1. **3 dagen voor deadline:**
   - Hangfire job checkt openstaande betalingen
   - Stuurt reminder email: "Vergeet niet te betalen voor [datum]"
   - Email bevat payment link

2. **Op deadline dag:**
   - Als nog niet betaald: tweede reminder
   - Email: "Laatste dag om te betalen"

3. **Na deadline:**
   - Payment status → "Overdue"
   - Enrollment status blijft "Pending Payment"
   - Trainer ontvangt notificatie: "[Student] heeft deadline gemist"
   - Optioneel: na X dagen → Enrollment status → "Cancelled"

4. **Leerling betaalt alsnog:**
   - Klikt payment link in email
   - Voltooit betaling via Mollie
   - Payment status → "Paid"
   - Enrollment status → "Confirmed"
   - Beide partijen ontvangen bevestiging

### 5.2 Flow: Trainer plant nieuwe lessenreeks

1. Trainer logt in op dashboard
2. Navigeert naar "Nieuwe lessenreeks"
3. Vult in:
   - Titel (bv. "Beginners Tennis - Voorjaar 2026")
   - Beschrijving
   - Start/einddatum
   - Dag van de week + tijdstip
   - Aantal lessen (genereert automatisch data)
   - Baan selectie
   - Max aantal deelnemers
   - Prijs per persoon
4. Preview van alle lesdatums
5. Bevestigt aanmaak
6. Reeks wordt zichtbaar op publieke pagina

### 5.3 Flow: Admin beheert school

1. Admin logt in
2. Dashboard toont:
   - Aantal actieve leerlingen
   - Inkomsten deze maand
   - Bezettingsgraad banen
   - Aankomende lessen
3. Admin kan:
   - Trainers toevoegen/bewerken
   - Banen configureren
   - Instellingen aanpassen
   - Rapporten genereren
   - Alle lessen inzien (alle trainers)

---

## 6. Business Model & Go-to-Market

### 6.1 Pricing Strategy (SaaS)

**Optie 1: Per Trainer**

- €29/maand per trainer
- Onbeperkt aantal leerlingen
- Alle features inbegrepen
- Jaarabonnement: €290/jaar (2 maanden korting)

**Optie 2: Per School (meerdere trainers)**

- €79/maand voor 2-5 trainers
- €149/maand voor 6-15 trainers
- €299/maand voor 16+ trainers (enterprise)

**Transactiekosten:**

- Mollie/Stripe fees doorberekenen (geen extra marge)
- Of: opslag van 1-2% boven payment provider fees

**Aanbeveling:** Start met Optie 2 (per school) voor betere revenue

### 6.2 Subscription Management & Upgrades/Downgrades

#### **Upgrade Strategie (Eenvoudig)**

**Regel:** Upgraden kan altijd, direct actief vanaf volgende factuurdatum.

**Flow:**

1. School kiest hogere tier
2. Bevestiging: "Vanaf [datum] betaal je €X/maand"
3. Nieuwe limiet direct actief (prorated billing)

**Geen restricties** - je kunt altijd meer betalen voor meer capaciteit.

---

#### **Downgrade Strategie (Hard Limits)** ⭐

**Regel:** Downgraden kan alleen als je binnen de limieten van de nieuwe tier valt.

**Voorbeeld scenario:**

```
Current tier: €149/maand (6-15 trainers) → 12 actieve trainers
Gewenste tier: €79/maand (2-5 trainers)

✗ GEBLOKKEERD: "Je hebt 12 trainers, maar deze tier heeft max 5 trainers"
→ Actie vereist: Deactiveer eerst 7 trainers, dan opnieuw proberen
```

**Validatie checks bij downgrade:**

- ✅ Aantal actieve trainers ≤ nieuwe tier limiet
- ✅ Aantal actieve banen ≤ nieuwe tier limiet (indien van toepassing)
- ✅ Data storage ≤ nieuwe tier limiet (indien van toepassing)

**Implementation principes:**

```csharp
// Backend validatie
public class DowngradePlanValidator
{
    public async Task<ValidationResult> ValidateDowngrade(
        Organization org,
        SubscriptionPlan targetPlan)
    {
        var activeTrainers = await CountActiveTrainers(org.Id);

        if (activeTrainers > targetPlan.MaxTrainers)
        {
            return ValidationResult.Failure(
                $"Je hebt {activeTrainers} actieve trainers. " +
                $"Het {targetPlan.Name} plan heeft maximaal {targetPlan.MaxTrainers} trainers. " +
                $"Deactiveer eerst {activeTrainers - targetPlan.MaxTrainers} trainers."
            );
        }

        return ValidationResult.Success();
    }
}
```

**Frontend flow:**

1. School klikt "Downgrade" naar lagere tier
2. Systeem checkt huidige usage vs nieuwe limits
3. **Als binnen limits:** Bevestiging → Downgrade goedgekeurd
4. **Als buiten limits:**
   - Toon duidelijke error met exacte aantallen
   - Link naar "Trainers beheren" pagina
   - Mogelijkheid om trainers direct te deactiveren
   - Na aanpassing: opnieuw proberen

**Voordelen van Hard Limits:**

- ✅ Simpel te implementeren en te begrijpen
- ✅ Geen edge cases of verwarrende situaties
- ✅ Eerlijke pricing (betaal voor wat je gebruikt)
- ✅ Voorkomt misbruik van het systeem
- ✅ Duidelijke user guidance (wat moet je doen)

**Trainer Deactivatie:**

- Gedeactiveerde trainers kunnen **niet** meer inloggen
- Hun historische data blijft **behouden** (lessen, enrollment history)
- Kunnen later **gereactiveerd** worden (bij upgrade)
- Lessen blijven zichtbaar maar read-only

**Alternatief voor v2 (niet in MVP):**

- Grace period: 30 dagen om aan te passen na downgrade
- Automatische suggesties: "Deze trainers zijn het minst actief"
- Soft limits: waarschuwing maar nog wel toegang (tijdelijk)

### 6.3 Launch Strategie

**Fase 1: Pilot (Maand 1-3)**

- 1-2 scholen die je al kent
- Gratis tijdens pilot periode
- Intensieve feedback loops
- Features aanscherpen

**Fase 2: Private Beta (Maand 4-6)**

- 5-10 scholen via netwerk pilotklanten
- 50% korting gedurende 6 maanden
- Actief testimonials verzamelen
- Case studies ontwikkelen

**Fase 3: Public Launch (Maand 7+)**

- Volledige prijzen
- Marketing via:
  - Google Ads (zoekwoorden: "tennis planning software", "padel lessensysteem")
  - LinkedIn targeting tennisscholen
  - Partnerships met tennisbonden (Tennis Vlaanderen, KNLTB)
  - Content marketing (blog over efficiënt lessenplanning)

---

## 7. Risico's & Mitigaties

### 7.1 Technische Risico's

| Risico                                    | Impact | Waarschijnlijkheid | Mitigatie                                                        |
| ----------------------------------------- | ------ | ------------------ | ---------------------------------------------------------------- |
| Payment integratie complexer dan verwacht | Hoog   | Medium             | Begin vroeg met Mollie/Stripe POC, gebruik ready-made libraries  |
| Email deliverability problemen            | Medium | Medium             | Gebruik gerenommeerde service (Resend), implementeer SPF/DKIM    |
| Performance issues bij multi-tenant setup | Medium | Laag               | Database indexing, caching strategie, load testing vroeg         |
| Data privacy (GDPR) compliance            | Hoog   | Medium             | Privacy by design, expliciete consent flows, data export functie |

### 7.2 Product/Markt Risico's

| Risico                                      | Impact | Waarschijnlijkheid | Mitigatie                                                      |
| ------------------------------------------- | ------ | ------------------ | -------------------------------------------------------------- |
| Feature bloat - te veel willen in MVP       | Hoog   | Hoog               | Strikte scope discipline, alleen must-haves                    |
| Pilotklanten springen af                    | Hoog   | Medium             | Frequente check-ins, early value delivery, goede ondersteuning |
| Betalingsbereidheid lager dan gedacht       | Hoog   | Medium             | Pricing validation tijdens pilot, flexibele packages           |
| Concurrentie lanceert vergelijkbaar product | Medium | Laag               | Snelle iterations, focus op service & lokale markt             |

### 7.3 Operationele Risico's

| Risico                                  | Impact | Waarschijnlijkheid | Mitigatie                                                           |
| --------------------------------------- | ------ | ------------------ | ------------------------------------------------------------------- |
| Ondersteuningsmoeite hoger dan verwacht | Medium | Hoog               | Goede documentatie, video tutorials, FAQ, chatbot overwegen         |
| Scope creep tijdens ontwikkeling        | Hoog   | Hoog               | Duidelijke requirements doc, change request process                 |
| Solo development bottleneck             | Hoog   | Medium             | Prioriteren, modulaire architectuur voor eventuele uitbreiding team |

---

## 8. Development Roadmap

### 8.1 Maand 1: Fundament

**Week 1-2: Setup & Design**

- [ ] Development environment opzetten (.NET 8 SDK, PostgreSQL, Node.js)
- [ ] Solution structuur aanmaken (Clean Architecture layers)
- [ ] Database schema finaliseren (EF Core entities)
- [ ] **i18n Setup (future-ready):**
  - [ ] Backend: Install Microsoft.Extensions.Localization
  - [ ] Backend: Create Resources/ folder structure
  - [ ] Backend: Create EmailTemplates.nl.resx, ValidationMessages.nl.resx
  - [ ] Frontend: Install next-intl
  - [ ] Frontend: Setup i18n.ts configuration
  - [ ] Frontend: Create messages/nl.json with initial strings
  - [ ] Add PreferredLanguage field to User entity
- [ ] Wireframes voor alle interfaces
- [ ] UI component library setup (Shadcn in Next.js)
- [ ] Git repositories + CI/CD pipeline (GitHub Actions)

**Week 3-4: .NET Backend Fundament**

- [ ] Domain entities implementeren (Organization, User, Court, Subscription, etc.)
- [ ] **Subscription entity met plan limits:**
  - [ ] SubscriptionPlan enum (Starter, Professional, Business, Enterprise)
  - [ ] PlanLimits static class voor max trainers per tier
  - [ ] Subscription status tracking
- [ ] EF Core configurations en migrations
- [ ] ApplicationDbContext met multi-tenancy filters
- [ ] JWT authenticatie setup met ASP.NET Identity
- [ ] MediatR + FluentValidation setup
- [ ] Swagger/OpenAPI configuratie
- [ ] CORS policy voor Next.js frontend

### 8.2 Maand 2: Core Planning Features

**Week 5-6: Lessen & Reeksen (Backend)**

- [ ] CQRS Commands: CreateLesson, UpdateLesson, DeleteLesson
- [ ] CQRS Queries: GetLessons, GetLessonById, GetLessonsByCourt
- [ ] LessonSeries CQRS handlers
- [ ] AutoMapper DTOs (LessonDto, LessonSeriesDto)
- [ ] API Controllers met Swagger annotations
- [ ] Unit tests voor handlers

**Week 5-6: Lessen & Reeksen (Frontend)**

- [ ] Next.js API client setup (TypeScript interfaces)
- [ ] Trainer dashboard: lessen aanmaken formulier
- [ ] Kalenderweergave component (FullCalendar.io of custom)
- [ ] Week/maand view implementatie
- [ ] Court availability display

**Week 7-8: Inschrijvingen (Backend)**

- [ ] Enrollment CQRS handlers (Create, Update, Cancel)
- [ ] Business logic: check max participants, validate dates
- [ ] Publieke API endpoints (geen auth vereist voor browse)
- [ ] Email service setup (SendGrid/Mailgun)
- [ ] Background jobs (Hangfire) voor emails

**Week 7-8: Inschrijvingen (Frontend)**

- [ ] Publieke pagina: beschikbare lessenreeksen tonen
- [ ] Inschrijvingsformulier met validatie
- [ ] Enrollment confirmation flow
- [ ] Email templates (React Email of plain HTML)

### 8.3 Maand 3: Betalingen & Polish

**Week 9-10: Payment Integration (Backend)**

- [ ] Mollie API account setup + API keys
- [ ] MolliePaymentService implementatie
- [ ] CreatePayment command (genereer payment link)
- [ ] **Deferred payment support:**
  - [ ] PaymentType enum (Immediate, Deferred)
  - [ ] DueDate tracking in Payment entity
  - [ ] PaymentDeadline in Enrollment entity
- [ ] ProcessPaymentWebhook command (webhook handler)
- [ ] Payment status updates in database
- [ ] **Payment reminder Hangfire jobs:**
  - [ ] CheckOverduePayments job (daily)
  - [ ] SendPaymentReminders job (3 days before, on deadline)
- [ ] Factuur PDF generatie (QuestPDF of similar library)
- [ ] S3/Object Storage integratie voor PDFs

**Week 9-10: Payment Integration (Frontend)**

- [ ] Checkout flow met keuze: "Nu betalen" of "Later betalen"
- [ ] Redirect naar Mollie (voor directe betaling)
- [ ] Payment status polling/callback handling
- [ ] Success/failure paginas
- [ ] **Pending payment UI:**
  - [ ] Waarschuwing badge bij openstaande betaling
  - [ ] "Betaal nu" button in student portal
  - [ ] Deadline countdown display
- [ ] Payment history in dashboard met status indicators

**Week 11-12: Leerling Portal, Subscription Management & Testing**

- [ ] Student login & JWT token management
- [ ] Mijn lessen overzicht (API + frontend)
- [ ] Afmeld functionaliteit (met cancellation policy)
- [ ] **Subscription Management (Admin):**
  - [ ] Backend: ChangePlanCommand met downgrade validation
  - [ ] Backend: CheckDowngradeEligibility query
  - [ ] Backend: DeactivateTrainer command
  - [ ] Frontend: Subscription settings page
  - [ ] Frontend: Plan comparison & upgrade/downgrade UI
  - [ ] Frontend: Trainer management met deactivatie
  - [ ] Frontend: Downgrade blocker met duidelijke error messages
  - [ ] "Prepare Downgrade" helper page
- [ ] Email notificatie systeem verfijnen (templates)
- [ ] Integration tests (WebApplicationFactory)
- [ ] End-to-end tests (Playwright of Cypress)
- [ ] Performance testing (basic load tests)
- [ ] Bug fixes & UI polish

### 8.4 Maand 4-6: Pilot & Iteratie

**Week 13-16: Pilot Fase 1**

- [ ] Deploy naar staging
- [ ] Onboarding pilot school 1
- [ ] Data migratie (indien bestaande data)
- [ ] Training sessies met trainers
- [ ] Feedback verzameling
- [ ] Prioriteit bugs fixen

**Week 17-24: Iteratie & Uitbreiding**

- [ ] Feedback implementeren
- [ ] Extra features op basis van pilot learnings
- [ ] Performance optimalisatie
- [ ] Tweede pilot school onboarden
- [ ] Documentatie & help center
- [ ] Voorbereiden public beta

### 8.5 Maand 7+: Launch & Schaling

- [ ] Marketing website
- [ ] Pricing page & checkout flow
- [ ] Self-service onboarding flow
- [ ] Customer support kanalen
- [ ] Analytics & monitoring
- [ ] Eerste marketing campagne

### 8.6 Docker Deployment Setup

**Backend API Dockerfile:**

```dockerfile
# TennisPlanningTool.API/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Presentation/TennisPlanningTool.API/TennisPlanningTool.API.csproj", "API/"]
COPY ["src/Application/TennisPlanningTool.Application/TennisPlanningTool.Application.csproj", "Application/"]
COPY ["src/Domain/TennisPlanningTool.Domain/TennisPlanningTool.Domain.csproj", "Domain/"]
COPY ["src/Infrastructure/TennisPlanningTool.Infrastructure/TennisPlanningTool.Infrastructure.csproj", "Infrastructure/"]
RUN dotnet restore "API/TennisPlanningTool.API.csproj"
COPY . .
WORKDIR "/src/API"
RUN dotnet build "TennisPlanningTool.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TennisPlanningTool.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TennisPlanningTool.API.dll"]
```

**Frontend Next.js Dockerfile:**

```dockerfile
# Dockerfile (Next.js app root)
FROM node:20-alpine AS base

FROM base AS deps
WORKDIR /app
COPY package*.json ./
RUN npm ci

FROM base AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

FROM base AS runner
WORKDIR /app
ENV NODE_ENV production
RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs
COPY --from=builder /app/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static
USER nextjs
EXPOSE 3000
ENV PORT 3000
CMD ["node", "server.js"]
```

**Docker Compose (lokale development):**

```yaml
# docker-compose.yml
version: "3.8"

services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: tennisplanner
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: dev_password
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  api:
    build:
      context: ./backend
      dockerfile: src/Presentation/TennisPlanningTool.API/Dockerfile
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=tennisplanner;Username=admin;Password=dev_password
      - JwtSettings__Secret=your-super-secret-key-min-32-chars
      - Mollie__ApiKey=test_xxxxx
    depends_on:
      - postgres

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    ports:
      - "3000:3000"
    environment:
      - NEXT_PUBLIC_API_URL=http://localhost:5000
    depends_on:
      - api

volumes:
  postgres_data:
```

**GitHub Actions CI/CD:**

```yaml
# .github/workflows/deploy-api.yml
name: Deploy .NET API to Scaleway

on:
  push:
    branches: [main]
    paths:
      - "backend/**"

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Build Docker image
        run: |
          cd backend
          docker build -t tennisplanner-api:${{ github.sha }} -f src/Presentation/TennisPlanningTool.API/Dockerfile .

      - name: Push to Scaleway Container Registry
        run: |
          docker tag tennisplanner-api:${{ github.sha }} rg.fr-par.scw.cloud/tennisplanner/api:latest
          docker push rg.fr-par.scw.cloud/tennisplanner/api:latest

      - name: Deploy to Scaleway Instances
        run: |
          # SSH into server and pull/restart container
          ssh user@yourserver.scw.cloud "docker pull rg.fr-par.scw.cloud/tennisplanner/api:latest && docker-compose up -d"
```

---

## 9. Kritische Beslissingen (open items)

### 9.1 Te Beslissen Voor Start Ontwikkeling

**1. Mollie vs Stripe?**

- **Mollie:** Meer lokaal (NL/BE), iDEAL prominent
- **Stripe:** Internationaler, meer features, betere developer experience
- **Aanbeveling:** Start met Mollie (target markt), Stripe als toekomstige optie

**2. Multi-tenancy strategie**

- **Optie A:** Shared database met OrganizationId filtering (EF Core global query filters)
- **Optie B:** Database per school (complex, maar betere isolatie)
- **Aanbeveling:** Shared database (simpeler voor MVP, EF Core heeft uitstekende global filter support)

**3. Freemium vs Paid Only?**

- **Freemium:** Gratis tier voor 1 trainer, max 20 leerlingen
- **Paid only:** Alleen betaalde plans, wel free trial 14 dagen
- **Aanbeveling:** Paid only met trial (betere kwaliteit leads, minder misbruik)

**4. White-label mogelijkheid?**

- Kunnen scholen hun eigen branding gebruiken?
- **MVP:** Nee, teveel complexiteit
- **v2:** Overwegen als enterprise feature

### 9.2 Te Valideren Tijdens Pilot

- Optimale lessenreeks lengte (meeste scholen doen 8, 10, of 12 lessen?)
- Cancellation policy regels (hoe lang van tevoren afmelden?)
- Welke rapporten zijn echt nodig voor scholen?
- Mobiele app wens (native app) of is web responsive voldoende?
- Integratie met bestaande tennisbond systemen (lidmaatschap checks)?

---

## 10. Succes Metrics

### 10.1 MVP Success Criteria (einde maand 6)

**Product Metrics:**

- ✅ 2 scholen actief gebruiken het systeem
- ✅ 10+ trainers plannen lessen via platform
- ✅ 100+ leerlingen hebben zich ingeschreven
- ✅ €5.000+ transactievolume via platform
- ✅ <5 kritieke bugs per maand

**Gebruikersfeedback:**

- ✅ NPS score > 40 bij trainers
- ✅ 80%+ van trainers vindt het makkelijker dan Excel
- ✅ 90%+ van leerlingen vindt inschrijven eenvoudig

**Technisch:**

- ✅ 99%+ uptime
- ✅ <2 seconden laadtijd belangrijkste paginas
- ✅ 0 payment failures door technische oorzaak

### 10.2 Launch Success Criteria (einde jaar 1)

**Business:**

- 20+ betalende scholen
- €5.000 MRR (Monthly Recurring Revenue)
- <10% churn per maand
- CAC (Customer Acquisition Cost) < 3x maand omzet

**Product:**

- Self-service onboarding <30 minuten
- <1 support ticket per school per maand

---

## 11. Aanbevelingen & Conclusie

### 11.1 Belangrijkste Aanbevelingen

1. **Start Klein, Leer Snel**
   - Focus op MVP features, resist scope creep
   - Wekelijkse feedback loops met pilot scholen
   - Itereer op basis van real usage data

2. **Technische Pragmatiek (.NET Specifiek)**
   - **Clean Architecture**: Houd layers gescheiden, Domain heeft geen dependencies
   - **CQRS met MediatR**: Één handler per use case, makkelijk testbaar
   - **EF Core Migrations**: Gebruik descriptieve namen, review altijd generated SQL
   - **Global Query Filters**: Configureer OrganizationId filtering in OnModelCreating
   - **Repository Pattern**: Vermijd - EF Core DbContext is al een UoW + Repository
   - **DTOs overal**: Expose nooit domain entities via API, gebruik AutoMapper
   - **Validation**: FluentValidation in Application layer, DataAnnotations als fallback
   - **Exception Handling**: Gebruik middleware, niet try-catch in elke controller
   - **Logging**: Serilog met structured logging, log naar console + file/seq

3. **Business Focus**
   - Valideer pricing vroeg (maand 2-3)
   - Bouw relatie op met eerste klanten (toekomstige referenties)
   - Documenteer alles vanaf dag 1 (vermindert support last)

4. **Risico Management**
   - Bouw payment integratie vroeg (maand 2)
   - GDPR compliance vanaf start inbouwen
   - Backup & monitoring vanaf dag 1

### 11.2 Go/No-Go Criteria

**GO als:**

- ✅ Je hebt commitment van 1-2 pilot scholen
- ✅ Je kan 3-6 maanden investeren in development
- ✅ Je hebt budget voor hosting + tools (~€100-200/maand)

**NO-GO als:**

- ❌ Pilot scholen zijn niet enthousiast na eerste demo
- ❌ Je ontdekt grote concurrent die exact dit doet voor BE/NL
- ❌ Betalingsbereidheid is <€20/trainer/maand

### 11.3 Volgende Stappen

**Deze week:**

1. Validatie gesprek met pilot scholen:
   - Feature prioriteiten bevestigen
   - Pricing feedback
   - Commitment verkrijgen voor 6 maanden pilot

2. Design wireframes:
   - Low-fi schetsen van alle 4 interfaces
   - User flows uitwerken
   - Feedback loop starten

**Volgende 2 weken:** 3. Tech setup:

- Repository aanmaken
- Development environment
- Database schema finaliseren

4. Begin development:
   - Start met Maand 1 roadmap
   - Wekelijkse voortgang tracking

---

## Bijlagen

### A. Veelgestelde Vragen (FAQ)

**Q: Waarom geen native mobiele app?**  
A: Web responsive is sneller te ontwikkelen en onderhouden. Voor MVP focussen we op één codebase die overal werkt. Native app kan in v2 als de vraag er is.

**Q: Hoe zit het met privacy van leerling data?**  
A: GDPR compliant vanaf dag 1. Minimale data collection, expliciete consent, recht op vergetelheid geïmplementeerd.

**Q: Wat als een school wil overstappen van bestaand systeem?**  
A: Data import functionaliteit via CSV voor leerlingen en lesgeschiedenis. Persoonlijke onboarding tijdens pilot fase.

**Q: Ondersteuning voor meerdere talen?**  
A: MVP start Nederlands-only maar is i18n-ready (next-intl + Microsoft.Extensions.Localization). Frans toevoegen = 2-3 dagen werk (translations copy-paste). Zie sectie 4.5 voor volledige i18n strategie.

### B. Technische Specificaties

**Browser Support:**

- Chrome/Edge (laatste 2 versies)
- Safari (laatste 2 versies)
- Firefox (laatste 2 versies)
- Mobiele browsers: iOS Safari, Chrome Android

**Minimum Requirements:**

- JavaScript enabled
- Cookies enabled voor authenticatie
- Internet snelheid: 1 Mbps+ aanbevolen

**Server Specs (Scaleway):**

- Backend Instance: DEV1-M of DEV1-L (2-4 vCPU, 4-8GB RAM)
- Frontend Instance: DEV1-S (1 vCPU, 2GB RAM voldoende voor Next.js)
- Database: Managed PostgreSQL (10GB+ storage, 2GB RAM minimum)
- Object Storage: 50GB voor facturen en uploads
- Email: Transactional Email (TEM) service

**Geschatte Maandelijkse Kosten (MVP fase - 10 scholen):**

```
Infrastructure:
- Backend Container (DEV1-M):      ~€15/maand
- Frontend Container (DEV1-S):     ~€8/maand
- PostgreSQL Managed DB (10GB):    ~€12/maand
- Object Storage (50GB):           ~€1/maand
- Email (3000 emails/maand):       ~€0.68/maand (na free 300)
- Bandwidth:                       ~€2/maand

TOTAAL:                            ~€38-40/maand
```

**Opschaling bij groei (50 scholen):**

```
- Backend: DEV1-L                  ~€25/maand
- Database: 50GB storage           ~€20/maand
- Email (15K emails/maand):        ~€3.75/maand
- Object Storage (200GB):          ~€4/maand

TOTAAL:                            ~€60-70/maand
```

Ruim binnen budget van €100-200/maand. Email is verwaarloosbaar in totale kosten.

### C. .NET Development Best Practices

**Essential NuGet Packages:**

```bash
# Core
dotnet add package MediatR
dotnet add package MediatR.Extensions.Microsoft.DependencyInjection
dotnet add package FluentValidation.DependencyInjectionExtensions
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection

# Database
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore

# Authentication
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt

# Logging
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File

# Payments
dotnet add package Mollie.Api

# i18n (Internationalization)
dotnet add package Microsoft.Extensions.Localization

# Email (native .NET SMTP - geen extra packages nodig)
# System.Net.Mail is included in .NET runtime

# Email
dotnet add package SendGrid

# Background Jobs
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.PostgreSql

# API
dotnet add package Swashbuckle.AspNetCore

# Testing
dotnet add package xunit
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

**EF Core Global Query Filter (Multi-tenancy):**

```csharp
// ApplicationDbContext.cs
protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);

    // Multi-tenancy filter - alleen data van huidige organisatie tonen
    builder.Entity<Lesson>()
        .HasQueryFilter(e => e.OrganizationId == _currentUserService.OrganizationId);

    builder.Entity<Enrollment>()
        .HasQueryFilter(e => e.OrganizationId == _currentUserService.OrganizationId);

    // Herhaal voor alle tenant-scoped entities
}
```

**Result Pattern (vermijd exceptions voor business logic):**

```csharp
public class Result<T>
{
    public bool Succeeded { get; set; }
    public T Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static Result<T> Success(T data) =>
        new() { Succeeded = true, Data = data };

    public static Result<T> Failure(params string[] errors) =>
        new() { Succeeded = false, Errors = errors.ToList() };
}
```

**JWT Configuration (appsettings.json):**

```json
{
  "JwtSettings": {
    "Secret": "your-super-secret-key-minimum-32-characters-long",
    "Issuer": "TennisPlanningTool",
    "Audience": "TennisPlanningToolUsers",
    "ExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  },
  "Mollie": {
    "ApiKey": "test_xxxxxxxxxx",
    "WebhookUrl": "https://api.tennisplanner.be/api/webhooks/mollie"
  },
  "SendGrid": {
    "ApiKey": "SG.xxxxxxxxxx",
    "FromEmail": "noreply@tennisplanner.be",
    "FromName": "Tennis Planner"
  }
}
```

**Common Pitfalls te Vermijden:**

1. **Geen async/await in loops** - gebruik `Task.WhenAll()` voor parallel
2. **DbContext is niet thread-safe** - gebruik scoped lifetime
3. **N+1 queries** - gebruik `.Include()` of projection met `.Select()`
4. **Tracking alles** - gebruik `.AsNoTracking()` voor read-only queries
5. **Secrets in code** - gebruik User Secrets (dev) en Environment Variables (prod)
6. **Hardcoded OrganizationId** - gebruik ICurrentUserService dependency injection
7. **Geen cancellation tokens** - pass `CancellationToken` door naar async methods

### D. Woordenlijst

**Business Termen:**

- **MRR**: Monthly Recurring Revenue - maandelijkse terugkerende inkomsten
- **MVP**: Minimum Viable Product - eerste werkende versie met kern features
- **NPS**: Net Promoter Score - klanttevredenheid metric
- **CAC**: Customer Acquisition Cost - kosten om nieuwe klant te werven
- **Churn**: Percentage klanten dat stopt per periode
- **Multi-tenancy**: Meerdere organisaties op één systeem/database
- **Self-service**: Klanten kunnen zelf zonder hulp onboarden

**Technische Termen (.NET Specifiek):**

- **CQRS**: Command Query Responsibility Segregation - scheiding tussen write en read operaties
- **MediatR**: Library voor mediator pattern (decoupling tussen controllers en handlers)
- **EF Core**: Entity Framework Core - ORM voor database access
- **DTO**: Data Transfer Object - object voor data transport tussen layers
- **AutoMapper**: Library voor object-to-object mapping
- **FluentValidation**: Library voor input validatie met fluent API
- **JWT**: JSON Web Token - stateless authenticatie token
- **Global Query Filter**: EF Core feature voor automatische WHERE clause (multi-tenancy)
- **Result Pattern**: Return type voor success/failure zonder exceptions
- **Clean Architecture**: Architectuur pattern met onion layers (Domain → Application → Infrastructure → Presentation)

### E. Scaleway Transactional Email Setup Guide

**Quick Setup (5 minuten):**

1. **Enable TEM in Scaleway Console:**
   - Navigate to Transactional Email in Scaleway Console
   - Select Essential plan (free tier 300 emails/maand)
   - Add domain: `tennisplanner.be`

2. **Configure DNS Records:**
   Scaleway geeft je automatisch deze records:

   ```
   SPF:  v=spf1 include:_spf.scw-tem.cloud ~all
   DKIM: [unique key provided by Scaleway]
   MX:   mx.tem.scw.cloud (priority 10)
   ```

   Voeg toe in je DNS provider (bijv. Scaleway Domains & DNS):
   - TXT record voor SPF
   - TXT record voor DKIM (key name + value van console)
   - MX record (optioneel maar aanbevolen)

3. **Verify Domain:**
   - Klik "Verify domain" in console
   - Wacht 5-10 minuten voor DNS propagation
   - Status wordt "Validated" ✅

4. **Get SMTP Credentials:**
   - In console: Create SMTP credentials
   - Username: `your-email@tennisplanner.be`
   - Password: genereer in console, kopieer (éénmalig!)
   - Save credentials in appsettings

5. **Test Email:**
   ```bash
   # Quick SMTP test
   curl --url 'smtp://smtp.tem.scw.cloud:587' \
     --mail-from 'noreply@tennisplanner.be' \
     --mail-rcpt 'test@example.com' \
     --upload-file email.txt \
     --user 'your-email@tennisplanner.be:password'
   ```

**Monitoring & Analytics:**

- Dashboard: Scaleway Console → Transactional Email → Statistics
- Metrics: Sent, delivered, bounced, failed
- Cockpit integration: Detailed logs en metrics
- Webhooks: Configure voor real-time delivery status (optioneel)

**Best Practices:**

- Altijd SPF + DKIM configureren (vereist voor deliverability)
- Test met eigen email eerst
- Monitor bounce rate (hoog = reputation damage)
- Use template validation (min 10 chars in subject/body)
- Queue emails via Hangfire (niet direct in request)

**Troubleshooting:**

- **535 Authentication failed:** Check SMTP credentials
- **550 Domain not validated:** Verify DNS records correct
- **High bounce rate:** Check email format, clean up lists
- **Rate limited:** Contact Scaleway support voor quota increase

---

**Document Einde**

_Dit document is een levend document en zal worden bijgewerkt naarmate het project vordert en nieuwe inzichten ontstaan._
