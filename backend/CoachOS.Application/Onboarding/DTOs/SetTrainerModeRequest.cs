namespace CoachOS.Application.Onboarding.DTOs;

/// <summary>
/// Door de admin gekozen trainer-setup. Backend houdt alleen de uiteindelijke boolean bij
/// (<c>AdminsActAsTrainers</c>) plus een <c>TrainerModeChosenAt</c> stempel. De 3-state UI
/// (solo / team+coach / team-only-admin) wordt FE-zijdig naar één boolean herleid.
/// </summary>
public record SetTrainerModeRequest(bool AdminActsAsTrainer);
