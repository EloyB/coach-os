namespace CoachOS.Application.Configuration;

/// <summary>Bind aan de <c>SuperAdmin:</c> sectie. Het e-mailadres krijgt de interne facturatiemeldingen.</summary>
public class SuperAdminOptions
{
    public const string Section = "SuperAdmin";

    public string Email { get; set; } = string.Empty;
}
