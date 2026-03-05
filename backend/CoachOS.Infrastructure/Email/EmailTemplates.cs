namespace CoachOS.Infrastructure.Email;

/// <summary>
/// Builds on-brand HTML email templates for CoachOS.
/// All emails share the same base shell — add new templates as static methods.
/// </summary>
public static class EmailTemplates
{
    // ── Brand colours (inline CSS for email client compatibility) ──────────────
    private const string Green = "#2D5016";
    private const string Lime = "#D0FF14";
    private const string Beige = "#E8DCC4";
    private const string OffWhite = "#FAFAF8";

    // ── Base shell ─────────────────────────────────────────────────────────────

    private static string Base(string previewText, string bodyHtml) => $"""
        <!DOCTYPE html>
        <html lang="nl">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <meta http-equiv="X-UA-Compatible" content="IE=edge" />
          <title>CoachOS</title>
          <!--[if mso]>
          <noscript>
            <xml><o:OfficeDocumentSettings><o:PixelsPerInch>96</o:PixelsPerInch></o:OfficeDocumentSettings></xml>
          </noscript>
          <![endif]-->
        </head>
        <body style="margin:0;padding:0;background-color:{OffWhite};font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
          <!-- Preview text (hidden) -->
          <div style="display:none;max-height:0;overflow:hidden;mso-hide:all;">{previewText}&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;</div>

          <table width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:{OffWhite};">
            <tr>
              <td align="center" style="padding:40px 16px;">
                <table width="600" cellpadding="0" cellspacing="0" border="0" style="max-width:600px;width:100%;">

                  <!-- Header -->
                  <tr>
                    <td style="background-color:{Green};border-radius:12px 12px 0 0;padding:28px 40px;">
                      <table width="100%" cellpadding="0" cellspacing="0" border="0">
                        <tr>
                          <td>
                            <span style="font-size:22px;font-weight:700;color:#ffffff;letter-spacing:-0.5px;">
                              Coach<span style="color:{Lime};">OS</span>
                            </span>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>

                  <!-- Body -->
                  <tr>
                    <td style="background-color:#ffffff;padding:40px;border-left:1px solid #f0f0f0;border-right:1px solid #f0f0f0;">
                      {bodyHtml}
                    </td>
                  </tr>

                  <!-- Footer -->
                  <tr>
                    <td style="background-color:{OffWhite};border-radius:0 0 12px 12px;border:1px solid #f0f0f0;border-top:none;padding:24px 40px;text-align:center;">
                      <p style="margin:0;font-size:12px;color:#9ca3af;line-height:1.6;">
                        Dit bericht is verstuurd door CoachOS. Vragen? Stuur een e-mail naar
                        <a href="mailto:support@coach-hq.com" style="color:{Green};text-decoration:none;">support@coach-hq.com</a>
                      </p>
                      <p style="margin:8px 0 0;font-size:11px;color:#d1d5db;">
                        &copy; {DateTime.UtcNow.Year} CoachOS &mdash; Studio Swyft
                      </p>
                    </td>
                  </tr>

                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    // ── Button helper ──────────────────────────────────────────────────────────

    private static string Button(string url, string label) => $"""
        <table cellpadding="0" cellspacing="0" border="0" style="margin:32px 0;">
          <tr>
            <td style="background-color:{Green};border-radius:8px;">
              <a href="{url}"
                 style="display:inline-block;padding:14px 28px;font-size:15px;font-weight:600;color:#ffffff;text-decoration:none;border-radius:8px;letter-spacing:-0.2px;">
                {label}
              </a>
            </td>
          </tr>
        </table>
        """;

    // ── Templates ──────────────────────────────────────────────────────────────

    /// <summary>Trainer invite email sent when an admin invites a new trainer.</summary>
    public static (string Subject, string Html) TrainerInvite(
        string firstName,
        string organizationName,
        string inviteUrl)
    {
        string subject = $"Je bent uitgenodigd als trainer bij {organizationName}";

        string body = $"""
            <h1 style="margin:0 0 8px;font-size:26px;font-weight:700;color:#111827;letter-spacing:-0.5px;">
              Welkom bij CoachOS, {firstName}!
            </h1>
            <p style="margin:0 0 24px;font-size:15px;color:#6b7280;line-height:1.6;">
              Je bent uitgenodigd als <strong style="color:#111827;">trainer</strong> bij
              <strong style="color:#111827;">{organizationName}</strong>.
              Stel je wachtwoord in en begin direct met het beheren van je lessen.
            </p>

            {Button(inviteUrl, "Account activeren")}

            <table width="100%" cellpadding="0" cellspacing="0" border="0"
                   style="background-color:{OffWhite};border-radius:8px;margin-top:8px;">
              <tr>
                <td style="padding:16px 20px;">
                  <p style="margin:0;font-size:13px;color:#9ca3af;">
                    Of kopieer deze link in je browser:
                  </p>
                  <p style="margin:6px 0 0;font-size:13px;word-break:break-all;">
                    <a href="{inviteUrl}" style="color:{Green};text-decoration:none;">{inviteUrl}</a>
                  </p>
                </td>
              </tr>
            </table>

            <p style="margin:28px 0 0;font-size:13px;color:#9ca3af;line-height:1.6;">
              Deze uitnodigingslink is 72 uur geldig. Heb jij deze uitnodiging niet verwacht?
              Dan kun je deze e-mail veilig negeren.
            </p>
            """;

        return (subject, Base($"Je bent uitgenodigd als trainer bij {organizationName}", body));
    }
}
