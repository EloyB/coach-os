namespace CoachOS.Application.Abstractions;

/// <summary>Controle van een btw-nummer via de Europese VIES-dienst.</summary>
public interface IVatNumberValidator
{
    /// <param name="normalizedVatNumber">Bv. <c>BE0123456789</c>.</param>
    Task<VatCheckResult> CheckAsync(string normalizedVatNumber, CancellationToken ct = default);
}

public enum VatCheckResult
{
    Valid,
    Invalid,

    /// <summary>VIES niet bereikbaar of fout: niet blokkeren, wel als niet-geverifieerd bewaren.</summary>
    Unavailable
}
