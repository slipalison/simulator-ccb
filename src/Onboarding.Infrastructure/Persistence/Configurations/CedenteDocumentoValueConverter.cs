using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Infrastructure.Persistence.Configurations;

/// <summary>
/// ValueConverter for CedenteDocumento discriminated union (D-09).
/// Writes: extracts the document value string via Match (CPF for PF, CNPJ for PJ).
/// Reads: returns a placeholder — reconstruction from database requires shadow properties
/// (DocumentoTipo, CpfValue, CnpjCedenteValue) via the repository pattern (D-12).
/// The Documento property is never read directly from DB; the repository reconstructs
/// using shadow properties and the appropriate .Pf()/.Pj() factory method.
/// </summary>
internal sealed class CedenteDocumentoValueConverter : ValueConverter<CedenteDocumento, string>
{
    public CedenteDocumentoValueConverter()
        : base(
            doc => doc.Match(
                pf => pf.Cpf.Value,
                pj => pj.Cnpj.Value),
            _ => CedenteDocumento.Pf(Cpf.Create("00000000000")),
            convertsNulls: false)
    {
    }
}